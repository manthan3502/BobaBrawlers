using UnityEngine;
namespace BobaBrawlers
{
    public sealed class FallingEcho : MonoBehaviour
    {
        float start;
        Vector3 scale;
        void Awake() { start = Time.time; scale = transform.localScale; }
        void Update()
        {
            float t = Time.time - start;
            transform.position += Vector3.down * (2 + t * 5) * Time.deltaTime;
            transform.Rotate(80 * Time.deltaTime, 140 * Time.deltaTime, 50 * Time.deltaTime);
            transform.localScale = scale * Mathf.Clamp01(1 - t / .75f);
            if (t > .8f) Destroy(gameObject);
        }
    }
}
