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
