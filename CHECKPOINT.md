# CHECKPOINT — Rookie to CEO

계획 원본: `~/.claude/plans/lovely-tickling-forest.md`
로드맵 상세: `docs/DEVELOPMENT_PLAN.md`

## M0: 문서/리포 초기화
- [x] docs/GDD.md 작성
- [x] docs/DEVELOPMENT_PLAN.md 작성
- [x] git init + .gitignore
- [x] CHECKPOINT.md, AUDIT.log 생성
- 검증: 파일 존재 확인 + `git status`
- done-when: 전부 존재하고 git 리포로 초기 커밋 가능
- 상태: done

## M1: Unity 설치
- [x] `brew install --cask unity-hub`
- [x] `open -a "Unity Hub"`로 GUI 실행
- [x] (사용자) Unity Hub 로그인 + Unity 6000.5.4f1 Editor 설치
- 검증: `/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app` 존재 확인됨
- done-when: Editor 실행 파일 경로 확인됨
- 상태: done

## M2: 프로젝트 생성 & 자동화 파이프라인
- [x] `Unity -batchmode -createProject RookieToCEO -quit`
- [x] Assets/{Scripts,Editor,Prefabs,Scenes,ScriptableObjects} 구조
- [x] Editor 자동화 스크립트 골격 (PackageSetup, PackageQuery, SceneBuilder)
- [x] 필수 패키지 설치 (URP 17.5.0, Input System 1.20.0, Test Framework 1.7.0, 2D Sprite 1.0.0, 2D Tilemap 1.0.0)
- [x] Day/Night/Boss 빈 씬 3개 생성
- 검증: 배치모드로 프로젝트가 에러 없이 열림 (m2_finalimport.log, m2_scenebuilder.log에 error CS 없음)
- done-when: 프로젝트 폴더 + 자동화 스크립트 골격 존재, 배치모드 실행 에러 0
- 상태: done
- 비고: `Client.Add`를 패키지마다 순차 호출하는 최초 방식은 도메인 리로드로 콜백 구독이 끊겨
  무한 대기에 빠졌다. `Client.Search`로 버전만 조회 → manifest.json 직접 반영 → 순수
  배치 실행으로 우회했고, PackageSetup.cs는 `AddAndRemove` + `[InitializeOnLoad]`로 재작성.

## M3: 코어 시스템
- [x] PlayerController (WASD 이동, HP/평판·스탯 보관)
- [x] StatSystem (업무처리력/손속도/눈치/멘탈관리/일머리/짬)
- [x] ReputationSystem (HP0→평판-1→부활→3초 무적)
- [x] TargetingUtility (최근접 타겟팅 계산, M4 무기 시스템에서 재사용 예정)
- [x] RookieToCEO.Runtime.asmdef 분리 + EditMode 테스트 asmdef 연결
- 검증: `Unity -batchmode -runTests -testPlatform EditMode` → 16/16 통과
- done-when: 관련 EditMode 테스트 전부 통과
- 상태: done
- 비고: 커스텀 asmdef는 자동 생성 Assembly-CSharp을 참조할 수 없어서 처음엔
  컴파일 에러가 났다. Assets/Scripts 아래에 RookieToCEO.Runtime.asmdef를 만들고
  테스트 asmdef가 그걸 참조하도록 고쳐서 해결.

## M4: 무기 시스템
- [x] 키보드 샷건 (부채꼴 다중 타겟, KeyboardShotgunWeapon)
- [x] 스테이플러 연사 (좁은 직선 관통형, StaplerRapidFireWeapon)
- [x] 업무 떠넘기기 (액티브, 쿨타임 12초 + 짬 스탯 반영, 넉백, WorkDumpSkill)
- [x] 퇴사 통보 (궁극기, 공포/슬로우/보스정지, 게이지 충전, ResignationUltimate)
- [x] 공용 로직: Cooldown, Gauge, ConeTargetingUtility, WeaponMath
- [x] EnemyRegistry (M5 적 연동을 위한 서비스 로케이터 골격)
- 검증: `Unity -batchmode -runTests -testPlatform EditMode` → 34/34 통과 (M3 16 + M4 18)
- done-when: 4개 무기 로직 테스트 통과
- 상태: done

## M5: 적 & 스폰
- [x] EnemyBase(공통: HP/데미지/넉백/공포/슬로우/접촉데미지) + 5종
      (EmailEnvelope/DocumentStack/PostItRush/MeetingCalendar/ClaimPhone)
