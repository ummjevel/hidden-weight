using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HiddenWeight.UI;
using HiddenWeight.Player;
using HiddenWeight.Enemies;

namespace HiddenWeight.World
{
    // 방 단위 카메라.
    //
    // 예전에는 매 프레임 플레이어 위치로 Lerp했다. 그러면 걷기·점프 한 번에도 화면이 계속
    // 따라 흔들려서 배경과 지형을 읽을 수 없었다. 지금은 화면 중앙에 데드존을 두고, 관심점이
    // 데드존을 벗어날 때만 SmoothDamp로 따라간다. 관심점은 플레이어가 아니라
    // "플레이어 + 진행 방향 예측 + 낙하 보정"이다.
    //
    // 흔들림은 기준 위치와 분리해서 마지막에 더한다. 예전처럼 transform.position에 섞으면
    // 다음 프레임의 추적 시작점이 흔들린 위치가 되어 기준이 조금씩 밀렸다.
    [RequireComponent(typeof(Camera))]
    public class RoomCamera : MonoBehaviour
    {
        [Header("화면")]
        [SerializeField] float baseOrthographicSize = 6f;

        [Header("고정 추적")]
        [Tooltip("켜면 데드존·방향 예측·낙하 보정·앵커 구도를 전부 건너뛰고 플레이어를 " +
                 "화면 정중앙에 그대로 물고 간다. 균열(3맵)처럼 구도 연출 없이 캐릭터만 " +
                 "따라가야 하는 지역용.")]
        [SerializeField] bool lockToPlayer;

        [Tooltip("고정 추적의 따라붙는 시간. 0이면 매 프레임 플레이어 좌표에 그대로 용접되어 " +
                 "물리 스텝의 미세한 흔들림이 화면 전체에 그대로 드러난다. 0.05 안팎이면 " +
                 "고정처럼 보이면서도 그 떨림만 걸러진다.")]
        [SerializeField] float lockSmoothTime = 0.05f;

        [Header("데드존 (화면 중앙 기준 유닛)")]
        [SerializeField] float deadZoneHalfWidth = 2.5f;
        [SerializeField] float deadZoneUp = 1.5f;
        [SerializeField] float deadZoneDown = 1f;

        [Header("추적 시간")]
        [SerializeField] float followSmoothTime = 0.2f;
        [SerializeField] float riseSmoothTime = 0.28f;   // 점프 정점에서 천천히
        [SerializeField] float fallSmoothTime = 0.12f;   // 낙하는 착지점을 먼저 보여줘야 한다
        [SerializeField] float exitSmoothTime = 0.12f;   // 출구 구간에서 카메라가 늦으면 전환이 끊겨 보인다
        [SerializeField] float roomBlendSeconds = 0.35f;

        [Header("방향 예측")]
        [SerializeField] float lookAheadDistance = 1.75f;
        [SerializeField] float exitLookAheadDistance = 2.5f;
        [SerializeField] float facingHoldSeconds = 0.15f;   // 반대 키를 스쳐도 예측이 뒤집히지 않게
        [SerializeField] float lookAheadSmoothTime = 0.25f;

        [Header("낙하")]
        [SerializeField] float fallSpeedThreshold = 6f;     // 이보다 빨리 떨어져야 낙하로 본다
        [SerializeField] float fallDownBias = 1.2f;         // 플레이어를 화면 위쪽에 두고 아래 지형을 보여준다
        [SerializeField] float fallBiasSmoothTime = 0.18f;

        [Header("전투")]
        [SerializeField] float encounterSize = 6.25f;
        [SerializeField] float bossSize = 6.5f;
        [SerializeField] float encounterBlendSeconds = 0.35f;
        [SerializeField] float encounterLeash = 3f;

        [Header("줌")]
        [SerializeField] float zoomSmoothTime = 0.3f;

        [Header("흔들림")]
        [SerializeField] float defaultShakeDuration = 0.12f;
        [SerializeField] float defaultShakeMagnitude = 0.06f;

        Camera _cam;

        // 연출 오프셋(흔들림)을 뺀 기준 위치. 추적은 오직 이 값만 적분한다.
        Vector2 _base;
        float _velocityX, _velocityY;

