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
        PlayerAttack _attack;

        // PlayerState → 클립 이름. 시트 행 이름과 1:1로 맞춰 뒀다(Gameplay/README.md).
        static string ClipFor(PlayerState state) => "Player" + state;

        // 덮어쓰기 계층.
        //
        // 숨죽이기·자각·피격은 "지금 어떤 상태인가"와 별개로 보여줘야 하는 동작이다. 상태
        // 클립과 같은 층에 두면 숨죽인 채 한 걸음만 옮겨도 StateChanged가 들어와 PlayerWalk가
        // 웅크린 자세를 덮어써 버린다. 그래서 이 계층이 살아 있는 동안에는 상태 변화를 무시하고,
        // 끝나면 현재 상태 클립으로 스스로 돌아간다.
        enum Stage { None, Intro, Loop, Outro }

        Stage _stage;
        string _loopClip;

        public bool HasOverride => _stage != Stage.None;
        public string CurrentClip => _spriteAnimator != null ? _spriteAnimator.CurrentClip : null;

        // 시작 클립을 한 번 재생하고, loop가 있으면 그다음부터 계속 유지한다.
        // 시트가 아직 없는 클립이면 아무 것도 하지 않는다 — 아트가 덜 들어왔다고 해서
        // 스킬 자체가 멈추면 안 된다.
        public void BeginOverride(string begin, string loop = null)
        {
            if (_spriteAnimator == null) return;

            bool hasBegin = !string.IsNullOrEmpty(begin) && _spriteAnimator.Has(begin);
            bool hasLoop = !string.IsNullOrEmpty(loop) && _spriteAnimator.Has(loop);
            if (!hasBegin && !hasLoop) return;

            _loopClip = hasLoop ? loop : null;

            if (hasBegin)
            {
                _spriteAnimator.Play(begin, true);
                _stage = Stage.Intro;
            }
            else
            {
                _spriteAnimator.Play(loop, true);
                _stage = Stage.Loop;
            }
        }

        // 마무리 클립을 한 번 재생하고 상태 클립으로 돌아간다. end가 없으면 즉시 돌아간다.
        public void EndOverride(string end = null)
        {
            if (_spriteAnimator == null || _stage == Stage.None) return;

            _loopClip = null;

            if (!string.IsNullOrEmpty(end) && _spriteAnimator.Has(end))
            {
                _spriteAnimator.Play(end, true);
                _stage = Stage.Outro;
                return;
            }

            RestoreState();
        }

        // 피격처럼 짧게 한 번만 끼워 넣는다. 유지 중인 루프가 있으면(숨죽이기 자세 등)
        // 끝난 뒤 그 루프로 돌아간다 — 맞았다고 웅크린 자세가 풀려서는 안 된다.
        public void PlayOnce(string clip)
        {
            if (_spriteAnimator == null || string.IsNullOrEmpty(clip)) return;
            if (!_spriteAnimator.Has(clip)) return;

            _spriteAnimator.Play(clip, true);
            _stage = Stage.Intro;
        }

        void RestoreState()
        {
            _stage = Stage.None;
            _loopClip = null;
            if (_controller != null) HandleStateChanged(_controller.State);
        }

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _controller = GetComponent<PlayerController>();
            _attack = GetComponent<PlayerAttack>();
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
            if (_attack != null) _attack.Attacked += HandleAttacked;
        }

        void OnDisable()
        {
            if (_controller != null) _controller.StateChanged -= HandleStateChanged;
            if (_attack != null) _attack.Attacked -= HandleAttacked;
        }

        // 공격 스윙은 상태가 아니라 덮어쓰기 계층에서 끝까지 재생한다. Attack 상태는
        // 판정 시간(attackActiveTime=0.1초)만큼만 유지돼서, 상태 클립으로 틀면 6프레임
        // 스윙(0.375초)의 준비 동작 1~2프레임만 보이고 참격 프레임에 닿기 전에 Idle로
        // 덮여 버린다 — "공격 모션이 안 바뀐 것 같다"의 정체가 이것이었다.
        void HandleAttacked() => PlayOnce("PlayerAttack");

        void Update()
        {
            if (_sprite != null && _controller != null)
            {
                _sprite.flipX = _controller.Facing < 0;
            }

            AdvanceOverride();
        }

        // 1회 재생 클립이 끝나면 다음 단계로 넘긴다. Intro는 유지할 루프가 있으면 그쪽으로,
        // 없으면 상태 클립으로. Outro는 항상 상태 클립으로.
        void AdvanceOverride()
        {
            if (_stage == Stage.None || _stage == Stage.Loop) return;
            if (_spriteAnimator == null || !_spriteAnimator.IsFinished) return;

            if (_stage == Stage.Intro && !string.IsNullOrEmpty(_loopClip))
            {
                _spriteAnimator.Play(_loopClip, true);
                _stage = Stage.Loop;
                return;
            }

            RestoreState();
        }

        void HandleStateChanged(PlayerState state)
        {
            if (_animator != null)
            {
                _animator.SetInteger(StateParam, (int)state);
            }

            // 시트에 해당 클립이 없으면(예: Land만 있고 WallJump는 아직 없는 경우) 그대로 둔다.
            if (_spriteAnimator == null) return;

            // 덮어쓰기 중에는 상태 변화를 화면에 반영하지 않는다. 끝날 때 RestoreState가
            // 그 시점의 상태로 한 번에 맞춘다.
            if (_stage != Stage.None) return;

            string clip = ClipFor(state);
            if (_spriteAnimator.Has(clip)) _spriteAnimator.Play(clip);
        }
    }
}
