# Common 모듈 — 공통 시스템

> 기획서 19.1(공통 시스템), 3장(회귀), 6장(플레이어 상태) 대응.
> 낮·밤 어디서나 쓰이는 상태·이동·생명·오디오 기반.

## 파일

| 파일 | 역할 | 기획서 |
|---|---|---|
| `GameState.cs` | 게임 상태·층 단계 enum | 19.1 |
| `RunState.cs` | 한 번의 플레이(런) 동안의 층·스탯·무기·통계. 회귀 시 리셋 | 3.3, 3.2 |
| `GameManager.cs` | 흐름 오케스트레이터. 씬 전환·층 진행·회귀 처리 | 2.1, 3.2 |
| `PlayerController.cs` | WASD 이동(낮·밤 공통), Shift 달리기(밤) | 4.1~4.3 |
| `PlayerHealth.cs` | HP(멘탈)·평판·부활·무적·해고 | 6장 |
| `AudioManager.cs` | SFX/BGM 재생, 사운드 id 상수 | 15.4 |

## 핵심 규칙 구현

- **부활 사이클**(6.3): `PlayerHealth.TakeDamage` → HP 0 → 평판 1 감소 → 2초 후 제자리 부활 → 전체 회복 → 3초 무적. 평판 0이면 `GameManager.OnFired()`로 해고·회귀.
- **피격 무적**(4.4): 피격 후 0.5초간 재피격 불가(`hitInvulnSeconds`).
- **회귀 리셋**(3.3/11.9): `RunState.ResetToFirstDay()` — 층·스탯·무기·평판·레벨 초기화. 결과용 누적 통계는 유지.
- **층 진행**(2.1): `OnDaySurvived` → 4층이면 엔딩, 아니면 밤. `OnNightCleared` → 무기 추가 후 다음 층 낮. `OnNightFailed`/`OnFired` → 1층 회귀.

## 씬 배치

- `GameManager`, `AudioManager`는 `Boot` 씬에 두고 `DontDestroyOnLoad`로 유지.
- `PlayerController` + `PlayerHealth` + `Rigidbody2D`(Dynamic, gravity 0, freeze rotation Z) + `Collider2D`는 플레이어 프리팹에 붙인다.
- 밤 씬의 플레이어는 `PlayerController.SetCanRun(true)`로 달리기 허용.

## 다른 모듈과의 연결

- **Day**: `DayCombatManager`가 생존/해고 시 `GameManager.OnDaySurvived/OnFired` 호출.
- **Night**: `NightStealthManager`가 탈출/발각 시 `GameManager.OnNightCleared/OnNightFailed` 호출.
- **UI**: `PlayerHealth`의 `HpChanged`/`ReputationChanged` 이벤트, `GameManager.StateChanged`를 구독.

## 의존성 주의

`PlayerHealth`는 `GameManager.Instance.Run`을 참조한다. 낮·밤 씬은 반드시 `Boot`을 거쳐 진입하거나, 테스트 시 `GameManager`가 씬에 존재해야 한다.
