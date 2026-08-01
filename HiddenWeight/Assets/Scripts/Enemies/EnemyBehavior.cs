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

        protected void FaceTowards(int direction)
        {
            var scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (direction >= 0 ? 1 : -1);
            transform.localScale = scale;
        }
    }
}
