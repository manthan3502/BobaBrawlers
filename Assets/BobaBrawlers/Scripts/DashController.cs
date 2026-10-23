using System;
using System.Collections.Generic;
using UnityEngine;

namespace BobaBrawlers
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class DashController : MonoBehaviour
    {
        public float chargeDuration = 1.15f;
        public float cooldown = 1.0f;
        public float dashDuration = 0.23f;
        public bool IsCharging { get; private set; }
        public bool IsDashing => Time.time < dashUntil;
        public float Charge { get; private set; }
        public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);
        public event Action<string> Feedback;
        Fighter fighter;
        float dashUntil = -1f, readyAt, power;
        bool fullSignalled, cooldownSignalled = true;
        bool endingDash;
        readonly HashSet<Fighter> hits = new HashSet<Fighter>();

        void Awake() { fighter = GetComponent<Fighter>(); }
        void FixedUpdate()
        {
            if (!endingDash || IsDashing) return;
            endingDash = false; ReturnToMovementSpeed();
        }
        void ReturnToMovementSpeed()
        {
            if (!fighter || !fighter.Body || fighter.Body.isKinematic) return;
            Vector3 velocity = fighter.Body.linearVelocity;
            Vector3 flat = Vector3.ClampMagnitude(new Vector3(velocity.x, 0, velocity.z), fighter.moveSpeed);
            fighter.Body.linearVelocity = flat + Vector3.up * velocity.y;
        }
        void Update()
        {
            if (!GameManager.Instance || !GameManager.Instance.Running || !fighter.Alive) return;
            if (IsCharging)
            {
                Charge = Mathf.Min(1f, Charge + Time.deltaTime / chargeDuration);
                if (Charge >= 1f && !fullSignalled) { fullSignalled = true; Feedback?.Invoke("Full"); }
            }
            if (!cooldownSignalled && CooldownRemaining <= 0)
            {
                cooldownSignalled = true;
                Feedback?.Invoke(hits.Count == 0 ? "Miss" : "Ready");
            }
        }

        public bool BeginCharge()
        {
            if (!fighter.Alive || !GameManager.Instance.Running || fighter.Body.position.y < 0 ||
                CooldownRemaining > 0 || IsDashing || IsCharging) return false;
            IsCharging = true; Charge = 0f; fullSignalled = false;
            Feedback?.Invoke("Charging");
            return true;
        }

        public bool Release()
        {
            if (!IsCharging || !fighter.Alive || !GameManager.Instance.Running) return false;
            power = Charge;
            IsCharging = false;
            dashUntil = Time.time + dashDuration;
            endingDash = true;
            readyAt = dashUntil + cooldown;
            cooldownSignalled = false;
            hits.Clear();
            fighter.Body.linearVelocity = fighter.Facing * Mathf.Lerp(12f, 21f, power) + Vector3.up * fighter.Body.linearVelocity.y;
            Feedback?.Invoke("Release");
            GameManager.Instance.Feedback.Dash(transform.position, fighter.Facing);
            return true;
        }

        public bool TryHit(Fighter target)
        {
            if (!IsDashing || !target.Alive || target.IsProtected || !GameManager.Instance.Running || hits.Contains(target)) return false;
            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0;
            if (Vector3.Dot(fighter.Facing, direction.normalized) < 0.1f) return false;
            hits.Add(target);
            target.ReceiveHit(fighter, (fighter.Facing + direction.normalized).normalized, Mathf.Lerp(8.5f, 16.5f, power));
            Feedback?.Invoke("Hit");
            return true;
        }

        public void Cancel()
        {
            if (IsDashing) ReturnToMovementSpeed();
            endingDash = false; IsCharging = false; Charge = 0; dashUntil = -1f;
        }
        public void ResetState() { Cancel(); readyAt = 0; hits.Clear(); cooldownSignalled = true; }
    }
}
