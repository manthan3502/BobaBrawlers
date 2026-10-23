using UnityEngine;

namespace BobaBrawlers
{
    public sealed class KnockoutZone : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            var fighter = other.GetComponent<Fighter>();
            if (fighter && GameManager.Instance) GameManager.Instance.KnockOut(fighter);
        }
    }
}
