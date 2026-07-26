using UnityEngine;

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

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _sprite = GetComponentInChildren<SpriteRenderer>();
            _controller = GetComponent<PlayerController>();
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
        }
    }
}
