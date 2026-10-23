using UnityEngine;

namespace BobaBrawlers
{
    [RequireComponent(typeof(Fighter))]
    public sealed class EnemyAIController : MonoBehaviour
    {
        public enum State { Approach, Reposition, Charge, Attack, Recover, KnockedOut }
        public State CurrentState { get; private set; }
        Fighter fighter;
        float nextAttack, releaseAt;
        void Awake() { fighter = GetComponent<Fighter>(); }
        void OnEnable() { nextAttack = Time.time + 2.2f; CurrentState = State.Approach; }
        void Update()
        {
            var game = GameManager.Instance;
            if (!game || !game.Running || !fighter.Alive) { CurrentState = State.KnockedOut; return; }
            var player = game.Player;
            if (!player || !player.Alive) { fighter.Intent = Vector3.zero; return; }
            Vector3 p = transform.position; p.y = 0;
            Vector3 delta = player.transform.position - transform.position; delta.y = 0;
            if (fighter.Dash.IsDashing) { CurrentState = State.Attack; return; }
            if (fighter.Dash.IsCharging)
            {
                CurrentState = State.Charge;
                fighter.Intent = delta.normalized * 0.1f;
                if (Time.time >= releaseAt) fighter.Dash.Release();
                return;
            }
            if (fighter.Dash.CooldownRemaining > 0) CurrentState = State.Recover;
            else CurrentState = State.Approach;
            Vector3 direction = delta.normalized;
            // Follow an annular route when a straight pursuit would cross the hole.
            Vector3 end = player.transform.position; end.y = 0;
            Vector3 segment = end - p;
            float t = segment.sqrMagnitude > 0 ? Mathf.Clamp01(-Vector3.Dot(p, segment) / segment.sqrMagnitude) : 0;
            if ((p + segment * t).magnitude < 2.8f || p.magnitude < 2.9f)
            {
                Vector3 radial = p.sqrMagnitude > 0.01f ? p.normalized : Vector3.right;
                Vector3 tangent = Vector3.Cross(Vector3.up, radial);
                if (Vector3.Dot(tangent, delta) < 0) tangent = -tangent;
                direction = (tangent + radial * Mathf.Clamp(4f - p.magnitude, -0.4f, 1.5f)).normalized;
                CurrentState = State.Reposition;
            }
            if (p.magnitude > 6.6f) { direction = -p.normalized; CurrentState = State.Reposition; }
            if (delta.magnitude < 1.7f && p.magnitude > 3.2f && p.magnitude < 6.4f)
            {
                // Keep room to telegraph the next dash instead of pushing endlessly
                // at point-blank range until the attack-distance check can never pass.
                direction = (-delta.normalized + Vector3.Cross(Vector3.up, delta.normalized) * .35f).normalized;
            }
            if (game.Presentation) direction = game.Presentation.AvoidObstacle(p, direction);
            // Separate allies so multiple opponents do not stack into one physics pile.
            foreach (var ally in game.Fighters)
            {
                if (ally == fighter || ally.isPlayer || !ally.Alive) continue;
                Vector3 separation = p - new Vector3(ally.transform.position.x, 0, ally.transform.position.z);
                if (separation.sqrMagnitude < 2.3f) direction += separation.normalized * .55f;
            }
            fighter.Intent = Vector3.ClampMagnitude(direction, 1);
            Vector3 projected = p + delta.normalized * 3.5f;
            if (Time.time >= nextAttack && delta.magnitude < 4.5f && delta.magnitude > 1.2f &&
                projected.magnitude > 2.8f && projected.magnitude < 7f && CurrentState != State.Reposition)
            {
                if (fighter.Dash.BeginCharge())
                {
                    releaseAt = Time.time + Mathf.Max(.4f, game.Level.enemyReactionTime);
                    nextAttack = releaseAt + game.Level.enemyAttackInterval;
                }
            }
        }
    }
}
