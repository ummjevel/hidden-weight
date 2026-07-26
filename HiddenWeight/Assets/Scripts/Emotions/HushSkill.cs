using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.Player;

namespace HiddenWeight.Emotions
{
    // 숨죽이기. 홀드 중 축소·은신. 레이어를 PlayerHushed로 바꿔 GazeHazard의 시야에서 벗어난다.
    public class HushSkill : EmotionSkill
    {
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
        }

        protected override void OnTick(float dt) { }

        protected override void OnEnd()
        {
            Player.gameObject.layer = _originalLayer;
            Player.transform.localScale = _originalScale;
            var atk = Player.GetComponent<PlayerAttack>();
            if (atk != null) atk.CanAttack = true;
        }

        // 축소 상태에서 좁은 틈을 지나려면 콜라이더도 줄어야 한다. localScale 변경이
        // CapsuleCollider2D에 자동 반영되므로 별도 처리는 필요 없다.
    }
}
