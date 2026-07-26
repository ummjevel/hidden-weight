# Enemies 모듈 — 지역별 순찰형 적 1종의 체력·이동·접촉 피해

> 기획서 5.4(전투) 대응.
> 적은 단일 프리팹(`Enemy` + `EnemyPatrol` + `ContactDamage`)에 지역별 `EnemyData`(Data 모듈) 에셋만 갈아끼워
> 잔재/응시/균열 세 구역의 색·속도·흔들림 차이를 만든다. 이동/피격 로직 자체는 지역마다 동일하다.

## 파일

| 파일 | 역할 | 기획서 대응 |
|---|---|---|
| `Enemy.cs` | 체력(HP)·피격·넉백·색 플래시 처리. `IDamageable` 구현, `EnemyData` 값 적용, 전역 인스턴스 목록(`All`) 관리 | 5.4 "HP 2, 피격 시 넉백" |
| `EnemyPatrol.cs` | 지형 위 왕복 이동, 낭떠러지·벽 감지 후 반전, 균열 지역 전용 상하 흔들림 | 5.4 "구역별 이동 속도/지터" |
| `ContactDamage.cs` | 플레이어와의 접촉 판정(Collision/Trigger Stay) 시 `PlayerHealth.TakeDamage` 호출 | 5.4 "접촉 피해 1" |

## 핵심 규칙 구현

- **HP**: `EnemyData.maxHealth` 기본값 2 (`Enemy_Residue`/`Enemy_Gaze`/`Enemy_Fracture` 세 에셋 모두 2로 동일).
- **접촉 피해**: `EnemyData.contactDamage` 기본값 1 (세 에셋 모두 1). `ContactDamage.damage`가 0이 아니면 그 값을 우선 사용하고, 0이면 `Enemy.Data.contactDamage`를 쓴다.
- **넉백**: `EnemyData.knockbackForce` 기본값 6 (세 에셋 모두 6). `Enemy.TakeDamage`에서 `(자신 위치 - 공격 원점)` 방향으로 `Rigidbody2D.linearVelocity`를 즉시 그 크기로 설정한다.
- **피격 플래시**: `TakeDamage` 호출 시 스프라이트 색을 0.1초간 흰색으로 바꿨다가 `EnemyData.tint`로 복귀(코루틴).
- **사망**: HP가 0 이하가 되면 `Died` 이벤트를 쏘고 `Destroy(gameObject)`. 별도의 사망 애니메이션/이펙트는 없음.
- **구역별 이동 속도** (`EnemyData.moveSpeed`, 실측치 — 기획서 수치와 일치):
  - 잔재(`Enemy_Residue`): 1.2, tint ≈ (0.42, 0.36, 0.32) 탁한 회갈색
  - 응시(`Enemy_Gaze`): 2.0, tint ≈ (0.48, 0.37, 0.65) 보라
  - 균열(`Enemy_Fracture`): 1.6, tint ≈ (0.56, 0.85, 0.77) 파스텔 민트
- **균열 지역 지터**: `EnemyData.wobbleAmplitude`/`wobbleFrequency`로 구현되어 있음. `Enemy_Fracture.asset`만 `wobbleAmplitude = 0.2`(나머지 두 에셋은 0). 다만 실제 구현은 기획서가 말하는 "0.2유닛 순찰 폭 변화"가 아니라, `EnemyPatrol.FixedUpdate`에서 `Mathf.Sin(Time.time * wobbleFrequency) * wobbleAmplitude`를 `Rigidbody2D.linearVelocity.y`에 매 프레임 더하는 **수직 흔들림(bobbing)**이다. 좌우 순찰 폭 자체는 지역과 무관하게 낭떠러지/벽 감지로만 결정된다.

## 씬 배치

- Enemy 프리팹 구성: `Rigidbody2D` + `Enemy`(EnemyData 슬롯에 지역별 에셋 할당) + `EnemyPatrol`(`edgeCheck` 빈 자식 오브젝트, `groundMask` 레이어 지정) + `ContactDamage`.
- `EnemyPatrol.edgeCheck`는 발끝보다 진행 방향 앞쪽에 배치해 낭떠러지 감지용 `OverlapCircle`(반경 0.1)이 걸리게 한다.
- `groundMask`는 바닥 레이어를 가리키며, 같은 마스크로 전방 0.5유닛 `Raycast`를 쏴 벽도 감지한다. 바닥/벽 레이어 세팅이 비어 있으면 항상 반전(Flip)만 반복하므로 반드시 지정해야 한다.
- `ContactDamage`는 플레이어와 부딪히는 콜라이더(Collision2D 또는 Trigger2D, Stay 이벤트 기준)에 붙이며, 대상은 `GetComponentInParent<PlayerHealth>()`로 찾으므로 Player 루트에 `PlayerHealth`가 있어야 한다.
- 지역별로 `EnemyData` 에셋(`Enemy_Residue`/`Enemy_Gaze`/`Enemy_Fracture`)만 교체하면 색·속도·(균열의 경우) 흔들림이 자동 반영된다.

## 다른 모듈과의 연결

- `Enemy`는 `HiddenWeight.World.IDamageable`(`World/Interactions.cs`)을 구현한다: `bool IsAlive { get; }`, `void TakeDamage(int amount, Vector2 sourcePosition)`. Player의 `PlayerAttack`은 `GetComponentInParent<IDamageable>()`로만 접근하고 `Enemy` 타입을 직접 참조하지 않는다 — Player→Enemies 역참조를 피하기 위한 설계(기획서 3.1절 예외).
- `ContactDamage` → `HiddenWeight.Player.PlayerHealth.TakeDamage(int, Vector2)`를 직접 호출한다. 이는 허용된 의존 방향(Enemies ──▶ Player)이라 문제없다.
- `Enemy`/`EnemyPatrol`은 `HiddenWeight.Data.EnemyData`(ScriptableObject)를 참조해 지역별 수치를 읽는다.

## 의존성 주의

- Enemies → Core, Data, Player 방향만 허용된다. Enemies가 Player의 구체 타입(`PlayerAttack` 등)을 참조하는 것은 괜찮지만, 반대로 Player가 `Enemy`/`EnemyPatrol`/`ContactDamage`를 직접 참조하면 순환 의존이 생기므로 반드시 `IDamageable`을 통해서만 접근하게 유지할 것.
- 새 적 종류를 추가하더라도 새 컴포넌트를 만들기보다 `EnemyData` 필드를 확장하는 방식을 우선 검토한다(현재 구조가 "데이터만 갈아끼우는" 것을 전제로 하고 있음).
- `ContactDamage.damage` 필드(인스펙터 오버라이드)와 `EnemyData.contactDamage`가 동시에 존재하므로, 값을 바꿀 때는 어느 쪽이 우선되는지(`damage != 0`이면 인스펙터 값 우선) 헷갈리지 않도록 주의.
