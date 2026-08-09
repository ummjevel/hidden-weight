[English](README.en.md)

# Hidden Weight (눈뜨는 꿈)

> 꿈속 세계를 탐험하며 다양한 감정을 경험하고, 그 감정들이 플레이와 세계를 변화시키는 메트로배니아 게임.
> 감정을 통해 성장하며 최종적으로 '자아'를 찾아 현실로 나아가는 여정을 그린다.

## 플레이 및 영상 링크

| 항목 | 링크 |
|---|---|
| 플레이 링크 / 설치 파일 | https://ummjevel.itch.io/hidden-weight |
| 플레이 영상 | https://youtu.be/6jfNqsfcH8g?si=b2SN3ac4RHLJu3vk |

## 게임 소개

*Hidden Weight*는 싱글플레이 2D 사이드스크롤 액션 어드벤처(메트로배니아)다. 플레이어는 감정을 수집하는 대신, 감정 능력으로 공간과 전투를 새롭게 해석하며 이미 지나온 장소를 다시 찾아가 흩어진 기억과 세계의 의미를 스스로 연결해 나간다.

세계는 시간축을 따라 세 지역으로 나뉜다.

| 지역 | 대응 감정·시간 | 색채 |
|---|---|---|
| **Residue (잔재)** | 과거 · 죄책감 | 어둡고 무거운 앰버 |
| **Gaze (응시)** | 현재 · 수치심 | 시선과 청록 계열의 긴장 |
| **Fracture (균열)** | 미래 · 불안 | 밝고 화사하지만 미세하게 어긋난 색과 형태 |

## 핵심 시스템

지역을 이동하며 얻는 감정 능력은 이동과 전투, 환경 해석 방식을 동시에 바꾼다.

| 능력 | 키 | 효과 |
|---|---|---|
| Rewind (되감기) | K (지역별 자동 매핑) | 대상을 이전 상태로 되돌려 통로를 연다 |
| Hush (숨죽이기) | K (지역별 자동 매핑) | 존재를 낮춰 위협의 감지를 피한다 |
| Foresight (예지) | K (지역별 자동 매핑) | 다가올 위치·상태를 미리 확인한다 |

자세한 수치와 게이팅 규칙은 [`docs/EMOTION_SYSTEM.md`](docs/EMOTION_SYSTEM.md)를 참고한다.

### 조작법

| 동작 | 키보드 |
|---|---|
| 이동 | A / D 또는 방향키 |
| 달리기 | Shift 홀드 |
| 점프 / 벽점프 | Space |
| 대시 | Left Ctrl |
| 공격 | J |
| 감정 스킬 | K |
| 일시정지 | Esc |

키보드와 게임패드를 동등하게 지원한다.

## 개발 환경

- **엔진**: Unity 6000.5.4f1
- **렌더링**: URP(Universal Render Pipeline) 17.5.0, 2D Renderer
- **언어**: C#
- **입력**: Legacy `Input` 시스템 (Input System 패키지는 설치되어 있으나 미사용)

## 프로젝트 구조

```
hidden-weight/
├── CLAUDE.md              디자인 컨텍스트(사용자·브랜드 성격·톤앤매너)
├── PROJECT_STRUCTURE.md   유니티 프로젝트 설정·모듈 구조·배치모드 명령 상세
├── docs/                  기획·레벨·아트·오디오·제출 문서 전체 (아래 참고)
└── HiddenWeight/          Unity 프로젝트 루트
    └── Assets/Scripts/    게임 코드 (9개 모듈)
```

`Assets/Scripts/` 아래 코드는 단방향 의존 그래프를 따르는 9개 모듈로 나뉜다.

```
Data ← Core ← Player ← { World, Enemies } ← Emotions ← { UI, Ending }
```

| 모듈 | 역할 |
|---|---|
| `Core` | 전역 게임 상태, 씬 흐름, 세이브/로드, 오디오 |
| `Data` | ScriptableObject 기반 밸런스 데이터 테이블 |
| `Player` | 이동·전투·생존 상태 머신 |
| `World` | 방·플랫폼·게이트와 감정 상호작용 인터페이스, 포탈 기반 룸 시스템 |
| `Emotions` | 감정 스킬(Rewind / Hush / Foresight) |
| `Enemies` | 적 기본 클래스 및 지역별 행동 패턴 |
| `Ending` | 엔딩 시퀀스 연출 |
| `UI` | HUD, 메뉴, 파편 로그 등 코드 기반 UI |
| `Editor` | 프로젝트 생성·빌드용 배치모드 툴 |

