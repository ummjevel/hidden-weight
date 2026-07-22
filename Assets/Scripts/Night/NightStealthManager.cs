using System;
using UnityEngine;
using HanGame.Common;
using HanGame.Data;

namespace HanGame.Night
{
    /// <summary>
    /// 밤 잠입 씬 오케스트레이터. 기획서 11장.
    /// 조사 → 출구 탈출로 성공, 발각·시간초과로 실패(즉시 1층 회귀).
    /// </summary>
    public class NightStealthManager : MonoBehaviour
    {
        [Header("Night Configs (index 0 = 1층 밤)")]
        [SerializeField] private NightConfig[] nightConfigs;

        [Header("Scene Objects")]
        [SerializeField] private InvestigationPoint objective;
        [SerializeField] private ExitZone exit;
        [SerializeField] private VisionCone[] watchers; // 경비·야근자·CCTV의 VisionCone
        [SerializeField] private NoiseSystem noise;

        public float TimeRemaining { get; private set; }
        public bool WeaponAcquired { get; private set; }
        public bool Finished { get; private set; }

        public event Action<float> TimeTicked;
        public event Action WeaponInvestigated;
        public event Action Failed;
        public event Action Succeeded;

        private NightConfig _config;

        private void Start()
        {
            int floor = GameManager.Instance != null ? GameManager.Instance.Run.Floor : 1;
            int idx = Mathf.Clamp(floor - 1, 0, nightConfigs.Length - 1);
            _config = nightConfigs[idx];

            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Night);

            TimeRemaining = _config != null ? _config.timeLimit : 60f;

            // 밤에는 달리기 허용, 공격 불가(공격 시스템은 밤 씬에 배치하지 않음).
            if (Player.Local != null && Player.Local.Controller != null)
                Player.Local.Controller.SetCanRun(true);

            if (noise != null) noise.SetEnabled(_config == null || _config.noiseEnabled);

            // 감시자 발각 구독.
            if (watchers != null)
                foreach (var w in watchers)
                    if (w != null) w.PlayerSpotted += OnSpotted;

            if (objective != null) objective.Completed += OnObjectiveDone;
            if (exit != null) exit.PlayerReached += OnExitReached;
        }

        private void Update()
        {
            if (Finished) return;

            TimeRemaining -= Time.deltaTime;
            TimeTicked?.Invoke(TimeRemaining);

            if (TimeRemaining <= 0f)
                Fail(); // 제한 시간 초과(기획서 11.9)
        }

        private void OnSpotted()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.GuardSpotted);
            Fail();
        }

        private void OnObjectiveDone()
        {
            WeaponAcquired = true;
            WeaponInvestigated?.Invoke();
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.ApprovalStamp);
        }

        private void OnExitReached()
        {
            // 조사 완료 + 출구 도착이라야 성공(기획서 11.3).
            if (WeaponAcquired) Succeed();
        }

        private void Succeed()
        {
            if (Finished) return;
            Finished = true;
            Succeeded?.Invoke();
            string reward = _config != null ? _config.rewardWeaponId : null;
            if (GameManager.Instance != null) GameManager.Instance.OnNightCleared(reward);
        }

        private void Fail()
        {
            if (Finished) return;
            Finished = true;
            Failed?.Invoke();
            // 발각·시간초과 → 즉시 전체 진행 초기화, 1층 회귀(기획서 11.9).
            if (GameManager.Instance != null) GameManager.Instance.OnNightFailed();
        }
    }
}
