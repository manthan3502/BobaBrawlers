using UnityEngine;
namespace BobaBrawlers
{
    public sealed class ArenaObstacle : MonoBehaviour
    {
        public Transform decorativeTop;
        Vector3 start;
        void Awake() { start = transform.position; }
        void Update()
        {
            if (decorativeTop && GameManager.Instance && GameManager.Instance.Running && !GamePreferences.ReducedMotion)
                decorativeTop.localRotation = Quaternion.Euler(0, Mathf.Sin(Time.time * .6f + start.x) * 8, 0);
        }
    }
}
