# Weapons 모듈 — 무기와 스킬

> 기획서 8장 대응. 자동 무기 2종 + 액티브 1종 + 궁극기 1종.
> 무기 수치는 [Data 모듈](../Data/README.md)의 `WeaponData` 에셋에서 관리.

## 파일

| 파일 | 무기 | 입력 | 기획서 |
|---|---|---|---|
| `AutoAttackSystem.cs` | 키보드 샷건·스테이플러(자동) | 자동 | 4.3/8.2/8.3 |
| `Projectile.cs` | 공용 발사체(관통/비관통) | — | 4.4/8.3 |
| `TaskDelegateSkill.cs` | 업무 떠넘기기(액티브) | `Space` | 8.4 |
| `ResignationUltimate.cs` | 퇴사 통보(궁극기) | `R` | 8.5 |

## 무기별 동작

- **키보드 샷건**(시작): 가장 가까운 적 방향 부채꼴 다발(`pellets`/`spreadAngle`). 느리지만 넓음. 발사마다 대상 재탐색.
- **스테이플러 연사**(1층 밤): 단일 직선 발사체, `pierces=false`로 첫 적 적중 시 소멸. 빠른 단일 대상.
- **업무 떠넘기기**(2층 밤): 반경 내 적을 바깥으로 밀어냄. 쿨타임 12초(권장), '짬' 강화로 감소.
- **퇴사 통보**(3층 밤): 일반 적 공포(도주), 정예 감속, CEO 지시서 3초 정지. 처리 시 게이지 충전, 가득 차면 사용.

## 획득/보유 판정

각 무기는 `RunState.HasWeapon(id)`로 보유 여부를 확인해 동작한다. `id`는 `WeaponIds` 상수와 `WeaponData.id`가 일치해야 한다. 밤 탐방 성공 시 `GameManager.OnNightCleared(weaponId)`가 무기를 추가한다.

## 자동 조준

`AutoAttackSystem`은 `EnemyRegistry.Nearest(playerPos, range)`로 매 발사 시 최근접 적을 다시 찾는다(마우스 조준 없음, 기획서 4.3). `PlayerStats`의 공격력/공격속도/사거리 배수를 반영한다.

## 상사의 시선 연동

`AutoAttackSystem`은 `BossGaze.PlayerCaught`가 true면 발사를 멈춘다('일하는 척', 기획서 10.2). 이동은 계속 가능.

## 씬 배치

플레이어 프리팹(또는 하위 오브젝트)에 4개 컴포넌트를 붙이고, 각 `WeaponData` 에셋과 프리팹(발사체)을 인스펙터에서 연결. `ResignationUltimate.spawner`에 `EnemySpawner`를 연결하면 처리 이벤트로 게이지가 찬다.
