# Data 모듈 — 데이터 테이블(ScriptableObject)

> 기획서 19.4 대응. 수치를 코드에 넣지 않고 에셋으로 관리한다(기획서 권장).
> 24장의 후속 문서(`COMBAT_BALANCE.md`, `WAVE_TABLE.md`)는 이 에셋들이 대체한다.

## ScriptableObject 목록

| 파일 | 메뉴 경로 | 담는 수치 |
|---|---|---|
| `PlayerData.cs` | HanGame/Player Data | 최대 HP, 이동속도, 평판, 경험치 곡선, 커피 회복 |
| `WeaponData.cs` | HanGame/Weapon Data | 무기·스킬 수치(4종). `id`는 `WeaponIds`와 일치 |
| `UpgradeData.cs` | HanGame/Upgrade Data | 스탯 강화 1종(6종 생성) |
| `EnemyData.cs` | HanGame/Enemy Data | 적 1종 기본 수치(6종 생성) |
| `WaveTable.cs` | HanGame/Wave Table | 층별 60초 생성표 |
| `FloorConfig.cs` | HanGame/Floor Config | 층 낮 설정(배수·상사의 시선) |
| `NightConfig.cs` | HanGame/Night Config | 밤 제한 시간·조사 보상·소음 |
| `GuardRouteData.cs` | HanGame/Guard Route | 경비 경로·시야 |

`EnemyType.cs`는 enum 정의(적 종류/행동)로 SO가 아니다.

## 만들어야 할 에셋(권장 폴더)

```
Assets/Data/
├── Player/PlayerData.asset
├── Weapons/  KeyboardShotgun, StaplerRapid, TaskDelegate, ResignationNotice
├── Upgrades/ AttackPower, AttackSpeed, MoveSpeed, MaxHp, AttackRange, ActiveCooldown
├── Enemies/  EmailEnvelope, PaperStack, UrgentPostit, MeetingCalendar, ClaimPhone, CeoDirective
├── Floors/   Floor1~Floor4 (FloorConfig) + Wave1~Wave4 (WaveTable)
└── Night/    Night1~Night3 (NightConfig) + GuardRoute 에셋들
```

## 초기 수치 가이드(기획서 반영)

- **난이도 배수**(FloorConfig, 5.5): 1층 100/100/100 → 4층 170/115/180(HP/속도/생성량). 체력보다 생성량 중심.
- **강화 6종**(UpgradeData, 7.2): 업무처리력 +15%×5, 손속도 +12%×5, 눈치 +8%×3, 멘탈관리(HP+20%,회복20%)×3, 일머리 +10%×3, 짬 -10%×3(`requiresWeaponId = "task_delegate"`).
- **무기**(WeaponData, 8장): 키보드 샷건(부채꼴 5발/60°, 느림·넓음), 스테이플러(단일 직선, `pierces=false`), 업무 떠넘기기(반경3/쿨12), 퇴사 통보(공포3초/CEO정지3초).
- **웨이브**(WaveTable, 5.3): 0~15초 기본 → 15~30초 종류 추가 → 30~45초 조합 강화 → 45~60초 정예. `maxAlive`로 동시 개체 수 제한.
- **밤**(NightConfig, 11.4): 제한 60초, 조사 1.5초. 보상은 층별 무기 id.

## 코드에서 읽는 곳

- `EnemySpawner` ← `FloorConfig` + `WaveTable`
- `AutoAttackSystem`/무기 ← `WeaponData`
- `StatUpgradeSystem` ← `UpgradeData[]`
- `NightStealthManager` ← `NightConfig`
- `GuardPatrol` ← `GuardRouteData`
