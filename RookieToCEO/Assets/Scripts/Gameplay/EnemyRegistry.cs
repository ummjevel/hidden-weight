using System.Collections.Generic;
using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay
{
    // GDD 6번(적 구성)의 분류. 퇴사 통보(궁극기)가 카테고리별로 다르게 반응하기 때문에 필요하다.
    public enum EnemyCategory
    {
        Normal, // 일반 업무 (이메일 봉투, 서류 더미, 포스트잇, 회의요청 달력, 클레임 전화기)
        Elite,  // 정예 업무
        Boss,   // CEO 최종 지시서
    }

    // 무기/스킬이 "현재 존재하는 적들의 위치·데미지 적용"에 접근하기 위한 간단한 서비스 로케이터.
    // M5에서 EnemyBase가 스폰/사망 시 Register/Unregister를 호출해 목록을 채운다.
    // (지금은 M4 무기 로직이 컴파일/동작하는 데 필요한 최소한의 골격만 둔다.)
    public class EnemyRegistry : MonoBehaviour
    {
        public static EnemyRegistry Instance { get; private set; }

        private class Entry
        {
            public Transform Transform;
            public IDamageable Damageable;
            public EnemyCategory Category;
        }

        private readonly List<Entry> _enemies = new List<Entry>();
        private readonly List<Vector2> _positionsCache = new List<Vector2>();

        private void Awake()
        {
            Instance = this;
        }

        public void Register(Transform enemyTransform, IDamageable damageable, EnemyCategory category)
        {
            _enemies.Add(new Entry { Transform = enemyTransform, Damageable = damageable, Category = category });
        }

        public void Unregister(Transform enemyTransform)
        {
            _enemies.RemoveAll(e => e.Transform == enemyTransform);
        }

        // 매 호출마다 최신 위치로 다시 채운다. 적 수가 많아지면(M9) 캐싱/이벤트 기반으로 최적화할 수 있다.
        public IReadOnlyList<Vector2> Positions
        {
            get
            {
                _positionsCache.Clear();
                foreach (var e in _enemies) _positionsCache.Add(e.Transform.position);
                return _positionsCache;
            }
        }

        public void DamageAt(int index, int amount)
        {
            if (index < 0 || index >= _enemies.Count) return;
            _enemies[index].Damageable.TakeDamage(amount);
        }

        public void KnockbackAt(int index, Vector2 direction, float force)
        {
            if (index < 0 || index >= _enemies.Count) return;
            (_enemies[index].Damageable as ICrowdControllable)?.ApplyKnockback(direction, force);
        }

        public void ApplyFearToAllNormal(float duration)
        {
            foreach (var e in _enemies)
            {
                if (e.Category != EnemyCategory.Normal) continue;
                (e.Damageable as ICrowdControllable)?.ApplyFear(duration);
            }
        }

        public void ApplySlowToAllElite(float duration, float multiplier)
        {
            foreach (var e in _enemies)
            {
                if (e.Category != EnemyCategory.Elite) continue;
                (e.Damageable as ICrowdControllable)?.ApplySlow(duration, multiplier);
            }
        }

        public void PauseBoss(float duration)
        {
            foreach (var e in _enemies)
            {
                if (e.Category != EnemyCategory.Boss) continue;
                (e.Damageable as IBossPausable)?.ApplyPause(duration);
            }
        }
    }
}