        // 데드존이 붙잡고 있는 추적점. 플레이어만 이 값을 민다.
        //
        // 예측·낙하 보정을 여기 섞으면 안 된다. 섞으면 방향을 바꿀 때 예측점이 4유닛 움직여도
        // 데드존이 그중 2.5유닛을 먹어버려 화면이 1유닛만 돈다 — 스펙이 요구하는 "예측 전환"이
        // 사라진다. 그래서 데드존은 플레이어에게만 걸고, 연출 오프셋은 그 결과에 더한다.
        Vector2 _focus;
        bool _focusReady;

        float _lookAhead, _lookAheadVelocity;
        int _lookAheadDir = 1;
        float _facingHoldTimer;

        float _fallBias, _fallBiasVelocity;
        float _sizeVelocity;
        float _roomBlendTimer;

        Vector2 _shakeOffset;
        Coroutine _shakeRoutine;

        PlayerController _boundPlayer;
        Rigidbody2D _playerBody;

        readonly List<CameraAnchor> _anchors = new List<CameraAnchor>();
        Encounter _encounter;

        public static RoomCamera Instance { get; private set; }
        public Room CurrentRoom { get; private set; }
        public event System.Action<Room> RoomChanged;

        // 겹친 앵커 중 마지막에 들어간 것이 이긴다.
        CameraAnchor ActiveAnchor
        {
            get
            {
                for (int i = _anchors.Count - 1; i >= 0; i--)
                    if (_anchors[i] != null) return _anchors[i];
                return null;
            }
        }

        void Awake()
        {
            Instance = this;
            _cam = GetComponent<Camera>();
            _cam.orthographicSize = baseOrthographicSize;
            _base = transform.position;
        }

        void OnEnable() => Encounter.EncounterStateChanged += HandleEncounterStateChanged;

        void OnDisable() => Encounter.EncounterStateChanged -= HandleEncounterStateChanged;

        void LateUpdate()
        {
            var player = PlayerController.Instance;
            if (player == null) return; // 씬에 플레이어가 아직 없을 수 있다

            BindPlayer(player);

            float dt = Time.deltaTime;
            if (_roomBlendTimer > 0f) _roomBlendTimer -= dt;

            UpdateSize(dt);

            Vector2 target;
            float smoothX, smoothY;
            ResolveTarget(player, dt, out target, out smoothX, out smoothY);

            target = ClampToRoom(target, CurrentRoom);

            _base.x = Mathf.SmoothDamp(_base.x, target.x, ref _velocityX, smoothX, Mathf.Infinity, dt);
            _base.y = Mathf.SmoothDamp(_base.y, target.y, ref _velocityY, smoothY, Mathf.Infinity, dt);

            transform.position = new Vector3(
                _base.x + _shakeOffset.x,
                _base.y + _shakeOffset.y,
                transform.position.z);
        }

        void BindPlayer(PlayerController player)
        {
            if (_boundPlayer == player) return;
            _boundPlayer = player;
            _playerBody = player.GetComponent<Rigidbody2D>();
        }

