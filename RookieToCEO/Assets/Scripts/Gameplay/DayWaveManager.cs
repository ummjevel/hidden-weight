using System;
using System.Collections.Generic;
using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay
{
    // GDD 5번(층별 낮 디펜스 구조)의 60초 웨이브를 진행시키는 매니저.
    // 스폰(SpawnManager), 레벨업(LevelSystem, 3택1 선택 중 시간정지), 상사의 눈치(BossGlanceSystem)를
    // 하나로 묶는다. 실제 UI(3택1 버튼)는 M9 폴리싱에서 붙이고, 지금은 PendingChoices와
    // ResolveStatChoice()로 로직만 완결되게 만들어 EditMode 밖에서도 바로 연결 가능하게 했다.
    public class DayWaveManager : MonoBehaviour
    {
        private const float WaveDurationSeconds = 60f; // GDD: 낮 전투는 층마다 1분

        [SerializeField] private int floor = 1;
        [SerializeField] private SpawnManager spawnManager;
        [SerializeField] private PlayerController player;
        [SerializeField] private float screenHalfWidth = 8f; // 상사의 눈치 정규화 좌표 계산용

        private BossGlanceSystem _bossGlance;
        private System.Random _random;

        public float ElapsedSeconds { get; private set; }
        public bool IsPaused { get; private set; } // 레벨업 3택1 선택 대기 중
        public bool IsComplete { get; private set; }
        public List<StatType> PendingChoices { get; private set; }

        public event Action OnWaveComplete;

        private void Awake()
        {
            _random = new System.Random();
            _bossGlance = CreateBossGlanceForFloor(floor);
        }

        private void OnEnable()
        {
            if (player != null) player.Level.OnLevelUp += HandleLevelUp;
        }

        private void OnDisable()
        {
            if (player != null) player.Level.OnLevelUp -= HandleLevelUp;
        }

        private void Update()
        {
            if (IsPaused || IsComplete) return;

            var deltaTime = Time.deltaTime;
            ElapsedSeconds += deltaTime;

            spawnManager?.Tick(ElapsedSeconds, deltaTime);

            var normalizedX = player != null
                ? Mathf.Clamp01((player.transform.position.x + screenHalfWidth) / (screenHalfWidth * 2f))
                : 0.5f;
            _bossGlance.Tick(deltaTime, normalizedX);

            if (player != null)
            {
                player.IsPretendingToWork = _bossGlance.IsPretendingToWork;
            }

            if (ElapsedSeconds >= WaveDurationSeconds)
            {
                Complete();
            }
        }

        private void HandleLevelUp()
        {
            IsPaused = true;
            PendingChoices = StatChoiceGenerator.PickThree(_random);
        }

        // UI(또는 테스트/자동화)가 3택1 중 하나를 선택하면 호출한다.
        public void ResolveStatChoice(StatType chosen)
        {
            if (!IsPaused || player == null) return;

            if (chosen == StatType.MentalCare)
            {
                player.ApplyMentalCareLevelUp(); // 내부에서 StatSystem.LevelUp까지 처리
            }
            else
            {
                player.Stats.LevelUp(chosen);
            }

            PendingChoices = null;
            IsPaused = false;
        }

        private void Complete()
        {
            IsComplete = true;
            OnWaveComplete?.Invoke();
        }

        // GDD 9번 층별 변화: 1층 느림 / 2층 넓은 시야 / 3층 두 번 이동 / 4층은 보스 웨이브 중
        // 한 번 추가 발동(GDD 13번, M8에서 BossWaveManager와 함께 연결).
        private static BossGlanceSystem CreateBossGlanceForFloor(int floor)
        {
            return floor switch
            {
                1 => new BossGlanceSystem(sweepSeconds: 6f, gazeHalfWidth: 0.08f, sweepCount: 1),
                2 => new BossGlanceSystem(sweepSeconds: 4f, gazeHalfWidth: 0.15f, sweepCount: 1),
                3 => new BossGlanceSystem(sweepSeconds: 4f, gazeHalfWidth: 0.08f, sweepCount: 2),
                4 => new BossGlanceSystem(sweepSeconds: 4f, gazeHalfWidth: 0.08f, sweepCount: 1),
                _ => new BossGlanceSystem(sweepSeconds: 4f, gazeHalfWidth: 0.08f, sweepCount: 1),
            };
        }
    }
}