- [x] DashState (포스트잇 돌진형 순수 상태머신)
- [x] WaveSpawnTable (층별 시간대 등장 규칙 + 생성속도 배율, 순수 로직)
- [x] SpawnManager (WaveSpawnTable 결과로 실제 Instantiate)
- [x] PlayerController에 공격속도 디버프(회의요청 달력용) 추가
- 검증: `Unity -batchmode -runTests -testPlatform EditMode` → 47/47 통과 (누적)
- done-when: 5종 적 로직 + 스폰 테이블 테스트 통과
- 상태: done
- 비고: 적 프리팹(스프라이트)은 아직 없음 - docs/DEVELOPMENT_PLAN.md 아트 파이프라인 방침대로
  M9 전까지는 프로그래머 아트로 대체 예정. SpawnManager는 프리팹이 비어있으면 조용히 스킵.
  테스트 메서드명이 숫자로 시작하면(예: "30초_...") CS1519 컴파일 에러가 난다는 것을 확인.

## M6: 낮 디펜스 루프
- [x] DayWaveManager (60초 타이머, SpawnManager/BossGlance/LevelSystem 통합)
- [x] LevelSystem + StatChoiceGenerator (경험치 → 레벨업 → 3택1 선택지, 시간정지는 IsPaused로 구현)
- [x] BossGlanceSystem (상사의 눈치, 층별 변형: 1층 느림/2층 넓은 시야/3층 두 번 이동)
- [x] "일하는 척" 상태를 PlayerController.IsPretendingToWork로 무기에 실제 연결(자동공격 정지)
- [x] 적 처치 시 경험치/게이지 즉시 지급(물리적 서류 줍기는 M9 폴리싱으로 단순화)
- 검증: `Unity -batchmode -runTests -testPlatform EditMode` → 62/62 통과 (누적)
- done-when: 웨이브 사이클 시뮬레이션 통과
- 상태: done
- 비고: 3택1 실제 UI(Canvas/버튼)는 아직 없음 - PendingChoices/ResolveStatChoice()로 로직은
  완결. Random 타입이 UnityEngine/System 양쪽에 있어 모호성 에러(CS0104) 발생 → System.Random
  으로 명시. BossGlanceSystem 최초 구현은 큰 deltaTime 한 번에 여러 단계를 못 건너뛰어서
  경계 테스트가 깨졌고, 남은 시간을 이어서 처리하는 캐스케이드 루프로 재작성.

## M7: 밤 잠입
- [x] NightMissionState (60초 제한, 조사/탈출/발각/타임아웃 상태머신, 순수 로직)
- [x] VisionSensorBase(+GuardSensor/CctvSensor) - M4의 ConeTargetingUtility 재사용해 시야 판정
- [x] InvestigationPoint(E 상호작용) + ExitPoint(트리거 탈출)
- [x] NightManager: 발각/시간초과 -> ReputationSystem.LoseReputationDirectly() 페널티,
      성공 -> weaponRewardComponent 활성화(무기 실보유 확정)
- 검증: `Unity -batchmode -runTests -testPlatform EditMode` → 71/71 통과 (누적)
- done-when: 시야/탈출 로직 테스트 통과
- 상태: done
- 비고: "다음 층으로 강제 이동"은 층 진행/씬 전환 매니저가 아직 없어 NightManager의
  OnMissionFinished 이벤트를 훅 포인트로만 남겨둠(추후 GameFlow 매니저에서 연결).
  숫자로 시작하는 테스트 메서드명 실수를 세 번째로 반복 - 재발 방지용 메모리 기록함.

## M8: CEO 웨이브 & 엔딩
- [x] BossWaveState (0~20/20~40/40~60초 3단계, 순수 로직) + BossWaveManager
      (M5 SpawnManager 재사용, floor=4 WaveSpawnTable 그대로 활용)
- [x] CeoFinalOrderBoss (무적 소환수, EnemyBase.TakeDamage virtual화로 오버라이드)
- [x] HazardZone (빨간 구역 지속피해), BossGlanceSystem.ForceTrigger (4층 추가 발동)
- [x] IBossPausable로 퇴사통보 3초 정지 연결
- [x] EndingTrigger (BossWaveManager.OnWaveSuccess 구독, 엔딩 연출은 M9 폴리싱 예정)
- 검증: `Unity -batchmode -runTests -testPlatform EditMode` → 78/78 통과 (누적)
- done-when: 보스 패턴 로직 테스트 통과 + 엔딩 트리거 확인
- 상태: done