        // 앵커가 있으면 앵커 구도가, 없으면 데드존 추적이 목표를 정한다.
        void ResolveTarget(PlayerController player, float dt,
                           out Vector2 target, out float smoothX, out float smoothY)
        {
            var anchor = ActiveAnchor;
            Vector2 playerPos = player.transform.position;

            // 캐릭터 고정: 구도를 만드는 모든 보정을 건너뛴다. 데드존도 여기서 플레이어에
            // 붙여 둬야 나중에 고정을 꺼도 추적점이 화면 밖에서 시작하지 않는다.
            if (lockToPlayer)
            {
                _focus = playerPos;
                _focusReady = true;
                target = playerPos;
                smoothX = smoothY = lockSmoothTime;
                return;
            }

            // 데드존은 앵커 구간에서도 계속 플레이어를 따라 둔다. 그래야 앵커에서 풀려날 때
            // 추적점이 이미 제자리에 있어 화면이 되돌아오며 튀지 않는다.
            if (!_focusReady) { _focus = playerPos; _focusReady = true; }
            _focus = ApplyDeadZone(_focus, playerPos, deadZoneHalfWidth, deadZoneUp, deadZoneDown);

            if (anchor != null && anchor.Mode != CameraAnchorMode.Exit)
            {
                target = Leash(anchor.FocusPoint, playerPos, anchor.MaxLeash);
                smoothX = smoothY = Mathf.Max(anchor.BlendSeconds, 0.01f);
                // 앵커 구도에서 빠져나올 때 예측이 튀지 않게 계속 감쇠시켜 둔다.
                UpdateLookAhead(player, dt, lookAheadDistance);
                _fallBias = Mathf.SmoothDamp(_fallBias, 0f, ref _fallBiasVelocity, fallBiasSmoothTime, Mathf.Infinity, dt);
                return;
            }

            Vector2 encounterFocus;
            if (anchor == null && TryGetEncounterFocus(out encounterFocus))
            {
                target = Leash(encounterFocus, playerPos, encounterLeash);
                smoothX = smoothY = encounterBlendSeconds;
                UpdateLookAhead(player, dt, lookAheadDistance);
                _fallBias = Mathf.SmoothDamp(_fallBias, 0f, ref _fallBiasVelocity, fallBiasSmoothTime, Mathf.Infinity, dt);
                return;
            }

            bool exiting = anchor != null; // 여기까지 왔으면 Exit 앵커다
            UpdateLookAhead(player, dt, exiting ? exitLookAheadDistance : lookAheadDistance);
            UpdateFallBias(player, dt);

            target = _focus + new Vector2(_lookAhead, -_fallBias);

            bool falling = IsFallingFast(player);
            smoothX = exiting ? exitSmoothTime : followSmoothTime;
            smoothY = falling ? fallSmoothTime
                    : !player.IsGrounded && VerticalSpeed > 0f ? riseSmoothTime
                    : exiting ? exitSmoothTime : followSmoothTime;

            // 방이 막 바뀌었으면 경계가 통째로 옮겨 간 셈이라 더 넉넉하게 흡수한다.
            if (_roomBlendTimer > 0f)
            {
                smoothX = Mathf.Max(smoothX, roomBlendSeconds);
                smoothY = Mathf.Max(smoothY, roomBlendSeconds);
            }
        }

        void UpdateLookAhead(PlayerController player, float dt, float distance)
        {
            // 방향키를 잠깐 반대로 스쳤다고 예측점이 즉시 반전하면 화면이 덜컹거린다.
            if (player.Facing != _lookAheadDir)
            {
                _facingHoldTimer += dt;
                if (_facingHoldTimer >= facingHoldSeconds)
                {
                    _lookAheadDir = player.Facing;
                    _facingHoldTimer = 0f;
                }
            }
            else _facingHoldTimer = 0f;

            _lookAhead = Mathf.SmoothDamp(_lookAhead, _lookAheadDir * distance,
                                          ref _lookAheadVelocity, lookAheadSmoothTime, Mathf.Infinity, dt);
        }

        void UpdateFallBias(PlayerController player, float dt)
        {
            float wanted = IsFallingFast(player) ? fallDownBias : 0f;
            _fallBias = Mathf.SmoothDamp(_fallBias, wanted, ref _fallBiasVelocity, fallBiasSmoothTime, Mathf.Infinity, dt);
        }

        float VerticalSpeed => _playerBody != null ? _playerBody.linearVelocity.y : 0f;

        bool IsFallingFast(PlayerController player)
            => !player.IsGrounded && VerticalSpeed < -fallSpeedThreshold;

        void UpdateSize(float dt)
        {
            var anchor = ActiveAnchor;
            float wanted = baseOrthographicSize;

            if (anchor != null && anchor.SizeOverride > 0f) wanted = anchor.SizeOverride;
            else if (_encounter != null) wanted = _encounter.BossEnemy != null ? bossSize : encounterSize;

            _cam.orthographicSize = Mathf.SmoothDamp(_cam.orthographicSize, wanted,
                                                     ref _sizeVelocity, zoomSmoothTime, Mathf.Infinity, dt);
        }

        bool TryGetEncounterFocus(out Vector2 focus)
        {
            focus = default;
            if (_encounter == null) return false;

            var col = _encounter.GetComponent<Collider2D>();
            if (col == null) return false;

            focus = col.bounds.center;
            return true;
        }

        void HandleEncounterStateChanged(Encounter encounter, bool active)
        {
            if (active) _encounter = encounter;
            else if (_encounter == encounter) _encounter = null;
        }

        public void PushAnchor(CameraAnchor anchor)
        {
            if (anchor == null || _anchors.Contains(anchor)) return;
            _anchors.Add(anchor);
        }

        public void PopAnchor(CameraAnchor anchor) => _anchors.Remove(anchor);

        // 기본 세기(착지처럼 약한 흔들림)로 흔든다.
        public void Shake() => Shake(defaultShakeDuration, defaultShakeMagnitude);

