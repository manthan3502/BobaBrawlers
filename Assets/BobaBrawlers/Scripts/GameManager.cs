using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BobaBrawlers
{
    public enum MatchState { Menu, Playing, Paused, Victory, Defeat }
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public LevelDefinition Level;
        public LevelDefinition[] levels;
        public Fighter[] ingredientPrefabs;
        public ArenaPresentation Presentation;
        public int LevelIndex { get; private set; }
        public int Points { get; private set; }
        public int Combo { get; private set; }
        public int HighestCombo { get; private set; }
        float lastScored = -100f;
        public Fighter playerPrefab, opponentPrefab;
        public UIManager UI;
        public FeedbackController Feedback;
        public MatchState State { get; private set; } = MatchState.Menu;
        public bool Running => State == MatchState.Playing;
        // Optional explicit device pairing for automated input and multi-keyboard hosts.
        // Ordinary play follows the current physical keyboard.
        public Keyboard InputKeyboard { get; set; }
        public Keyboard ActiveKeyboard => InputKeyboard ?? Keyboard.current;
        public Fighter Player { get; private set; }
        public readonly List<Fighter> Fighters = new List<Fighter>();
        public readonly ScoreManager Score = new ScoreManager();
        public float Remaining { get; private set; }
        readonly Dictionary<Fighter, float> respawns = new Dictionary<Fighter, float>();
        readonly List<Fighter> due = new List<Fighter>();

        void Awake() { Instance = this; Time.timeScale = 1; AudioListener.pause = false; Application.targetFrameRate = 60; Application.runInBackground = true; }
        void Start()
        {
            UI.Initialize(this);
            UI.ShowMenu();
            if (Presentation) Presentation.ShowMenu(true);
            GamePreferences.Fullscreen = GamePreferences.Fullscreen;
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--qa") >= 0 ||
                System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--final-qa") >= 0)
                gameObject.AddComponent<FinalVerification>();
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--record-demo") >= 0)
                gameObject.AddComponent<DemoRecorder>();
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--performance-check") >= 0)
                gameObject.AddComponent<PerformanceSmoke>();
        }
        void Update()
        {
            var k = ActiveKeyboard;
            if (k != null && k.escapeKey.wasPressedThisFrame)
            {
                if (Running) Pause(); else if (State == MatchState.Paused) Resume();
            }
            if (!Running) return;
            Remaining = Mathf.Max(0, Remaining - Time.deltaTime);
            if (Remaining <= 0) { Finish(false); return; }
            due.Clear();
            foreach (var pair in respawns) if (Time.time >= pair.Value) due.Add(pair.Key);
            foreach (var fighter in due)
            {
                Vector3 position;
                if (TrySafeSpawn(fighter, out position)) { fighter.Respawn(position); respawns.Remove(fighter); }
            }
            foreach (var fighter in Fighters)
                if (fighter.Alive && fighter.transform.position.y < -1.8f) KnockOut(fighter);
        }

        public void StartMatch()
        {
            Time.timeScale = 1; AudioListener.pause = false; respawns.Clear();
            foreach (var f in Fighters) { f.gameObject.SetActive(false); Destroy(f.gameObject); }
            Fighters.Clear(); Score.Reset(); Points = Combo = HighestCombo = 0; lastScored = -100f;
            Remaining = Level.duration; State = MatchState.Playing;
            if (Presentation) { Presentation.ShowMenu(false); Presentation.ApplyLevel(LevelIndex, Level); }
            Feedback.SetMusicContext(false); Feedback.RoundStart();
            Player = Spawn(playerPrefab, new Vector3(0, 0.65f, -5.3f));
            var look = Player.GetComponent<FighterPresentation>(); if (look) look.ApplySkin(GamePreferences.Skin);
            for (int i = 0; i < Level.opponents; i++)
            {
                float angle = (-35f + i * 35f) * Mathf.Deg2Rad;
                var prefab = ingredientPrefabs != null && ingredientPrefabs.Length > 0 ? ingredientPrefabs[i % ingredientPrefabs.Length] : opponentPrefab;
                var opponent = Spawn(prefab, new Vector3(Mathf.Sin(angle) * 5.3f, 0.65f, Mathf.Cos(angle) * 5.3f));
                opponent.moveSpeed = Level.enemySpeed;
                opponent.Dash.chargeDuration = Level.enemyReactionTime / Mathf.Max(.1f, Level.enemyCharge);
            }
            UI.ShowHud();
        }
        public bool StartLevel(int index)
        {
            if (levels == null || index < 0 || index >= levels.Length || index >= GamePreferences.Unlocked) return false;
            LevelIndex = index; Level = levels[index]; StartMatch(); return true;
        }
        public void PlayCampaign() { StartLevel(0); }
        public void NextLevel() { if (State == MatchState.Victory && LevelIndex < 2) StartLevel(LevelIndex + 1); }
        Fighter Spawn(Fighter prefab, Vector3 position)
        {
            var fighter = Instantiate(prefab, position, Quaternion.identity);
            Fighters.Add(fighter); fighter.Respawn(position);
            return fighter;
        }

        public void KnockOut(Fighter fighter)
        {
            if (!Running || !fighter.Eliminate()) return;
            bool credited = Score.Record(fighter, Player);
            if (credited)
            {
                Combo = Time.time - lastScored <= 7f ? Combo + 1 : 1; lastScored = Time.time;
                HighestCombo = Mathf.Max(HighestCombo, Combo); Points += 100 * Combo;
            }
            Feedback.Knockout(fighter.transform.position, fighter.isPlayer, fighter.visual);
            UI.Popup(fighter.isPlayer ? "FAHHH!" : credited ? Combo > 1 ? $"{Combo}x SWEET STREAK!" : "+1 KO!" : "RING OUT", fighter.isPlayer ? Palette.Orange : Palette.Lime);
            fighter.gameObject.SetActive(false);
            if (Score.Hearts <= 0) { Finish(false); return; }
            if (Score.Knockouts >= Level.knockoutTarget) { Finish(true); return; }
            respawns[fighter] = Time.time + 1.8f;
        }

        public bool TrySafeSpawn(Fighter fighter, out Vector3 position)
        {
            for (int i = 0; i < 12; i++)
            {
                float angle = (i * 30f + (fighter.isPlayer ? 180f : 0f)) * Mathf.Deg2Rad;
                Vector3 candidate = new Vector3(Mathf.Sin(angle) * 5.3f, 0.65f, Mathf.Cos(angle) * 5.3f);
                bool safe = true;
                foreach (var other in Fighters)
                    if (other != fighter && other.Alive && (other.transform.position - candidate).sqrMagnitude < 6.25f) safe = false;
                if (Presentation && !Presentation.SpawnClear(candidate)) safe = false;
                if (safe && Physics.Raycast(candidate + Vector3.up, Vector3.down, out RaycastHit hit, 2f) && hit.collider.GetComponent<Fighter>() == null && !hit.collider.GetComponent<ArenaObstacle>())
                { position = candidate; return true; }
            }
            position = Vector3.zero; return false;
        }
        public void Pause()
        {
            if (!Running) return;
            foreach (var fighter in Fighters) fighter.Dash.Cancel();
            State = MatchState.Paused; Time.timeScale = 0; AudioListener.pause = true; UI.ShowPause();
        }
        public void Resume() { if (State != MatchState.Paused) return; State = MatchState.Playing; Time.timeScale = 1; AudioListener.pause = false; UI.ShowHud(); }
        public void ToMenu()
        {
            Time.timeScale = 1; AudioListener.pause = false; State = MatchState.Menu; respawns.Clear();
            foreach (var fighter in Fighters) { fighter.Freeze(); fighter.gameObject.SetActive(false); }
            if (Presentation) Presentation.ShowMenu(true);
            Feedback.SetMusicContext(true);
            UI.ShowMenu();
        }
        public void Finish(bool victory)
        {
            if (!Running) return;
            State = victory ? MatchState.Victory : MatchState.Defeat;
            Time.timeScale = 1; AudioListener.pause = false;
            respawns.Clear();
            foreach (var fighter in Fighters) fighter.Freeze();
            if (victory)
            {
                Points += Score.Hearts * 150 + Mathf.FloorToInt(Remaining) * 5;
                GamePreferences.Complete(LevelIndex, Points, HighestCombo);
            }
            else GamePreferences.RecordPerformance(LevelIndex, Points, HighestCombo);
            Feedback.Result(victory, LevelIndex == 2);
            if (Presentation)
            {
                Presentation.ShowResult();
                foreach (var fighter in Fighters) fighter.gameObject.SetActive(false);
            }
            UI.ShowResult(victory);
        }
        public void SetRemainingForVerification(float value) { Remaining = value; }
        void OnDestroy() { if (Instance == this) { Instance = null; Time.timeScale = 1; AudioListener.pause = false; } }
        public void Exit()
        {
            PlayerPrefs.Save();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
