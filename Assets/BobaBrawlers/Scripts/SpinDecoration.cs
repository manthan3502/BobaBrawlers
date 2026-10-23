using UnityEngine;
namespace BobaBrawlers
{
    public sealed class SpinDecoration : MonoBehaviour
    {
        public Vector3 spin = new Vector3(0, 12, 0);
        public float bob = .08f;
        Vector3 start;
        void Awake() { start = transform.localPosition; }
        void Update()
        {
            if (GamePreferences.ReducedMotion || !GameManager.Instance || GameManager.Instance.State == MatchState.Paused) return;
            transform.Rotate(spin * Time.deltaTime);
            transform.localPosition = start + Vector3.up * Mathf.Sin(Time.time * 1.4f + start.x) * bob;
        }
    }
}
