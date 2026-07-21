using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay.Enemies
{
    // GDD 13번 "CEO 최종 지시서". "CEO라는 사람을 직접 공격하지 않습니다" - 데미지를 무시하는
    // 무적 소환수로, 제자리에서 일반 업무를 계속 소환한다. 퇴사 통보(ResignationUltimate)를
    // 맞으면 IBossPausable을 통해 3초간 소환이 멈춘다(GDD: "CEO 웨이브는 3초간 정지").
    public class CeoFinalOrderBoss : EnemyBase, IBossPausable
    {
        [SerializeField] private float summonIntervalSeconds = 5f;
        [SerializeField] private GameObject summonPrefab; // 소환할 일반 업무 프리팹(임시: 프로그래머 아트)
        [SerializeField] private float summonRadius = 3f;

        private Cooldown _summonCooldown;
        private float _pauseTimer;

        protected override void Awake()
        {
            moveSpeed = 0f; // GDD: 직접 공격 대상이 아니라 제자리에서 업무를 계속 소환
            category = EnemyCategory.Boss;
            base.Awake();
            _summonCooldown = new Cooldown(summonIntervalSeconds);
        }

        protected override void Update()
        {
            base.Update();

            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
                return; // 퇴사 통보로 정지된 동안은 소환하지 않는다.
            }

            _summonCooldown.Tick(Time.deltaTime);
            if (_summonCooldown.IsReady)
            {
                Summon();
                _summonCooldown.TryUse();
            }
        }

        protected override Vector2 GetMoveDirection() => Vector2.zero;

        public override void TakeDamage(int amount)
        {
            // 의도적으로 아무것도 하지 않는다 (GDD 13번: 직접 공격 대상이 아님).
        }

        public void ApplyPause(float duration)
        {
            _pauseTimer = Mathf.Max(_pauseTimer, duration);
        }

        private void Summon()
        {
            if (summonPrefab == null) return;

            var angle = Random.Range(0f, Mathf.PI * 2f);
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * summonRadius;
            Instantiate(summonPrefab, (Vector2)transform.position + offset, Quaternion.identity);
        }
    }
}
