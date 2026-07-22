using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HanGame.Common
{
    /// <summary>
    /// 게임 흐름 오케스트레이터. 씬 전환과 층 진행을 담당한다.
    /// 기획서 2.1 진행, 3.2 회귀 규칙, 19.1 게임 상태 전환.
    /// Boot 씬에 하나만 두고 DontDestroyOnLoad로 유지한다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene Names")]
        [SerializeField] private string dayScene = "Day";
        [SerializeField] private string nightScene = "Night";

        [Header("Config")]
        [SerializeField] private int startingReputation = 3;

        public RunState Run { get; private set; }
        public GameState State { get; private set; } = GameState.Boot;

        /// <summary>상태가 바뀔 때 UI 등이 구독한다.</summary>
        public event Action<GameState> StateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Run = new RunState { DefaultReputation = startingReputation, Reputation = startingReputation };
        }

        private void Update()
        {
            if (State == GameState.Day || State == GameState.Night)
                Run.PlayTime += Time.unscaledDeltaTime;
        }

        // ── 흐름 진입점 ────────────────────────────────────────────

        /// <summary>새 게임 시작(첫 출근).</summary>
        public void StartNewRun()
        {
            Run.ResetToFirstDay();
            Run.Reputation = Run.DefaultReputation;
            EnterDay();
        }

        public void EnterDay()
        {
            Run.Phase = FloorPhase.Day;
            SetState(GameState.Day);
            SceneManager.LoadScene(dayScene);
        }

        public void EnterNight()
        {
            Run.Phase = FloorPhase.Night;
            SetState(GameState.Night);
            SceneManager.LoadScene(nightScene);
        }

        // ── 낮 결과 ────────────────────────────────────────────────

        /// <summary>낮 전투 생존(60초 통과). 4층이면 최종 클리어.</summary>
        public void OnDaySurvived()
        {
            if (Run.IsFinalFloor)
            {
                SetState(GameState.Ending);
                return; // 엔딩 연출 → ShowResult()
            }
            // 1~3층: 밤 잠입으로.
            EnterNight();
        }

        /// <summary>낮 전투 중 평판 0. 해고 후 1층 회귀. 기획서 3.2.</summary>
        public void OnFired()
        {
            SetState(GameState.Fired);
            Run.ResetToFirstDay();
            Run.Reputation = Run.DefaultReputation;
            // Fired 연출 종료 후 UI/연출이 EnterDay() 호출.
        }

        // ── 밤 결과 ────────────────────────────────────────────────

        /// <summary>밤 탐방 성공(무기 획득 후 탈출). 다음 층 낮으로.</summary>
        public void OnNightCleared(string acquiredWeaponId)
        {
            if (!string.IsNullOrEmpty(acquiredWeaponId) && !Run.HasWeapon(acquiredWeaponId))
                Run.Weapons.Add(acquiredWeaponId);

            Run.NightClears++;
            Run.Floor++;
            EnterDay();
        }

        /// <summary>밤 탐방 실패(발각·시간초과). 즉시 1층 회귀. 기획서 11.9.</summary>
        public void OnNightFailed()
        {
            SetState(GameState.Fired);
            Run.ResetToFirstDay();
            Run.Reputation = Run.DefaultReputation;
            // Fired 연출 종료 후 EnterDay().
        }

        public void ShowResult()
        {
            SetState(GameState.Result);
        }

        // ── 상태 헬퍼 ─────────────────────────────────────────────

        public void SetState(GameState next)
        {
            State = next;
            StateChanged?.Invoke(next);
        }

        public bool IsPaused => Time.timeScale == 0f;
    }
}
