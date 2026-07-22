using System;
using UnityEngine;
using HanGame.Common;
using HanGame.Data;

namespace HanGame.Day
{
    /// <summary>
    /// 낮 전투 적. 이동·공격·피격·처리(사망)를 담당. 기획서 9장.
    /// EnemyData 기본 수치에 FloorConfig 배수를 곱해 초기화한다.
    /// 처리 시 경험치/커피 드롭. 사람을 공격하지 않고 플레이어만 목표(9.1).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Enemy : MonoBehaviour
    {
        public EnemyData Data { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsElite { get; private set; }
        public EnemyType Type => Data != null ? Data.type : EnemyType.EmailEnvelope;

        // 처리 완료 시 (적, 경험치, 커피드롭여부) 통지. 스포너/드롭 시스템이 구독.
        public event Action<Enemy> Killed;

        private Rigidbody2D _rb;
        private float _hp;
        private float _speed;
        private float _contactDamage;
        private float _attackTimer;

        // 이동/행동 배수(퇴사 통보 공포·정예 감속·스턴에 사용).
        private float _speedFactor = 1f;
        private float _fearTimer;      // > 0이면 도주
        private float _stunTimer;      // > 0이면 정지

        // 돌진(Dasher) 상태.
        private bool _dashing;
        private float _dashTelegraphTimer;
        private Vector2 _dashDir;

        public void Init(EnemyData data, FloorConfig floor, bool elite = false)
        {
            Data = data;
            IsElite = elite;
            _rb = GetComponent<Rigidbody2D>();

            float hpMul = floor != null ? floor.hpMultiplier : 1f;
            float spMul = floor != null ? floor.speedMultiplier : 1f;

            _hp = data.maxHp * hpMul * (elite ? 2.5f : 1f);
            _speed = data.moveSpeed * spMul;
            _contactDamage = data.contactDamage;

            IsDead = false;
            EnemyRegistry.Register(this);
        }

        private void OnDestroy() => EnemyRegistry.Unregister(this);

        private void FixedUpdate()
        {
            if (IsDead) return;
            var player = Player.Local;
            if (player == null) return;

            if (_stunTimer > 0f) { _stunTimer -= Time.fixedDeltaTime; return; }

            Vector2 pos = _rb.position;
            Vector2 target = player.Position;
            Vector2 toPlayer = target - pos;
            float dist = toPlayer.magnitude;
            Vector2 dir = dist > 0.001f ? toPlayer / dist : Vector2.zero;

            // 공포 상태: 플레이어 반대 방향 도주(기획서 8.5).
            if (_fearTimer > 0f)
            {
                _fearTimer -= Time.fixedDeltaTime;
                Move(-dir, _speed * 1.1f);
                return;
            }

            switch (Data.behavior)
            {
                case EnemyBehavior.Ranged:
                    RangedBehavior(pos, dir, dist);
                    break;
                case EnemyBehavior.Dasher:
                    DasherBehavior(dir, dist);
                    break;
                default: // Chaser, Tank, Debuffer, Boss는 접근 이동 공통
                    Move(dir, _speed);
                    break;
            }
        }

        private void Move(Vector2 dir, float speed)
        {
            _rb.MovePosition(_rb.position + dir * (speed * _speedFactor * Time.fixedDeltaTime));
        }

        private void RangedBehavior(Vector2 pos, Vector2 dir, float dist)
        {
            // 사거리 유지: 너무 가까우면 물러나고 멀면 접근.
            if (dist < Data.attackRange * 0.8f) Move(-dir, _speed);
            else if (dist > Data.attackRange) Move(dir, _speed);
            // 사거리 안이면 제자리에서 공격(발사체는 Weapons/무기 프리팹로 처리 권장).
        }

        private void DasherBehavior(Vector2 dir, float dist)
        {
            if (_dashing)
            {
                Move(_dashDir, Data.dashSpeed);
                _dashTelegraphTimer -= Time.fixedDeltaTime;
                if (_dashTelegraphTimer <= 0f) _dashing = false;
                return;
            }

            if (dist <= Data.dashRange && _attackTimer <= 0f)
            {
                // 예고 후 돌진.
                _dashDir = dir;
                _dashing = true;
                _dashTelegraphTimer = Data.dashTelegraph;
                _attackTimer = Data.attackInterval;
            }
            else
            {
                Move(dir, _speed * 0.6f); // 대기 접근은 느리게
            }
            _attackTimer -= Time.fixedDeltaTime;
        }

        // 접촉 피해(근접형). 원거리형은 발사체가 별도 처리.
        private void OnCollisionStay2D(Collision2D col) => TryContactDamage(col.collider);
        private void OnTriggerStay2D(Collider2D other) => TryContactDamage(other);

        private void TryContactDamage(Collider2D other)
        {
            if (IsDead || Data.attackRange > 0f) return; // 원거리형은 접촉 피해 없음
            var player = other.GetComponent<PlayerHealth>();
            if (player == null) return;

            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                player.TakeDamage(_contactDamage);
                _attackTimer = Data.attackInterval;
            }
        }

        // ── 피격/처리 ─────────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            _hp -= amount;
            if (_hp <= 0f) Kill();
        }

        public void Kill()
        {
            if (IsDead) return;
            IsDead = true;
            EnemyRegistry.Unregister(this);
            Killed?.Invoke(this); // 스포너가 경험치·커피 드롭·통계 처리
            // 처리 연출(휴지통·도장 등)은 프리팹의 애니메이션/파티클로.
            Destroy(gameObject);
        }

        // ── 상태 이상(무기·궁극기용) ───────────────────────────────

        /// <summary>퇴사 통보: 일반 적 공포(도주), 정예 감속.</summary>
        public void ApplyFear(float duration, float eliteSlow)
        {
            if (IsElite) { _speedFactor = eliteSlow; Invoke(nameof(ResetSpeedFactor), duration); }
            else _fearTimer = duration;
        }

        /// <summary>CEO 웨이브 정지.</summary>
        public void ApplyStun(float duration) => _stunTimer = Mathf.Max(_stunTimer, duration);

        private void ResetSpeedFactor() => _speedFactor = 1f;

        /// <summary>업무 떠넘기기: 바깥으로 밀어냄.</summary>
        public void Push(Vector2 origin, float force)
        {
            if (_rb == null) return;
            Vector2 dir = ((Vector2)transform.position - origin).normalized;
            _rb.MovePosition(_rb.position + dir * force * 0.1f);
        }
    }
}
