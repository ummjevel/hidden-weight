using System.Collections;
using UnityEngine;
using HiddenWeight.World;

namespace HiddenWeight.Enemies
{
    // 일정 간격으로 공격체를 던지는 행동. 잔재 보행자의 "석재 파편"이 이것이다
    // (ResidueEnemyProjectiles_v1 1행).
    //
    // 순찰을 멈추지 않는다. 이 적의 정체는 여전히 순찰형이고, 원거리 공격은 플레이어가
    // 멀찍이서 안전하게 관찰만 하는 것을 막는 압박 수단이다. 그래서 근접 사거리 안에서는
    // 던지지 않는다 — 붙어 있는데 원거리 공격까지 겹치면 피할 방법이 없다.
    public class RangedAttackBehavior : EnemyBehavior
    {
        [SerializeField] string projectileName = "ProjSplinter";
        [SerializeField] float minRange = 3f;
        [SerializeField] float maxRange = 9f;
        [SerializeField] float intervalSeconds = 3.5f;
        [SerializeField] float heightTolerance = 2f;

        float _cooldown;
        bool _throwing;

        void Update()
        {
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;
            if (_throwing || _cooldown > 0f || Player == null) return;

            float distance = DistanceToPlayer;
            if (distance < minRange || distance > maxRange) return;
            if (Mathf.Abs(Player.position.y - transform.position.y) > heightTolerance) return;

            StartCoroutine(ThrowRoutine());
        }

        IEnumerator ThrowRoutine()
        {
            _throwing = true;

            int direction = DirectionToPlayer;
            FaceTowards(direction);

            // 던지기 전 예고. 모든 공격은 읽을 시간을 준다는 공통 규칙(EnemyBehavior 주석).
            ShowTelegraph(true);
            Self.PlayClip("Attack");
            yield return new WaitForSeconds(Data.telegraphSeconds);
            ShowTelegraph(false);

            // 예고 도중 플레이어가 붙었으면 던지지 않는다.
            if (DistanceToPlayer >= minRange)
                ProjectileSpawner.Fire(projectileName,
                    transform.position + new Vector3(direction * 0.6f, 0.2f, 0f),
                    new Vector2(direction, 0f));

            yield return new WaitForSeconds(Data.recoverSeconds);
            _cooldown = intervalSeconds;
            _throwing = false;
        }
    }
}
