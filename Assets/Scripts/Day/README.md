# Day 모듈 — 낮 전투

> 기획서 19.2(낮 전투 시스템), 5장(낮 디펜스), 7장(성장), 10장(상사의 눈치) 대응.

## 파일

| 파일 | 역할 | 기획서 |
|---|---|---|
| `DayCombatManager.cs` | 낮 씬 오케스트레이터. 시스템 연결, 레벨업 정지, 60초 통과 처리 | 5장 |
| `EnemySpawner.cs` | WaveTable 기반 사방 스폰, 동시 개체 수 제한, 종료 시 잔적 제거 | 5.1/5.3 |
| `Enemy.cs` | 적 이동·공격·피격·처리. 행동 유형별 AI, 상태이상 수용 | 9장 |
| `EnemyRegistry.cs` | 살아있는 적 정적 추적(자동 조준·시선·스킬 대상) | — |
| `WaveTimer.cs` | 60초 카운트다운(레벨업 시 timeScale=0으로 정지) | 5.1 |
| `ExperienceSystem.cs` | 경험치·레벨·임계치·레벨업 이벤트 | 7.1 |
| `StatUpgradeSystem.cs` | 강화 3종 후보 롤·선택 적용, 층간 유지 | 7.2/7.3 |
| `BossGaze.cs` | 상사의 시선 맵 기믹, '일하는 척' 상태 | 10장 |
| `Pickups.cs` | 경험치 서류·아메리카노 픽업 | 6.4/7.1 |

> **자동 조준·공격**(19.2)은 무기와 강하게 결합되어 [Weapons 모듈](../Weapons/README.md)의 `AutoAttackSystem`이 담당한다. `EnemyRegistry.Nearest`로 대상을 얻는다.

## 낮 전투 루프

```
DayCombatManager.Start()
  → 현재 층 FloorConfig 선택
  → EnemySpawner.Begin() + WaveTimer.Begin(60)
  → 매 프레임 BossGaze.Tick(elapsed)
적 처리(Enemy.Kill)
  → EnemySpawner.EnemyKilled → 경험치 서류/커피 드롭 + 통계
  → ExpPickup 수집 → ExperienceSystem.AddExp
레벨업(ExperienceSystem.LeveledUp)
  → Time.timeScale = 0 (전투 정지)
  → StatUpgradeSystem.RollOptions() → UI에 3종 전달
  → UI 선택 → DayCombatManager.ResolveLevelUp → timeScale = 1
60초(WaveTimer.Completed)
  → 잔적 제거 연출 → GameManager.OnDaySurvived()
```

## 적 행동 유형(Enemy.behavior)

- **Chaser/Tank/Debuffer/Boss**: 플레이어로 직선 접근(속도만 차이).
- **Dasher**: 사거리 진입 시 예고 후 돌진.
- **Ranged**: 사거리 유지(발사체는 무기 프리팹로 확장).
- 근접형은 접촉 피해(`attackRange == 0`), 원거리형은 접촉 피해 없음.

## 난이도

`FloorConfig`의 `hpMultiplier/speedMultiplier/spawnMultiplier`가 `Enemy.Init`와 `EnemySpawner`에 곱해진다. 기획서 5.5대로 체력보다 생성량 중심으로 올린다.

## 씬 배치(Day 씬)

- `DayCombatManager`(빈 오브젝트) — floors 배열에 Floor1~4 에셋, spawner/timer/bossGaze/upgrades 참조 연결.
- `EnemySpawner`, `WaveTimer`, `BossGaze`, `StatUpgradeSystem` 컴포넌트.
- 플레이어 프리팹(Common) 배치. `AutoAttackSystem`(Weapons) 포함.
- exp/coffee 픽업 프리팹 연결.