## M9: 밸런싱 확정 & 통합 검증
- [x] docs/DEVELOPMENT_PLAN.md 밸런스 표 TBD → 실수치 확정 (무기/적/스폰/레벨업 전부)
- [x] BalanceData ScriptableObject 신설 + 무기 2종/스킬 2종/적 5종에 옵션 오버라이드로 연결
      (`Assets/ScriptableObjects/BalanceData.asset` 실제 생성, Editor 자동화로 무인 생성)
- [x] EnemyBase.ApplyBalanceOverride() 가상 메서드 패턴으로 하위 5종 통일
- [x] 전체 EditMode 테스트 스위트 통과 (78/78, 회귀 없음)
- [x] 배치모드 빌드 성공 (StandaloneOSX, ~113MB, 에러 0)
- 검증: `Unity -batchmode -runTests -testPlatform EditMode` (78/78) + `-executeMethod BuildScript.BuildMac`
- done-when: 테스트 전부 통과, 빌드 산출물 생성 확인
- 상태: done

---

## MVP 전체 완료 (M0~M9)

Rookie to CEO MVP 범위(docs/GDD.md 14번) 전체가 코드 레벨로 구현·검증되었다.
남은 것은 GUI 의존 통합 작업(씬에 실제 GameObject/프리팹 배치, 도트 스프라이트 교체,
3택1 UI 실제 배치)이며, 이는 사용자가 Unity Editor를 직접 열어 진행하거나 별도 세션에서
Editor 자동화 스크립트를 추가로 작성해 이어갈 수 있다.

## M9 이후 폴리싱: Day 씬 프리팹 배치

- [x] Player 프리팹 (Rigidbody2D/Collider2D/PlayerController/무기 4종, 스테이플러·업무 떠넘기기·
      퇴사 통보는 GDD 12번대로 처음엔 비활성 → NightManager가 조사 성공 시 활성화)
- [x] 적 프리팹 5종 (이메일봉투/서류더미/포스트잇/회의요청달력/클레임전화기)
- [x] 플레이스홀더 아트: 코드로 생성한 흰색 32x32 PNG(Assets/Art/Placeholder/Square.png)를
      SpriteRenderer.color로 색만 다르게 틴트 (Unity 내장 리소스 이름을 추측하다 실패해서
      직접 텍스처를 만드는 방식으로 전환)
- [x] Day 씬에 Player, EnemyRegistry, SpawnManager(floor=1, 적 5종 매핑), DayWaveManager 배치,
      메인 카메라 orthographic 전환
- 검증: 배치 스크립트 실행 로그(에러 0) + EditMode 테스트 78/78 유지(회귀 없음)
- 비고: 스크립트를 재실행하면 Player/매니저가 중복 생성되는 문제가 있어
  `DestroyIfExists()`로 항상 정리 후 재생성하도록 멱등성 확보. 자동화 스크립트:
  `Assets/Editor/PrefabAndSceneBuilder.cs`.

## M9 이후 폴리싱: Night 씬 프리팹 배치

- [x] Guard(경비원) 프리팹 2개 배치, 서로 다른 방향(위쪽/왼쪽)을 보도록 회전
- [x] Cctv 프리팹 1개, 넓은 시야각(55°)으로 플레이어 시작점 방향을 감시
- [x] InvestigationPoint(조사 대상) 1개, ExitPoint(출구) 1개, DeskObstacle(책상 장애물) 3개
- [x] Player 인스턴스 재사용(Day 씬과 동일 프리팹) - 밤에는 자동 공격이 필요 없어
      KeyboardShotgunWeapon을 비활성화
- [x] NightManager 배치, weaponRewardComponent를 StaplerRapidFireWeapon으로 연결
      (GDD 12번: 1층 밤 보상 = 스테이플러 연사)
- 검증: 배치 스크립트 실행 로그(에러 0), 프리팹 인스턴스 9개(Player+조사+출구+경비2+CCTV+책상3)
  확인, EditMode 테스트 78/78 유지
- 자동화 스크립트: `Assets/Editor/NightScenePrefabBuilder.cs` (Day 씬 스크립트와 같은 패턴)

## M9 이후 폴리싱: Boss 씬 프리팹 배치

- [x] CeoFinalOrderBoss 프리팹(무적 소환수, 검은색·1.6배 크기로 구분, summonPrefab=이메일봉투)
- [x] HazardZone 프리팹(반투명 빨간 정사각형, 트리거 콜라이더)
- [x] Player 인스턴스 재사용 - 4층은 1~3층 밤을 전부 통과했다는 전제라 스테이플러/업무
      떠넘기기/퇴사 통보를 전부 활성화(Day/Night와 달리 전 무기 사용 가능)
