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

        // 잔재 지역 적 그림이 전부 어두운 앰버 톤이라 배경과 실루엣 구분이 잘 안 된다.
        // 여기 박아 두면 Awake마다 자동으로 입혀져서, 씬을 새로 짓거나 에디터 메뉴를 따로
        // 돌릴 필요 없이 게임을 시작하는 순간부터 항상 적용된다(PrefabBuilder.EnemyOutlineMaterial).
        [SerializeField] Material outlineMaterial;

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

            // 조우가 다시 세우는 동안 반동이 돌고 있었으면 그림이 밀린 자리에 남는다.
            if (_recoilRoutine != null) { StopCoroutine(_recoilRoutine); _recoilRoutine = null; }
            if (_artTransform != null) _artTransform.localPosition = _artHome;

            // 죽을 때 멈춰 둔 행동을 되살린다. 빠뜨리면 되살아난 적이 가만히 서 있다.
            SetBehavioursEnabled(true);
            foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = true;
            if (_rb != null)
            {
                _rb.simulated = true;
                _rb.linearVelocity = Vector2.zero;
            }
            if (_sprite != null) _sprite.color = data.tint;

            // 조우에 묶인 적은 방이 열릴 때 꺼져 있어 Start가 돌지 않는다. 그대로 두면
            // 전투가 시작되는 순간 적이 공중에서 떨어지며 등장한다 — 조우의 첫 인상이
            // 그것이 되어서는 안 된다.
            SnapToGround();
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
            if (_sprite != null && outlineMaterial != null) _sprite.material = outlineMaterial;

            // 피격 반동은 **그림에만** 준다. 루트를 움직이면 콜라이더가 같이 가서 판정이 어긋난다.
            if (_sprite != null && _sprite.transform != transform)
            {
                _artTransform = _sprite.transform;
                _artHome = _artTransform.localPosition;
            }

            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            _instances.Add(this);
        }

        void Start() => SnapToGround();

        // 배치 좌표는 정수로 잡혀 있는데 지형 윗면은 그렇지 않다. 그래서 방에 들어서는
        // 순간 적이 발밑 빈 자리만큼 툭 떨어졌다 — 균열 12개 방의 적이 전부 0.55~2.0
        // 떠 있었다(FractureEnemyAuditTool). 첫인상이 "허공에서 떨어지는 것"이 되면
        // 적이 그 자리에 살고 있었다는 느낌이 사라진다.
        //
        // 걸어 다니는 적만, 그리고 틈이 작을 때만 붙인다. 크게 뜬 것은 공중 배치가
        // 의도인 경우(보스 전장·비행체)와 구분할 수 없으므로 건드리지 않는다.
        const float MaxSnapGap = 1.2f;

        void SnapToGround()
        {
            if (_bodyCollider == null || GetComponent<EnemyPatrol>() == null) return;

            float bottom = _bodyCollider.bounds.min.y;
            var hit = Physics2D.Raycast(new Vector2(transform.position.x, bottom + 0.02f),
                                        Vector2.down, MaxSnapGap + 0.5f, groundMask);
            if (hit.collider == null) return;

            float gap = bottom - hit.point.y;
            if (gap <= 0.02f || gap > MaxSnapGap) return;

            transform.position -= new Vector3(0f, gap, 0f);
            // 복귀 지점도 같이 옮긴다. 안 그러면 체크포인트 복귀 때 다시 떠서 시작한다.
            _spawnPosition = transform.position;
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
        // 궤도가 시간의 함수인 적(균열의 FeintPatrol 등)은 이 보호를 받으면 안 된다.
        //
        // 이 클램프는 "걸어가다 낭떠러지 앞에서 멈춘다"는 반응형 이동을 전제한다. 그런데
        // 궤도형은 위치가 아니라 **시각**으로 어디 있어야 하는지가 정해져 있어서, 한 번
        // 속도를 0으로 눌리면 시간만 흐르고 위치는 뒤처져 영영 따라잡지 못한다. 실제로
        // F07의 새싹이 궤도 오른쪽 끝에 속도 0으로 영구히 고정됐고, 그 결과 예지가
        // 보여준 2초 뒤 위치에 적이 오지 않아 지역의 공정성 규칙이 깨졌다.
        //
        // 궤도형은 자기 경로가 지형 위에 있다는 것을 배치가 보장한다 — 보호가 필요 없다.
        public bool SuppressLedgeGuard { get; set; }

        void FixedUpdate()
        {
            if (_rb == null || _bodyCollider == null) return;
            if (SuppressLedgeGuard) return;
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
                // 막힌 타격은 들어간 타격과 소리부터 달라야 한다. 같은 EnemyHit을 쓰면
                // 딜이 들어가는 줄 알고 계속 정면에서 때리게 된다.
                AudioManager.Instance?.PlaySfx(SfxCue.EnemyBlock, 0.45f);
                if (_flashRoutine != null) StopCoroutine(_flashRoutine);
                _flashRoutine = StartCoroutine(FlashRoutine());
                PlayRecoil(sourcePosition);
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
            PlayRecoil(sourcePosition);
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

            // 행동 모듈을 먼저 멈춘다.
            //
            // 순찰은 매 프레임 PlayClip("Walk")를 부른다. 그대로 두면 방금 튼 사망 클립이
            // 곧바로 걷기(루프)로 바뀌고, IsFinished는 루프 클립에서 영원히 false다 —
            // 아래 while이 끝나지 않아 **죽은 적이 화면에 그대로 남는다**. 균열에 사망
            // 클립을 새로 넣자마자 실제로 그렇게 됐다(그전에는 사망 클립이 없어서 즉시
            // 사라지는 경로를 탔고, 그래서 이 문제가 드러나지 않았다).
            StopBehaviours();

            _animator.Play(clipPrefix + "Death", true);

            // 연출이 어떤 이유로든 끝나지 않아도 적이 영원히 남지는 않게 한다.
            float deadline = Time.time + 2f;
            while (_animator != null && !_animator.IsFinished && Time.time < deadline)
                yield return null;

            FinishDeath();
        }

        void StopBehaviours() => SetBehavioursEnabled(false);

        void SetBehavioursEnabled(bool enabled)
        {
            foreach (var behavior in GetComponents<EnemyBehavior>()) behavior.enabled = enabled;
            var patrol = GetComponent<EnemyPatrol>();
            if (patrol != null) patrol.enabled = enabled;
        }

        IEnumerator FlashRoutine()
        {
            if (_sprite != null)
            {
                var original = data.tint;
                _sprite.color = FlashColor(original);
                yield return new WaitForSeconds(0.14f);
                _sprite.color = original;
            }
            _flashRoutine = null;
        }

        // 맞은 쪽으로 흠칫 물러났다 돌아오는 반동.
        //
        // 번쩍임과 짧은 피격 클립만으로는 "맞았다"가 잘 안 읽힌다 — 색이 바뀌는 것은
        // 그림이 하는 일이고, 몸이 움직이는 것은 사건이 하는 일이다. 넉백은 있지만
        // 그건 체공/지형에 따라 거의 안 보일 때가 많다.
        //
        // **크기가 아니라 위치**를 움직인다. SpriteAnimator가 normalizedHeight로 매 프레임
        // 자식의 localScale을 다시 계산하므로 스케일 반동은 한 프레임 만에 지워진다.
        // 루트를 건드리는 것은 더 안 된다 — 콜라이더가 같이 움직여 판정이 어긋난다.
        Transform _artTransform;
        Vector3 _artHome;
        Coroutine _recoilRoutine;

        void PlayRecoil(Vector2 sourcePosition)
        {
            if (_artTransform == null) return;
            if (_recoilRoutine != null) StopCoroutine(_recoilRoutine);
            _recoilRoutine = StartCoroutine(RecoilRoutine(sourcePosition));
        }

        IEnumerator RecoilRoutine(Vector2 sourcePosition)
        {
            var away = (Vector2)transform.position - sourcePosition;
            away = away.sqrMagnitude < 0.0001f ? Vector2.right : away.normalized;

            float distance = UISettings.ReduceMotion ? 0.14f : 0.32f;
            const float backSeconds = 0.16f;

            // 밀리는 것은 **즉시**다. 타격은 순간이므로 나가는 동안 보간하면 그 프레임들이
            // 오히려 밀린 느낌을 지운다. 첫 프레임에 끝까지 밀어 두고 천천히 돌아온다.
            _artTransform.localPosition = _artHome + (Vector3)(away * distance);
            yield return null;

            for (float t = 0f; t < backSeconds; t += Time.deltaTime)
            {
                _artTransform.localPosition =
                    _artHome + (Vector3)(away * (distance * (1f - t / backSeconds)));
                yield return null;
            }

            _artTransform.localPosition = _artHome;
            _recoilRoutine = null;
        }

        // 피격 번쩍임의 색. 원래 색에서 **멀어지는** 방향으로 간다.
        //
        // 예전에는 무조건 흰색이었다. 잔재의 적은 어두운 앰버라 흰색이 확 튀지만, 균열의
        // 적은 원래 색이 연한 민트(0.62, 0.86, 0.72)·연보라라 흰색으로 바꿔도 거의 그대로다
        // — 때렸는데 아무 반응이 없는 것처럼 보이던 이유가 이것이다. 밝은 적은 반대로
        // 짙게 눌러 실루엣만 남긴다. 어느 쪽이든 "한 번 다른 것이 됐다"가 읽힌다.
        static Color FlashColor(Color tint)
        {
            float luminance = tint.r * 0.299f + tint.g * 0.587f + tint.b * 0.114f;
            var strong = luminance > 0.55f
                ? new Color(0.30f, 0.08f, 0.20f, tint.a)
                : new Color(1f, 1f, 1f, tint.a);
            return UISettings.ReduceFlash ? Color.Lerp(tint, strong, 0.35f) : strong;
        }
    }
}