모듈별 상세 아키텍처는 [`docs/code/README.md`](docs/code/README.md)를, 프로젝트 설정과 의존 관계 전체 규칙은 [`PROJECT_STRUCTURE.md`](PROJECT_STRUCTURE.md)를 참고한다.

## 문서 안내 (`docs/`)

| 분류 | 문서 |
|---|---|
| 기획 | [`GAME_DESIGN.md`](docs/GAME_DESIGN.md), [`EMOTION_SYSTEM.md`](docs/EMOTION_SYSTEM.md), [`WORLD_MAP.md`](docs/WORLD_MAP.md), [`CONTENT_SYSTEM.md`](docs/CONTENT_SYSTEM.md), [`NARRATIVE_CONTENT.md`](docs/NARRATIVE_CONTENT.md), [`DESIGN_IMPLEMENTATION_GAP_ANALYSIS.md`](docs/DESIGN_IMPLEMENTATION_GAP_ANALYSIS.md) |
| 레벨 디자인 | [`LEVEL_00_INDEX.md`](docs/LEVEL_00_INDEX.md)부터 지역별(`LEVEL_10~50_*.md`) 상세 문서 |
| UI / 아트 / 오디오 | [`UI_UX_DESIGN.md`](docs/UI_UX_DESIGN.md), [`ANIMATION_ART_SPEC.md`](docs/ANIMATION_ART_SPEC.md), [`concept-art/`](docs/concept-art/), 지역별 오디오 생성 문서 |
| 코드 아키텍처 | [`code/README.md`](docs/code/README.md) |
| 제작 / 제출 | `PRODUCTION_*.md`, [`submission/`](docs/submission/) |

## 빌드 및 실행

1. Unity Hub에서 `HiddenWeight/` 폴더를 프로젝트로 연다. (요구 버전: **6000.5.4f1**)
2. 배치모드로 컴파일·테스트·빌드를 실행할 수 있다 (경로는 각자 환경에 맞게 치환).

```bash
# 컴파일 확인
<UNITY_PATH> -batchmode -quit -nographics -projectPath <PROJECT_PATH>/HiddenWeight \
  -executeMethod HiddenWeight.EditorTools.BuildScript.Compile

# macOS 빌드
<UNITY_PATH> -batchmode -quit -nographics -projectPath <PROJECT_PATH>/HiddenWeight \
  -executeMethod HiddenWeight.EditorTools.BuildScript.BuildMac
```

프로젝트를 처음부터 재현해야 하는 경우, `Editor` 모듈의 배치모드 도구를 순서대로 실행한다: `ProjectSetup.Run` → `DataAssetBuilder.Run` → `PlaceholderArtBuilder.Run` → `PrefabBuilder.Run` → `ZoneSceneBuilder.Run`. 전체 절차와 정확한 명령은 [`PROJECT_STRUCTURE.md`](PROJECT_STRUCTURE.md)를 참고한다.

WebGL 빌드를 로컬에서 미리 볼 때는 `HiddenWeight/WebBuild/run_local_server.sh`(또는 `.bat`)를 실행한다. `file://`로 열면 발생하는 CORS/MIME 문제를 피하기 위한 최소 로컬 서버다.

## 테스트

`HiddenWeight/Assets/Tests/`에 두 종류의 테스트가 있다.

- **EditMode**: 순수 로직 테스트 (`ProgressStateTests`, `SaveServiceTests` 등)
- **PlayMode**: 지역 순회, 플레이스루 등 실제 씬을 구동하는 테스트

Unity 배치모드에서 실행하려면 `-runTests` 옵션을 사용한다. 자세한 명령은 [`PROJECT_STRUCTURE.md`](PROJECT_STRUCTURE.md) §6을 참고한다.

## 팀

**IF98** (가안)

팀원: 김승혁, 임채원, 전민정

역할과 AI 도구 사용 내역 등 자세한 내용은 [`docs/submission/SUBMISSION_00_FACTS.md`](docs/submission/SUBMISSION_00_FACTS.md)를 참고한다.
