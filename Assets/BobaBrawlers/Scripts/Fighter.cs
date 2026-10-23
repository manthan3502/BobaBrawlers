using System;
using UnityEngine;

namespace BobaBrawlers
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider), typeof(DashController))]
    public sealed class Fighter : MonoBehaviour
    {
        public bool isPlayer;
        public Transform visual;
        public float moveSpeed = 5.3f;
        public Vector3 Facing { get; private set; } = Vector3.forward;
        public Vector3 Intent { get; set; }
        public bool Alive { get; private set; } = true;
        public float ProtectedUntil { get; private set; }
        public Fighter LastAttacker { get; private set; }
        public float LastAttackAt { get; private set; } = -100f;
        public Rigidbody Body { get; private set; }
        public DashController Dash { get; private set; }
        public bool IsProtected => Time.time < ProtectedUntil;
        public event Action<Fighter> KnockedOut;
        float staggerUntil;
        Vector3 visualScale;
        FighterPresentation presentation;
        Renderer[] renderers;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Dash = GetComponent<DashController>();
            visualScale = visual ? visual.localScale : Vector3.one;
            presentation = GetComponent<FighterPresentation>();
            renderers = visual ? visual.GetComponentsInChildren<Renderer>() : System.Array.Empty<Renderer>();
        }

        void FixedUpdate()
        {
            if (!Alive || Body.isKinematic || !GameManager.Instance || !GameManager.Instance.Running) return;
            // Falling fighters keep their momentum; steering cannot rescue a committed fall.
            if (Body.position.y < -0.15f) return;
            if (Intent.sqrMagnitude > 0.02f && !Dash.IsDashing)
                Facing = Intent.normalized;
            if (!Dash.IsDashing && Time.time >= staggerUntil)
            {
                Vector3 target = Vector3.ClampMagnitude(Intent, 1f) * moveSpeed * (Dash.IsCharging ? 0.55f : 1f);
                Vector3 v = Body.linearVelocity;
                Vector3 flat = Vector3.MoveTowards(new Vector3(v.x, 0, v.z), target, 27f * Time.fixedDeltaTime);
                Body.linearVelocity = flat + Vector3.up * v.y;
            }
            Vector3 velocity = Body.linearVelocity;
            Vector3 horizontal = Vector3.ClampMagnitude(new Vector3(velocity.x, 0, velocity.z), 24f);
            Body.linearVelocity = horizontal + Vector3.up * velocity.y;
        }

        void Update()
        {
            if (!visual) return;
            if (presentation) return;
            visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.LookRotation(-Facing), Time.deltaTime * 16f);
            float breath = Mathf.Sin(Time.time * 4f) * 0.025f;
            Vector3 target = visualScale + new Vector3(-breath * 0.5f, breath, -breath * 0.5f);
            if (Dash.IsCharging) target = Vector3.Scale(visualScale, new Vector3(1.12f, 0.84f, 1.12f));
            if (Dash.IsDashing) target = Vector3.Scale(visualScale, new Vector3(0.86f, 0.86f, 1.24f));
            visual.localScale = Vector3.Lerp(visual.localScale, target, Time.deltaTime * 16f);
            // Protection is visible without hiding the collider or changing collision rules.
            foreach (Renderer r in renderers)
                r.enabled = !IsProtected || Mathf.Sin(Time.time * 24f) > -0.25f;
        }

        void OnCollisionEnter(Collision collision)
        {
            var other = collision.collider.GetComponent<Fighter>();
            if (other) Dash.TryHit(other);
        }

        public void ReceiveHit(Fighter attacker, Vector3 direction, float strength)
        {
            if (!Alive || IsProtected || !GameManager.Instance.Running) return;
            LastAttacker = attacker;
            LastAttackAt = Time.time;
            staggerUntil = Time.time + 0.46f;
            Dash.Cancel();
            Vector3 flat = direction; flat.y = 0;
            Body.linearVelocity = flat.normalized * Mathf.Clamp(strength, 4f, 17f) + Vector3.up * 1.1f;
            GameManager.Instance.Feedback.Impact(transform.position);
            if (presentation) presentation.Hit();
        }

        public bool Eliminate()
        {
            if (!Alive || !GameManager.Instance.Running) return false;
            Alive = false;
            Intent = Vector3.zero;
            Dash.Cancel();
            KnockedOut?.Invoke(this);
            return true;
        }

        public void Respawn(Vector3 position, float protection = 1.5f)
        {
            gameObject.SetActive(true);
            Body.isKinematic = false;
            Body.position = position;
            transform.position = position;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Alive = true;
            LastAttacker = null;
            LastAttackAt = -100f;
            ProtectedUntil = Time.time + protection;
            staggerUntil = Time.time;
            Intent = Vector3.zero;
            Facing = isPlayer ? Vector3.forward : Vector3.back;
            Dash.ResetState();
            if (presentation) presentation.Spawned();
        }

        public void Freeze()
        {
            Intent = Vector3.zero;
            Dash.Cancel();
            if (!Body.isKinematic) Body.linearVelocity = Vector3.zero;
            Body.isKinematic = true;
        }
    }
}
