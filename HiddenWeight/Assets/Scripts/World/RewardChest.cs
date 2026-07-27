using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    // 조건을 만족하면 한 번만 주는 고정 보상(RESIDUE_ROOM_IMPLEMENTATION.md 2.2절).
    // 정예 처치 보상, 보스 승리 보상, 비밀방 끝 보상이 전부 이걸 쓴다.
    //
    // 되감기 복제 방지: 지급 여부를 오브젝트가 아니라 ProgressState의 id로 기록한다
    // (EMOTION_SYSTEM.md — 되감기는 적의 드롭이나 이미 받은 보상을 되살리지 않는다).
    // 되감기로 상자를 원위치로 돌려놓아도 같은 id는 두 번 지급되지 않는다.
    public class RewardChest : MonoBehaviour
    {
        [SerializeField] string rewardId;
        [SerializeField] int currency;
        [SerializeField] bool healthShard;      // 최대 체력 +1
        [SerializeField] bool openOnStart;      // 조건 없이 방에 놓인 고정 보상
        [SerializeField] GameObject visual;     // 아직 안 받았을 때만 보이는 몸체

        bool _given;

        void Start()
        {
            _given = GameManager.Instance != null && GameManager.Instance.Progress.IsRewardTaken(rewardId);
            if (visual != null) visual.SetActive(!_given);

            if (openOnStart && !_given) enabled = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;
            Give();
        }

        // 조우 관리자·보스가 승리 시 직접 부른다.
        public void Give()
        {
            if (_given) return;

            var progress = GameManager.Instance.Progress;
            if (!progress.TakeReward(rewardId)) return; // 이미 받은 보상이면 아무것도 하지 않는다

            if (currency > 0) progress.AddCurrency(currency);
            if (healthShard) progress.AddHealthShard();

            _given = true;
            if (visual != null) visual.SetActive(false);
        }
    }
}
