using System;
using System.Collections;
using UnityEngine;
using HanGame.Common;
using HanGame.Data;

namespace HanGame.Day
{
    /// <summary>
    /// 낮 전투 씬 오케스트레이터. 기획서 5장.
    /// 스포너·타이머·상사의 시선·레벨업·드롭을 묶고, 60초 생존 시 층 통과 처리.
    /// </summary>
    public class DayCombatManager : MonoBehaviour
    {
        [Header("Floor Configs (index 0 = 1층)")]
        [SerializeField] private FloorConfig[] floors;

        [Header("Systems")]
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private WaveTimer timer;
        [SerializeField] private BossGaze bossGaze;
        [SerializeField] private StatUpgradeSystem upgrades;

        [Header("Drops")]
        [SerializeField] private GameObject expPickupPrefab;
        [SerializeField] private GameObject coffeePickupPrefab;

        [Header("Level Up")]
        [SerializeField] private bool pauseOnLevelUp = true;

        // UI가 구독: 레벨업 시 후보 3종 전달.
        public event Action<System.Collections.Generic.List<UpgradeData>> LevelUpOffered;
        public event Action DaySurvived;

        private FloorConfig _floor;

        private void Start()
        {
            int floorIndex = Mathf.Clamp((GameManager.Instance != null ? GameManager.Instance.Run.Floor : 1) - 1, 0, floors.Length - 1);
            _floor = floors[floorIndex];

            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Day);

            // 시스템 연결.
            spawner.EnemyKilled += OnEnemyKilled;
            timer.Completed += OnWaveComplete;

            var exp = FindObjectOfType<ExperienceSystem>();
            if (exp != null) exp.LeveledUp += OnLeveledUp;

            bossGaze.Configure(_floor);
            spawner.Begin(_floor);
            timer.Begin(_floor.dayDuration);

            if (_floor.isFinalFloor && AudioManager.Instance != null)
                AudioManager.Instance.PlaySfx(Sfx.CeoWaveWarn);
        }

        private void Update()
        {
            if (timer.Running) bossGaze.Tick(timer.Elapsed);
        }

        // ── 적 처리 → 경험치/커피 드롭 ─────────────────────────────

        private void OnEnemyKilled(Enemy e)
        {
            if (GameManager.Instance != null) GameManager.Instance.Run.TasksProcessed++;

            if (expPickupPrefab != null && e.Data != null)
            {
                var go = Instantiate(expPickupPrefab, e.transform.position, Quaternion.identity);
                if (go.TryGetComponent<ExpPickup>(out var pickup)) pickup.SetAmount(e.Data.expReward);
            }

            if (coffeePickupPrefab != null && e.Data != null && UnityEngine.Random.value < e.Data.coffeeDropChance)
                Instantiate(coffeePickupPrefab, e.transform.position, Quaternion.identity);
        }

        // ── 레벨업(시간 정지) ─────────────────────────────────────

        private void OnLeveledUp(int level)
        {
            if (upgrades == null) return;
            var options = upgrades.RollOptions();
            if (options.Count == 0) return; // 모두 최대 중첩

            if (pauseOnLevelUp) Time.timeScale = 0f; // 전투 시간 정지(기획서 7.1)
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.DayLevelUp);
            LevelUpOffered?.Invoke(options);
        }

        /// <summary>UI가 강화를 선택하면 호출.</summary>
        public void ResolveLevelUp(UpgradeData picked)
        {
            if (upgrades != null) upgrades.Pick(picked);
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Day);
        }

        // ── 60초 생존 → 통과 ──────────────────────────────────────

        private void OnWaveComplete() => StartCoroutine(SurviveSequence());

        private IEnumerator SurviveSequence()
        {
            spawner.ClearRemaining(); // 남은 일반 적 처리 연출 후 제거
            yield return new WaitForSeconds(1f);

            DaySurvived?.Invoke();
            if (GameManager.Instance != null) GameManager.Instance.OnDaySurvived();
        }
    }
}
