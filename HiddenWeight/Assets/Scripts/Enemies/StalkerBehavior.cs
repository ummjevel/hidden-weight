using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.Enemies
{
    // 순찰형 — 응시의 "눈먼 순례자"(GAZE_LEVEL_DESIGN.md 6.1절). 지팡이로 앞을 더듬고
    // 소리에 반응한다. 보는 것이 아니라 듣는 적이라 시야각이 없고, 반경 하나로만 판단한다.
    //
    // 이 적의 존재 이유는 "숨죽이기가 정말 통한다"를 몸으로 알려 주는 것이다. 그래서
    // 숨죽인 플레이어에게는 감지 반경이 hushedDetectMultiplier배로 줄어든다 — 바로 옆을
    // 지나가지 않는 한 못 찾는다.
    //
    // 순찰은 기존 EnemyPatrol이 그대로 맡고, 추적 중에만 꺼 둔다. 둘 다 FixedUpdate에서
    // linearVelocity를 쓰기 때문에 동시에 켜 두면 서로 덮어써 제자리에서 떤다.
    public class StalkerBehavior : EnemyBehavior
    {
        [SerializeField] float chaseSpeedMultiplier = 1.6f;
        [SerializeField] float loseDelay = 1.2f; // 놓친 뒤 이만큼 더 찾다가 순찰로 돌아간다

        EnemyPatrol _patrol;
        float _lostTimer;

        public bool IsChasing { get; private set; }

        // 지금 이 적이 플레이어를 감지할 수 있는 거리. 숨죽이기 여부에 따라 달라진다.
        public float EffectiveRange
        {
            get
            {
                if (Player == null) return Data.detectRange;
                return PlayerLayers.IsHushed(Player.gameObject)
                    ? Data.detectRange * Data.hushedDetectMultiplier
                    : Data.detectRange;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _patrol = GetComponent<EnemyPatrol>();
        }

        void FixedUpdate()
        {
            if (Player == null) { StopChase(); return; }

            bool heard = DistanceToPlayer <= EffectiveRange
                && Mathf.Abs(Player.position.y - transform.position.y) <= 2f;

            if (heard) _lostTimer = loseDelay;
            else if (_lostTimer > 0f) _lostTimer -= Time.fixedDeltaTime;

            if (_lostTimer <= 0f) { StopChase(); return; }

            if (!IsChasing)
            {
                IsChasing = true;
                if (_patrol != null) _patrol.enabled = false;
            }

            int direction = DirectionToPlayer;
            FaceTowards(direction);
            Body.linearVelocity = new Vector2(
                direction * Data.moveSpeed * chaseSpeedMultiplier, Body.linearVelocity.y);
            Self.PlayClip("Walk");
        }

        void StopChase()
        {
            if (!IsChasing) return;

            IsChasing = false;
            Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
            if (_patrol != null) _patrol.enabled = true;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.7f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? EffectiveRange : 8f);
        }
    }
}
