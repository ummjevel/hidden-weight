using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.Player;

namespace HiddenWeight.Enemies
{
    // 교체 가능한 적 행동 모듈의 공통 베이스(CONTENT_SYSTEM.md 3.1절 "행동 역할").
    // Enemy는 체력·피격만 담당하고, "무엇을 하는 적인가"는 이 컴포넌트를 갈아끼워 정한다.
    // 순찰형은 기존 EnemyPatrol이 그대로 맡는다.
    //
    // 공통 규칙: 모든 공격은 Data.telegraphSeconds만큼 예고를 먼저 낸다. 명세 1.4절이
    // "새 적을 처음 보여주는 위치와 실제 조우 지점 사이에 2초 이상 관찰 시간"을 요구하고,
    // 전투 검증 항목이 "공격 예고를 읽을 수 있는지"이기 때문이다. 난도는 예고를 줄여서가
    // 아니라 조합으로 올린다(R10 보스 항목과 같은 원칙).
    [RequireComponent(typeof(Enemy))]
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyBehavior : MonoBehaviour
    {
        protected Enemy Self { get; private set; }
        protected Rigidbody2D Body { get; private set; }
        protected SpriteRenderer Sprite { get; private set; }
        protected EnemyData Data => Self.Data;

        // 플레이어가 없는 상태(씬 전환 중 등)에서도 안전하게 돌아가야 한다.
        protected static Transform Player
            => PlayerController.Instance != null ? PlayerController.Instance.transform : null;

        protected float DistanceToPlayer
            => Player == null ? float.MaxValue : Vector2.Distance(transform.position, Player.position);

        protected int DirectionToPlayer
            => Player == null ? 1 : (Player.position.x >= transform.position.x ? 1 : -1);

        protected virtual void Awake()
        {
            Self = GetComponent<Enemy>();
            Body = GetComponent<Rigidbody2D>();
            Sprite = GetComponentInChildren<SpriteRenderer>();
        }

        // 예고 중임을 색과 소리로 알린다. 플레이스홀더 아트라 실루엣·모션으로는 아직 구분이
        // 안 되므로, 최소한 "지금 뭔가 온다"는 신호는 있어야 검증 항목을 만족한다.
        //
        // 소리를 함께 내는 이유: 화면 밖이나 시야 가장자리의 적은 색 변화를 놓친다. 예고를
        // 귀로도 받으면 카메라가 다른 곳을 보고 있어도 회피 타이밍을 잡을 수 있다.
        protected void ShowTelegraph(bool on)
        {
            if (on && !_telegraphing)
                Core.AudioManager.Instance?.PlaySfx(Core.SfxCue.EnemyTelegraph, 0.4f);
            _telegraphing = on;

            if (Sprite == null) return;
            Sprite.color = on ? Color.Lerp(Data.tint, Color.white, 0.6f) : Data.tint;
        }

        // 예고는 매 프레임 다시 켜지는 곳이 있어(GuardBehavior) 전환 순간에만 소리를 낸다.
        bool _telegraphing;

        // 이동 속도에 맞춰 걷기/대기 그림을 고르고 재생 속도를 맞춘다.
        //
        // 이걸 하지 않으면 두 가지가 함께 어긋난다 — 멈춰 있는데 걷는 그림이 돌아가고(제자리
        // 걸음), 걷는 그림의 fps가 고정이라 이동 속도와 무관해 발이 지면 위를 미끄러진다.
        // 균열 적은 궤도가 시간 함수라 속도가 계속 변하므로 특히 눈에 띈다.
        protected void UpdateLocomotionClip(float speedX)
        {
            float reference = Data != null ? Mathf.Abs(Data.moveSpeed) : 0f;
            float pace = Mathf.Abs(speedX);
            bool walking = reference > 0.01f && pace > reference * 0.15f;

            Self.PlayClip(walking ? "Walk" : "Idle");

            if (_animator == null) _animator = GetComponentInChildren<World.SpriteAnimator>();
            if (_animator != null)
                _animator.PlaybackSpeed = walking
                    ? Mathf.Clamp(pace / reference, 0.35f, 1.6f)
                    : 1f;
        }

        World.SpriteAnimator _animator;

        // 방향 전환을 한 프레임에 끝내면 그림이 툭 뒤집힌다. 가로 배율을 0을 지나 반대로
        // 보간해 "돌아서는" 중간 프레임을 만든다 — 그림을 새로 그리지 않고도 전환이 읽힌다.
        //
        // 판정에는 손대지 않는다. _facing이 논리적 방향이고 배율은 겉모습일 뿐이라,
        // 돌아서는 도중에도 이동과 공격 방향은 이미 새 방향이다.
        const float TurnSeconds = 0.12f;
        int _facing;
        float _turnTimer = -1f;
        float _baseScaleX;

        protected int Facing => _facing >= 0 ? 1 : -1;

        protected void FaceTowards(int direction)
        {
            int next = direction >= 0 ? 1 : -1;
            if (_baseScaleX <= 0f) _baseScaleX = Mathf.Abs(transform.localScale.x);
            if (_baseScaleX <= 0f) _baseScaleX = 1f;

            if (_facing == 0)
            {
                _facing = next;
                ApplyFacingScale(1f);
                return;
            }

            if (next == _facing) return;

            _facing = next;
            _turnTimer = 0f;
        }

        protected virtual void LateUpdate()
        {
            if (_turnTimer < 0f) return;

            _turnTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_turnTimer / TurnSeconds);
            // 0.5에서 배율이 0이 되었다가 반대 방향으로 펴진다.
            ApplyFacingScale(Mathf.Abs(t * 2f - 1f));
            if (t >= 1f) _turnTimer = -1f;
        }

        void ApplyFacingScale(float amount)
        {
            var scale = transform.localScale;
            // 완전히 0이면 렌더러가 사라져 한 프레임 깜빡인다. 아주 얇게 남긴다.
            scale.x = _baseScaleX * Mathf.Max(0.05f, amount) * _facing;
            transform.localScale = scale;
        }
    }
}
