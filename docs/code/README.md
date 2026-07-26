# Hidden Weight — 코드 문서 (Code Docs)

이 디렉터리는 `HiddenWeight/Assets/Scripts` 이하 **전체 소스 코드의 파일별 기능 문서**다.
게임 기획은 [../GAME_DESIGN.md](../GAME_DESIGN.md), 구현 설계는
[../superpowers/specs/2026-07-26-hidden-weight-unity-mvp-design.md](../superpowers/specs/2026-07-26-hidden-weight-unity-mvp-design.md),
프로젝트 구조/모듈 개요는 [../../PROJECT_STRUCTURE.md](../../PROJECT_STRUCTURE.md)를 참고한다.

## 게임 한 줄 요약

몽환의 우주에서 눈을 뜬 소녀가 잔재(과거·죄책감)·응시(현재·수치심)·균열(미래·불안) 세 지역을
지나며 되감기·숨죽이기·예지 세 감정 스킬과 자각을 얻고, 마지막에 정적인 침실에서 자각으로
스스로의 이상 징후를 들여다보며 "거짓 깨어남"에서 "진짜 각성"으로 넘어가는 **2D 플랫포머
탐험 게임**. 전투는 최소한(근접 공격 1종, 적 1종, HP 3)이고 게임오버는 없다 — 죽으면 마지막
체크포인트로 되돌아간다.

## 씬 흐름

```text
Bootstrap(GameManager 생성) → Title → Zone_Prologue(튜토리얼)
  → Zone_Residue(되감기 획득) → Zone_Gaze(숨죽이기 + 자각 획득) → Zone_Fracture(예지 획득)
  → (백트래킹) Zone_Residue 재진입: 되감기 && 자각 && 균열 클리어 3중 조건 → 최종 파편
  → Ending(거짓 깨어남 → 진짜 각성) → Title
```

## 문서 구성 (모듈별)

| 문서 | 대상 폴더 | 내용 | 기획서 대응 |
|---|---|---|---|
| [Core.md](Core.md) | `Assets/Scripts/Core` | 게임 상태, 진행도(`ProgressState`), 씬 흐름(`SceneFlow`), 체크포인트, 오디오 | 2.1, 5.6, 5.7 |
| [Data.md](Data.md) | `Assets/Scripts/Data` | ScriptableObject 데이터 테이블(플레이어·감정·적·지역·밸런스) | 6장, 6.1절 |
| [Player.md](Player.md) | `Assets/Scripts/Player` | 이동·대시·벽클링/벽점프 상태머신, 근접 공격, 체력·무적·리스폰, 입력, 애니메이션 | 5.1, 5.4 |
| [World.md](World.md) | `Assets/Scripts/World` | 룸·카메라, 게이트, 되감기/예지/자각 대상 상호작용 인터페이스, 장애물, 파편, 지역 전환 | 5.5 |
| [Emotions.md](Emotions.md) | `Assets/Scripts/Emotions` | 감정 스킬 3종(되감기·숨죽이기·예지)과 자각 시스템 | 5.2, 5.3 |
| [Enemies.md](Enemies.md) | `Assets/Scripts/Enemies` | 적 본체, 순찰 AI, 접촉 피해 | 5.4 |
| [Ending.md](Ending.md) | `Assets/Scripts/Ending` | 2단 엔딩 시퀀스(거짓 깨어남 → 진짜 각성), 이상 오브젝트 | 5.7, 3.4절 |
| [UI.md](UI.md) | `Assets/Scripts/UI` | HUD, 파편 로그, 일시정지, 화면 페이드, 타이틀 | HUD·연출 UI 전반 |
| [Editor.md](Editor.md) | `Assets/Scripts/Editor` | 프로젝트 세팅·데이터/프리팹/씬 자동 생성·빌드 검증(배치모드 CLI 도구) | 8장, 9장 |

각 모듈 폴더에도 기능 개요를 담은 `README.md`가 함께 있다. 이 `docs/code/` 문서는 그보다 상세한
**파일 단위** 설명(역할 / 상속·의존 / 주요 멤버 / 동작)을 제공한다.

## 아키텍처 원칙

- **모듈 분리**: `Core / Data / Player / World / Emotions / Enemies / Ending / UI / Editor` 9개
  모듈로 책임을 나눈다. 네임스페이스는 `HiddenWeight.<모듈>`(Editor만 `HiddenWeight.EditorTools`).
- **단방향 의존**: `Editor`(전부 참조 가능)를 빼면 순환이 없다.
  `Data ← Core ← Player ← {World, Enemies} ← Emotions ← {UI, Ending}` 방향으로 쌓인다.
- **데이터 외부화**: 수치는 `Data`의 ScriptableObject 에셋으로 관리해 코드 재컴파일 없이
  밸런싱한다. `Editor/DataAssetBuilder`가 배치모드로 에셋을 생성한다.
- **인터페이스로 역참조 차단**: `World/Interactions.cs`는 어떤 모듈에도 의존하지 않는 순수
  계약 파일이다. `IRewindable`/`IForeseeable`/`IAwarenessReactive`는 Emotions가 World의 구체
  클래스를 몰라도 상호작용하게 하고, `IDamageable`은 Player가 Enemies를 몰라도 공격할 수
  있게 한다 (`PlayerAttack`은 `Enemy` 대신 `IDamageable`만 참조 — Player→World 예외 1건).
- **정적 훅으로 역방향 호출 뒤집기**: Core/World가 UI를 몰라도 UI 효과를 트리거할 수 있도록
  UI가 스스로 등록하는 훅 3종을 쓴다 — `SceneFlow.FadeLoader`(UI `ScreenFader`가 등록,
  Core/World가 호출), `GameManager.FragmentPresenter`(UI `FragmentLog`가 등록, World
  `StoryFragment`가 호출), `GameManager.RespawnRequested`(Player `PlayerHealth`가 구독,
  Core `GameManager.RespawnPlayer()`가 발행). `World/Interactions.cs`의
  `AwarenessRegistry`도 같은 원리다 — World가 등록 창구만 두고 Emotions의 `AwarenessSystem`이
  그 목록을 읽는다.
- **예외 한 건**: `World/StoryFragment.Collect()`는 스킬을 부여한 뒤
  `EmotionSkillController.Instance?.RefreshActive()`를 호출한다. 설계 문서가 명시적으로
  허용한 유일한 `World → Emotions` 참조다.
- **테스트는 순수 로직에만**: `ProgressState`(EditMode 테스트)만 자동화하고, MonoBehaviour와
  물리·연출은 배치모드 컴파일/빌드 검증으로 대신한다.

## 조작 요약

| 입력 | 기능 |
|---|---|
| `A`/`D` (←/→) | 좌우 이동 |
| `Shift` (홀드) | 달리기 |
| `Space` | 점프 / 벽점프 |
| `Left Ctrl` | 대시 |
| `J` | 공격 |
| `K` | 감정 스킬 (지역별 자동 전환: 되감기 홀드 / 숨죽이기 홀드 / 예지 탭) |
| `L` (홀드) | 자각 |
| `Esc` | 일시정지 |

자세한 조작 규칙(입력 게이트 예외 등)은 [PROJECT_STRUCTURE.md](../../PROJECT_STRUCTURE.md)의
조작표와 [Player.md](Player.md#playerinputcs)를 본다.
