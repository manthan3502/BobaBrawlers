using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace BobaBrawlers
{
    // Opt-in integration harness: run the development player with --qa.
    // It exercises production components and real physics, then writes evidence and exits.
    public sealed class RuntimeVerification : MonoBehaviour
    {
        readonly List<string> checks = new List<string>();
        int failed, errors, warnings;
        GameManager game;
        Keyboard keyboard;
        string output;
        void OnEnable() { Application.logMessageReceived += Log; }
        void OnDisable() { Application.logMessageReceived -= Log; }
        void Log(string message, string stack, LogType type)
        {
            if (type == LogType.Warning) { warnings++; checks.Add("WARNING: " + message); }
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            { errors++; checks.Add("ERROR: " + message + "\n" + stack); }
        }
        void Check(string name, bool passed)
        {
            checks.Add((passed ? "PASS: " : "FAIL: ") + name);
            if (!passed) failed++;
            Debug.Log(checks[checks.Count - 1]);
        }
        void Press(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
        Fighter Opponent => game.Fighters.Find(f => !f.isPlayer);
        void DisableAI() { foreach (var f in game.Fighters) if (!f.isPlayer) f.GetComponent<EnemyAIController>().enabled = false; }
        void Put(Fighter f, Vector3 at, float protection = 0)
        {
            f.Respawn(at, protection); Physics.SyncTransforms();
        }
        void NewRound()
        {
            Press(); game.StartMatch(); DisableAI();
            Put(game.Player, new Vector3(0, .58f, -5)); Put(Opponent, new Vector3(0, .58f, 5));
        }
        void Click(string title)
        {
            foreach (var button in FindObjectsByType<Button>())
                if (button.name == title) { button.onClick.Invoke(); return; }
            Check("Find navigation button: " + title, false);
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            // Render the actual camera and live Canvas to a texture, including when
            // Windows has hidden/minimized the player and no backbuffer is available.
            var camera = Camera.main;
            var canvas = game.UI.GetComponentInChildren<Canvas>();
            var texture = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32);
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases();
            var request = new RenderPipeline.StandardRequest { destination = texture };
            camera.SubmitRenderRequest(request);
            var previous = RenderTexture.active; RenderTexture.active = texture;
            var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply();
            File.WriteAllBytes(Path.Combine(output, name + ".png"), pixels.EncodeToPNG());
            Destroy(pixels); RenderTexture.active = previous; RenderTexture.ReleaseTemporary(texture);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
            yield return null;
        }
        IEnumerator Start()
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../QA")); Directory.CreateDirectory(output);
            game = GameManager.Instance;
            keyboard = InputSystem.AddDevice<Keyboard>("VerificationKeyboard"); keyboard.MakeCurrent();
            yield return new WaitForSeconds(.5f);
            Check("Main menu starts", game.State == MatchState.Menu && game.UI.CurrentScreen == "Menu");
            yield return Capture("01-main-menu");
            Click("LEVEL SELECT"); Check("Level selection navigation", game.UI.CurrentScreen == "Levels");
            Click("BACK"); Click("HOW TO PLAY"); Check("How to play navigation", game.UI.CurrentScreen == "HowTo");
            Click("GOT IT"); Click("SETTINGS"); Check("Settings navigation", game.UI.CurrentScreen == "Settings");
            float originalSfx = game.Feedback.SfxVolume;
            foreach (var slider in FindObjectsByType<Slider>()) slider.value = .35f;
            PlayerPrefs.Save();
            Check("Volume sliders update and persist preferences", Mathf.Abs(game.Feedback.SfxVolume - .35f) < .01f && Mathf.Abs(PlayerPrefs.GetFloat("Boba.SFX") - .35f) < .01f);
            game.Feedback.SetSfx(originalSfx); game.Feedback.SetMusic(.5f); PlayerPrefs.Save();
            Click("BACK"); Click("PLAY  /  FIRST SIP"); DisableAI();
            Check("Play starts locked Level 1 rules", game.Running && game.Score.Hearts == 3 && game.Score.Knockouts == 0 && game.Level.duration == 90 && game.Level.knockoutTarget == 2 && game.Fighters.Count == 2);
            yield return new WaitForSeconds(1.6f);
            var p = game.Player; var e = Opponent;
            Vector3 start = p.transform.position;
            Press(Key.D); yield return new WaitForSeconds(.3f); Press(); yield return new WaitForSeconds(.2f);
            Check("Keyboard movement and facing", p.transform.position.x > start.x + .5f && p.Facing.x > .9f);
            Check("Arcade braking", new Vector2(p.Body.linearVelocity.x, p.Body.linearVelocity.z).magnitude < .5f);
            Press(Key.Space); yield return new WaitForSeconds(.6f);
            Check("Hold Space accumulates charge", p.Dash.IsCharging && p.Dash.Charge > .4f && p.Dash.Charge < .8f);
            yield return new WaitForSeconds(.65f);
            Check("Charge is capped at full", p.Dash.Charge == 1f);
            Press(); yield return null; yield return new WaitForFixedUpdate();
            Check("Space release dashes in chosen direction", p.Dash.IsDashing && p.Body.linearVelocity.x > 12 && Mathf.Abs(p.Body.linearVelocity.z) < 1);
            Check("Cooldown rejects second charge", !p.Dash.BeginCharge());
            yield return new WaitForSeconds(.25f);
            // Stop before the intentional dash leaves the platform, then verify pause timing.
            Put(p, new Vector3(-4, .58f, -3));
            Press(Key.Escape); yield return null; yield return null; Press();
            float remaining = game.Remaining;
            yield return new WaitForSecondsRealtime(.4f);
            Check("Escape pauses, freezes timer and cancels charging", game.State == MatchState.Paused && game.Remaining == remaining && !p.Dash.IsCharging && !p.Dash.BeginCharge());
            yield return Capture("02-paused");
            Click("SETTINGS"); Check("Pause settings preserves paused state", game.State == MatchState.Paused);
            Click("BACK"); Click("RESUME"); yield return new WaitForSeconds(.15f);
            Check("Resume advances match", game.Running && game.Remaining < remaining);
            NewRound(); p = game.Player; e = Opponent;
            Put(p, new Vector3(4, .58f, -3)); Put(e, new Vector3(4, .58f, -.6f));
            p.Intent = Vector3.forward; p.GetComponent<PlayerController>().scriptedInput = true;
            yield return new WaitForFixedUpdate(); p.Intent = Vector3.zero;
            p.Dash.BeginCharge(); yield return new WaitForSeconds(1.2f); p.Dash.Release();
            yield return new WaitForSeconds(.16f);
            Check("Real dash collision applies knockback and attribution", e.LastAttacker == p && e.Body.linearVelocity.z > 3f);
            Check("One dash cannot hit the same target twice", !p.Dash.TryHit(e));
            p.Freeze(); e.Freeze();
            yield return Capture("03-gameplay");
            NewRound(); p = game.Player; e = Opponent;
            p.GetComponent<PlayerController>().scriptedInput = true;
            Put(p, new Vector3(5.2f, .58f, 0)); Put(e, new Vector3(6.8f, .58f, 0));
            p.Intent = Vector3.right; yield return new WaitForFixedUpdate(); p.Intent = Vector3.zero;
            p.Dash.BeginCharge(); yield return new WaitForSeconds(1.2f); p.Dash.Release();
            yield return new WaitForSeconds(.1f); p.Freeze();
            yield return new WaitForSeconds(.7f);
            Check("Charged collision causes a physically attributed ring-out", !e.Alive && game.Score.Knockouts == 1);
            NewRound(); p = game.Player; e = Opponent;
            Put(p, new Vector3(4, .58f, -3)); Put(e, new Vector3(0, .58f, 0));
            yield return new WaitForSeconds(.7f);
            Check("Central aperture permits a real physics fall", !e.Alive);
            Check("Unassisted enemy fall gives no KO", game.Score.Knockouts == 0);
            game.KnockOut(e); Check("Duplicate enemy elimination ignored", game.Score.Knockouts == 0);
            yield return new WaitForSeconds(1.9f);
            Check("Enemy respawns at safe radius with protection", e.Alive && e.IsProtected && new Vector2(e.transform.position.x, e.transform.position.z).magnitude > 3f);
            e.ReceiveHit(p, Vector3.forward, 16);
            Check("Respawn protection blocks attacks and attribution", e.LastAttacker == null);
            Put(e, new Vector3(9, .58f, 0));
            yield return new WaitForSeconds(.7f);
            Check("Outer edge permits a real physics fall", !e.Alive);
            NewRound(); p = game.Player; e = Opponent;
            e.ReceiveHit(p, Vector3.forward, 8); e.Body.isKinematic = true;
            yield return new WaitForSeconds(4.2f); game.KnockOut(e);
            Check("Expired attack attribution gives no KO", game.Score.Knockouts == 0);
            NewRound(); p = game.Player; e = Opponent;
            e.ReceiveHit(p, Vector3.forward, 8); game.KnockOut(e);
            Check("Recent player attack credits one KO", game.Score.Knockouts == 1);
            game.KnockOut(e); Check("Duplicate credited KO does not score again", game.Score.Knockouts == 1);
            yield return new WaitForSeconds(1.9f); Put(e, new Vector3(0, .58f, 5));
            e.ReceiveHit(p, Vector3.forward, 8); game.KnockOut(e);
            Check("Two credited KOs win and show result", game.State == MatchState.Victory && game.UI.CurrentScreen == "Victory");
            Check("Victory unlocks next level", PlayerPrefs.GetInt("Boba.Unlocked", 1) >= 2);
            int ko = game.Score.Knockouts; game.KnockOut(p);
            Check("Completed match cannot score or deduct hearts", game.Score.Knockouts == ko && game.Score.Hearts == 3);
            yield return Capture("04-victory");
            Click("PLAY AGAIN"); DisableAI(); p = game.Player;
            Check("Restart resets score lives timer", game.Score.Knockouts == 0 && game.Score.Hearts == 3 && game.Remaining > 89);
            Put(p, new Vector3(0, .58f, 0)); yield return new WaitForSeconds(.7f);
            Check("Player fall deducts exactly one heart", !p.Alive && game.Score.Hearts == 2);
            game.KnockOut(p); Check("Duplicate player fall ignored", game.Score.Hearts == 2);
            yield return new WaitForSeconds(1.9f);
            Check("Player safely respawns with protection", p.Alive && p.IsProtected && p.transform.position.y > 0);
            game.KnockOut(p); yield return new WaitForSeconds(1.9f); game.KnockOut(p);
            Check("Three falls end match in defeat", game.State == MatchState.Defeat && game.Score.Hearts == 0 && game.UI.CurrentScreen == "Defeat");
            yield return Capture("05-defeat");
            NewRound(); game.SetRemainingForVerification(.08f); yield return new WaitForSeconds(.2f);
            Check("Timer expiry loses match", game.State == MatchState.Defeat && game.Score.Hearts == 3);
            NewRound(); p = game.Player; e = Opponent;
            e.GetComponent<EnemyAIController>().enabled = true;
            Vector3 enemyStart = e.transform.position;
            yield return new WaitForSeconds(2.2f);
            Check("AI approaches and routes around central hole", e.Alive && Vector3.Distance(enemyStart, e.transform.position) > .5f && new Vector2(e.transform.position.x, e.transform.position.z).magnitude > 2.2f);
            Put(p, new Vector3(4, .58f, 4)); Put(e, new Vector3(4, .58f, 1));
            bool charged = false, attacked = false;
            float end = Time.time + 6;
            while (Time.time < end)
            {
                charged |= e.Dash.IsCharging; attacked |= e.Dash.IsDashing;
                yield return null;
            }
            Check("AI telegraphs charge and performs an attack", charged && attacked);
            game.Pause(); Click("MAIN MENU");
            Check("Return to menu cleans match state", game.State == MatchState.Menu && Time.timeScale == 1 && game.UI.CurrentScreen == "Menu");
            game.KnockOut(game.Player); Check("Menu cannot deduct lives", game.Score.Hearts > 0);
            Check("No runtime console errors", errors == 0);
            Check("No runtime console warnings", warnings == 0);
            checks.Add($"TOTAL: {checks.FindAll(s => s.StartsWith("PASS:")).Count} passed, {failed} failed, {errors} runtime errors");
            File.WriteAllLines(Path.Combine(output, "runtime-verification.txt"), checks);
            InputSystem.RemoveDevice(keyboard);
            if (failed > 0 || errors > 0) Application.Quit(1);
            else Click("EXIT");
        }
    }
}
