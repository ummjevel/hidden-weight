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
- [ ] PlayerController (이동, 자동 최근접 타겟팅)
- [ ] StatSystem (업무처리력/손속도/눈치/멘탈관리/일머리/짬)
- [ ] ReputationManager (HP0→평판-1→부활→3초 무적)
- 검증: `Unity -batchmode -runTests -testPlatform EditMode`
- done-when: 관련 EditMode 테스트 전부 통과
- 상태: pending

## M4: 무기 시스템
- [ ] 키보드 샷건 (부채꼴 다중 타겟)
- [ ] 스테이플러 연사 (관통형 최근접)
- [ ] 업무 떠넘기기 (액티브, 쿨타임 12초, 넉백)
- [ ] 퇴사 통보 (궁극기, 공포/슬로우/보스정지, 게이지 충전)
- 검증: EditMode 테스트 (쿨타임/데미지/게이지 계산)
- done-when: 4개 무기 로직 테스트 통과
- 상태: pending

## M5: 적 & 스폰
- [ ] EnemyBase + 5종 (이메일봉투/서류더미/포스트잇/회의요청달력/클레임전화기)
- [ ] SpawnManager (층별 시간대 스폰 테이블)
- 검증: EditMode 스폰 타이밍 테스트
- done-when: 5종 적 로직 + 스폰 테이블 테스트 통과
- 상태: pending

## M6: 낮 디펜스 루프
- [ ] DayWaveManager (60초 타이머)
- [ ] 경험치 드롭 + 레벨업 3택1 UI (시간정지)
- [ ] 상사의 눈치 상태머신 (층별 변형)
- 검증: 60초 시뮬레이션 테스트 정상 종료
- done-when: 웨이브 사이클 시뮬레이션 통과
- 상태: pending

## M7: 밤 잠입
- [ ] NightManager (60초 제한)
- [ ] 경비 2 + CCTV 1 시야 판정
- [ ] 조사 상호작용 (무기 3종 순차 해금) + 출구 탈출 판정
- [ ] 발각/시간초과 페널티 (무기 미획득 + 평판-1 + 강제 층이동)
- 검증: EditMode 시야 판정 테스트
- done-when: 시야/탈출 로직 테스트 통과
- 상태: pending

## M8: CEO 웨이브 & 엔딩
- [ ] BossWaveManager (0~20/20~40/40~60초 3단계 패턴)
- [ ] 빨간 구역 지속피해, 퇴사통보 3초 정지
- [ ] 엔딩 트리거
- 검증: EditMode 테스트 + 통합 실행 로그
- done-when: 보스 패턴 로직 테스트 통과 + 엔딩 트리거 확인
- 상태: pending

## M9: 밸런싱 확정 & 통합 검증
- [ ] docs/DEVELOPMENT_PLAN.md 밸런스 표 TBD → 실수치 확정
- [ ] ScriptableObject 데이터 반영
- [ ] 전체 EditMode 테스트 스위트 통과
- [ ] 배치모드 빌드 성공
- 검증: `Unity -batchmode -runTests` 전체 스위트 + `-buildTarget` 빌드
- done-when: 테스트 전부 통과, 빌드 산출물 생성 확인
- 상태: pending
