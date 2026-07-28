using System.Collections;
using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Enemies
{
    // 정예 — 응시의 "얼굴 없는 재판관"(GAZE_LEVEL_DESIGN.md 6.2절).
    //
    // 이 적 하나가 지역의 결론을 미리 시험한다: 숨죽이기를 계속 유지하는 것만으로는 이길 수
    // 없다(11절 UX 완료 기준). 세 규칙으로 그것을 만든다.
    //   1) 정면 방어 — 앞에서 때리면 막힌다. GuardBehavior와 같은 IGuard 계약을 쓴다.
    //   2) 시선이 꺼지면 등을 보인다 — 이때만 방어가 풀리고, 그 순간이 유일한 공격 기회다.
    //      주변 시선 장치(GazeHazard)의 점멸 주기를 읽어 판단하므로, 전장의 정보가 곧 공략법이다.
    //   3) 숨죽이면 공격을 멈추고 출구를 막는다 — 안전하지만 앞으로 나아가지는 못한다.
    public class JudgeBehavior : EnemyBehavior, IGuard
    {
        [SerializeField] LayerMask playerMask;
        [SerializeField] GazeHazard[] watchedGazes;  // 이들이 모두 꺼지면 등을 보인다
        [SerializeField] Transform exitBlockPoint;   // 숨죽인 플레이어를 상대로 이동할 자리

        bool _attacking;

        // 주변 시선이 전부 꺼진 순간. 지정된 시선이 없으면 항상 정면을 지킨다.
        public bool IsExposed
        {
            get
            {
                if (watchedGazes == null || watchedGazes.Length == 0) return false;
                foreach (var gaze in watchedGazes)
                    if (gaze != null && gaze.IsGazeOn) return false;
                return true;
            }
        }

        bool PlayerHushed => Player != null && PlayerLayers.IsHushed(Player.gameObject);

        // 정면 각도 안에서 들어온 공격은 막는다. 단, 등을 보이는 동안에는 전부 통한다.
        public bool BlocksFrom(Vector2 sourcePosition)
        {
            if (IsExposed) return false;

            var toSource = (sourcePosition - (Vector2)transform.position).normalized;
            var facing = new Vector2(Mathf.Sign(transform.localScale.x), 0f);
            return Vector2.Angle(facing, toSource) <= Data.guardArc * 0.5f;
        }

        protected override void Awake()
        {
            base.Awake();
            var patrol = GetComponent<EnemyPatrol>();
            if (patrol != null) patrol.enabled = false;
        }

        void Update()
        {
            if (_attacking || Player == null) return;

            // 등을 보이는 동안은 굳어 있다. 플레이어가 뒤로 돌아 들어갈 시간을 준다.
            if (IsExposed)
            {
                Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                ShowTelegraph(false);
                Self.PlayClip("Idle");
                return;
            }

            if (PlayerHushed)
            {
                // 찾지 못하니 때리지 않는다. 대신 출구 쪽으로 자리를 옮겨 "숨어서 지나가기"만으로는
                // 진행할 수 없게 만든다.
                MoveTowardsBlockPoint();
                return;
            }

            FaceTowards(DirectionToPlayer);

            if (DistanceToPlayer <= Data.attackRange) StartCoroutine(AttackRoutine());
            else if (DistanceToPlayer <= Data.detectRange)
                Body.linearVelocity = new Vector2(DirectionToPlayer * Data.moveSpeed, Body.linearVelocity.y);
            else
                Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
        }

        void MoveTowardsBlockPoint()
        {
            if (exitBlockPoint == null)
            {
                Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                return;
            }

            float dx = exitBlockPoint.position.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.3f)
            {
                Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                return;
            }

            int direction = dx > 0f ? 1 : -1;
            FaceTowards(direction);
            Body.linearVelocity = new Vector2(direction * Data.moveSpeed, Body.linearVelocity.y);
        }

        IEnumerator AttackRoutine()
        {
            _attacking = true;
            Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);

            // 느린 강공격. 예고가 길어 읽고 뒤로 돌 시간이 있다(난도는 예고를 줄여서 올리지 않는다).
            ShowTelegraph(true);
            Self.PlayClip("Attack");
            yield return new WaitForSeconds(Data.telegraphSeconds);
            ShowTelegraph(false);

            var center = (Vector2)transform.position
                + new Vector2(Mathf.Sign(transform.localScale.x) * Data.attackRange * 0.5f, 0f);
            var hit = Physics2D.OverlapCircle(center, Data.attackRange * 0.5f, playerMask);
            if (hit != null)
            {
                var health = hit.GetComponentInParent<PlayerHealth>();
                if (health != null) health.TakeDamage(Data.contactDamage, transform.position);
            }

            yield return new WaitForSeconds(Data.recoverSeconds);
            _attacking = false;
        }
    }
}
