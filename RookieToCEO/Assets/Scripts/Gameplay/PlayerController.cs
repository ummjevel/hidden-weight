using RookieToCEO.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RookieToCEO.Gameplay
{
    // GDD 2번(조작 방식): WASD 이동을 담당한다.
    // 마우스 조준은 쓰지 않으므로(GDD 2번) InputActionAsset을 따로 만드는 대신
    // Input System의 Keyboard.current를 직접 읽는 가장 단순한 방식을 사용했다.
    // 자동 공격/오토타겟팅은 무기 스크립트(M4)에서 TargetingUtility를 사용해 처리하고,
    // 여기서는 이동과 HP/평판(ReputationSystem), 스탯(StatSystem) 보관만 책임진다.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        // 기본 최대 HP(=멘탈). 정확한 값은 M9 밸런싱에서 확정한다.
        private const int BaseMaxHp = 100;

        [SerializeField] private float baseMoveSpeed = 3f; // 눈치 스탯으로 배율 적용

        private Rigidbody2D _rigidbody;
        private Vector2 _moveInput;

        public StatSystem Stats { get; } = new StatSystem();
        public ReputationSystem Reputation { get; private set; }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            Reputation = new ReputationSystem(BaseMaxHp);
        }

        private void Update()
        {
            ReadMoveInput();
            Reputation.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            var speed = baseMoveSpeed * Stats.MoveSpeedMultiplier;
            _rigidbody.MovePosition(_rigidbody.position + _moveInput.normalized * speed * Time.fixedDeltaTime);
        }

        private void ReadMoveInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                _moveInput = Vector2.zero;
                return;
            }

            var x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            var y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            _moveInput = new Vector2(x, y);
        }

        // 멘탈 관리 스탯을 올렸을 때 호출 (레벨업 UI는 M6에서 구현).
        public void ApplyMentalCareLevelUp()
        {
            Stats.LevelUp(StatType.MentalCare);
            Reputation.SetMaxHp(BaseMaxHp + Stats.BonusMaxHp, healToFull: false);
        }
    }
}
