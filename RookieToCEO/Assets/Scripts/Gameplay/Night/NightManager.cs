using System;
using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay.Night
{
    // GDD 10~12번(밤 탐방 구조/발각과 실패/무기 획득 과정)을 실제로 진행시키는 매니저.
    // 상태 전이 자체는 NightMissionState(순수 로직, EditMode 테스트 대상)에 맡기고,
    // 여기서는 센서/상호작용 지점의 이벤트를 받아 그 상태에 반영하고, 결과에 따라
    // 무기 지급/평판 페널티 같은 실제 게임 효과를 적용한다.
    public class NightManager : MonoBehaviour
    {
        [SerializeField] private PlayerController player;

        // 이 밤에 조사하면 얻는 무기 컴포넌트. 기본적으로 비활성화해 두었다가(GDD 12번:
        // "무기를 획득한 뒤 출구까지 탈출해야 실제로 보유하게 된다") 성공 시에만 활성화한다.
        [SerializeField] private Behaviour weaponRewardComponent;

        public NightMissionState State { get; } = new NightMissionState();
        public bool IsFinished => State.IsFinished;

        public event Action<NightMissionOutcome> OnMissionFinished;

        // GameFlowManager가 씬 전환 후 지속되는(DontDestroyOnLoad) Player로 갈아끼울 때 호출한다.
        public void SetPlayer(PlayerController newPlayer)
        {
            player = newPlayer;
        }

        // GameFlowManager가 몇 층 밤인지에 따라(1층=스테이플러/2층=업무 떠넘기기/3층=퇴사 통보)
        // 이번 밤 조사로 얻을 무기를 지정한다(GDD 12번).
        public void SetWeaponReward(Behaviour reward)
        {
            weaponRewardComponent = reward;
        }

        private void Update()
        {
            if (State.IsFinished) return;

            State.Tick(Time.deltaTime);
            if (State.IsFinished)
            {
                Resolve();
            }
        }

        public void ReportDetection()
        {
            if (State.IsFinished) return;

            State.MarkDetected();
            Resolve();
        }

        public void ReportInvestigated()
        {
            State.MarkInvestigated();
        }

        public void ReportReachedExit()
        {
            if (State.IsFinished) return;

            State.ReachExit();
            Resolve();
        }

        private void Resolve()
        {
            switch (State.Outcome)
            {
                case NightMissionOutcome.Success:
                    if (weaponRewardComponent != null) weaponRewardComponent.enabled = true;
                    break;

                case NightMissionOutcome.SuccessWithoutWeapon:
                    break; // GDD: 보상도 페널티도 없음

                case NightMissionOutcome.FailedDetected:
                case NightMissionOutcome.FailedTimeout:
                    // GDD 11번: 무기 미획득 + 평판 1 감소. "다음 층으로 강제 이동"은
                    // GameFlowManager가 OnMissionFinished를 구독해 처리한다.
                    player?.Reputation.LoseReputationDirectly();
                    break;
            }

            OnMissionFinished?.Invoke(State.Outcome);
        }
    }
}
