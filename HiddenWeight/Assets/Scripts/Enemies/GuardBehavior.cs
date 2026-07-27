using System.Collections;
using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.Enemies
{
    // 방어형 정예 — "굳은 잔재"(CONTENT_SYSTEM.md 4절). 좁은 관문·정예 조우에 놓는다.
    //
    // 명세(S2): "굳은 잔재는 전방 방어, 느린 강공격", "측면 벽을 복원하면 돌진을 막거나
    // 뒤쪽 공격 경로를 만든다". 즉 정면으로 두드리면 안 되고 뒤를 잡아야 하는 적이다.
    // 그래서 정면 각도 안에서 들어온 피해는 막고, 뒤에서 들어온 피해만 통과시킨다.
    public class GuardBehavior : EnemyBehavior
    {
        [SerializeField] LayerMask playerMask;

        bool _attacking;

        // 정면에서 들어온 공격인지. Enemy가 피해를 적용하기 전에 물어본다.
        public bool BlocksFrom(Vector2 sourcePosition)
        {
            var toSource = ((Vector2)sourcePosition - (Vector2)transform.position).normalized;
            var facing = new Vector2(Mathf.Sign(transform.localScale.x), 0f);
            float angle = Vector2.Angle(facing, toSource);
            return angle <= Data.guardArc * 0.5f;
        }

        void Update()
        {
            if (_attacking || Player == null) return;

            // 항상 플레이어를 향한다. 뒤를 잡으려면 플레이어가 돌아 들어가야 한다.
            FaceTowards(DirectionToPlayer);

            if (DistanceToPlayer <= Data.attackRange) StartCoroutine(AttackRoutine());
            else if (DistanceToPlayer <= Data.detectRange)
                Body.linearVelocity = new Vector2(DirectionToPlayer * Data.moveSpeed, Body.linearVelocity.y);
            else
                Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
        }

        IEnumerator AttackRoutine()
        {
            _attacking = true;
            Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);

            // 느린 강공격. 예고가 길어 읽고 뒤로 돌 시간이 있다.
            ShowTelegraph(true);
            yield return new WaitForSeconds(Data.telegraphSeconds);
            ShowTelegraph(false);

            var hit = Physics2D.OverlapCircle(
                (Vector2)transform.position + new Vector2(Mathf.Sign(transform.localScale.x) * Data.attackRange * 0.5f, 0f),
                Data.attackRange * 0.5f,
                playerMask);
            if (hit != null)
            {
                var health = hit.GetComponentInParent<PlayerHealth>();
                if (health != null) health.TakeDamage(Data.contactDamage, transform.position);
            }

            // 공격 후 빈틈. 이 창에서 반격한다.
            yield return new WaitForSeconds(Data.recoverSeconds);
            _attacking = false;
        }
    }
}
