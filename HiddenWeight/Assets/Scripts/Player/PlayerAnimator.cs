using UnityEngine;
using HiddenWeight.World;

namespace HiddenWeight.Player
{
    // PlayerController.StateChanged를 애니메이터 정수 파라미터로 반영하고
    // 스프라이트 좌우 반전을 처리한다. Animator가 없는 플레이스홀더 단계에서는
    // flipX 갱신만 하고 나머지는 아무 것도 하지 않는다.
    public class PlayerAnimator : MonoBehaviour
    {
        static readonly int StateParam = Animator.StringToHash("State");

        Animator _animator;
        SpriteRenderer _sprite;
        PlayerController _controller;
        SpriteAnimator _spriteAnimator;

        // PlayerState → 클립 이름. 시트 행 이름과 1:1로 맞춰 뒀다(Gameplay/README.md).
        static string ClipFor(PlayerState state) => "Player" + state;

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _controller = GetComponent<PlayerController>();
            _spriteAnimator = GetComponentInChildren<SpriteAnimator>();

            // 애니메이터가 있으면 그것이 그리는 렌더러가 진짜다. GetComponentInChildren는
            // 루트에 남아 있는(꺼진) 구형 렌더러를 먼저 집어서, 안 보이는 그림만 뒤집힌다.
            _sprite = _spriteAnimator != null && _spriteAnimator.Renderer != null
                ? _spriteAnimator.Renderer
                : GetComponentInChildren<SpriteRenderer>();
        }

        void OnEnable()
        {
            if (_controller != null) _controller.StateChanged += HandleStateChanged;
        }

        void OnDisable()
        {
            if (_controller != null) _controller.StateChanged -= HandleStateChanged;
        }

        void Update()
        {
            if (_sprite != null && _controller != null)
            {
                _sprite.flipX = _controller.Facing < 0;
            }
        }

        void HandleStateChanged(PlayerState state)
        {
            if (_animator != null)
            {
                _animator.SetInteger(StateParam, (int)state);
            }

            // 시트에 해당 클립이 없으면(예: Land만 있고 WallJump는 아직 없는 경우) 그대로 둔다.
            if (_spriteAnimator == null) return;

            string clip = ClipFor(state);
            if (_spriteAnimator.Has(clip)) _spriteAnimator.Play(clip);
        }
    }
}
