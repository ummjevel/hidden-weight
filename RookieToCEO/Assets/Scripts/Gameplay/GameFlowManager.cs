using RookieToCEO.Core;
using RookieToCEO.Gameplay.Boss;
using RookieToCEO.Gameplay.Night;
using RookieToCEO.Gameplay.Skills;
using RookieToCEO.Gameplay.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RookieToCEO.Gameplay
{
    // GDD 전체 흐름(1층 낮→밤→2층 낮→밤→3층 낮→밤→4층 CEO 웨이브→엔딩, GDD 14번 플로우차트)을
    // 실제로 진행시키는 최상위 매니저. Bootstrap 씬에 한 번만 존재하며 DontDestroyOnLoad로
    // 스스로와 Player를 살려둔 채 Day/Night/Boss 씬을 순서대로 로드한다.
    //
    // Day/Night/Boss 씬은 전부 자기 안에 프리팹으로 배치된 "임시 Player" 인스턴스를 하나씩
    // 가지고 있는데(PrefabAndSceneBuilder 등으로 배치), 이 매니저가 씬이 로드될 때마다 그
    // 임시 인스턴스를 지우고 지속되는 Player로 바꿔치기한다 - 그래야 GDD 4/7번이 요구하는
    // "스탯은 다음 층까지 유지, 회귀 시에만 초기화"가 실제로 성립한다.
    public class GameFlowManager : MonoBehaviour
    {
        private const string DaySceneName = "Day";
        private const string NightSceneName = "Night";
        private const string BossSceneName = "Boss";
        private const string EndingSceneName = "Ending";

        [SerializeField] private PlayerController player; // Bootstrap 씬에 미리 배치해둔 지속 Player

        public int CurrentFloor { get; private set; } = 1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(player.gameObject);

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        public static GameFlowManager Instance { get; private set; }

        private void Start()
        {
            SceneManager.LoadScene(DaySceneName);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RemoveScenesOwnPlayerCopy(scene);

            var dayWaveManager = FindObjectOfType<DayWaveManager>();
            if (dayWaveManager != null)
            {
                WireDayScene(dayWaveManager);
                return;
            }

            var nightManager = FindObjectOfType<NightManager>();
            if (nightManager != null)
            {
                WireNightScene(nightManager);
                return;
            }

            var bossWaveManager = FindObjectOfType<BossWaveManager>();
            if (bossWaveManager != null)
            {
                WireBossScene(bossWaveManager);
            }
        }

        // Day/Night/Boss 씬 각각에 미리 배치된 "임시 Player" 프리팹 인스턴스는 지속되는 Player로
        // 대체하기 위해 지운다 (지속 Player 자신은 scene이 달라서 걸리지 않는다).
        private void RemoveScenesOwnPlayerCopy(Scene scene)
        {
            foreach (var candidate in FindObjectsOfType<PlayerController>())
            {
                if (candidate != player && candidate.gameObject.scene == scene)
                {
                    Destroy(candidate.gameObject);
                }
            }
        }

        private void WireDayScene(DayWaveManager dayWaveManager)
        {
            player.transform.position = Vector3.zero;

            dayWaveManager.SetPlayer(player);
            dayWaveManager.SetFloor(CurrentFloor);
            FindObjectOfType<SpawnManager>()?.SetFloor(CurrentFloor);

            dayWaveManager.OnWaveComplete += HandleDayComplete;
        }

        private void WireNightScene(NightManager nightManager)
        {
            player.transform.position = new Vector3(0f, -6f, 0f);

            nightManager.SetPlayer(player);
            nightManager.SetWeaponReward(GetNightRewardForFloor(CurrentFloor));

            nightManager.OnMissionFinished += HandleNightFinished;
        }

        private void WireBossScene(BossWaveManager bossWaveManager)
        {
            player.transform.position = Vector3.zero;

            bossWaveManager.SetPlayer(player);
            FindObjectOfType<SpawnManager>()?.SetFloor(4);

            bossWaveManager.OnWaveSuccess += HandleBossSuccess;
            bossWaveManager.OnWaveFailure += HandleBossFailure;
        }

        // GDD 12번: 1층 밤=스테이플러 연사, 2층 밤=업무 떠넘기기, 3층 밤=퇴사 통보.
        private Behaviour GetNightRewardForFloor(int floor)
        {
            return floor switch
            {
                1 => player.GetComponent<StaplerRapidFireWeapon>(),
                2 => player.GetComponent<WorkDumpSkill>(),
                3 => player.GetComponent<ResignationUltimate>(),
                _ => null,
            };
        }

        private void HandleDayComplete()
        {
            SceneManager.LoadScene(NightSceneName);
        }

        private void HandleNightFinished(NightMissionOutcome outcome)
        {
            if (player.Reputation.IsGameOver)
            {
                RegressToFloor1();
                return;
            }

            CurrentFloor++;
            SceneManager.LoadScene(CurrentFloor <= 3 ? DaySceneName : BossSceneName);
        }

        private void HandleBossSuccess()
        {
            SceneManager.LoadScene(EndingSceneName);
        }

        private void HandleBossFailure()
        {
            RegressToFloor1();
        }

        // GDD 7번: 평판 0 -> 해고 -> 재입사 -> 1층으로 회귀. 낮에 올린 스탯/레벨, 밤에 획득한
        // 무기, 층, HP, 평판을 전부 초기화한다.
        private void RegressToFloor1()
        {
            CurrentFloor = 1;

            player.Stats.ResetAll();
            player.Level.ResetAll();
            player.Reputation.ResetForNewRun();

            DisableIfPresent<StaplerRapidFireWeapon>();
            DisableIfPresent<WorkDumpSkill>();
            DisableIfPresent<ResignationUltimate>();

            SceneManager.LoadScene(DaySceneName);
        }

        private void DisableIfPresent<T>() where T : Behaviour
        {
            var component = player.GetComponent<T>();
            if (component != null) component.enabled = false;
        }
    }
}
