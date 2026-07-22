using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay.Items
{
    // GDD 4번 "회복 아이템": 적이 낮은 확률로 떨어뜨리는 아메리카노. 플레이어가 가까이 가면
    // HP(멘탈)를 소량 회복하고 사라진다. 프로토타입 범위라 아메리카노 하나만 구현했다
    // (믹스커피의 "회복 + 5초간 공격속도 증가" 효과는 GDD에서도 보류 대상).
    [RequireComponent(typeof(Collider2D))]
    public class CoffeeDrop : MonoBehaviour
    {
        [SerializeField] private int healAmount = 15;

        // M9: 배정되면 이 값으로 위 기본값을 덮어써서 코드 재컴파일 없이 밸런스를 조정할 수 있다.
        [SerializeField] private BalanceData balanceData;

        private void Awake()
        {
            if (balanceData != null)
            {
                healAmount = balanceData.coffeeHealAmount;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null) return;

            player.Reputation.Heal(healAmount);
            Destroy(gameObject);
        }
    }
}