- [x] EnemyRegistry, SpawnManager(floor=4, 이메일/서류/포스트잇/회의요청/CEO 5종 매핑),
      BossWaveManager(hazardZonePrefab 연결), EndingTrigger(BossWaveManager와 같은 오브젝트) 배치
- 검증: 배치 스크립트 실행 로그(에러 0), 프리팹 인스턴스 1개(Player, 나머지는 런타임 동적
  스폰) 확인, EditMode 테스트 78/78 유지
- 자동화 스크립트: `Assets/Editor/BossScenePrefabBuilder.cs`
- 이걸로 Day/Night/Boss 세 씬 모두 프리팹 배치 완료. 남은 폴리싱은 3택1 레벨업 UI와
  실제 도트 스프라이트 교체.

## M9 이후 폴리싱: 3택1 레벨업 UI

- [x] `LevelUpChoiceUI` (Assets/Scripts/UI) - DayWaveManager.IsPaused/PendingChoices를 보고
      패널을 표시하고, 버튼 클릭 시 ResolveStatChoice() 호출. StatType별 GDD 4번 표시 명칭
      (업무처리력/손속도/눈치/멘탈 관리/일머리/짬) 매핑 포함
- [x] `LevelUpUIBuilder` (Editor 자동화) - Canvas/EventSystem/패널/버튼 3개를 코드로 만들어
      Day 씬에 배치
- [x] com.unity.ugui 패키지(2.5.0) 신규 설치 + RookieToCEO.Runtime.asmdef에 "UnityEngine.UI"
      참조 추가
- 검증: 배치 스크립트 실행 로그(에러 0), Day 씬에 LevelUpCanvas/EventSystem/ChoiceButton0~2
  확인, EditMode 테스트 78/78 유지
- 비고: `com.unity.modules.ui`(엔진 내장 모듈)만으로는 Button/Text를 못 쓴다는 걸 처음 알았다 -
  그건 저수준 렌더링(Canvas/RectTransform)만 제공하고, 실제 UI 컴포넌트는 별도 패키지
  `com.unity.ugui`(어셈블리명 `UnityEngine.UI`)에서 온다. 컴파일 에러로 발견해서 M2와 같은
  방식(PackageQuery로 버전 조회 → manifest.json 직접 반영 → 순수 배치 임포트)으로 설치했다.
  EventSystem에는 새 Input System 전용 프로젝트라 레거시 StandaloneInputModule 대신
  InputSystemUIInputModule을 붙여야 버튼 클릭이 동작한다.

## M9 이후 폴리싱: 밸런스 이슈 수정

Unity Editor로 프로젝트를 열어 점검한 결과, 실제 수치를 계산해 발견한 문제 2가지를 고쳤다.

- [x] **층별 스폰 밀도 차등 추가**: `WaveSpawnTable.GetBaseSpawnIntervalSeconds(floor)` 신설
      (1층 2.5초 → 2층 2.0초 → 3층 1.5초 → 4층 1.2초). 이전에는 모든 층이 같은 기준 간격(2초)을
      써서 GDD 5번이 요구하는 "1층 적음 → 3층 크게 증가" 난이도 곡선이 전혀 반영 안 되고 있었다.
      `SpawnManager`가 이 값을 읽도록 수정.
- [x] **스테이플러 연사 레벨업 연결**: `StaplerRapidFireWeapon`이 `PlayerController.Level.OnLevelUp`
      을 구독해 플레이어가 레벨업할 때마다 함께 레벨업하도록 연결. 이전에는 `LevelUp()`을
      호출하는 코드가 어디에도 없어 GDD 3번 "레벨이 오르면 연사속도와 관통 증가"가 죽은
      기능이었다. 트리거 방식(플레이어 레벨과 연동)은 GDD에 명시가 없어 판단해 정함.
- 검증: `Unity -batchmode -runTests -testPlatform EditMode` → 80/80 통과
- 비고: 스테이플러 레벨업을 검증하려고 MonoBehaviour(`AddComponent`)로 EditMode 테스트를
  작성했는데, **EditMode 테스트는 Awake/OnEnable/Update 같은 MonoBehaviour 생명주기를
  전혀 실행하지 않는다는 걸 확인**(Debug.Log로 직접 검증, 한 번도 안 찍힘). 이 프로젝트가
  지금까지 지켜온 대로 MonoBehaviour는 EditMode에서 직접 테스트하지 않고 순수 로직만
  테스트하는 원칙을 재확인하고 해당 테스트는 삭제. PlayMode 테스트가 있어야 검증 가능한
  영역이라 이번엔 보류.
