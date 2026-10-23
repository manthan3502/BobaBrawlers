using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace BobaBrawlers
{
    // Opt-in offscreen GPU/render-loop smoke benchmark. Not a human playtest.
    public sealed class PerformanceSmoke : MonoBehaviour
    {
        readonly List<float> frameTimes = new List<float>();
        readonly List<float> renderTimes = new List<float>();
        int errors;
        void OnEnable() { Application.logMessageReceived += Log; }
        void OnDisable() { Application.logMessageReceived -= Log; }
        void Log(string m, string t, LogType type) { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors++; }
        IEnumerator Start()
        {
            var game = GameManager.Instance; int unlocked = PlayerPrefs.GetInt("Boba.Unlocked", 1); PlayerPrefs.SetInt("Boba.Unlocked", 3);
            int best = GamePreferences.Best(2), bestCombo = GamePreferences.BestCombo;
            game.StartLevel(2); var target = RenderTexture.GetTemporary(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var camera = Camera.main; var canvas = game.UI.Canvas; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            float began = Time.realtimeSinceStartup, last = began;
            int restarts = 0;
            while (Time.realtimeSinceStartup - began < 24)
            {
                if (!game.Running) { game.StartLevel(2); restarts++; }
                game.Player.GetComponent<PlayerController>().scriptedInput = true;
                Vector3 p = game.Player.transform.position; p.y = 0;
                game.Player.Intent = (Vector3.Cross(Vector3.up, p.normalized) + p.normalized * Mathf.Clamp(5.3f - p.magnitude, -1, 1)).normalized;
                if (game.Player.Dash.CooldownRemaining <= 0 && !game.Player.Dash.IsCharging && Mathf.Sin(Time.time * 1.8f) > .95f) game.Player.Dash.BeginCharge();
                if (game.Player.Dash.IsCharging && game.Player.Dash.Charge > .5f) game.Player.Dash.Release();
                yield return new WaitForEndOfFrame();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                camera.SubmitRenderRequest(new RenderPipeline.StandardRequest { destination = target });
                watch.Stop(); float now = Time.realtimeSinceStartup;
                if (now - began > 4) { frameTimes.Add((now - last) * 1000); renderTimes.Add((float)watch.Elapsed.TotalMilliseconds); }
                last = now;
            }
            frameTimes.Sort(); renderTimes.Sort();
            float sum = 0; foreach (float value in frameTimes) sum += value;
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../QA/Final/performance-smoke.txt"));
            File.WriteAllText(path, $"Environment: {SystemInfo.operatingSystem}\nCPU: {SystemInfo.processorType}\nGPU: {SystemInfo.graphicsDeviceName}\nResolution: 1920x1080 offscreen camera + Canvas\nScenario: Final Brawl, three active AI, scripted circular movement and dashes, real physics\nWarmup: 4 seconds; measured duration: approximately 20 seconds\nSamples: {frameTimes.Count}\nAverage render-loop interval: {sum / frameTimes.Count:0.00} ms\nObserved offscreen loop rate: {1000 / (sum / frameTimes.Count):0.0} fps\nMedian interval: {frameTimes[frameTimes.Count / 2]:0.00} ms\n95th percentile interval: {frameTimes[(int)(frameTimes.Count * .95f)]:0.00} ms\nMedian synchronous render submission: {renderTimes[renderTimes.Count / 2]:0.00} ms\nMatch restarts: {restarts}\nRuntime errors: {errors}\nThis is an offscreen smoke measurement on this host, not a guarantee of visible-window performance on other laptops.\n");
            PlayerPrefs.SetInt("Boba.Unlocked", unlocked); PlayerPrefs.SetInt("Boba.Best.2", best); PlayerPrefs.SetInt("Boba.BestCombo", bestCombo); PlayerPrefs.Save(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; RenderTexture.ReleaseTemporary(target);
            Application.Quit(errors == 0 ? 0 : 1);
        }
    }
}
