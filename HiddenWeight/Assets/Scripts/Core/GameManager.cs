using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.Core
{
    // 게임 전역 싱글턴. 진행도(ProgressState)와 현재 지역 데이터, 게임 상태를 들고 있다.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] BalanceData balance;
        public BalanceData Balance => balance;

        public ProgressState Progress { get; private set; }
        public GameState State { get; private set; }
        public ZoneData CurrentZoneData { get; private set; }

        public event System.Action<GameState> StateChanged;

        // Player 모듈을 직접 참조하지 않기 위한 역방향 훅. PlayerHealth가 구독한다.
        public event System.Action<Vector3> RespawnRequested;

        // World가 UI(FragmentLog)를 직접 참조하지 않기 위한 훅. UI가 채운다.
        public static System.Action<string> FragmentPresenter;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Progress = new ProgressState();
        }

        public void SetState(GameState next)
        {
            if (State == next) return;

            State = next;
            Time.timeScale = next == GameState.Paused ? 0f : 1f;
            StateChanged?.Invoke(next);
        }

        public void EnterZone(ZoneId id)
        {
            Progress.CurrentZone = id;
            CurrentZoneData = balance.GetZone(id);
            // grantedSkill 해금은 여기서 하지 않는다. 지역 안의 픽업(StoryFragment)에서 처리한다.
        }

        public void RespawnPlayer() => RespawnRequested?.Invoke(Progress.LastCheckpoint);
    }
}
