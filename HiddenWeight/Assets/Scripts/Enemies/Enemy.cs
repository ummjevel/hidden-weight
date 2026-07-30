using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.World;
using HiddenWeight.UI;
using HiddenWeight.Core;

namespace HiddenWeight.Enemies
{
    // 순찰형 적 1종의 체력·피격 반응. 지역별 수치는 EnemyData 에셋으로 갈아끼운다.
    // IDamageable을 구현해 Player 모듈이 Enemy를 직접 참조하지 않고도 피해를 줄 수 있게 한다
    // (World/Interactions.cs 참고 — 순수 계약 파일이라 Player가 참조해도 의존 방향이 깨지지 않는다).
    //
    // DefaultExecutionOrder(1000): EnemyPatrol/ChargerBehavior 등 행동 스크립트는 전부 기본 순서(0)로
    // 자기 FixedUpdate에서 매 스텝 velocity를 무조건 다시 정한다. 예전에 LateUpdate에서 낭떠러지
    // 감지 후 velocity.x를 0으로 눌러도, 바로 다음 물리 스텝의 그 스크립트들이 다시 dir*speed로
    // 덮어써서 클램프가 실제로는 절대 반영되지 않았다(로그로 확인: groundAhead=False가 140프레임
    // 연속 찍히는 동안 위치가 계속 앞으로 갔다). 이 컴포넌트를 다른 행동들보다 "늦게" 실행되게
    // 강제해서, 같은 물리 스텝 안에서 걔들이 정한 값을 마지막에 덮어쓰게 한다.
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(Rigidbody2D))]
    public class Enemy : MonoBehaviour, IDamageable
    {
        [SerializeField] EnemyData data;

        // 걷다가 낭떠러지를 만나면 멈춘다(층 밖으로 떨어지지 않게). EnemyPatrol은 이미 자체
        // edgeCheck로 미리 방향을 돌리지만, 플레이어를 쫓아가거나 다가서는 행동(Stalker의 추격,
        // Guard/Judge/SplitSelf의 접근 등)은 지형을 보지 않고 속도만 정한다. 그 행동들을 전부
        // 고쳐 쓰는 대신, 모든 적이 공통으로 갖는 이 컴포넌트에서 한 번만 막는다.
        [SerializeField] LayerMask groundMask;

        static readonly List<Enemy> _all = new List<Enemy>();
        static readonly List<Enemy> _instances = new List<Enemy>();
        public static IReadOnlyList<Enemy> All => _all;

        Rigidbody2D _rb;
        Collider2D _bodyCollider;
        SpriteRenderer _sprite;
        Coroutine _flashRoutine;
        HiddenWeight.World.SpriteAnimator _animator;

        // 행동 모듈이 "지금 뭘 하는지"를 알려 주면 그에 맞는 클립을 재생한다.
        // 클립 이름은 종류별 접두사 + 동작(예: WalkerWalk)이고, 접두사는 빌더가 넣어 준다.
        [SerializeField] string clipPrefix = "";

        // Encounter가 관리하는 적은 죽어도 파괴하지 않는다(되살릴 수 있어야 하므로).
        bool _managedByEncounter;
        Vector3 _spawnPosition;
        Quaternion _spawnRotation;

        public void SetManagedByEncounter(bool managed) => _managedByEncounter = managed;

        // 조우 재시작용. 체력을 되돌리고 다시 세운다.
        public void ResetForEncounter()
        {
            Health = data.maxHealth;
            HealthChanged?.Invoke(Health, data.maxHealth);
            foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = true;
            if (_rb != null)
            {
                _rb.simulated = true;
                _rb.linearVelocity = Vector2.zero;
            }
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
            _bodyCollider = GetComponent<Collider2D>();
            _animator = GetComponentInChildren<HiddenWeight.World.SpriteAnimator>();
            _sprite = _animator != null && _animator.Renderer != null
                ? _animator.Renderer
                : GetComponentInChildren<SpriteRenderer>();

            Health = data.maxHealth;
            if (_sprite != null) _sprite.color = data.tint;
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _instances.Add(this);
        }

        void OnEnable() => _all.Add(this);

        void OnDisable() => _all.Remove(this);

        void OnDestroy() => _instances.Remove(this);

        public static void ResetUnmanagedEnemies()
        {
            // 복귀 중 활성 상태가 바뀌므로 원본 목록을 복사해 순회한다.
            foreach (var enemy in _instances.ToArray())
            {
                if (enemy == null || enemy._managedByEncounter) continue;
                enemy.transform.SetPositionAndRotation(enemy._spawnPosition, enemy._spawnRotation);
                enemy.ResetForEncounter();
                enemy.gameObject.SetActive(true);
            }
        }

        // FixedUpdate에서 검사한다(LateUpdate가 아니다) — DefaultExecutionOrder(1000) 덕분에
        // 같은 물리 스텝 안에서 다른 행동 스크립트들의 FixedUpdate보다 반드시 나중에 실행되므로,
        // 여기서 정한 값이 이번 스텝에 실제로 반영된다. LateUpdate에서 하면 이번 프레임엔 맞게
        // 눌러도 다음 물리 스텝에서 그 스크립트들이 다시 덮어써 버려 클램프가 무의미해진다.
        void FixedUpdate()
        {
            if (_rb == null || _bodyCollider == null) return;
            if (_rb.bodyType != RigidbodyType2D.Dynamic) return; // 매복 중(천장에 붙음)인 경우 등은 그대로 둔다

            float vx = _rb.linearVelocity.x;
            if (Mathf.Abs(vx) < 0.01f) return;

            var bounds = _bodyCollider.bounds;
            int dir = vx > 0f ? 1 : -1;

            // "지금 서 있다"는 판정을 몸통 중앙이 아니라 진행 방향 반대쪽(뒷발)에서 본다.
            // 중앙 밑이 이미 허공이어도 뒷발은 아직 바닥을 밟고 있을 수 있다 — 그 순간에
            // 잡아야 멈추지, 중앙까지 넘어간 뒤엔 이미 기울어지는 중이라 멈춰도 늦다.
            bool standingNow = Physics2D.OverlapCircle(
                new Vector2(bounds.center.x - dir * bounds.extents.x, bounds.min.y - 0.05f), 0.08f, groundMask);
            if (!standingNow) return;

            // 진행 방향(앞발) 쪽에 바닥이 없으면 그 자리에서 멈춘다.
            bool groundAhead = Physics2D.OverlapCircle(
                new Vector2(bounds.center.x + dir * (bounds.extents.x + 0.1f), bounds.min.y - 0.15f),
                0.08f, groundMask);

            if (!groundAhead) _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        }

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
            AudioManager.Instance?.PlaySfx(SfxCue.EnemyHit, 0.4f);
            HealthChanged?.Invoke(Mathf.Max(0, Health), data.maxHealth);

            // 공격 방향의 반대쪽(자신 위치 - 공격 원점)으로 밀려난다.
            var direction = ((Vector2)transform.position - sourcePosition).normalized;
            _rb.linearVelocity = direction * data.knockbackForce;

            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
            PlayClip("Hit");

            if (Health <= 0)
            {
                AudioManager.Instance?.PlaySfx(SfxCue.EnemyDeath, 0.55f);
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

        // 일반 적과 조우 적 모두 지우지 않고 재운다. 체크포인트 휴식과 사망 복귀 때
        // ResetUnmanagedEnemies/Encounter가 같은 인스턴스를 다시 세울 수 있어야 한다.
        void FinishDeath()
        {
            gameObject.SetActive(false);
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
