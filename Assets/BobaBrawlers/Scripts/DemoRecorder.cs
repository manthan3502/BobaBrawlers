using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace BobaBrawlers
{
    // Opt-in offline capture of production Unity rendering AND the engine's mixed audio.
    // Scripted input and actor placement are deliberately disclosed in the evidence.
    public sealed class DemoRecorder : MonoBehaviour
    {
        string folder;
        bool recording, audioStarted;
        int frame, channels, sampleRate, observedFalls;
        long sampleFrames;
        BinaryWriter wave;
        Keyboard keyboard;
        GameManager game;
        readonly List<string> events = new List<string>();
        readonly Dictionary<string, int> preferences = new Dictionary<string, int>();
        float sfx, music;
        void Press(params Key[] keys) { if (!keyboard.enabled) InputSystem.EnableDevice(keyboard); game.InputKeyboard = keyboard; keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
        void Click(string title)
        { foreach (var b in FindObjectsByType<Button>()) if (b.name == title && b.interactable) { b.onClick.Invoke(); return; } throw new Exception("Recording button missing: " + title); }
        void Mark(string message) { events.Add($"{frame / 30f:0.000}s / frame {frame}: {message}"); }
        IEnumerator Wait(float seconds) { int end = frame + Mathf.RoundToInt(seconds * 30); while (frame < end) yield return null; }
        void Header()
        {
            wave = new BinaryWriter(File.Create(Path.Combine(folder, "game-mix.wav")));
            wave.Write(Encoding.ASCII.GetBytes("RIFF")); wave.Write(0); wave.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            wave.Write(16); wave.Write((short)1); wave.Write((short)channels); wave.Write(sampleRate);
            wave.Write(sampleRate * channels * 2); wave.Write((short)(channels * 2)); wave.Write((short)16);
            wave.Write(Encoding.ASCII.GetBytes("data")); wave.Write(0);
        }
        IEnumerator Frames()
        {
            while (recording)
            {
                yield return new WaitForEndOfFrame();
                int count = AudioRenderer.GetSampleCountForCaptureFrame();
                using (var buffer = new NativeArray<float>(count * channels, Allocator.Temp))
                {
                    if (!AudioRenderer.Render(buffer)) throw new Exception("Unity mixed audio render failed");
                    for (int i = 0; i < buffer.Length; i++) wave.Write((short)Mathf.RoundToInt(Mathf.Clamp(buffer[i], -1, 1) * 32767));
                }
                sampleFrames += count;
                if (game.Feedback.PlayerFallPlayCount != observedFalls)
                { observedFalls = game.Feedback.PlayerFallPlayCount; Mark("FAHHH from production player fall; clip=" + game.Feedback.FallSource.clip.name); }
                FinalVerification.CaptureNow(Path.Combine(folder, $"frame-{frame++:00000}.png"), 1280, 720);
            }
        }
        IEnumerator MoveAndDash()
        {
            var p = game.Player;
            foreach (var fighter in game.Fighters) if (!fighter.isPlayer) fighter.GetComponent<EnemyAIController>().enabled = false;
            p.Respawn(new Vector3(-4, .58f, -3), 0); Physics.SyncTransforms();
            Press(Key.D); yield return Wait(.55f); Press(); yield return Wait(.6f);
            Press(Key.Space); yield return Wait(1.4f); Press(); yield return Wait(.5f); Press(); yield return Wait(1.2f);
            p.Freeze();
        }
        IEnumerator PlayerFall(bool center)
        {
            Press(); var p = game.Player;
            p.Respawn(center ? new Vector3(0, .58f, 0) : new Vector3(9, .58f, 0), 0); Physics.SyncTransforms();
            Mark(center ? "Staged physical center-hole fall" : "Staged physical outer-edge fall");
            yield return Wait(3.5f);
        }
        IEnumerator PhysicalKO()
        {
            Press(); var p = game.Player; var e = game.Fighters.Find(f => !f.isPlayer);
            e.GetComponent<EnemyAIController>().enabled = false;
            while (!e.Alive && game.Running) yield return null;
            p.Respawn(new Vector3(5.2f, .58f, 0), 0); e.Respawn(new Vector3(6.8f, .58f, 0), 0); Physics.SyncTransforms();
            Mark("Actors staged for physical charged dash ring-out");
            Press(Key.D); yield return Wait(.067f); Press(Key.Space); yield return Wait(1.3f); Press(); yield return Wait(.1f); p.Freeze();
            yield return Wait(2.2f);
            Mark("Result: KOs=" + game.Score.Knockouts + ", state=" + game.State);
        }
        IEnumerator Start()
        {
            game = GameManager.Instance; folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../QA/Final/RecordingFrames")); Directory.CreateDirectory(folder);
            foreach (string old in Directory.GetFiles(folder, "frame-*.png")) File.Delete(old);
            foreach (string key in new[] { "Boba.Unlocked", "Boba.Skin", "Boba.BestCombo", "Boba.Best.0", "Boba.Best.1", "Boba.Best.2" }) preferences[key] = PlayerPrefs.GetInt(key, key == "Boba.Unlocked" ? 1 : 0);
            sfx = game.Feedback.SfxVolume; music = game.Feedback.MusicVolume;
            game.Feedback.SetSfx(.85f); game.Feedback.SetMusic(.45f);
            keyboard = InputSystem.AddDevice<Keyboard>("DemoKeyboard"); InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            game.InputKeyboard = keyboard;
            foreach (var d in InputSystem.devices) if (d is Keyboard && d != keyboard) InputSystem.DisableDevice(d);
            Time.captureFramerate = 30;
            sampleRate = AudioSettings.outputSampleRate;
            var mode = AudioSettings.speakerMode;
            channels = mode == AudioSpeakerMode.Mono ? 1 : mode == AudioSpeakerMode.Quad ? 4 : mode == AudioSpeakerMode.Surround ? 5 : mode == AudioSpeakerMode.Mode5point1 ? 6 : mode == AudioSpeakerMode.Mode7point1 ? 8 : 2;
            audioStarted = AudioRenderer.Start(); if (!audioStarted) throw new Exception("Unity AudioRenderer could not start");
            Header(); recording = true; StartCoroutine(Frames());
            Mark("Genuine Unity offline recording; scripted demonstration, not human play");
            yield return Wait(4);
            Click("CHOOSE YOUR PEARL");
            for (int i = 0; i < 3; i++)
            { if (GamePreferences.Skin != i) Click("SELECT " + (i + 1)); Mark("Live model selection: " + GamePreferences.SkinNames[i]); yield return Wait(4); }
            Click("SELECT 1"); Click("READY / BACK"); Click("LEVEL SELECT"); yield return Wait(4); Click("PLAY ROUND 1"); yield return Wait(2);
            yield return MoveAndDash(); yield return PlayerFall(true); yield return PhysicalKO(); yield return PhysicalKO();
            yield return Wait(4);
            game.ToMenu(); Click("CHOOSE YOUR PEARL"); Click("SELECT 2"); yield return Wait(2.5f); Click("READY / BACK"); Click("PLAY"); yield return Wait(2);
            yield return MoveAndDash(); yield return PlayerFall(false);
            game.ToMenu(); Click("CHOOSE YOUR PEARL"); Click("SELECT 3"); yield return Wait(2.5f); Click("READY / BACK"); Click("PLAY"); yield return Wait(2);
            yield return MoveAndDash(); yield return PhysicalKO(); yield return PhysicalKO(); yield return Wait(5);
            while (frame < 2100) yield return null;
            recording = false; yield return null;
            CloseAudio(); Time.captureFramerate = 0;
            File.WriteAllLines(Path.Combine(folder, "recording-events.txt"), events);
            File.WriteAllText(Path.Combine(folder, "recording-info.txt"), $"Actual Unity rendered frames: {frame}\nPlayback rate: 30 fps\nDuration: {frame / 30f:0.000}s\nUnity AudioRenderer mixed PCM: {sampleFrames} sample frames, {sampleRate} Hz, {channels} channels\nAudio duration: {(double)sampleFrames / sampleRate:0.000}s\nAutomated staged offline demonstration, not human playtesting. Production controllers, physics, scene rendering and engine audio. No post-dubbed sound or concept imagery.\n");
            foreach (var pair in preferences) PlayerPrefs.SetInt(pair.Key, pair.Value);
            game.Feedback.SetSfx(sfx); game.Feedback.SetMusic(music); PlayerPrefs.Save();
            InputSystem.RemoveDevice(keyboard); game.Exit();
        }
        void CloseAudio()
        {
            if (wave != null)
            {
                long size = wave.BaseStream.Length; wave.Seek(4, SeekOrigin.Begin); wave.Write((int)(size - 8)); wave.Seek(40, SeekOrigin.Begin); wave.Write((int)(size - 44)); wave.Dispose(); wave = null;
            }
            if (audioStarted) { AudioRenderer.Stop(); audioStarted = false; }
        }
        void OnDestroy() { CloseAudio(); Time.captureFramerate = 0; }
    }
}
