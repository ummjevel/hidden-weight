using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.World;
using HiddenWeight.UI;

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
        HiddenWeight.World.SpriteAnimator _animator;

        // 행동 모듈이 "지금 뭘 하는지"를 알려 주면 그에 맞는 클립을 재생한다.
        // 클립 이름은 종류별 접두사 + 동작(예: WalkerWalk)이고, 접두사는 빌더가 넣어 준다.
        [SerializeField] string clipPrefix = "";

        // Encounter가 관리하는 적은 죽어도 파괴하지 않는다(되살릴 수 있어야 하므로).
        bool _managedByEncounter;

        public void SetManagedByEncounter(bool managed) => _managedByEncounter = managed;

        // 조우 재시작용. 체력을 되돌리고 다시 세운다.
        public void ResetForEncounter()
        {
            Health = data.maxHealth;
            HealthChanged?.Invoke(Health, data.maxHealth);
            if (_sprite != null) _sprite.color = data.tint;
        }

        public void PlayClip(string action)
        {
            if (_animator == null || string.IsNullOrEmpty(clipPrefix)) return;

            string clip = clipPrefix + action;
            if (_animator.Has(clip)) _animator.Play(clip);
        }

        public EnemyData Data => data;
        public int Health { get; private set; }
        public bool IsAlive => Health > 0;

        public event System.Action<Enemy> Died;
        public event System.Action<int, int> HealthChanged;

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponentInChildren<HiddenWeight.World.SpriteAnimator>();
            _sprite = _animator != null && _animator.Renderer != null
                ? _animator.Renderer
                : GetComponentInChildren<SpriteRenderer>();

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
            // "안 통한다"를 읽는다. 구현 타입이 아니라 IGuard를 묻는다 — 잔재의 굳은 잔재와
            // 응시의 얼굴 없는 재판관이 서로 다른 조건으로 같은 판정을 쓰기 때문이다.
            var guard = GetComponent<IGuard>();
            if (guard != null && guard.BlocksFrom(sourcePosition))
            {
                if (_flashRoutine != null) StopCoroutine(_flashRoutine);
                _flashRoutine = StartCoroutine(FlashRoutine());
                return;
            }

            Health -= amount;
            HealthChanged?.Invoke(Mathf.Max(0, Health), data.maxHealth);

            // 공격 방향의 반대쪽(자신 위치 - 공격 원점)으로 밀려난다.
            var direction = ((Vector2)transform.position - sourcePosition).normalized;
            _rb.linearVelocity = direction * data.knockbackForce;

            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
            PlayClip("Hit");

            if (Health <= 0)
            {
                Died?.Invoke(this);

                // 사망 클립이 있으면 끝까지 보여주고 사라진다(없으면 즉시). 응시 적 4종과
                // 보스들이 여기에 해당한다 — 애써 만든 사망 프레임이 재생될 틈도 없이
                // 오브젝트가 꺼지는 것이 원래 문제였다.
                if (_animator != null && !string.IsNullOrEmpty(clipPrefix)
                    && _animator.Has(clipPrefix + "Death"))
                    StartCoroutine(DeathRoutine());
                else
                    FinishDeath();
            }
        }

        // 조우에 속한 적은 지우지 않고 재운다. 지워 버리면 사망 후 재도전할 때
        // 이미 잡은 적이 영영 돌아오지 않아, 반복 사망으로 보스·조우를 조금씩 깎는
        // 흐름이 생긴다(CONTENT_SYSTEM.md 3.2절: 일반 적은 사망 후 재생성).
        void FinishDeath()
        {
            if (_managedByEncounter) gameObject.SetActive(false);
            else Destroy(gameObject);
        }

        IEnumerator DeathRoutine()
        {
            // 판정은 즉시 죽는다. 그림만 남아 사망 연출을 마친다.
            foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = false;
            if (_rb != null) _rb.simulated = false;

            _animator.Play(clipPrefix + "Death", true);
            while (_animator != null && !_animator.IsFinished) yield return null;

            FinishDeath();
        }

        IEnumerator FlashRoutine()
        {
            if (_sprite != null)
            {
                var original = data.tint;
                _sprite.color = UISettings.ReduceFlash ? Color.Lerp(original, Color.white, 0.25f) : Color.white;
                yield return new WaitForSeconds(0.1f);
                _sprite.color = original;
            }
            _flashRoutine = null;
        }
    }
}
