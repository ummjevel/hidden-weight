using UnityEngine;

namespace HanGame.Common
{
    /// <summary>
    /// WASD 이동. 낮·밤 공통(기획서 4.1/4.2). Shift 달리기는 밤에만 활성.
    /// Rigidbody2D(Dynamic, Gravity 0, Freeze Rotation Z) 필요.
    /// 마우스 조준 없음: 이동과 회피에만 집중(기획서 4.3).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float runMultiplier = 1.6f; // 밤 달리기

        [Header("Night")]
        [SerializeField] private bool canRun = false; // 밤 씬에서 true

        private Rigidbody2D _rb;
        private Vector2 _input;
        private float _speedBonus = 1f; // '눈치' 스탯 등 외부 배수

        public bool IsRunning { get; private set; }
        public Vector2 Facing { get; private set; } = Vector2.down;

        /// <summary>외부에서 이동 잠금(레벨업, 상사의 시선 '일하는 척' 등 - 시선은 이동 허용이므로 주의).</summary>
        public bool MovementLocked { get; set; }

        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        public void SetSpeedBonus(float multiplier) => _speedBonus = Mathf.Max(0.1f, multiplier);
        public void SetCanRun(bool value) => canRun = value;

        private void Update()
        {
            if (MovementLocked || Time.timeScale == 0f)
            {
                _input = Vector2.zero;
                return;
            }

            _input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (_input.sqrMagnitude > 1f) _input.Normalize();

            IsRunning = canRun && Input.GetKey(KeyCode.LeftShift) && _input.sqrMagnitude > 0.01f;

            if (_input.sqrMagnitude > 0.01f)
                Facing = _input.normalized;
        }

        private void FixedUpdate()
        {
            float speed = moveSpeed * _speedBonus * (IsRunning ? runMultiplier : 1f);
            _rb.MovePosition(_rb.position + _input * (speed * Time.fixedDeltaTime));
        }
    }
}
