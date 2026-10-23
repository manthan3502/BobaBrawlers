using UnityEngine;

namespace BobaBrawlers
{
    [CreateAssetMenu(menuName = "Boba Brawlers/Level")]
    public sealed class LevelDefinition : ScriptableObject
    {
        public string title = "First Sip";
        public int opponents = 1;
        public int knockoutTarget = 2;
        public float duration = 90f;
        public float enemySpeed = 3.1f;
        public float enemyReactionTime = 1.1f;
        public float enemyAttackInterval = 2.8f;
        public float enemyCharge = .65f;
        public Color theme = new Color(.2f, .41f, .95f);
        public string difficulty = "EASY";
        public int obstacleCount;
    }
}
