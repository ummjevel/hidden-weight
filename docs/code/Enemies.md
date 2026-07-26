# Enemies 모듈

지역을 순찰하는 적 1종(`Enemy`)의 체력·피격·이동·접촉 피해를 담당한다. 세 파일(`ContactDamage`, `Enemy`, `EnemyPatrol`)이 한 프리팹에 함께 붙어 동작하며, 구역별 차이는 코드가 아니라 `HiddenWeight.Data.EnemyData` 에셋 값으로만 만든다.

## ContactDamage.cs
- **역할**: 플레이어와 접촉이 유지되는 동안(`OnCollisionStay2D`/`OnTriggerStay2D`) 매 프레임 `PlayerHealth.TakeDamage`를 호출해 접촉 피해를 준다. 무적 시간(피격 후 잠깐 무적) 처리는 이 스크립트가 아니라 `PlayerHealth` 쪽에서 담당한다(주석에 명시).
- **상속/의존**: `MonoBehaviour`. `using HiddenWeight.Player;`로 `PlayerHealth`를 직접 참조한다(Enemies → Player, 허용된 방향).
- **주요 멤버**:
  - `[SerializeField] int damage = 1` — 인스펙터에서 개별 오버라이드할 피해량. 0이면 `Enemy.Data.contactDamage`를 대신 쓴다.
  - `Enemy _enemy` — `Awake`에서 `GetComponent<Enemy>()`로 캐시.
  - `int Damage` (프로퍼티) — `damage != 0 ? damage : (_enemy != null ? _enemy.Data.contactDamage : 1)`.
  - `void TryDamage(GameObject target)` — `target.GetComponentInParent<PlayerHealth>()`를 찾아 `TakeDamage(Damage, transform.position)` 호출.
- **동작**: `Collision2D`(물리 충돌)와 `Collider2D`(트리거) 두 경로 모두 `Stay` 이벤트로 받아 동일하게 `TryDamage`를 호출한다. 대상에 `PlayerHealth`가 없으면(플레이어가 아니면) 아무 일도 일어나지 않는다.

## Enemy.cs
- **역할**: 적 1종의 체력, 피격 반응(넉백·색 플래시), 사망 처리를 담당하는 핵심 컴포넌트. 구역별 수치(`EnemyData`)를 받아 적용한다.
- **상속/의존**: `MonoBehaviour, HiddenWeight.World.IDamageable`. `[RequireComponent(typeof(Rigidbody2D))]`. `using HiddenWeight.Data;`(EnemyData), `using HiddenWeight.World;`(IDamageable).
  - `IDamageable`(`World/Interactions.cs`)은 `bool IsAlive { get; }`와 `void TakeDamage(int amount, Vector2 sourcePosition)` 두 멤버만 요구하는 순수 계약 인터페이스다. Enemy가 이를 구현하고, Player의 `PlayerAttack`은 `GetComponentInParent<IDamageable>()`로만 접근함으로써 Player → Enemies 구체 타입 참조(순환 의존)를 피한다.
- **주요 멤버**:
  - `[SerializeField] EnemyData data` — 지역별 수치 에셋. `public EnemyData Data => data`로 외부(`EnemyPatrol`, `ContactDamage`)에 노출.
  - `static List<Enemy> _all` / `public static IReadOnlyList<Enemy> All` — 씬에 존재하는 모든 살아있는 Enemy를 `OnEnable`/`OnDisable`에서 등록/해제하는 전역 레지스트리.
  - `public int Health { get; private set; }`, `public bool IsAlive => Health > 0`.
  - `public event System.Action<Enemy> Died` — 사망 시 발행.
  - `void Awake()` — `Rigidbody2D`, 자식의 `SpriteRenderer` 캐시, `Health = data.maxHealth`, 스프라이트 색을 `data.tint`로 초기화.
  - `public void TakeDamage(int amount, Vector2 sourcePosition)` — 죽어있으면 무시. 체력 차감 후, `(자신 위치 - 공격 원점)` 정규화 방향으로 `Rigidbody2D.linearVelocity`를 `data.knockbackForce` 크기로 즉시 설정(넉백). 피격 플래시 코루틴 시작. 체력이 0 이하면 `Died` 이벤트 발행 후 `Destroy(gameObject)`.
  - `IEnumerator FlashRoutine()` — 스프라이트를 흰색으로 바꾼 뒤 0.1초 대기, `data.tint`로 복귀.
