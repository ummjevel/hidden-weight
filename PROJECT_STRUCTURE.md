# HAN GAME — 프로젝트 구조

> [GAME_DESIGN.md](GAME_DESIGN.md) 기획을 Unity로 구현하기 위한 모듈 구조 문서.
> 코드는 `Assets/Scripts/` 아래 모듈로 분리하고, 데이터는 ScriptableObject로 관리한다.

## 1. Unity 프로젝트 설정

| 항목 | 값 |
|---|---|
| 엔진 | Unity 2022.3 LTS 이상 (2D URP 권장) |
| 템플릿 | 2D (Built-in 또는 URP 2D) |
| 카메라 | 2D 탑뷰 (Orthographic). 아트는 쿼터뷰 스프라이트 사용 가능 |
| 물리 | Physics2D |
| 입력 | 기본은 `Input.GetAxisRaw`. 신규 Input System도 호환 가능 |

### 최초 세팅
1. Unity Hub에서 2D 프로젝트 생성.
2. 이 저장소의 `Assets/` 폴더를 프로젝트 `Assets/`에 병합.
3. `Assets/Scenes/`에 씬 3개 생성: `Boot`, `Day`, `Night`.
4. `GameManager` 프리팹을 `Boot` 씬에 배치하고 `DontDestroyOnLoad`로 유지.
5. `Assets/Data/` 아래에서 ScriptableObject 에셋을 생성해 수치를 채운다 ([Data/README.md](Assets/Scripts/Data/README.md) 참고).

## 2. 확정한 P0 기본값

기획서 23장 P0 항목에 대해 아래 권장안으로 개발을 시작한다. 수치는 데이터 에셋에서 조정한다.

| 항목 | 채택값 |
|---|---|
| 카메라 | 2D 탑뷰(스프라이트 쿼터뷰 허용) |
| 평판 기본 개수 | 3 |
| 밤 제한 시간 | 60초 |
| 밤 달리기 소음 시스템 | 포함(토글 가능, `NoiseSystem.enabled`) |
| 플레이어·무기·적·웨이브 수치 | 데이터 에셋 초기값(문서 5·7·8·9장 권장안 반영) |

## 3. 모듈 구조

```
Assets/Scripts/
├── Common/     공통 시스템 (게임 상태, 이동, HP·평판, 층 진행, 오디오, 런 상태)
├── Data/       ScriptableObject 데이터 테이블 (플레이어·무기·적·웨이브·층·경비)
├── Day/        낮 전투 (적 생성, 적 AI, 자동 공격, 경험치·레벨업, 강화, 상사의 시선, 웨이브 타이머)
├── Weapons/    무기·스킬 (키보드 샷건, 스테이플러, 업무 떠넘기기, 퇴사 통보)
├── Night/      밤 잠입 (경비 순찰, 시야 판정, CCTV, 조사, 무기 획득, 탈출, 소음)
└── UI/         HUD 및 화면 (낮 HUD, 밤 HUD, 레벨업, 결과)
```

각 모듈 폴더에는 해당 기능을 설명하는 `README.md`가 있다.

| 모듈 | 문서 | 기획서 대응 |
|---|---|---|
| Common | [Common/README.md](Assets/Scripts/Common/README.md) | 19.1 공통 시스템, 3·6장 |
| Data | [Data/README.md](Assets/Scripts/Data/README.md) | 19.4 데이터 관리 |
| Day | [Day/README.md](Assets/Scripts/Day/README.md) | 19.2 낮 전투, 5·7·10장 |
| Weapons | [Weapons/README.md](Assets/Scripts/Weapons/README.md) | 8장 무기와 스킬 |
| Night | [Night/README.md](Assets/Scripts/Night/README.md) | 19.3 밤 잠입, 11장 |
| UI | [UI/README.md](Assets/Scripts/UI/README.md) | 14장 UI |

## 4. 전체 게임 흐름과 모듈 연결

```text
GameManager(Common)
  ├─ RunState(Common): 현재 층·스탯·무기 보유 상태 저장
  ├─ 낮 진입 → DayCombatManager(Day) 실행
  │     생존 → RunState.floor++ → 밤 진입(1~3층) 또는 최종 클리어(4층)
  │     평판 0 → 해고 → RunState 리셋 → 1층 낮 재시작
  └─ 밤 진입 → NightStealthManager(Night) 실행
        무기 획득 후 탈출 → RunState에 무기 추가 → 다음 층 낮
        발각·시간초과 → RunState 리셋 → 1층 낮 재시작
```

## 5. 개발 순서 권장

1. Common → 게임이 씬 전환·상태를 돌릴 수 있게 한다.
2. Data → 수치 에셋을 만든다.
3. Day → 낮 전투 루프를 먼저 완성한다(핵심 재미).
4. Weapons → 낮 전투에 무기를 붙인다.
5. Night → 밤 잠입 루프를 붙인다.
6. UI → HUD와 결과 화면.

기획서 24장의 후속 기획 문서(`COMBAT_BALANCE.md`, `WAVE_TABLE.md` 등)는 Data 모듈의 에셋 수치로 대체·연동한다.
