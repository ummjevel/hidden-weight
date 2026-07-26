using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Emotions
{
    // 되감기. 홀드하는 동안 채널링하고, channelTime을 채우면 가장 가까운 대상이 되감긴다.
    // moveSpeedMultiplier가 0이라 베이스가 채널링 중 MovementLocked를 자동으로 켠다.
    // 채널링 중 피격당하면 즉시 캔슬되고 쿨타임만 소모한다 (기획서 EMOTION_SYSTEM 1.2절).
    public class RewindSkill : EmotionSkill
    {
        [SerializeField] LayerMask interactableMask;

        public override EmotionId Id => EmotionId.Rewind;

        IRewindable _target;
        float _channel;
        PlayerHealth _health;
        public float ChannelProgress => Data.channelTime <= 0f ? 1f : _channel / Data.channelTime;

        void Start()
        {
            // Player(베이스가 Awake에서 캡처하는 PlayerController.Instance)는 씬 단독 로드 시
            // 컴포넌트 Awake 순서에 따라 null/파괴된 인스턴스일 수 있다. 스킬은 PlayerHealth와
            // 같은 GameObject에 붙어 있으므로 자기 자신에서 찾는 것이 실행 순서와 무관하게 안전하다.
            _health = GetComponent<PlayerHealth>();
            if (_health != null) _health.Damaged += HandleDamaged;
        }

        void OnDestroy()
        {
            if (_health != null) _health.Damaged -= HandleDamaged;
        }

        // 피격 캔슬. End()가 쿨타임을 그대로 걸어 주므로 "캔슬 + 쿨타임만 소모"가 된다.
        void HandleDamaged()
        {
            if (IsActive) End();
        }

        protected override void OnBegin()
        {
            _target = FindNearestTarget();
            _channel = 0f;
            if (_target == null) { End(); return; }   // 대상이 없으면 즉시 취소, 쿨타임 없음
        }

        protected override void OnTick(float dt)
        {
            if (_target == null || !_target.CanRewind) { End(); return; }
            _channel += dt;
            if (_channel >= Data.channelTime)
            {
                _target.Rewind();
                End();
            }
        }

        protected override void OnEnd()
        {
            // 대상 없이 즉시 취소된 경우에만 쿨타임을 걸지 않는다.
            if (_channel == 0f && _target == null) SkipCooldown = true;
            _target = null;
            _channel = 0f;
        }

        IRewindable FindNearestTarget()
        {
            var hits = Physics2D.OverlapCircleAll(Player.transform.position, Data.range, interactableMask);
            IRewindable best = null;
            float bestSqr = float.MaxValue;
            foreach (var h in hits)
            {
                var r = h.GetComponentInParent<IRewindable>();
                if (r == null || !r.CanRewind) continue;
                float sqr = ((Vector2)r.Transform.position - (Vector2)Player.transform.position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = r; }
            }
            return best;
        }
    }
}
