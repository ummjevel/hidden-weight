using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.Player;

namespace HiddenWeight.Emotions
{
    // 숨죽이기. 홀드 중 축소·은신. 레이어를 PlayerHushed로 바꿔 GazeHazard의 시야에서 벗어난다.
    // 해제 순간 짧은 무적을 줘서 원래 크기로 돌아오자마자 피격당하는 것을 막는다
    // (기획서 EMOTION_SYSTEM 2.2절, 0.2초).
    public class HushSkill : EmotionSkill
    {
        [SerializeField] float releaseInvulnSeconds = 0.2f;

        public override EmotionId Id => EmotionId.Hush;

        int _originalLayer;
        Vector3 _originalScale;

        protected override void OnBegin()
        {
            _originalLayer = Player.gameObject.layer;
            _originalScale = Player.transform.localScale;
            Player.gameObject.layer = LayerMask.NameToLayer("PlayerHushed");
            Player.transform.localScale = _originalScale * Data.hushScale;
            var atk = Player.GetComponent<PlayerAttack>();
            if (atk != null) atk.CanAttack = false;

            // 웅크렸다가 그 자세를 유지한다. 이동 상태가 바뀌어도 자세가 풀리면 안 되므로
            // 상태 클립이 아니라 덮어쓰기 계층에 건다(PlayerAnimator 참고).
            Animator?.BeginOverride("HushBegin", "HushMove");
        }

        protected override void OnTick(float dt) { }

        protected override void OnEnd()
        {
            Player.gameObject.layer = _originalLayer;
            Player.transform.localScale = _originalScale;
            var atk = Player.GetComponent<PlayerAttack>();
            if (atk != null) atk.CanAttack = true;

            var health = GetComponent<PlayerHealth>(); // 스킬과 같은 GameObject에 있다
            if (health != null) health.GrantInvulnerability(releaseInvulnSeconds);

            Animator?.EndOverride("HushEnd");
        }

        // 스킬과 같은 GameObject에 붙어 있다. Awake에서 캡처하지 않는 이유는 EmotionSkill의
        // Player 프로퍼티와 같다 — 같은 오브젝트 안의 Awake 순서는 보장되지 않는다.
        PlayerAnimator _animator;
        PlayerAnimator Animator
            => _animator != null ? _animator : (_animator = GetComponent<PlayerAnimator>());

        // 축소 상태에서 좁은 틈을 지나려면 콜라이더도 줄어야 한다. localScale 변경이
        // CapsuleCollider2D에 자동 반영되므로 별도 처리는 필요 없다.
    }
}
