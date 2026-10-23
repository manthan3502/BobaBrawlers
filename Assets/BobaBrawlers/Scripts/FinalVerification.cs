using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace BobaBrawlers
{
    // Standalone integration coverage of production input, physics, UI and campaign.
    public sealed class FinalVerification : MonoBehaviour
    {
        readonly List<string> checks = new List<string>();
        int failed, errors, warnings;
        float began;
        GameManager game;
        Keyboard keyboard;
        string output;
        readonly Dictionary<string, int> originalInts = new Dictionary<string, int>();
        readonly Dictionary<string, float> originalFloats = new Dictionary<string, float>();
        bool finished;
        void OnEnable() { began = Time.realtimeSinceStartup; Application.logMessageReceived += Log; }
        void OnDisable() { Application.logMessageReceived -= Log; }
        void Log(string message, string trace, LogType type)
        {
            if (type == LogType.Warning) { warnings++; checks.Add("WARNING: " + message); }
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) { errors++; checks.Add("ERROR: " + message + "\n" + trace); }
        }
        void Update()
        {
            if (!finished && Time.realtimeSinceStartup - began > 240)
            { Check("Verification watchdog completed within 240s", false); Finish(); }
        }
        void Check(string name, bool passed) { checks.Add((passed ? "PASS: " : "FAIL: ") + name); if (!passed) failed++; Debug.Log(checks[checks.Count - 1]); }
        void Press(params Key[] keys)
        {
            if (!keyboard.enabled) InputSystem.EnableDevice(keyboard);
            game.InputKeyboard = keyboard; keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        }
        Fighter Opponent => game.Fighters.Find(f => !f.isPlayer);
        void DisableAI() { foreach (var f in game.Fighters) if (!f.isPlayer) f.GetComponent<EnemyAIController>().enabled = false; }
        void Put(Fighter f, Vector3 at, float protection = 0) { f.Respawn(at, protection); Physics.SyncTransforms(); }
        void NewRound(int index = 0)
        {
            Press(); game.StartLevel(index); DisableAI();
            Put(game.Player, new Vector3(0, .58f, -5));
            for (int i = 1; i < game.Fighters.Count; i++) Put(game.Fighters[i], new Vector3(-3 + (i - 1) * 2.4f, .58f, 4.3f));
        }
        bool Click(string title)
        {
            foreach (var button in FindObjectsByType<Button>()) if (button.name == title && button.interactable) { button.onClick.Invoke(); return true; }
            Check("Find usable button " + title, false); return false;
        }
        IEnumerator Capture(string name, int width = 1920, int height = 1080)
        {
            yield return new WaitForSecondsRealtime(.22f); yield return new WaitForEndOfFrame();
            CaptureNow(Path.Combine(output, name + ".png"), width, height);
        }
        public static void CaptureNow(string path, int width, int height)
        {
            foreach (var portrait in FindObjectsByType<CharacterPortrait>()) portrait.Render();
            var camera = Camera.main; var canvas = GameManager.Instance.UI.Canvas;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases(); camera.SubmitRenderRequest(new RenderPipeline.StandardRequest { destination = target });
            var previous = RenderTexture.active; RenderTexture.active = target;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG()); Destroy(pixels); RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
        }
        void SavePreferences()
        {
            foreach (var key in new[] { "Boba.Unlocked", "Boba.Skin", "Boba.Shake", "Boba.Fullscreen", "Boba.ReducedMotion", "Boba.BestCombo", "Boba.Best.0", "Boba.Best.1", "Boba.Best.2" }) originalInts[key] = PlayerPrefs.GetInt(key, key == "Boba.Unlocked" || key == "Boba.Shake" ? 1 : 0);
            foreach (var key in new[] { "Boba.SFX", "Boba.Music" }) originalFloats[key] = PlayerPrefs.GetFloat(key, key == "Boba.SFX" ? .65f : .5f);
        }
        void Finish()
        {
            if (finished) return; finished = true;
            foreach (var pair in originalInts) PlayerPrefs.SetInt(pair.Key, pair.Value);
            foreach (var pair in originalFloats) PlayerPrefs.SetFloat(pair.Key, pair.Value); PlayerPrefs.Save();
            checks.Add($"TOTAL: {checks.FindAll(s => s.StartsWith("PASS:")).Count} passed, {failed} failed, {errors} runtime errors, {warnings} runtime warnings");
            Directory.CreateDirectory(output); File.WriteAllLines(Path.Combine(output, "runtime-verification.txt"), checks);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            Time.timeScale = 1; AudioListener.pause = false;
            if (failed + errors + warnings > 0) Application.Quit(1); else { game.ToMenu(); Click("EXIT"); }
        }
        IEnumerator Start()
        {
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../QA/Final")); Directory.CreateDirectory(output);
            game = GameManager.Instance; SavePreferences();
            PlayerPrefs.SetInt("Boba.Unlocked", 1); GamePreferences.Skin = 0;
            GamePreferences.Shake = true; GamePreferences.ReducedMotion = false;
            keyboard = InputSystem.AddDevice<Keyboard>("FinalVerificationKeyboard"); keyboard.MakeCurrent();
            game.InputKeyboard = keyboard;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var device in InputSystem.devices) if (device is Keyboard && device != keyboard) InputSystem.DisableDevice(device);
            yield return new WaitForSeconds(.8f);
            Check("Existing Unity scene loads into main menu", game.State == MatchState.Menu && game.UI.CurrentScreen == "Menu");
            Check("Three level configurations preserve requested rules", game.levels.Length == 3 && game.levels[0].opponents == 1 && game.levels[0].knockoutTarget == 2 && game.levels[0].duration == 90 && game.levels[1].opponents == 2 && game.levels[1].knockoutTarget == 3 && game.levels[1].duration == 90 && game.levels[2].opponents == 3 && game.levels[2].knockoutTarget == 5 && game.levels[2].duration == 100);
            Check("Main menu original music is playing", game.Feedback.MusicPlaying && game.Feedback.MusicSource.clip.name == "MenuLoop");
            yield return Capture("01-main-menu");
            Click("CHOOSE YOUR PEARL"); Check("Character selection opens", game.UI.CurrentScreen == "Characters");
            Click("SELECT 2"); Check("Cosmetic choice is saved", GamePreferences.Skin == 1);
            yield return Capture("02-character-selection");
            var portraits = FindObjectsByType<CharacterPortrait>();
            Check("Selection displays three live 3D playable model previews", portraits.Length == 3 && Array.TrueForAll(portraits, portrait => portrait.Model && portrait.Model.GetComponent<FighterPresentation>().preview));
            Check("All three portrait textures contain visible rendered model pixels", portraits.Length == 3 && Array.TrueForAll(portraits, portrait => portrait.HasRenderedModel()));
            var rigs = game.playerPrefab.GetComponent<FighterPresentation>().characterRigs;
            Check("Sphere, rounded jelly cube and tapered cup use distinct meshes", rigs.Length == 3 && rigs[0].body.GetComponent<MeshFilter>().sharedMesh != rigs[1].body.GetComponent<MeshFilter>().sharedMesh && rigs[1].body.GetComponent<MeshFilter>().sharedMesh != rigs[2].body.GetComponent<MeshFilter>().sharedMesh && rigs[0].body.GetComponent<MeshFilter>().sharedMesh != rigs[2].body.GetComponent<MeshFilter>().sharedMesh);
            for (int character = 0; character < 3; character++)
            {
                if (GamePreferences.Skin != character) Click("SELECT " + (character + 1));
                Check("Character " + character + " selected and persisted", PlayerPrefs.GetInt("Boba.Skin") == character);
                yield return Capture("18-character-" + character + "-selection");
                Click("READY / BACK"); Click("PLAY"); DisableAI();
                var actor = game.Player; var look = actor.GetComponent<FighterPresentation>(); var collider = actor.GetComponent<SphereCollider>();
                Check("Character " + character + " spawns matching visual with identical combat root", look.CharacterIndex == character && look.characterRigs[character].root.gameObject.activeSelf && actor.moveSpeed == game.playerPrefab.moveSpeed && collider.radius == game.playerPrefab.GetComponent<SphereCollider>().radius && actor.Dash.chargeDuration == 1.15f && actor.Dash.cooldown == 1 && actor.Dash.dashDuration == .23f && game.Score.Hearts == 3 && actor.GetComponentsInChildren<Collider>().Length == 1);
                yield return new WaitForSeconds(1.6f);
                Put(actor, new Vector3(-4, .58f, -3));
                Press(Key.D); yield return new WaitForSeconds(.3f); Press(Key.Space); yield return new WaitForSeconds(1.2f);
                Check("Character " + character + " moves and fully charges via production input", actor.transform.position.x > -3.5f && actor.Dash.Charge == 1);
                yield return Capture("21-character-" + character + "-gameplay");
                Press(); yield return null; yield return new WaitForFixedUpdate();
                Check("Character " + character + " dashes with same power and real trail", actor.Dash.IsDashing && actor.Body.linearVelocity.x > 18 && look.trail.emitting);
                actor.Freeze(); yield return new WaitForSeconds(.25f);
                game.KnockOut(actor); yield return new WaitForSeconds(1.9f);
                Check("Character " + character + " survives respawn with selected rig", actor.Alive && look.CharacterIndex == character && look.characterRigs[character].root.gameObject.activeSelf);
                game.StartMatch(); DisableAI();
                Check("Character " + character + " persists through restart", game.Player.GetComponent<FighterPresentation>().CharacterIndex == character);
                PlayerPrefs.SetInt("Boba.Unlocked", 3); game.StartLevel(1); DisableAI();
                Check("Character " + character + " persists into next level", game.Player.GetComponent<FighterPresentation>().CharacterIndex == character);
                game.ToMenu(); Click("CHOOSE YOUR PEARL");
            }
            Click("SELECT 2"); PlayerPrefs.SetInt("Boba.Unlocked", 1);
            Click("READY / BACK"); Click("LEVEL SELECT");
            Check("Locked levels reject start requests", !game.StartLevel(1) && !game.StartLevel(2));
            yield return Capture("03-level-selection-locked");
            Click("BACK"); Click("HOW TO PLAY"); Check("How-to screen opens", game.UI.CurrentScreen == "HowTo"); Click("GOT IT");
            Click("SETTINGS");
            foreach (var slider in FindObjectsByType<Slider>()) slider.value = .3f;
            PlayerPrefs.Save(); Check("Music and SFX sliders update sources and preferences", Mathf.Abs(game.Feedback.SfxVolume - .3f) < .01f && Mathf.Abs(game.Feedback.MusicVolume - .3f) < .01f && Mathf.Abs(game.Feedback.MusicSource.volume - .126f) < .01f);
            foreach (var toggle in FindObjectsByType<Toggle>()) if (toggle.name == "SCREEN SHAKE" || toggle.name == "REDUCED MOTION") toggle.isOn = !toggle.isOn;
            Check("Shake and reduced-motion toggles persist", !GamePreferences.Shake && GamePreferences.ReducedMotion);
            GamePreferences.Shake = true; GamePreferences.ReducedMotion = false;
            foreach (var toggle in FindObjectsByType<Toggle>()) if (toggle.name == "FULLSCREEN") toggle.isOn = true;
            yield return new WaitForSecondsRealtime(.4f);
            Check("Fullscreen toggle applies borderless mode", GamePreferences.Fullscreen && Screen.fullScreenMode == FullScreenMode.FullScreenWindow);
            foreach (var toggle in FindObjectsByType<Toggle>()) if (toggle.name == "FULLSCREEN") toggle.isOn = false;
            Screen.SetResolution(1920, 1080, false); yield return new WaitForSecondsRealtime(.4f);
            Check("Fullscreen toggle restores windowed mode", !GamePreferences.Fullscreen && Screen.fullScreenMode == FullScreenMode.Windowed);
            yield return Capture("04-settings"); Click("BACK"); Click("PLAY"); DisableAI();
            Check("Play starts First Sip with three lives", game.Running && game.Fighters.Count == 2 && game.Score.Hearts == 3);
            var skinBlock = new MaterialPropertyBlock(); game.Player.GetComponent<FighterPresentation>().tintRenderers[0].GetPropertyBlock(skinBlock);
            Check("Selected cosmetic applied to actual player material", GamePreferences.Skin == 1 && skinBlock.GetColor("_BaseColor") == GamePreferences.SkinColors[1]);
            Check("Gameplay music changes context", game.Feedback.MusicSource.clip.name == "BrawlLoop" && game.Feedback.MusicPlaying);
            yield return new WaitForSeconds(1.6f);
            var p = game.Player; var e = Opponent; Vector3 start = p.transform.position;
            Press(Key.D); yield return new WaitForSeconds(.3f); Press(); yield return new WaitForSeconds(.22f);
            Check("Production keyboard movement and facing", p.transform.position.x > start.x + .5f && p.Facing.x > .9f);
            Check("Arcade braking is stable", new Vector2(p.Body.linearVelocity.x, p.Body.linearVelocity.z).magnitude < .5f);
            Press(Key.Space); yield return new WaitForSeconds(.6f); Check("Space hold accumulates charge", p.Dash.IsCharging && p.Dash.Charge > .4f && p.Dash.Charge < .8f);
            yield return new WaitForSeconds(.65f); Check("Charge capped at full", p.Dash.Charge == 1);
            yield return Capture("05-charge-action");
            Press(); yield return null; yield return new WaitForFixedUpdate();
            Check("Release dashes in chosen direction", p.Dash.IsDashing && p.Body.linearVelocity.x > 10); Check("Cooldown rejects immediate re-charge", !p.Dash.BeginCharge());
            yield return new WaitForSeconds(.24f); Put(p, new Vector3(-4, .58f, -3));
            Press(Key.Escape); yield return null; yield return null; Press();
            float clock = game.Remaining; yield return new WaitForSecondsRealtime(.3f);
            Check("Escape pause freezes clock and blocks dash", game.State == MatchState.Paused && game.Remaining == clock && !p.Dash.BeginCharge() && AudioListener.pause);
            yield return Capture("06-pause"); Click("SETTINGS"); Check("Pause settings keep match paused", game.State == MatchState.Paused); Click("BACK"); Click("RESUME");
            yield return new WaitForSeconds(.12f); Check("Resume restores match time and audio", game.Running && game.Remaining < clock && !AudioListener.pause);
            NewRound(); p = game.Player; p.GetComponent<PlayerController>().scriptedInput = true;
            Put(p, new Vector3(-5, .58f, -3)); p.Intent = Vector3.right; yield return new WaitForFixedUpdate(); p.Intent = Vector3.zero;
            p.Dash.BeginCharge(); yield return new WaitForSeconds(1.2f); Vector3 dashStart = p.transform.position; p.Dash.Release();
            yield return new WaitForSeconds(1.25f);
            Check("Full dash has controlled travel and returns to braking", p.Alive && Vector3.Distance(dashStart, p.transform.position) < 6 && new Vector2(p.Body.linearVelocity.x, p.Body.linearVelocity.z).magnitude < .5f);
            NewRound(); p = game.Player; e = Opponent; Put(p, new Vector3(4, .58f, -3)); Put(e, new Vector3(4, .58f, -.6f));
            p.GetComponent<PlayerController>().scriptedInput = true; p.Intent = Vector3.forward; yield return new WaitForFixedUpdate(); p.Intent = Vector3.zero;
            p.Dash.BeginCharge(); yield return new WaitForSeconds(1.2f); p.Dash.Release(); yield return new WaitForSeconds(.16f);
            Check("Real charged contact applies knockback and attribution", e.LastAttacker == p && e.Body.linearVelocity.z > 3);
            Check("Same dash cannot hit a target twice", !p.Dash.TryHit(e));
            p.Freeze(); e.Freeze(); yield return Capture("07-dash-impact");
            NewRound(); p = game.Player; e = Opponent; p.GetComponent<PlayerController>().scriptedInput = true;
            Put(p, new Vector3(5.2f, .58f, 0)); Put(e, new Vector3(6.8f, .58f, 0));
            p.Intent = Vector3.right; yield return new WaitForFixedUpdate(); p.Intent = Vector3.zero;
            p.Dash.BeginCharge(); yield return new WaitForSeconds(1.2f); p.Dash.Release(); yield return new WaitForSeconds(.1f); p.Freeze(); yield return new WaitForSeconds(.7f);
            Check("Physical dash ring-out awards a KO", !e.Alive && game.Score.Knockouts == 1);
            yield return Capture("08-knockout");
            NewRound(); p = game.Player; e = Opponent; Put(e, new Vector3(0, .58f, 0)); yield return new WaitForSeconds(.7f);
            Check("Real center hole permits physical elimination", !e.Alive && game.Score.Knockouts == 0);
            game.KnockOut(e); Check("Duplicate enemy KO is ignored", game.Score.Knockouts == 0);
            yield return new WaitForSeconds(1.9f); Check("Enemy safely respawns with attack protection", e.Alive && e.IsProtected && new Vector2(e.transform.position.x, e.transform.position.z).magnitude > 3);
            e.ReceiveHit(p, Vector3.forward, 15); Check("Protected fighter rejects hit attribution", e.LastAttacker == null);
            Put(e, new Vector3(9, .58f, 0)); yield return new WaitForSeconds(.7f); Check("Outer perimeter permits physical elimination", !e.Alive);
            NewRound(); p = game.Player; e = Opponent; e.ReceiveHit(p, Vector3.forward, 8); e.Freeze(); yield return new WaitForSeconds(4.2f); game.KnockOut(e);
            Check("Expired attacker credit does not score", game.Score.Knockouts == 0);
            NewRound(); p = game.Player; e = Opponent; e.ReceiveHit(p, Vector3.forward, 8); game.KnockOut(e); game.KnockOut(e);
            Check("Recent attacker credit scores exactly once", game.Score.Knockouts == 1 && game.Points == 100);
            yield return new WaitForSeconds(1.9f); Put(e, new Vector3(0, .58f, 5)); e.ReceiveHit(p, Vector3.forward, 8); game.KnockOut(e);
            Check("First Sip wins and unlocks Sugar Rush", game.State == MatchState.Victory && GamePreferences.Unlocked == 2 && GamePreferences.Best(0) > 0);
            Check("Combo feedback and performance score are recorded", game.HighestCombo >= 2 && game.Points > 300);
            yield return Capture("09-level-complete");
            int lives = game.Score.Hearts; game.KnockOut(p); Check("Results reject late elimination", game.Score.Hearts == lives);
            Click("NEXT LEVEL"); DisableAI(); Check("Next Level starts Sugar Rush with two opponents", game.LevelIndex == 1 && game.Fighters.Count == 3 && game.Level.knockoutTarget == 3);
            yield return Capture("10-sugar-rush");
            yield return WinCurrentRound();
            Check("Sugar Rush wins and unlocks Final Brawl", game.State == MatchState.Victory && GamePreferences.Unlocked == 3);
            Click("NEXT LEVEL"); DisableAI(); Check("Final Brawl starts with three opponents and 100 seconds", game.LevelIndex == 2 && game.Fighters.Count == 4 && game.Level.duration == 100);
            Check("Final level enables two physics obstacles", game.Presentation.obstacles[0].activeSelf && game.Presentation.obstacles[1].activeSelf);
            yield return Capture("11-final-brawl"); yield return WinCurrentRound();
            Check("Final level produces champion result", game.State == MatchState.Victory && game.UI.CurrentScreen == "Champion" && GamePreferences.Best(2) > 0);
            yield return Capture("12-champion");
            Click("RETRY"); DisableAI(); Check("Retry retains selected round and resets rules", game.LevelIndex == 2 && game.Score.Hearts == 3 && game.Score.Knockouts == 0 && game.Remaining > 99);
            for (int index = 0; index < 3; index++)
            {
                NewRound(index); game.SetRemainingForVerification(.05f); yield return new WaitForSeconds(.15f);
                Check("Round " + (index + 1) + " times out to defeat", game.State == MatchState.Defeat);
                Click("RETRY"); DisableAI();
                Check("Round " + (index + 1) + " retry resets timer and hearts", game.Running && game.Score.Hearts == 3 && game.Score.Knockouts == 0);
            }
            NewRound(); p = game.Player;
            Check("Exact supplied FAHHH clip is assigned and decoded", game.Feedback.playerFallVocal && game.Feedback.playerFallVocal.name == "fahhh" && game.Feedback.playerFallVocal.loadState == AudioDataLoadState.Loaded && game.Feedback.playerFallVocal.channels == 2 && game.Feedback.playerFallVocal.length > 1.8f);
            int vocalCount = game.Feedback.PlayerFallPlayCount;
            game.Feedback.SetSfx(.65f); Put(p, new Vector3(0, .58f, 0)); yield return new WaitForSeconds(.7f);
            Check("Player physical fall loses one heart", !p.Alive && game.Score.Hearts == 2); game.KnockOut(p); Check("Duplicate player KO cannot deduct another heart", game.Score.Hearts == 2);
            Check("Center-hole fall starts exactly one supplied vocal", game.Feedback.PlayerFallPlayCount == vocalCount + 1 && game.Feedback.FallSource.clip == game.Feedback.playerFallVocal && game.Feedback.FallSource.isPlaying);
            yield return MeasureVocal("Center-hole");
            Check("Duplicate player trigger does not restart audio", game.Feedback.PlayerFallPlayCount == vocalCount + 1);
            Check("Music ducks beneath player reaction", game.Feedback.MusicSource.volume < game.Feedback.MusicVolume * .42f * .6f);
            game.Feedback.SetSfx(.2f); Check("SFX volume controls active supplied vocal", Mathf.Abs(game.Feedback.FallSource.volume - .2f) < .001f); game.Feedback.SetSfx(.65f);
            game.Feedback.SetSfx(0); yield return new WaitForSecondsRealtime(.15f);
            var muted = new float[512]; game.Feedback.FallSource.GetOutputData(muted, 0); float mutedPeak = 0;
            foreach (float sample in muted) mutedPeak = Mathf.Max(mutedPeak, Mathf.Abs(sample));
            Check("Muting SFX silences actual ongoing FAHHH source samples", game.Feedback.FallSource.isPlaying && mutedPeak < .00001f); game.Feedback.SetSfx(.65f);
            yield return new WaitForSeconds(1.9f); Check("Player respawns safely with protection", p.Alive && p.IsProtected);
            Put(p, new Vector3(9, .58f, 0)); yield return new WaitForSeconds(.7f);
            Check("Outer-edge player fall plays vocal once and deducts one heart", game.Feedback.PlayerFallPlayCount == vocalCount + 2 && game.Score.Hearts == 1 && game.Feedback.FallSource.isPlaying);
            yield return MeasureVocal("Outer-edge");
            yield return new WaitForSeconds(1.9f);
            int beforeEnemy = game.Feedback.PlayerFallPlayCount; Put(Opponent, new Vector3(0, .58f, 0)); yield return new WaitForSeconds(.7f);
            Check("Ordinary enemy hazard retains normal KO and excludes player vocal", !Opponent.Alive && game.Feedback.PlayerFallPlayCount == beforeEnemy);
            Put(p, new Vector3(0, .58f, 0)); yield return new WaitForSeconds(.7f);
            Check("Three lives lost triggers Game Over", game.State == MatchState.Defeat && game.Score.Hearts == 0);
            Check("Final-heart fall continues exact vocal after character deactivation", game.Feedback.PlayerFallPlayCount == vocalCount + 3 && game.Feedback.FallSource.isPlaying && game.Feedback.FallSource.clip == game.Feedback.playerFallVocal);
            yield return MeasureVocal("Final-heart");
            yield return Capture("13-game-over");
            // A fresh final-heart transition verifies a quick menu does not destroy the audio source.
            NewRound(); p = game.Player; game.KnockOut(p); yield return new WaitForSeconds(1.9f); game.KnockOut(p); yield return new WaitForSeconds(1.9f); game.KnockOut(p);
            game.ToMenu(); yield return new WaitForSecondsRealtime(.15f);
            Check("Menu transition preserves final-heart vocal playback", game.Feedback.FallSource && game.Feedback.FallSource.isPlaying);
            for (int index = 0; index < 3; index++)
            {
                NewRound(index); var enemy = Opponent; enemy.GetComponent<EnemyAIController>().enabled = true;
                Vector3 before = enemy.transform.position; yield return new WaitForSeconds(1.4f);
                Check("Round " + (index + 1) + " AI moves without immediate suicide", enemy.Alive && Vector3.Distance(before, enemy.transform.position) > .4f);
                Put(game.Player, new Vector3(4, .58f, 4)); Put(enemy, new Vector3(4, .58f, 1));
                bool charged = false, attacked = false; float end = Time.time + 5.5f;
                while (Time.time < end && game.Running) { charged |= enemy.Dash.IsCharging; attacked |= enemy.Dash.IsDashing; yield return null; }
                Check("Round " + (index + 1) + " AI telegraphs and attacks", charged && attacked);
            }
            game.ToMenu(); Click("LEVEL SELECT"); yield return Capture("14-levels-unlocked"); Click("BACK");
            Screen.SetResolution(1280, 720, false); yield return new WaitForSecondsRealtime(.4f);
            Check("1280x720 resolution applies", Screen.width == 1280 && Screen.height == 720);
            yield return Capture("15-menu-1280x720", 1280, 720);
            Click("CHOOSE YOUR PEARL"); yield return Capture("24-characters-1280x720", 1280, 720); Click("READY / BACK");
            Click("PLAY"); DisableAI(); yield return Capture("16-gameplay-1280x720", 1280, 720);
            Check("No missing fighter, visual or UI references", game.Player.visual && game.Player.GetComponent<FighterPresentation>().trail && game.UI.Canvas && game.Presentation.vortex);
            game.Pause(); Click("RESTART"); DisableAI(); Check("Pause Restart resumes fresh match", game.Running && Time.timeScale == 1 && game.Score.Hearts == 3);
            game.Pause(); Click("MAIN MENU"); Check("Menu restores normal timescale and audio", game.State == MatchState.Menu && Time.timeScale == 1 && !AudioListener.pause);
            Check("No Unity runtime errors", errors == 0); Check("No Unity runtime warnings", warnings == 0);
            Finish();
        }
        IEnumerator MeasureVocal(string context)
        {
            var source = game.Feedback.FallSource; int first = source.timeSamples; float peak = 0; var samples = new float[512];
            for (int i = 0; i < 12; i++)
            {
                source.GetOutputData(samples, 0);
                foreach (float sample in samples) peak = Mathf.Max(peak, Mathf.Abs(sample));
                yield return new WaitForSecondsRealtime(.025f);
            }
            checks.Add($"AUDIO MEASUREMENT {context}: clip={source.clip.name}, startSample={first}, endSample={source.timeSamples}, sourceOutputPeak={peak:0.000000}, volume={source.volume:0.00}");
            Check(context + " actual AudioSource output contains nonzero PCM and advancing samples", source.isPlaying && source.timeSamples > first && peak > .0001f);
        }
        IEnumerator WinCurrentRound()
        {
            DisableAI();
            while (game.Running && game.Score.Knockouts < game.Level.knockoutTarget)
            {
                Fighter enemy = game.Fighters.Find(f => !f.isPlayer && f.Alive);
                if (!enemy) { yield return new WaitForSeconds(.25f); continue; }
                Put(enemy, new Vector3(-3, .58f, 4)); enemy.ReceiveHit(game.Player, Vector3.forward, 8); game.KnockOut(enemy);
                yield return new WaitForSeconds(.1f);
            }
        }
    }
}
