# UI 모듈

게임의 화면 UI를 담당하는 스크립트 모음이다. 타이틀(BootMenu), 낮 전투 HUD(DayHUD), 레벨업 강화 선택 패널(LevelUpUI), 밤 잠입 HUD(NightHUD), 결과 화면(ResultScreen)으로 구성된다. 대부분 `FindObjectOfType`으로 게임플레이 시스템을 찾아 이벤트를 구독하고, `UnityEngine.UI`의 `Text`·`Slider`·`Image`·`Button`을 갱신하는 방식으로 동작한다.

## BootMenu.cs

- **역할**: 타이틀/부팅 화면. 첫 출근(게임 시작)과 종료 버튼을 제공하는 진입점.
- **상속/의존**: `MonoBehaviour`. `UnityEngine.UI.Button`. `HanGame.Common.GameManager` 참조.
- **주요 멤버**:
  - SerializeField: `startButton`, `quitButton`(Button).
- **동작**:
  - `Start`에서 각 버튼에 리스너 등록.
  - `OnStart`: `GameManager.StartNewRun()`으로 1층 낮 시작.
  - `OnQuit`: 에디터에서는 플레이 종료, 빌드에서는 `Application.Quit()`.

## DayHUD.cs

- **역할**: 낮 전투 HUD. HP·평판·층·남은시간·경험치·쿨타임·궁극기 게이지·상사의 시선 경고를 표시.
- **상속/의존**: `MonoBehaviour`. `UnityEngine.UI`(Slider, Image, Text, GameObject). `HanGame.Common`(Player, GameManager, PlayerHealth), `HanGame.Day`(ExperienceSystem, WaveTimer, TaskDelegateSkill, ResignationUltimate, BossGaze) 시스템을 참조.
- **주요 멤버**:
  - SerializeField: `hpBar`(Slider), `reputationBadges`(Image[] — 사원증 3칸), `floorText`/`timeText`/`levelText`(Text), `expBar`(Slider), `delegateCooldownFill`/`ultimateGaugeFill`(Image, fillAmount), `bossGazeWarning`(GameObject).
  - 내부: `_health`, `_exp`, `_timer`, `_delegate`, `_ultimate`, `_bossGaze` 시스템 참조.
- **동작**:
  - `Start`: 플레이어 및 각 시스템을 찾아 캐싱하고 이벤트 구독 — `HpChanged`, `ReputationChanged`, `ExpChanged`, `BossGaze.WarningRaised`. 경고를 끄고 층 텍스트 초기화.
  - `Update`: 매 프레임 남은 시간(`Ceil`), 업무 떠넘기기 쿨타임 fill, 궁극기(퇴사 통보) 게이지 fill, 레벨 텍스트 갱신.
  - `OnHp`: HP 비율로 슬라이더 갱신. `OnReputation`: 남은 평판 수만큼 사원증 배지 `enabled` 토글. `OnExp`: 다음 레벨까지 비율로 경험치 바 갱신.
  - `OnBossWarning`: `FlashWarning` 코루틴으로 경고를 2초간 표시 후 숨김.

## LevelUpUI.cs

- **역할**: 레벨업 시 서로 다른 강화 3개를 제시하고, 선택 결과를 전투 매니저에 전달하는 패널.
- **상속/의존**: `MonoBehaviour`. `UnityEngine.UI`(GameObject, Text, Image, Button). `HanGame.Data.UpgradeData`, `HanGame.Day.DayCombatManager`에 의존.
- **주요 멤버**:
  - 중첩 struct `OptionSlot { root, title, description, icon, button }`.
  - SerializeField: `panel`(GameObject), `slots`(OptionSlot[] — 3개).
  - 내부: `_manager`(DayCombatManager), `_current`(List<UpgradeData> 현재 옵션).
- **동작**:
  - `Start`: `DayCombatManager`를 찾아 `LevelUpOffered` 이벤트에 `Show`를 구독하고 패널을 숨김.
  - `Show(options)`: 패널을 켜고 슬롯별로 옵션 유무에 따라 `root` 활성화, title/description/icon 채움. 버튼 리스너를 초기화한 뒤 캡처한 인덱스로 `Pick` 등록.
  - `Pick(index)`: 선택 옵션을 확정, 패널을 닫고 `DayCombatManager.ResolveLevelUp(picked)` 호출. 시간제한은 없으며 시간 정지는 전투 매니저가 처리.

## NightHUD.cs

- **역할**: 밤 잠입 HUD. 남은시간·목표·무기 획득 여부·평판을 표시하고, 목표·출구를 화면상 방향 마커로 안내.
- **상속/의존**: `MonoBehaviour`. `UnityEngine.UI`(Text, GameObject, Image, RectTransform). `HanGame.Common.GameManager`, `HanGame.Night.NightStealthManager`에 의존. `Camera`로 월드→스크린 투영.
- **주요 멤버**:
  - SerializeField: `timeText`/`objectiveText`(Text), `weaponAcquiredMark`(GameObject), `objectiveMarker`/`exitMarker`(RectTransform 화면 마커), `objectiveWorld`/`exitWorld`(Transform 월드 대상), `worldCamera`(Camera), `reputationBadges`(Image[]).
  - 내부: `_manager`(NightStealthManager).
- **동작**:
  - `Start`: `NightStealthManager`를 찾아 `TimeTicked`, `WeaponInvestigated` 구독. 무기 마크를 숨기고, 카메라 미지정 시 `Camera.main` 사용. `Run.Reputation` 수만큼 평판 배지 표시.
  - `Update`: `UpdateMarker`로 목표·출구 월드 위치를 매 프레임 스크린 좌표로 투영해 마커 위치 갱신.
  - `OnTime`: 남은 시간을 올림해 표시(음수는 0). `OnWeaponAcquired`: 무기 획득 마크 활성화.

## ResultScreen.cs

- **역할**: 런 종료(결과) 화면. 최종 성과 요약을 보여주고 재도전 버튼을 제공.
- **상속/의존**: `MonoBehaviour`. `UnityEngine.UI`(GameObject, Text, Button). `HanGame.Common`(GameManager, GameState) 참조.
- **주요 멤버**:
  - SerializeField: `panel`(GameObject), `summaryText`(Text), `retryButton`(Button).
- **동작**:
  - `Start`: `GameManager.StateChanged`를 구독하고 패널을 숨김, 재도전 버튼에 리스너 등록.
  - `OnState`: 상태가 `GameState.Result`가 되면 `Show` 호출.
  - `Show`: `Run` 데이터로 요약 텍스트 구성 — 도달 층, 처리한 업무, 보유 무기 종수, 최종 레벨, 남은 평판, 야간 탐방 성공 횟수, 플레이 시간(초).
  - `OnRetry`: 패널을 닫고 `GameManager.StartNewRun()`으로 재시작.

