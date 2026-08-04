using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    // 웨이포인트를 따라 한 번 올라가는 승강기. 응시 G08 "시선 승강정"과 균열 F08 "역행 승강축"이
    // 같은 컴포넌트를 쓰고 경로만 다르게 준다 — 균열 쪽은 첫 웨이포인트를 아래로 두면 그대로
    // "먼저 내려갔다가 올라가는" 역행 승강기가 된다(FRACTURE_LEVEL_DESIGN.md 4.8절).
    //
    // MovingPlatform과 나누는 이유: 저쪽은 시간만으로 위치가 정해지는 무한 왕복이고, 이쪽은
    // 플레이어가 올라타야 시작하는 장치다. 종점에서 숏컷을 연 뒤 기본은 그대로 멈추지만
    // (returnDelay=0), returnDelay를 주면 그만큼 기다렸다가 출발점으로 돌아가 다시 탈 수
    // 있다 — 숏컷은 첫 종점 도착 때 한 번만 연다. 시간만으로 움직이는 MovingPlatform과
    // 합치지 않는 이유는 그대로다: 두 성질을 한 컴포넌트에 넣으면 예지 예측식이 상태에
    // 의존하게 되어 고스트가 어긋난다.
    [RequireComponent(typeof(Rigidbody2D))]
    public class LiftPlatform : MonoBehaviour, IForeseeable
    {
        [Tooltip("시작 위치 기준 상대 좌표. 순서대로 이동한다.")]
        [SerializeField] Vector2[] waypoints = { new Vector2(0f, 10f) };
        [SerializeField] float speed = 3f;
        [SerializeField] float startDelay = 0.6f;   // 올라탄 뒤 출발까지. 예지를 쓸 시간을 준다
        // 0이면 편도(종점에 도착한 채로 영구히 멈춘다 — 기존 동작). 0보다 크면 종점 도착 후
        // 이만큼 기다렸다가 출발점으로 되돌아가고, 도착하면 다시 탈 수 있게 초기화된다.
        [SerializeField] float returnDelay = 0f;
        [SerializeField] Shortcut linkedShortcut;   // 종점에 닿으면 열린다(숏컷 B)
        // 방이 씬으로 갈라진 뒤로 승강기와 숏컷은 서로 다른 씬에 산다(G08→G03). 유니티는
        // 씬을 넘는 오브젝트 참조를 저장하지 못해 linkedShortcut이 null로 구워지므로,
        // Rewindable과 같은 방식으로 id 기반 대안을 둔다.
        [SerializeField] string linkedShortcutId;

        Rigidbody2D _rb;
        SpriteRenderer _sprite;
        BoxCollider2D _box;
        Vector3 _origin;
        int _riderMask;

        int _leg = -1;      // -1이면 아직 출발 전
        float _delayTimer;
        bool _finished;
        bool _returning;     // 종점에서 출발점으로 되돌아가는 중
        bool _shortcutOpened; // 왕복 중 종점에 다시 닿아도 숏컷을 또 열지 않기 위한 가드

        public bool IsRunning => _leg >= 0 && !_finished;

        // 승강기가 지나갈 자리(시작 위치 기준 상대 좌표). PredictPosition은 출발한 뒤에만
        // 답을 주므로, 아직 아무도 올라타지 않은 방을 검사하는 쪽은 경로 자체를 봐야 한다.
        public Vector2[] Waypoints => waypoints;
        public bool IsFinished => _finished;

        public Transform Transform => transform;
        public Sprite CurrentSprite => _sprite != null ? _sprite.sprite : null;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            // 지역 아트는 루트를 끄고 자식에 그린다(ApplyPlatformArt). 예지 고스트가
            // 실제 보이는 그림을 복사하도록 보이는 렌더러를 잡는다.
            _sprite = GetComponent<SpriteRenderer>();
            if (_sprite == null || !_sprite.enabled)
                foreach (var candidate in GetComponentsInChildren<SpriteRenderer>())
                    if (candidate.enabled) { _sprite = candidate; break; }
            _box = GetComponent<BoxCollider2D>();
            _origin = transform.position;
            _riderMask = 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("PlayerHushed");
        }

        void Start()
        {
            // 왕복 승강기(returnDelay > 0)는 매번 다시 탈 수 있어야 하므로 항상 출발점에서
            // 시작한다 — "숏컷이 이미 열렸다"는 사실과 "승강기가 지금 어디 있는가"는 별개다.
            // 편도 승강기만 아래 규칙을 적용한다: 한 번 종점까지 운행해 숏컷을 연 뒤에는
            // 재방문 때 같은 이동을 기다리게 하지 않는다. Shortcut.Start의 실행 순서에
            // 기대지 않고 저장 상태를 직접 읽어 두 지역에서 동일하게 복원한다.
            if (returnDelay > 0f) return;

            string id = linkedShortcut != null ? linkedShortcut.Id : linkedShortcutId;
            if (string.IsNullOrEmpty(id) || waypoints == null || waypoints.Length == 0) return;
            if (GameManager.Instance == null || !GameManager.Instance.Progress.IsShortcutOpen(id)) return;

            var destination = Target(waypoints.Length - 1);
            transform.position = destination;
            _rb.position = destination;
            _leg = waypoints.Length - 1;
            _finished = true;
            _shortcutOpened = true;
        }

        // 발판 바로 위를 매 스텝 훑어 탑승자를 옮긴다.
        //
        // MovingPlatform처럼 OnCollisionEnter2D/Exit2D로 탑승자를 기억하는 방식은 수평 왕복에는
        // 통하지만 수직 승강에는 통하지 않는다. 위로 올라가는 동안 발판이 플레이어를 아래에서
        // 밀어 올리느라 접촉이 끊겼다 붙었다를 반복하고, Exit가 한 번 뜨는 순간 탑승자를 잊어
        // 발판만 혼자 올라가 버린다 — 봇이 승강기 중간에서 계속 떨어졌던 원인이다.
        void CarryRiders(Vector3 delta)
        {
            if (delta.sqrMagnitude <= 0f) return;

            var size = _box != null ? _box.size : new Vector2(3f, 0.5f);
            var center = (Vector2)transform.position + new Vector2(0f, size.y * 0.5f + 0.45f);
            var area = new Vector2(size.x * 0.95f, 1f);

            foreach (var hit in Physics2D.OverlapBoxAll(center, area, 0f, _riderMask))
                hit.transform.position += delta;
        }

        Vector3 Target(int leg)
            => _origin + (Vector3)waypoints[Mathf.Clamp(leg, 0, waypoints.Length - 1)];

        void FixedUpdate()
        {
            if (_finished || _leg < 0) return;

            if (_delayTimer > 0f)
            {
                _delayTimer -= Time.fixedDeltaTime;
                return;
            }

            var target = _returning ? _origin : Target(_leg);
            var next = Vector3.MoveTowards(transform.position, target, speed * Time.fixedDeltaTime);
            var delta = next - transform.position;

            _rb.MovePosition(next);
            CarryRiders(delta);

            if ((next - target).sqrMagnitude > 0.0001f) return;

            if (_returning)
            {
                // 출발점 복귀 완료. 다시 탈 수 있는 상태로 되돌린다.
                _returning = false;
                _leg = -1;
                AudioManager.Instance?.PlaySfx(SfxCue.LiftStop, 0.55f);
                return;
            }

            _leg++;
            if (_leg < waypoints.Length) return;

            // 종점 도착. 여기서만 숏컷이 열린다 — 도중에 내려도 열리지 않아야
            // "승강기를 상층까지 작동시키면"이라는 조건이 성립한다.
            _leg = waypoints.Length - 1;
            AudioManager.Instance?.PlaySfx(SfxCue.LiftStop, 0.55f);
            OpenLinkedShortcut();

            if (returnDelay > 0f)
            {
                // 편도로 멈추는 대신 이만큼 기다렸다가 출발점으로 되돌아간다.
                _delayTimer = returnDelay;
                _returning = true;
            }
            else
            {
                _finished = true;
            }
        }

        void OpenLinkedShortcut()
        {
            if (_shortcutOpened) return;
            _shortcutOpened = true;

            if (linkedShortcut != null)
            {
                linkedShortcut.Open();
            }
            // 숏컷이 다른 방 씬에 있어 지금 메모리에 없는 경우다. 진행 상태에만 열림을 남긴다
            // (Rewindable.TryOpenLinkedShortcut과 같은 이유).
            else if (!string.IsNullOrEmpty(linkedShortcutId) && GameManager.Instance != null)
            {
                GameManager.Instance.Progress.MarkShortcutOpen(linkedShortcutId);
            }
        }

        // 예지: 남은 경로를 그대로 따라가 lead초 뒤 위치를 계산한다. 아직 출발 전이면
        // 제자리 — "타면 어디로 갈지"가 아니라 "곧 어디에 있을지"를 보여주는 것이 규칙이다.
        public Vector3 PredictPosition(float leadSeconds)
        {
            if (_finished || _leg < 0) return transform.position;

            var position = transform.position;
            float remaining = leadSeconds - Mathf.Max(0f, _delayTimer);

            if (_returning)
            {
                if (remaining <= 0f) return position;
                float distance = Vector3.Distance(position, _origin);
                float travel = speed * remaining;
                return travel < distance ? Vector3.MoveTowards(position, _origin, travel) : _origin;
            }

            for (int leg = _leg; leg < waypoints.Length && remaining > 0f; leg++)
            {
                var target = Target(leg);
                float distance = Vector3.Distance(position, target);
                float travel = speed * remaining;
                if (travel < distance) return Vector3.MoveTowards(position, target, travel);

                position = target;
                remaining -= distance / speed;
            }
            return position;
        }

        public bool PredictActive(float leadSeconds) => true;

        // 출발은 "위에서 밟았을 때" 한 번만 판단하면 되므로 충돌 이벤트를 그대로 쓴다.
        // 탑승자 운반만 CarryRiders가 따로 맡는다.
        void OnCollisionEnter2D(Collision2D collision)
        {
            if (_leg >= 0 || _finished) return;
            if (!PlayerLayers.SteppedOnFromAbove(collision, transform)) return;

            _leg = 0;
            _delayTimer = startDelay;

            // 출발까지 startDelay만큼 뜸이 있어서, 소리가 없으면 밟은 게 먹혔는지 알 수 없다.
            AudioManager.Instance?.PlaySfx(SfxCue.LiftStart, 0.6f);
        }
    }
}
