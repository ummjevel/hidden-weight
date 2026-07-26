using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.Core;

namespace HiddenWeight.Player
{
    // 이동·점프·대시·벽점프 상태머신. 모든 튜닝 수치는 PlayerData에서 읽는다.
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("접지·벽 판정")]
        [SerializeField] Transform groundCheck;
        [SerializeField] Transform wallCheck;
        [SerializeField] LayerMask groundLayer;
        [SerializeField] LayerMask wallLayer;
        // OverlapBox 크기. 밸런스 수치가 아니라 판정 상자 지오메트리라 PlayerData가 아닌
        // 여기 인스펙터 필드로 둔다.
        [SerializeField] Vector2 groundCheckSize = new Vector2(0.6f, 0.15f);
        [SerializeField] Vector2 wallCheckSize = new Vector2(0.15f, 0.8f);

        Rigidbody2D _rb;
        PlayerData _data;
        PlayerAttack _attack;

        float _coyoteTimer;
        float _jumpBufferTimer;
        float _dashTimer;
        float _dashCooldownTimer;
        float _wallJumpLockTimer;
        float _knockbackLockTimer; // 넉백 직후 좌우 입력 무시 창(브리핑 명시 리터럴 0.2초)
        float _landTimer; // 착지 직후 Land 상태 유지 창(브리핑 명시 리터럴 0.12초)
        float _attackTimer;

        public static PlayerController Instance { get; private set; }
        public PlayerState State { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsOnWall { get; private set; }
        public int Facing { get; private set; } = 1;
        public float ExternalSpeedMultiplier { get; set; } = 1f;
        public bool MovementLocked { get; set; }

        public event System.Action<PlayerState> StateChanged;

        void Awake()
        {
            Instance = this;
            _rb = GetComponent<Rigidbody2D>();
            _attack = GetComponent<PlayerAttack>();
            _data = GameManager.Instance.Balance.player;
            _rb.gravityScale = _data.gravityScale;
        }

        void OnEnable()
        {
            if (_attack != null) _attack.Attacked += HandleAttacked;
        }

        void OnDisable()
        {
            if (_attack != null) _attack.Attacked -= HandleAttacked;
        }

        void HandleAttacked()
        {
            _attackTimer = _data.attackActiveTime;
        }

        void FixedUpdate()
        {
            // 1. 접지·벽 판정 갱신
            UpdateGroundAndWallChecks();

            // 2. 코요테 타이머와 점프 버퍼 타이머 갱신(브리핑 코드 그대로) + 그 외 타이머 감소
            _coyoteTimer = IsGrounded ? _data.coyoteTime : _coyoteTimer - Time.fixedDeltaTime;
            _jumpBufferTimer = PlayerInput.JumpPressed ? _data.jumpBufferTime : _jumpBufferTimer - Time.fixedDeltaTime;

            if (_dashCooldownTimer > 0f) _dashCooldownTimer -= Time.fixedDeltaTime;
            if (_knockbackLockTimer > 0f) _knockbackLockTimer -= Time.fixedDeltaTime;
            if (_landTimer > 0f) _landTimer -= Time.fixedDeltaTime;
            if (_attackTimer > 0f) _attackTimer -= Time.fixedDeltaTime;

            // 3. MovementLocked면 수평 속도를 0으로 만들고 상태를 Idle로 두고 나머지를 건너뛴다
            if (MovementLocked)
            {
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                SetState(PlayerState.Idle);
                return;
            }

            // 4. 대시 처리
            UpdateDash();
            bool dashing = _dashTimer > 0f;

            // 5. 벽점프 잠금 처리 — 좌우 입력을 무시
            bool wallJumpLocked = _wallJumpLockTimer > 0f;
            if (wallJumpLocked) _wallJumpLockTimer -= Time.fixedDeltaTime;

            bool wallClinging = false;

            if (!dashing)
            {
                // 6. 수평 이동 (벽점프 잠금·넉백 직후에는 좌우 입력 무시)
                if (!wallJumpLocked && _knockbackLockTimer <= 0f)
                {
                    ApplyHorizontalMovement();
                }

                // 7. 벽잡기
                wallClinging = ApplyWallCling();

                // 8. 점프
                TryJump(wallClinging);

                // 9. 가변 점프
                ApplyVariableJumpCut();

                // 10. 하강 중력
                ApplyFallGravity();
            }

            // 11. 상태 결정 후 StateChanged 발행
            DetermineState(wallClinging);
        }

        void UpdateGroundAndWallChecks()
        {
            bool wasGrounded = IsGrounded;
            IsGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

            // wallCheck는 Facing 방향으로 좌우 반전시켜 하나의 체크포인트로 양쪽 벽을 판정한다.
            var local = wallCheck.localPosition;
            local.x = Mathf.Abs(local.x) * Facing;
            wallCheck.localPosition = local;
            IsOnWall = Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0f, wallLayer);

            if (!wasGrounded && IsGrounded) _landTimer = 0.12f;
        }

        void UpdateDash()
        {
            if (_dashTimer <= 0f && PlayerInput.DashPressed && _dashCooldownTimer <= 0f)
            {
                _dashTimer = _data.dashDuration;
                _dashCooldownTimer = _data.dashCooldown;
            }

            if (_dashTimer > 0f)
            {
                _rb.linearVelocity = new Vector2(Facing * (_data.dashDistance / _data.dashDuration), 0f);
                _rb.gravityScale = 0f;
                _dashTimer -= Time.fixedDeltaTime;
            }
        }

        void ApplyHorizontalMovement()
        {
            float h = PlayerInput.Horizontal;
            if (h != 0f) Facing = h > 0f ? 1 : -1;

            float speed = (PlayerInput.RunHeld ? _data.runSpeed : _data.walkSpeed) * ExternalSpeedMultiplier;
            _rb.linearVelocity = new Vector2(h * speed, _rb.linearVelocity.y);
        }

        bool ApplyWallCling()
        {
            if (IsGrounded || !IsOnWall) return false;

            float h = PlayerInput.Horizontal;
            if (h == 0f || (int)Mathf.Sign(h) != Facing) return false;

            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -_data.wallSlideSpeed);
            return true;
        }

        void TryJump(bool wallClinging)
        {
            if (_jumpBufferTimer <= 0f || (_coyoteTimer <= 0f && !wallClinging)) return;

            if (wallClinging)
            {
                _rb.linearVelocity = new Vector2(-Facing * _data.wallJumpVelocity.x, _data.wallJumpVelocity.y);
                _wallJumpLockTimer = _data.wallJumpLockTime;
            }
            else
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _data.jumpVelocity);
            }

            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
        }

        void ApplyVariableJumpCut()
        {
            if (_rb.linearVelocity.y > 0f && !PlayerInput.JumpHeld)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.y * _data.variableJumpCut);
            }
        }

        void ApplyFallGravity()
        {
            _rb.gravityScale = _rb.linearVelocity.y < 0f
                ? _data.gravityScale * _data.fallGravityMultiplier
                : _data.gravityScale;
        }

        void DetermineState(bool wallClinging)
        {
            float vy = _rb.linearVelocity.y;
            bool hasHorizontalInput = PlayerInput.Horizontal != 0f;

            PlayerState next;
            if (_attackTimer > 0f) next = PlayerState.Attack;
            else if (_dashTimer > 0f) next = PlayerState.Dash;
            else if (_wallJumpLockTimer > 0f) next = PlayerState.WallJump;
            else if (!IsGrounded && wallClinging) next = PlayerState.WallCling;
            else if (_landTimer > 0f) next = PlayerState.Land;
            else if (!IsGrounded && vy > 0f && hasHorizontalInput) next = PlayerState.AirMove;
            else if (!IsGrounded && vy > 0f) next = PlayerState.Jump;
            else if (!IsGrounded && vy <= 0f) next = PlayerState.Fall;
            else if (IsGrounded && hasHorizontalInput && PlayerInput.RunHeld) next = PlayerState.Run;
            else if (IsGrounded && hasHorizontalInput) next = PlayerState.Walk;
            else next = PlayerState.Idle;

            SetState(next);
        }

        void SetState(PlayerState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            _rb.linearVelocity = direction.normalized * force;
            _knockbackLockTimer = 0.2f; // 브리핑 명시 리터럴: 넉백 후 좌우 입력 무시 창
        }

        public void TeleportTo(Vector3 position)
        {
            transform.position = position;
            _rb.linearVelocity = Vector2.zero;
        }
    }
}
