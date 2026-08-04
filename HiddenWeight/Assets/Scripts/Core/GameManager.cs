using UnityEngine;
using UnityEngine.SceneManagement;
using HiddenWeight.Data;

namespace HiddenWeight.Core
{
    // 게임 전역 싱글턴. 진행도(ProgressState)와 현재 지역 데이터, 게임 상태를 들고 있다.
    //
    // Awake가 반드시 다른 모든 컴포넌트보다 먼저 돌아야 한다. PlayerController·PlayerHealth·
    // PlayerAttack·EmotionSkillController·AwarenessSystem이 자기 Awake에서 GameManager.Instance를
    // 읽기 때문이다. 씬 안의 오브젝트 순서로는 이 순서가 보장되지 않으므로(씬 루트를 맨 앞으로
    // 옮겨도 Player 쪽 Awake가 먼저 돌았다) Unity가 이 목적으로 제공하는 실행 순서 지정을 쓴다.
    // 평소 플레이(Bootstrap→Title→지역)에서는 Bootstrap 인스턴스가 이미 살아있어 문제가 가려지지만,
    // 지역 씬을 에디터에서 직접 열거나 단독 로드하면 NullReference 5개가 그대로 터진다.
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] BalanceData balance;
        public BalanceData Balance => balance;

        // Bootstrap 씬의 GameManager 인스턴스만 true로 오버라이드된다(Task 13,
        // ZoneSceneBuilder가 씬에서만 뒤집는다 — 프리팹 기본값은 건드리지 않는다).
        [SerializeField] bool autoLoadTitle = false;

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
            SaveService.Bind(Progress);

            // 지역 씬이 로드될 때마다 CurrentZoneData/Progress.CurrentZone을 자동으로 맞춘다.
            // (씬 이름 -> ZoneData 매핑. ZoneTrigger는 이 값을 읽기만 할 뿐 채우지 않으므로
            // 여기서 채워두지 않으면 스킬 해금·다음 지역 이동·백트래킹 판정이 전부 깨진다.)
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                SaveService.Unbind();
            }
        }

        void Start()
        {
            if (autoLoadTitle) SceneFlow.Load(SceneFlow.Title);
        }

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var zone = balance != null ? balance.GetZoneByScene(scene.name) : null;
            if (zone != null) EnterZone(zone.id);
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

            // 같은 곡이면 AudioManager가 알아서 무시한다. 그래서 EnterZone이 여러 번 불려도
            // (씬 로드 훅 + ZoneMarker) 곡이 처음부터 다시 시작되지 않는다.
            if (AudioManager.Instance != null && CurrentZoneData != null)
                AudioManager.Instance.PlayZoneBgm(CurrentZoneData, 1.5f);
            // grantedSkill 해금은 여기서 하지 않는다. 지역 안의 픽업(StoryFragment)에서 처리한다.
        }

        public void RespawnPlayer()
        {
            // 발판 위상을 처음으로 되돌린다(설계 10절). 죽을 때마다 발판이 제각각의
            // 위상에 있으면 같은 점프가 시도마다 다른 타이밍을 요구해, 실패가 학습으로
            // 이어지지 않는다. 예지는 같은 시계를 읽으므로 정확도는 그대로다.
            World.ZoneClock.Reset();
            RespawnRequested?.Invoke(Progress.LastCheckpoint);
        }

        public void BeginNewGame()
        {
            Progress.ResetAll();
            SaveService.Delete();
            SetState(GameState.Playing);
            SceneFlow.LoadWithFade(SceneFlow.Prologue);
        }

        public bool ContinueGame()
        {
            if (!SaveService.TryLoad(Progress)) return false;
            CurrentZoneData = balance != null ? balance.GetZone(Progress.CurrentZone) : null;
            string scene = CurrentZoneData != null && !string.IsNullOrEmpty(CurrentZoneData.sceneName)
                ? CurrentZoneData.sceneName : SceneFlow.Prologue;
            SetState(GameState.Playing);
            SceneFlow.LoadWithFade(scene);
            return true;
        }

        public void SaveProgress() => SaveService.Save(Progress);
    }
}