        public void Shake(float duration, float magnitude)
        {
            if (UISettings.ReduceMotion) return;
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                _shakeOffset = Random.insideUnitCircle * magnitude;
                elapsed += Time.deltaTime;
                yield return null;
            }
            _shakeOffset = Vector2.zero;
            _shakeRoutine = null;
        }

        Vector2 HalfScreen => new Vector2(_cam.orthographicSize * _cam.aspect, _cam.orthographicSize);

        // 앵커 구도가 플레이어를 화면 밖으로 밀어내지 않게 목표를 플레이어 주변으로 묶는다.
        // maxDistance가 0 이하면 제한하지 않는다 — 퍼즐방처럼 완전히 고정된 구도용이다.
        public static Vector2 Leash(Vector2 focus, Vector2 player, float maxDistance)
        {
            if (maxDistance <= 0f) return focus;

            Vector2 offset = focus - player;
            return offset.magnitude <= maxDistance
                ? focus
                : player + offset.normalized * maxDistance;
        }

        // 관심점이 데드존 안이면 카메라를 그대로 두고, 벗어난 만큼만 목표를 민다.
        public static Vector2 ApplyDeadZone(Vector2 camera, Vector2 focus,
                                            float halfWidth, float up, float down)
        {
            Vector2 delta = focus - camera;
            return new Vector2(
                focus.x - Mathf.Clamp(delta.x, -halfWidth, halfWidth),
                focus.y - Mathf.Clamp(delta.y, -down, up));
        }

        // 카메라의 '중심'만 방 안에 가둔다. 화면 전체를 방 안에 가두면 방 사이 짧은 통로에서
        // 카메라만 멈춰, 플레이어는 화면 끝으로 사라지고 다음 방에 닿은 뒤에야 카메라가 크게
        // 튄다. 중심만 묶으면 가장자리에서 이웃 공간을 미리 보여 주면서도 현재 방에서 완전히
        // 벗어나지는 않아 전환이 연속적으로 읽힌다.
        //
        // 방이 씬으로 갈라진 뒤로는 이 동작이 필수다 — 문 너머는 아직 로드되지 않은 다른
        // 씬이라, 경계에서 미리 보여 주지 않으면 플레이어만 빈 화면으로 걸어 나간다.
        public static Vector2 ClampToRoom(Vector2 target, Room room)
        {
            if (room == null) return target;

            var b = room.WorldBounds;
            float x = Mathf.Clamp(target.x, b.min.x, b.max.x);
            float y = Mathf.Clamp(target.y, b.min.y, b.max.y);
            return new Vector2(x, y);
        }

        // CurrentRoom을 바꾸고 전환 구간을 연다. 급전환은 LateUpdate가 흡수한다.
        public void SetRoom(Room room)
        {
            if (CurrentRoom == room) return;
            CurrentRoom = room;
            _roomBlendTimer = roomBlendSeconds;
            RoomChanged?.Invoke(room);
        }

        // 감쇠 없이 즉시 자리를 잡는다 (씬 진입·리스폰·포탈 도착용).
        //
        // 입구 앵커: 플레이어를 화면 정중앙에 두지 않고 바라보는 방향 쪽 예측을 미리 채워
        // 방 안쪽이 더 보이게 한다. 그래서 도착 직후 플레이어는 화면 가로 60% 지점이 아니라
        // 진행 방향 반대쪽(약 35~40%)에 선다.
        public void SnapToPlayer()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            BindPlayer(player);

            _lookAheadDir = player.Facing;
            // 고정 모드에서는 입구 예측을 미리 채우면 도착 첫 프레임에만 옆으로 밀렸다가
            // 곧바로 중앙으로 튄다. 처음부터 정중앙에 세운다.
            _lookAhead = lockToPlayer ? 0f : _lookAheadDir * lookAheadDistance;
            _lookAheadVelocity = 0f;
            _facingHoldTimer = 0f;
            _fallBias = 0f;
            _fallBiasVelocity = 0f;
            _focus = player.transform.position;
            _focusReady = true;
            _velocityX = _velocityY = 0f;
            _roomBlendTimer = 0f;
            _shakeOffset = Vector2.zero;
            _cam.orthographicSize = baseOrthographicSize;
            _sizeVelocity = 0f;

            Vector2 focus = (Vector2)player.transform.position + new Vector2(_lookAhead, 0f);
            _base = ClampToRoom(focus, CurrentRoom);
            transform.position = new Vector3(_base.x, _base.y, transform.position.z);
        }
    }
}
