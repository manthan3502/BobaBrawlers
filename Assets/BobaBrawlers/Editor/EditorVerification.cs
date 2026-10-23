using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BobaBrawlers.Editor
{
    [InitializeOnLoad]
    public static class EditorVerification
    {
        const string Key = "Boba.FinalEditorCheck";
        static double deadline;
        static int initialFrame;
        static float initialClock;
        static int phase;
        static EditorVerification()
        {
            if (SessionState.GetBool(Key, false)) { deadline = EditorApplication.timeSinceStartup + 60; EditorApplication.update += Tick; }
        }
        public static void Begin()
        {
            EditorSceneManager.OpenScene("Assets/BobaBrawlers/Scenes/BobaArena.unity");
            SessionState.SetBool(Key, true); deadline = EditorApplication.timeSinceStartup + 60;
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            try
            {
                EditorApplication.QueuePlayerLoopUpdate();
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Editor Play Mode verification timed out");
                if (!EditorApplication.isPlaying || !GameManager.Instance) return;
                var game = GameManager.Instance;
                if (phase == 0 && game.UI.CurrentScreen == "Menu")
                {
                    game.PlayCampaign(); initialFrame = Time.frameCount; initialClock = game.Remaining; phase = 1; return;
                }
                if (phase == 1 && Time.frameCount - initialFrame > 90)
                {
                    Directory.CreateDirectory("QA/Final");
                    FinalVerification.CaptureNow(Path.GetFullPath("QA/Final/17-unity-editor-play-mode.png"), 1920, 1080);
                    bool advancing = game.Running && game.Remaining < initialClock && game.Player && game.Player.Body.position.y > -.1f;
                    File.WriteAllText("QA/Final/editor-verification.txt", $"Editor: {Application.unityVersion}\nPlay Mode: {EditorApplication.isPlaying}\nFrames advanced: {Time.frameCount - initialFrame}\nClock: {initialClock:0.00} -> {game.Remaining:0.00}\nPlayer alive: {game.Player.Alive}\nResult: {(advancing ? "PASS" : "FAIL")}\nEvidence is a real Editor Play Mode camera/Canvas render, not an Editor chrome screenshot.\n");
                    Complete(advancing ? 0 : 1);
                }
            }
            catch (Exception e)
            { Directory.CreateDirectory("QA/Final"); File.WriteAllText("QA/Final/editor-verification.txt", "FAIL: " + e); Complete(1); }
        }
        static void Complete(int code)
        { SessionState.SetBool(Key, false); EditorApplication.update -= Tick; EditorApplication.Exit(code); }
    }
}
