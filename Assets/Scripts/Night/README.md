# Night 모듈 — 밤 잠입

> 기획서 19.3(밤 잠입 시스템), 11장 대응.
> 밤에는 공격 불가. 한 번 발각·시간초과면 즉시 전체 진행 초기화(1층 회귀).

## 파일

| 파일 | 역할 | 기획서 |
|---|---|---|
| `NightStealthManager.cs` | 밤 씬 오케스트레이터. 조사·탈출·발각·타이머 | 11장 |
| `VisionCone.cs` | 부채꼴 시야 판정(벽 차단 Raycast). 경비·야근자·CCTV 공용 | 11.5/11.7 |
| `GuardPatrol.cs` | 경비 순찰(경로 반복, 시작 3초 정지, 소음 확인) | 11.5 |
| `NightWorker.cs` | 야근자(구역 대기 후 예고 없이 이동). 층당 1명 | 11.6 |
| `CCTV.cs` | 좌우 회전 감시 | 11.7 |
| `InvestigationPoint.cs` | 조사 대상. E로 1.5초 조사 | 11.4/8.6 |
| `ExitZone.cs` | 출구. 조사 후 도착 시 자동 탈출 | 11.3/11.4 |
| `NoiseSystem.cs` | 달리기 소음. 경비가 위치 확인(토글 가능) | 11.8 |

## 밤 잠입 루프

```
NightStealthManager.Start()
  → 현재 층 NightConfig 로드, 제한 60초, 달리기 허용
  → 감시자 VisionCone.PlayerSpotted 구독
조사(InvestigationPoint, E 1.5초) → WeaponAcquired = true
출구 도착(ExitZone) + WeaponAcquired → Succeed
  → GameManager.OnNightCleared(rewardWeaponId) → 다음 층 낮
발각(VisionCone) 또는 시간초과 → Fail
  → GameManager.OnNightFailed() → 1층 회귀
```

## 공정성(기획서 11.9/13.3)

한 번의 발각이 전체 초기화로 이어지므로 판정이 예측 가능·공정해야 한다.
- 경비 동선은 `GuardRouteData`로 고정(무작위 배치 금지).
- 시야는 벽(`Obstacle` 레이어)에 Raycast로 차단.
- 시작 후 3초 경비 정지로 파악 시간 제공.
- 시야 영역은 밤 HUD에 표시(UI 모듈).

## 씬 배치(Night 씬)

- `NightStealthManager` — nightConfigs(Night1~3), objective, exit, watchers(모든 VisionCone), noise 연결.
- 경비: `GuardPatrol` + `VisionCone`, `GuardRouteData` 에셋 연결.
- CCTV: `CCTV` + `VisionCone`.
- 벽·책상 콜라이더를 `Obstacle` 레이어로 지정(시야 차단용).
- 플레이어 프리팹 배치, `SetCanRun(true)`는 매니저가 자동 호출. **밤 씬에는 공격 시스템(AutoAttackSystem 등)을 넣지 않는다.**
- 조사 대상에 `InvestigationPoint`(Trigger Collider), 출구에 `ExitZone`(Trigger Collider).

## 레이어 설정

`Obstacle` 레이어를 프로젝트 설정에 추가하고 벽·책상에 지정해야 `VisionCone`/`NoiseSystem`이 정상 동작한다.