- **동작**: 실측 기본값(`EnemyData.cs` 기본값 및 실제 지역별 에셋)은 `maxHealth = 2`, `contactDamage = 1`, `knockbackForce = 6`으로 세 지역(`Enemy_Residue`/`Enemy_Gaze`/`Enemy_Fracture`) 모두 동일하다. 지역마다 달라지는 값은 `moveSpeed`와 `tint`, 그리고 균열 지역만 `wobbleAmplitude`다: 잔재 1.2(회갈색), 응시 2.0(보라), 균열 1.6(파스텔 민트) + `wobbleAmplitude = 0.2`(다른 두 지역은 0) — 기획서 5.4절 수치와 일치한다.

## EnemyPatrol.cs
- **역할**: 지형 위를 좌우로 왕복 순찰하고, 낭떠러지나 벽을 만나면 방향을 반전한다. 균열 지역 전용 흔들림(wobble)도 여기서 적용한다.
- **상속/의존**: `MonoBehaviour`. `[RequireComponent(typeof(Rigidbody2D))]`, `[RequireComponent(typeof(Enemy))]`. `using HiddenWeight.Data;`(EnemyData를 `Enemy.Data`를 통해 간접 참조).
- **주요 멤버**:
  - `[SerializeField] Transform edgeCheck` — 발끝보다 진행 방향 앞쪽에 두는 감지용 빈 오브젝트.
  - `[SerializeField] LayerMask groundMask` — 바닥/벽 판정에 쓰는 레이어 마스크.
  - `Rigidbody2D _rb`, `EnemyData _data`(`GetComponent<Enemy>().Data`에서 가져옴), `int _dir = 1`(현재 진행 방향, ±1).
  - `void FixedUpdate()` — 매 물리 프레임 낭떠러지/벽 감지 후 이동 적용.
  - `void Flip()` — 방향 반전, `localScale.x` 부호 반전(스프라이트 좌우 반전), `edgeCheck`의 로컬 x 위치도 새 방향에 맞게 반전.
- **동작**:
  - `bool groundAhead = Physics2D.OverlapCircle(edgeCheck.position, 0.1f, groundMask)` — `edgeCheck` 위치에 반경 0.1 원으로 바닥이 있는지 검사(없으면 낭떠러지).
  - `bool wallAhead = Physics2D.Raycast(transform.position, Vector2.right * _dir, 0.5f, groundMask)` — 진행 방향으로 0.5유닛 레이캐스트로 벽 검사.
  - 둘 중 하나라도 걸리면(`!groundAhead || wallAhead`) `Flip()` 호출.
  - `wobble` 계산: `_data.wobbleAmplitude <= 0f`면 0, 아니면 `Mathf.Sin(Time.time * _data.wobbleFrequency) * _data.wobbleAmplitude`.
  - 최종 속도: `_rb.linearVelocity = new Vector2(_dir * _data.moveSpeed, _rb.linearVelocity.y + wobble * Time.fixedDeltaTime)` — 즉, wobble은 좌우 순찰 폭이 아니라 **Y축(수직) 속도에 더해지는 사인파 흔들림**이다. 균열 지역(`Enemy_Fracture.asset`, `wobbleAmplitude = 0.2`)에서만 이 흔들림이 실제로 나타나고, 나머지 두 지역은 `wobbleAmplitude = 0`이라 흔들림이 없다.
