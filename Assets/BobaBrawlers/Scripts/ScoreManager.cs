using UnityEngine;

namespace BobaBrawlers
{
    public sealed class ScoreManager
    {
        public int Knockouts { get; private set; }
        public int Hearts { get; private set; } = 3;
        public const float AttributionWindow = 4f;
        public void Reset() { Knockouts = 0; Hearts = 3; }
        public bool Record(Fighter victim, Fighter player)
        {
            if (victim.isPlayer) { Hearts = Mathf.Max(0, Hearts - 1); return false; }
            bool credited = victim.LastAttacker == player && Time.time - victim.LastAttackAt <= AttributionWindow;
            if (credited) Knockouts++;
            return credited;
        }
    }
}
