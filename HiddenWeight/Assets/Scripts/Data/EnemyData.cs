using UnityEngine;

namespace HiddenWeight.Data
{
    // 적 1종의 수치를 담는 ScriptableObject.
    [CreateAssetMenu(fileName = "EnemyData", menuName = "HiddenWeight/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        public int maxHealth = 2;
        public float moveSpeed = 1.5f;
        public int contactDamage = 1;
        public Color tint = Color.white;
        public float knockbackForce = 6f;
        public float wobbleAmplitude = 0f; // 균열 지역만 0.2
        public float wobbleFrequency = 3f;
    }
}
