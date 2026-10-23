using UnityEngine;

namespace BobaBrawlers
{
    public sealed class ArenaPresentation : MonoBehaviour
    {
        public Transform vortex;
        public Renderer[] themeRenderers;
        public GameObject[] obstacles;
        public Fighter[] previewPrefabs;
        public Transform[] previewSpots;
        public Light[] accentLights;
        public Camera arenaCamera;
        public Vector3 gameplayPosition = new Vector3(0, 12f, -15f);
        public Vector3 gameplayTarget = new Vector3(0, -.4f, -1.4f);
        public Vector3 menuPosition = new Vector3(0, 8.3f, -14.5f);
        public Vector3 menuTarget = new Vector3(0, .1f, 0);
        public bool MenuVisible { get; private set; } = true;
        Fighter[] previews;
        Vector3 cameraBase;
        float shakeUntil, shakeStrength;
        MaterialPropertyBlock block;
        void Awake() { block = new MaterialPropertyBlock(); cameraBase = arenaCamera.transform.position; }
        public void ShowMenu(bool visible)
        {
            MenuVisible = visible;
            if (previews == null && previewPrefabs != null)
            {
                previews = new Fighter[previewSpots.Length];
                for (int i = 0; i < previews.Length; i++)
                {
                    var p = Instantiate(previewPrefabs[i % previewPrefabs.Length], previewSpots[i].position, Quaternion.identity);
                    p.name = "Menu showcase " + i;
                    p.GetComponent<Rigidbody>().isKinematic = true;
                    p.GetComponent<Collider>().enabled = false;
                    var input = p.GetComponent<PlayerController>(); if (input) input.enabled = false;
                    var ai = p.GetComponent<EnemyAIController>(); if (ai) ai.enabled = false;
                    p.GetComponent<FighterPresentation>().preview = true;
                    p.transform.localScale = Vector3.one * 1.8f;
                    previews[i] = p;
                }
            }
            if (previews != null) foreach (var p in previews) p.gameObject.SetActive(visible);
            if (obstacles != null && visible) foreach (var o in obstacles) o.SetActive(false);
        }
        public void PreviewSkin(int skin)
        {
            if (previews == null) return;
            foreach (var p in previews) if (p.isPlayer) p.GetComponent<FighterPresentation>().ApplySkin(skin);
        }
        public void ShowResult()
        {
            ShowMenu(true);
            PreviewSkin(GamePreferences.Skin);
            for (int i = 0; i < previews.Length; i++) previews[i].gameObject.SetActive(i < 2);
        }
        public void ApplyLevel(int index, LevelDefinition definition)
        {
            foreach (var r in themeRenderers)
            { block.Clear(); block.SetColor("_BaseColor", definition.theme); block.SetColor("_EmissionColor", definition.theme * 1.8f); r.SetPropertyBlock(block); }
            for (int i = 0; i < accentLights.Length; i++) accentLights[i].color = i % 2 == 0 ? definition.theme : Palette.Orange;
            for (int i = 0; i < obstacles.Length; i++) obstacles[i].SetActive(i < definition.obstacleCount);
        }
        public bool SpawnClear(Vector3 position)
        {
            foreach (var o in obstacles) if (o.activeSelf && Vector3.Distance(position, o.transform.position) < 1.8f) return false;
            return true;
        }
        public Vector3 AvoidObstacle(Vector3 position, Vector3 direction)
        {
            foreach (var o in obstacles)
            {
                if (!o.activeSelf) continue;
                Vector3 delta = position - o.transform.position; delta.y = 0;
                if (delta.sqrMagnitude < 4f && Vector3.Dot(direction, -delta) > 0)
                    direction = (direction + delta.normalized * 1.7f + Vector3.Cross(Vector3.up, delta.normalized) * .6f).normalized;
            }
            return direction;
        }
        public void Shake(float strength = .1f)
        {
            if (!GamePreferences.Shake || GamePreferences.ReducedMotion) return;
            shakeUntil = Time.unscaledTime + .16f; shakeStrength = Mathf.Min(.12f, strength);
        }
        void LateUpdate()
        {
            if (vortex && !GamePreferences.ReducedMotion && GameManager.Instance && GameManager.Instance.State != MatchState.Paused)
                vortex.Rotate(0, Time.deltaTime * 45, 0, Space.World);
            Vector3 destination = MenuVisible ? menuPosition : gameplayPosition;
            Vector3 target = MenuVisible ? menuTarget : gameplayTarget;
            cameraBase = Vector3.Lerp(cameraBase, destination, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 4));
            Vector3 offset = Time.unscaledTime < shakeUntil && GamePreferences.Shake && !GamePreferences.ReducedMotion ? new Vector3(Mathf.Sin(Time.unscaledTime * 120), Mathf.Cos(Time.unscaledTime * 97), 0) * shakeStrength : Vector3.zero;
            arenaCamera.transform.position = cameraBase + offset;
            arenaCamera.transform.rotation = Quaternion.Slerp(arenaCamera.transform.rotation, Quaternion.LookRotation(target - cameraBase), 1 - Mathf.Exp(-Time.unscaledDeltaTime * 5));
        }
    }
}
