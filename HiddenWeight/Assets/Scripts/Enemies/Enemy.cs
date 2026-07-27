using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.World;

namespace HiddenWeight.Enemies
{
    // 순찰형 적 1종의 체력·피격 반응. 지역별 수치는 EnemyData 에셋으로 갈아끼운다.
    // IDamageable을 구현해 Player 모듈이 Enemy를 직접 참조하지 않고도 피해를 줄 수 있게 한다
    // (World/Interactions.cs 참고 — 순수 계약 파일이라 Player가 참조해도 의존 방향이 깨지지 않는다).
    [RequireComponent(typeof(Rigidbody2D))]
    public class Enemy : MonoBehaviour, IDamageable
    {
        [SerializeField] EnemyData data;

        static readonly List<Enemy> _all = new List<Enemy>();
        public static IReadOnlyList<Enemy> All => _all;

        Rigidbody2D _rb;
        SpriteRenderer _sprite;
        Coroutine _flashRoutine;

        public EnemyData Data => data;
        public int Health { get; private set; }
        public bool IsAlive => Health > 0;

        public event System.Action<Enemy> Died;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _sprite = GetComponentInChildren<SpriteRenderer>();

            Health = data.maxHealth;
            if (_sprite != null) _sprite.color = data.tint;
        }

        void OnEnable() => _all.Add(this);

        void OnDisable() => _all.Remove(this);

        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            if (!IsAlive) return;

            // 방어형 정예는 정면에서 들어온 공격을 막는다(CONTENT_SYSTEM.md 3.1절 "방어형" —
            // 후방 이동 또는 스킬 활용을 요구하는 적). 막혀도 피격 반응은 보여줘야 플레이어가
            // "안 통한다"를 읽는다.
            var guard = GetComponent<GuardBehavior>();
            if (guard != null && guard.BlocksFrom(sourcePosition))
            {
                if (_flashRoutine != null) StopCoroutine(_flashRoutine);
                _flashRoutine = StartCoroutine(FlashRoutine());
                return;
            }

            Health -= amount;

            // 공격 방향의 반대쪽(자신 위치 - 공격 원점)으로 밀려난다.
            var direction = ((Vector2)transform.position - sourcePosition).normalized;
            _rb.linearVelocity = direction * data.knockbackForce;

            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());

            if (Health <= 0)
            {
                Died?.Invoke(this);
                Destroy(gameObject);
            }
        }

        IEnumerator FlashRoutine()
        {
            if (_sprite != null)
            {
                var original = data.tint;
                _sprite.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                _sprite.color = original;
            }
            _flashRoutine = null;
        }
    }
}
