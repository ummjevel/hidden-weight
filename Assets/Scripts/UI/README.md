# UI 모듈 — HUD와 화면

> 기획서 14장 대응. UGUI(Text/Image/Slider/Button) 기반. TextMeshPro로 교체 가능.

## 파일

| 파일 | 역할 | 기획서 |
|---|---|---|
| `BootMenu.cs` | 타이틀. 첫 출근 시작/종료 | 2.1 |
| `DayHUD.cs` | 낮 HUD(HP·평판·층·시간·경험치·쿨타임·게이지·시선 경고) | 14.1 |
| `NightHUD.cs` | 밤 HUD(시간·목표·목표/출구 마커·무기 획득·평판) | 14.2 |
| `LevelUpUI.cs` | 강화 3종 선택 패널 | 7.3 |
| `ResultScreen.cs` | 결과 화면 + 재시작 | 14.4 |

## 이벤트 구독 구조

UI는 로직을 갖지 않고 시스템 이벤트를 구독해 표시만 한다.

- `DayHUD` ← `PlayerHealth.HpChanged/ReputationChanged`, `ExperienceSystem.ExpChanged`, `WaveTimer.Remaining`, `TaskDelegateSkill`, `ResignationUltimate.Gauge`, `BossGaze.WarningRaised`
- `NightHUD` ← `NightStealthManager.TimeTicked/WeaponInvestigated`, 목표/출구 Transform 투영
- `LevelUpUI` ← `DayCombatManager.LevelUpOffered` → 선택 시 `ResolveLevelUp`
- `ResultScreen` ← `GameManager.StateChanged == Result`

## 평판 표현(기획서 14.3)

`reputationBadges` 이미지 배열(사원증 3칸)을 남은 평판 수만큼 `enabled`로 켠다. 스프라이트를 정상/주의/경고/해고 도장으로 교체하면 기획서 표현과 일치.

## 씬 배치

- Boot 씬: Canvas + `BootMenu`.
- Day 씬: Canvas + `DayHUD` + `LevelUpUI`(패널 비활성 시작) + `ResultScreen`.
- Night 씬: Canvas + `NightHUD`.
- 각 필드는 인스펙터에서 대응 UI 요소를 연결. 미연결 필드는 null 안전 처리되어 있어 부분 구현도 동작.

## 밤 마커

`NightHUD`의 목표/출구 마커는 월드 좌표를 `WorldToScreenPoint`로 투영해 위치를 잡는 간단 버전이다. 화면 밖 방향 화살표가 필요하면 화면 경계로 클램프하도록 확장한다(기획서 13.3 목표·출구 항상 표시).
