using UnityEngine;

namespace BobaBrawlers
{
    [RequireComponent(typeof(Fighter))]
    public sealed class FighterPresentation : MonoBehaviour
    {
        [System.Serializable]
        public class Rig
        {
            public Transform root, leftArm, rightArm, leftFoot, rightFoot, eyes, mouth;
            public Renderer body;
        }
        public Rig[] characterRigs;
        public int CharacterIndex { get; private set; }
        public Renderer[] tintRenderers;
        public Transform leftArm, rightArm, leftFoot, rightFoot, eyes, mouth, aimArrow, chargeHalo;
        public TrailRenderer trail;
        public bool preview;
        Fighter fighter;
        Vector3 basePosition, baseScale;
        float hitUntil, spawnUntil;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        Color tint;
        void Awake()
        {
            fighter = GetComponent<Fighter>();
            basePosition = fighter.visual.localPosition; baseScale = fighter.visual.localScale;
            renderers = fighter.visual.GetComponentsInChildren<Renderer>(true); block = new MaterialPropertyBlock(); tint = Palette.Orange;
            if (fighter.isPlayer) ApplySkin(GamePreferences.Skin);
        }
        public void ApplySkin(int index)
        {
            CharacterIndex = Mathf.Clamp(index, 0, 2);
            if (characterRigs != null && characterRigs.Length == 3)
            {
                for (int i = 0; i < 3; i++) characterRigs[i].root.gameObject.SetActive(i == CharacterIndex);
                var rig = characterRigs[CharacterIndex];
                leftArm = rig.leftArm; rightArm = rig.rightArm; leftFoot = rig.leftFoot; rightFoot = rig.rightFoot; eyes = rig.eyes; mouth = rig.mouth;
                tintRenderers = new[] { rig.body };
            }
            if (block == null) block = new MaterialPropertyBlock();
            tint = GamePreferences.SkinColors[Mathf.Clamp(index, 0, 2)];
            foreach (var r in tintRenderers)
            { block.SetColor("_BaseColor", tint); r.SetPropertyBlock(block); }
            if (trail) { trail.startColor = tint; trail.endColor = new Color(tint.r, tint.g, tint.b, 0); }
        }
        public void Hit() { hitUntil = Time.time + .35f; }
        public void Spawned() { spawnUntil = Time.time + .4f; if (trail) trail.Clear(); }
        void Update()
        {
            var game = GameManager.Instance;
            if (!fighter || !fighter.visual || !game) return;
            if (game.State == MatchState.Paused) { if (trail) trail.emitting = false; return; }
            bool winner = game.State == MatchState.Victory && fighter.isPlayer;
            bool loser = game.State == MatchState.Defeat && fighter.isPlayer;
            bool moving = !preview && fighter.Body.linearVelocity.sqrMagnitude > .3f && game.Running;
            bool falling = !preview && fighter.Body.position.y < -.1f;
            float t = preview || winner || loser ? Time.unscaledTime : Time.time;
            float bounce = GamePreferences.ReducedMotion ? 0 : Mathf.Abs(Mathf.Sin(t * (moving ? 11 : winner ? 8 : 3.5f))) * (moving || winner ? .1f : .035f);
            int style = fighter.isPlayer ? CharacterIndex : 0;
            if (style == 0 && !GamePreferences.ReducedMotion) bounce *= 1.6f;
            if (style == 2) bounce *= .25f;
            fighter.visual.localPosition = basePosition + Vector3.up * bounce + (style == 2 && !GamePreferences.ReducedMotion ? Vector3.right * Mathf.Sin(t * 3) * .045f : Vector3.zero);
            Vector3 scale = baseScale;
            if (style == 1 && !GamePreferences.ReducedMotion) scale = Vector3.Scale(scale, new Vector3(1 + Mathf.Sin(t * 5) * .045f, 1 - Mathf.Sin(t * 5) * .05f, 1));
            if (fighter.Dash.IsCharging && game.Running) scale = Vector3.Scale(scale, new Vector3(1.12f, .84f, 1.12f));
            if (fighter.Dash.IsDashing) scale = Vector3.Scale(scale, style == 1 ? new Vector3(.76f, .8f, 1.48f) : new Vector3(.91f, .91f, 1.22f));
            if (Time.time < hitUntil) scale = Vector3.Scale(scale, new Vector3(1.18f, .8f, 1.1f));
            if (Time.time < spawnUntil && !preview) scale *= Mathf.Lerp(.25f, 1f, 1 - (spawnUntil - Time.time) / .4f);
            fighter.visual.localScale = Vector3.Lerp(fighter.visual.localScale, scale, Time.deltaTime * 20);
            float yaw = preview ? Mathf.Sin(t * .5f) * 12 : Mathf.Clamp(fighter.Facing.x * 38, -38, 38);
            float roll = moving ? Mathf.Sin(t * 11) * 4 : loser ? 16 : 0;
            if (style == 1 && !GamePreferences.ReducedMotion) roll += Mathf.Sin(t * (winner ? 9 : 5)) * (winner ? 18 : 5);
            if (style == 2 && winner) roll = -12;
            if (falling) roll += t * 280;
            float lean = style == 2 && fighter.Dash.IsDashing ? -28 : loser ? 15 : 0;
            fighter.visual.localRotation = Quaternion.Slerp(fighter.visual.localRotation, Quaternion.Euler(lean, winner ? Mathf.Sin(t * 2) * (style == 2 ? 8 : 25) : yaw, roll), Time.unscaledDeltaTime * 13);
            float swing = moving ? Mathf.Sin(t * 11) * 28 : Mathf.Sin(t * 3.5f) * 5;
            if (leftArm) leftArm.localRotation = Quaternion.Euler(0, 0, winner ? -125 + swing : fighter.Dash.IsCharging ? -45 : -12 + swing);
            if (rightArm) rightArm.localRotation = Quaternion.Euler(0, 0, winner ? 125 - swing : fighter.Dash.IsCharging ? 45 : 12 - swing);
            if (style == 0 && fighter.Dash.IsDashing && rightArm) rightArm.localRotation = Quaternion.Euler(-90, 0, -55);
            if (style == 2 && winner) { leftArm.localRotation = Quaternion.Euler(0, 0, -165); rightArm.localRotation = Quaternion.Euler(0, 0, -40); }
            if (leftFoot) leftFoot.localRotation = Quaternion.Euler(swing, 0, 0);
            if (rightFoot) rightFoot.localRotation = Quaternion.Euler(-swing, 0, 0);
            if (eyes) eyes.localScale = new Vector3(1, Time.time < hitUntil || falling ? .28f : Mathf.Sin(t * 1.7f) > .996f ? .1f : 1, 1);
            if (mouth) mouth.localScale = falling || loser ? new Vector3(1, 1.8f, 1) : Vector3.one;
            if (aimArrow)
            {
                aimArrow.gameObject.SetActive(!preview && fighter.isPlayer && game.Running && !falling);
                aimArrow.rotation = Quaternion.LookRotation(fighter.Facing);
                aimArrow.position = new Vector3(transform.position.x, .06f, transform.position.z);
            }
            if (chargeHalo)
            {
                chargeHalo.gameObject.SetActive(!preview && game.Running && (fighter.Dash.IsCharging || fighter.IsProtected));
                chargeHalo.position = new Vector3(transform.position.x, .045f, transform.position.z);
                chargeHalo.localScale = Vector3.one * (1 + .06f * Mathf.Sin(t * 10));
                var haloRenderer = chargeHalo.GetComponent<Renderer>();
                block.SetColor("_BaseColor", fighter.Dash.Charge >= .98f ? Palette.Orange : tint);
                block.SetColor("_EmissionColor", (fighter.Dash.Charge >= .98f ? Palette.Orange : tint) * (2 + fighter.Dash.Charge * 2));
                haloRenderer.SetPropertyBlock(block); block.Clear();
            }
            if (trail) trail.emitting = !preview && game.Running && fighter.Dash.IsDashing;
            bool visible = preview || !fighter.IsProtected || Mathf.Sin(t * 24) > -.55f;
            foreach (var r in renderers) r.enabled = visible;
        }
    }
}
