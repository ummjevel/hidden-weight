using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay.Boss
{
    // GDD 13번 마지막 줄: "CEO 웨이브 방어 성공 -> 기존 CEO 퇴사 -> 주인공 CEO 취임 -> 엔딩".
    // 실제 엔딩 연출(UI, 씬 전환)은 M9 폴리싱에서 붙인다. 지금은 BossWaveManager의 성공 이벤트를
    // 받아 엔딩이 트리거됐다는 사실 자체를 확정하는 역할만 한다.
    [RequireComponent(typeof(BossWaveManager))]
    public class EndingTrigger : MonoBehaviour
    {
        public bool HasTriggeredEnding { get; private set; }

        private void OnEnable()
        {
            GetComponent<BossWaveManager>().OnWaveSuccess += HandleWaveSuccess;
        }

        private void OnDisable()
        {
            GetComponent<BossWaveManager>().OnWaveSuccess -= HandleWaveSuccess;
        }

        private void HandleWaveSuccess()
        {
            HasTriggeredEnding = true;
            Debug.Log("[EndingTrigger] CEO 웨이브 방어 성공 - 엔딩 연출 시작 (연출/씬 전환은 M9에서 구현)");
        }
    }
}
