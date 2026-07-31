using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using HiddenWeight.Player;
using HiddenWeight.UI;

namespace HiddenWeight.World
{
    // 방 전환 전체를 소유하는 유일한 지점. 문은 요청만 하고, 언로드·로드·플레이어 배치·
    // 암전·입력 잠금은 전부 여기서 일어난다. 실패해도 반드시 입력과 화면을 되돌린다 —
    // 검은 화면에서 조작이 막힌 채 멈추는 것이 최악이다.
    public class RoomLoader : MonoBehaviour
    {
        [SerializeField] string scenePrefix = "Room_Residue_";
        [SerializeField] float fadeSeconds = 0.2f;

        // 전환 직후 적의 선제공격을 막는 시간(LEVEL_01_STANDARD.md 1.2 진입 보호).
        [SerializeField] float entryProtectionSeconds = 1.5f;

        public static RoomLoader Instance { get; private set; }

        public string CurrentRoom { get; private set; }
        public bool IsTransitioning { get; private set; }
        public float EntryProtectedUntil { get; private set; }

        public event Action<string> RoomLoaded;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public string SceneNameFor(string roomName) => scenePrefix + roomName;

        public void RequestTransition(RoomDoor from)
        {
            if (IsTransitioning || from == null) return;

            // 돌아왔을 때 이 문이 즉시 다시 발동하지 않도록 미리 무장을 푼다.
            from.Disarm();
            StartCoroutine(Transition(from.TargetRoom, from.TargetDoorId));
        }

        public Coroutine LoadRoom(string roomName, string arriveAtDoorId)
            => StartCoroutine(Transition(roomName, arriveAtDoorId));

        IEnumerator Transition(string roomName, string arriveAtDoorId)
        {
            if (IsTransitioning) yield break;

            IsTransitioning = true;
            bool inputWasEnabled = PlayerInput.Enabled;
            PlayerInput.Enabled = false;
            var fader = ScreenFader.Instance;

            // try 안의 정상 경로들은 애니메이션 있는 Restore()로 이미 입력·암전을 되돌린다.
            // 하지만 RoomLoaded 구독자나 씬 로드 자체가 예외를 던지면 그 경로를 못 타므로,
            // finally가 최소한 즉시(애니메이션 없이)라도 반드시 복구되게 하는 안전망이다.
            // 검은 화면 + 입력 잠금인 채로 영원히 멈추는 것보다는, 예외 시 화면이 툭 하고
            // 돌아오는 편이 훨씬 낫다.
            try
            {
                if (fader != null) yield return fader.FadeTo(1f, fadeSeconds);

                // 링크 데이터 오타로 targetRoom이 현재 방과 같으면 같은 이름의 씬이 중복
                // 로드되어 이후 언로드·배치 로직이 잘못된 씬 핸들을 잡는다 — 여기서 바로 걸러낸다.
                if (roomName == CurrentRoom)
                {
                    Debug.LogError($"[RoomLoader] 이미 {roomName}에 있다. 전환을 취소한다 (링크 데이터 확인 필요).");
                    yield return Restore(fader, inputWasEnabled);
                    yield break;
                }

                string sceneName = SceneNameFor(roomName);
                if (!Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    Debug.LogError($"[RoomLoader] 씬 {sceneName} 을 빌드 세팅에서 찾을 수 없다. 전환을 취소한다.");
                    yield return Restore(fader, inputWasEnabled);
                    yield break;
                }

                string previous = CurrentRoom;
                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

                var loaded = SceneManager.GetSceneByName(sceneName);
                if (!loaded.IsValid())
                {
                    Debug.LogError($"[RoomLoader] 씬 {sceneName} 을 로드했지만 유효한 Scene을 찾지 못했다. 전환을 취소한다.");
                    yield return Restore(fader, inputWasEnabled);
                    yield break;
                }

                SceneManager.SetActiveScene(loaded);

                if (!string.IsNullOrEmpty(previous))
                {
                    var old = SceneManager.GetSceneByName(SceneNameFor(previous));
                    if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
                }

                CurrentRoom = roomName;
                PlacePlayer(loaded, roomName, arriveAtDoorId);
                SyncCamera(loaded);

                EntryProtectedUntil = Time.time + entryProtectionSeconds;
                RoomLoaded?.Invoke(roomName);

                yield return Restore(fader, inputWasEnabled);
            }
            finally
            {
                PlayerInput.Enabled = inputWasEnabled;
                IsTransitioning = false;
                if (fader != null) fader.SetAlpha(0f);
            }
        }

        IEnumerator Restore(ScreenFader fader, bool inputWasEnabled)
        {
            if (fader != null) yield return fader.FadeTo(0f, fadeSeconds);
            PlayerInput.Enabled = inputWasEnabled;
            IsTransitioning = false;
        }

        void PlacePlayer(Scene scene, string roomName, string arriveAtDoorId)
        {
            var player = PlayerController.Instance;
            if (player == null)
            {
                Debug.LogError("[RoomLoader] PlayerController.Instance가 없어 플레이어를 배치하지 못했다.");
                return;
            }

            Vector2 target;

            if (!string.IsNullOrEmpty(arriveAtDoorId)
                && TryFindDoor(scene, arriveAtDoorId, out var door))
            {
                target = door.ArrivalPosition;
                // 도착한 문 위에 서 있으므로, 벗어나기 전까지 발동하면 안 된다.
                door.Disarm();
            }
            else
            {
                if (!string.IsNullOrEmpty(arriveAtDoorId))
                    Debug.LogError($"[RoomLoader] {roomName} 에서 문 {arriveAtDoorId} 을 찾지 못했다. RoomStart로 대신 배치한다.");

                var start = FindInScene<RoomStart>(scene);
                if (start != null)
                {
                    target = start.transform.position;
                }
                else
                {
                    Debug.LogError($"[RoomLoader] {roomName} 에 RoomStart가 없다. (0,0)에 배치한다.");
                    target = Vector2.zero;
                }
            }

            // 걷던 방향은 건드리지 않는다. 연속으로 방을 지날 때 방향이 뒤집히면
            // 매번 다시 잡아야 해서 재돌파가 답답해진다.
            player.TeleportTo(new Vector3(target.x, target.y, player.transform.position.z));
        }

        void SyncCamera(Scene scene)
        {
            var camera = RoomCamera.Instance;
            if (camera == null)
            {
                Debug.LogError("[RoomLoader] RoomCamera.Instance가 없어 카메라를 동기화하지 못했다.");
                return;
            }

            var room = FindInScene<Room>(scene);
            if (room != null) camera.SetRoom(room);
            camera.SnapToPlayer();
        }

        static bool TryFindDoor(Scene scene, string doorId, out RoomDoor found)
        {
            found = null;
            if (!scene.IsValid() || !scene.isLoaded) return false;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var door in root.GetComponentsInChildren<RoomDoor>(true))
                {
                    if (door.DoorId != doorId) continue;
                    found = door;
                    return true;
                }
            }

            return false;
        }

        static T FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;

            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }

            return null;
        }

        // --- 테스트 전용 ---

        public void ConfigureForTests(string prefix) => scenePrefix = prefix;

        public void SetTransitioningForTests(bool value) => IsTransitioning = value;
    }
}
