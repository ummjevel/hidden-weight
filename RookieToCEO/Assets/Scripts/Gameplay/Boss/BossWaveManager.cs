using System;
using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay.Boss
{
    // GDD 13번(CEO 최종 웨이브)을 실제로 진행시키는 매니저. 단계 판정은 BossWaveState(순수 로직)에
    // 맡기고, 스폰 자체는 M5에서 만든 SpawnManager(floor=4로 설정)를 그대로 재사용한다
    // (WaveSpawnTable이 이미 0~20/20~40/40~60초 경계를 알고 있음).
    public class BossWaveManager : MonoBehaviour
    {
        [SerializeField] private SpawnManager spawnManager; // floor=4로 세팅된 SpawnManager 참조
        [SerializeField] private PlayerController player;
        [SerializeField] private GameObject hazardZonePrefab;
        [SerializeField] private float hazardSpawnInterval = 5f;
        [SerializeField] private float hazardSpawnRadius = 6f;
        [SerializeField] private float screenHalfWidth = 8f;

        private BossGlanceSystem _bossGlance;
        private Cooldown _hazardCooldown;
        private bool _forcedGlanceThisWave;

        public BossWaveState State { get; } = new BossWaveState();
        public bool IsSuccess { get; private set; }
        public bool IsFailure { get; private set; }

        public event Action OnWaveSuccess;
        public event Action OnWaveFailure;

        // GameFlowManager가 씬 전환 후 지속되는(DontDestroyOnLoad) Player로 갈아끼울 때 호출한다.
        public void SetPlayer(PlayerController newPlayer)
        {
            player = newPlayer;
        }

        private void Awake()
        {
            _hazardCooldown = new Cooldown(hazardSpawnInterval);
            // GDD 9번 4층 변화: 기본 30초 주기 + CEO 웨이브 중 한 번 추가 발동.
            _bossGlance = new BossGlanceSystem(sweepSeconds: 4f, gazeHalfWidth: 0.08f, sweepCount: 1);
        }

        private void Update()
        {
            if (IsSuccess || IsFailure) return;

            if (player != null && player.Reputation.IsGameOver)
            {
                Fail();
                return;
            }

            var deltaTime = Time.deltaTime;
            State.Tick(deltaTime);
            spawnManager?.Tick(State.ElapsedSeconds, deltaTime);
            TickBossGlance(deltaTime);

            if (State.CurrentPhase == BossWavePhase.FullRevision && !_forcedGlanceThisWave)
            {
                _bossGlance.ForceTrigger();
                _forcedGlanceThisWave = true;
            }

            if (State.CurrentPhase == BossWavePhase.CommuteCancelled)
            {
                _hazardCooldown.Tick(deltaTime);
                if (_hazardCooldown.IsReady)
                {
                    SpawnHazard();
                    _hazardCooldown.TryUse();
                }
            }

            if (State.IsTimeUp)
            {
                Succeed();
            }
        }

        private void TickBossGlance(float deltaTime)
        {
            var normalizedX = player != null
                ? Mathf.Clamp01((player.transform.position.x + screenHalfWidth) / (screenHalfWidth * 2f))
                : 0.5f;
            _bossGlance.Tick(deltaTime, normalizedX);

            if (player != null)
            {
                player.IsPretendingToWork = _bossGlance.IsPretendingToWork;
            }
        }

        private void SpawnHazard()
        {
            if (hazardZonePrefab == null || player == null) return;

            var angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            var distance = UnityEngine.Random.Range(1f, hazardSpawnRadius);
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            Instantiate(hazardZonePrefab, (Vector2)player.transform.position + offset, Quaternion.identity);
        }

        private void Succeed()
        {
            IsSuccess = true;
            OnWaveSuccess?.Invoke();
        }

        private void Fail()
        {
            IsFailure = true;
            OnWaveFailure?.Invoke();
        }
    }
}
