using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Player;

namespace HiddenWeight.World
{
    // 소형 회복물(CONTENT_SYSTEM.md 5절). 체력이 이미 가득이면 먹히지 않고 남는다 —
    // 필요할 때 돌아와 쓰라는 뜻이고, 명세가 회복물을 "전투 직전이 아니라 승리 보상이나
    // 체크포인트 쪽에" 두라고 한 배치 의도와도 맞는다.
    [RequireComponent(typeof(Collider2D))]
    public class HealingPickup : MonoBehaviour
    {
        [SerializeField] int amount = 1;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;

            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null || health.Current >= health.Max) return;

            health.Heal(amount);
            AudioManager.Instance?.PlaySfx(SfxCue.Heal, 0.5f);
            gameObject.SetActive(false);
        }
    }
}
