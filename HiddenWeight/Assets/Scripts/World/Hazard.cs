using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Player;

namespace HiddenWeight.World
{
    // 위험 영역. 가시·심연·압착 등 "닿으면 안 되는 곳"의 공통 처리다
    // (RESIDUE_ROOM_IMPLEMENTATION.md 2.2절 필수 요소).
    //
    // 명세가 요구하는 두 가지 복귀 방식을 한 컴포넌트로 덮는다.
    //   damage=0, recoveryPoint=null  → 옛 FallZone. 깊은 안전 바닥에 떨어졌을 때 체크포인트 복귀.
    //   damage=1, recoveryPoint=지정  → R07·R09식. 체력 1 잃고 "방 입구가 아니라 직전 안전 발판"으로.
    //
    // 복귀 지점을 지정하는 쪽이 기본이다. 방 안에서 되돌리는 편이 재도전 동선이 짧고,
    // 명세의 낙하 복구 시간 기준(R04 15초, S1 8초)을 지키기 쉽다.
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        [SerializeField] int damage = 1;
        [SerializeField] Transform recoveryPoint; // 비면 마지막 체크포인트로 보낸다

        // 같은 프레임에 트리거가 여러 번 겹쳐 두 번 처리되는 것을 막는다.
        bool _handling;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_handling) return;
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;

            _handling = true;
            Recover();
            _handling = false;
        }

        void Recover()
        {
            // 피해가 먼저다. 무적 중이면 TakeDamage가 알아서 무시하고, 복귀만 일어난다.
            if (damage > 0)
            {
                var health = PlayerController.Instance != null
                    ? PlayerController.Instance.GetComponent<PlayerHealth>()
                    : null;
                if (health != null) health.TakeDamage(damage, transform.position);
            }

            if (recoveryPoint != null)
            {
                // 체력이 0이 되어 이미 체크포인트로 돌아갔다면 방 안 발판으로 다시 끌어오지 않는다.
                var health = PlayerController.Instance.GetComponent<PlayerHealth>();
                if (health == null || health.Current > 0)
                    PlayerController.Instance.TeleportTo(recoveryPoint.position);
                return;
            }

            GameManager.Instance.RespawnPlayer();
        }
    }
}
