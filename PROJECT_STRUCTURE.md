# Hidden Weight — 프로젝트 구조

> [GAME_DESIGN.md](docs/GAME_DESIGN.md) 기획과
> [설계 문서](docs/superpowers/specs/2026-07-26-hidden-weight-unity-mvp-design.md)를 Unity로 구현한
> 프로젝트의 모듈 구조 문서. 코드는 `HiddenWeight/Assets/Scripts/` 아래 모듈로 분리하고,
> 수치는 ScriptableObject 데이터 에셋으로 관리한다.

## 1. Unity 프로젝트 설정

| 항목 | 값 |
|---|---|
| 엔진 | Unity 6000.5.4f1 |
| 경로 | 저장소 루트의 `HiddenWeight/` |
| 렌더링 | URP 17.5.0, 2D Renderer |
| 물리 | Physics2D |
| 입력 | Legacy `Input` (`Input.GetAxisRaw` / `GetKey` / `GetKeyDown`), Input System 패키지는 설치만 하고 Legacy 모드 유지 |
| 카메라 | Orthographic, 세로 크기 6 유닛 |
| 픽셀 | Pixels Per Unit 32 |
| 대상 해상도 | 1920×1080 |
| 패키지 | `com.unity.render-pipelines.universal`(17.5.0), `com.unity.2d.tilemap`(1.0.0), `com.unity.2d.sprite`(1.0.0), `com.unity.ugui`(2.5.0), `com.unity.test-framework`(1.7.0) |

### 레이어와 태그

| 레이어 | 용도 |
|---|---|
| `Ground` | 타일맵 지형, 발판 |
| `Wall` | 벽잡기 판정 대상 (지형과 별도 판정) |
| `Player` | 플레이어 |
| `PlayerHushed` | 숨죽이기 상태. `GazeHazard`가 무시한다 |
| `Enemy` | 적 |
| `Hazard` | 접촉 피해 영역 |
| `Interactable` | 파편·게이트·되감기 대상 |

### 최초 세팅 절차

이미 생성된 프로젝트를 다시 처음부터 세팅해야 할 때(또는 다른 머신에서 재현할 때)의 순서다.
실제로는 아래를 `Editor` 모듈의 배치모드 도구가 대신 실행한다 — 사람이 에디터를 열고 손으로
할 필요는 없다. 각 단계의 정확한 셸 명령은 6절 "배치모드 명령 모음"을 본다.

1. Unity Hub에서 `HiddenWeight/` 경로에 2D 프로젝트를 생성한다 (`Unity -createProject`).
2. `HiddenWeight/Packages/manifest.json`의 `dependencies`에 위 표의 4개 패키지를 추가한다.
   나머지 `com.unity.modules.*`는 Unity가 만든 그대로 둔다.
3. `HiddenWeight/Assets/Scripts/HiddenWeight.asmdef`(런타임)와
   `HiddenWeight/Assets/Scripts/Editor/HiddenWeight.Editor.asmdef`(에디터 전용)를 만든다.
4. `Editor/ProjectSetup.Run()`을 배치모드로 실행해 레이어·태그를 등록한다.
5. `Editor/DataAssetBuilder.Run()`으로 `PlayerData` / `EmotionData`×3 / `EnemyData`×3 /
   `ZoneData`×4 / `BalanceData` ScriptableObject 에셋을 생성한다.
6. `Editor/PlaceholderArtBuilder.Run()`으로 단색 플레이스홀더 스프라이트를 생성한다.
7. `Editor/PrefabBuilder.Run()`으로 Player·Enemy·MainCamera·HUD 등 프리팹을 생성한다.
8. `Editor/ZoneSceneBuilder.Run()`으로 씬 7개(`Bootstrap`, `Title`, `Zone_Prologue`,
   `Zone_Residue`, `Zone_Gaze`, `Zone_Fracture`, `Ending`)를 생성하고
   `EditorBuildSettings.scenes`에 등록한다.
9. `BuildScript.Compile()` / `BuildScript.BuildMac()`으로 컴파일·빌드를 검증한다.

## 2. 확정한 기본값

설계 문서 1.1절에서 기획서에 비어 있던 항목을 아래와 같이 결정했다.

| 항목 | 결정 | 근거 |
|---|---|---|
| 씬 구조 | 지역 = 씬 1개, 지역 내부는 룸 단위 카메라 전환 | 5.3절 게이팅이 선형 + 1회 백트래킹이라 씬 전환이 4회뿐. 팀 머지 충돌 회피 |
| 렌더 파이프라인 | URP 2D | 자각의 채도 상실(7.2), 지역별 색보정(7.1), 균열의 광원 어긋남(4.2)을 Volume·2D Light로 처리 |
| 레벨 제작 | Tilemap + 단색 도형 플레이스홀더 | 아트 교체 시 타일/스프라이트만 바꾸면 됨 |
| 주인공 | 소녀 캐릭터 (`docs/character_sprite_ref.png`) | 기획서 2.1절의 "작은 동그라미"는 폐기 |
| 이동 세트 | 이동·점프·대시·벽잡기·벽점프를 **처음부터 보유** | 레퍼런스 시트의 애니메이션 전부 활용. 게이팅은 감정 스킬로만 |
| 전투 | 최소 — 근접 공격 1종, 적 1종, HP 3, 체크포인트 리스폰 | 기획서에 적 설계가 없음. 1분 영상에도 전투 컷이 없음 |
| 엔딩 | 전용 씬 + 정적 1인칭 침실 스프라이트 레이어 | 3.4절 연출을 그대로 살리면서 구현 부담 최소 |
| 게임오버 | 없음 | 탐험 게임 톤 유지. HP 0이면 마지막 체크포인트로 되돌린다 |

**범위 밖(구현하지 않음)**: 기쁨·분노 확장 지역, 자각의 감정별 차등 감지 고도화, 다회
백트래킹·여러 엔딩 분기·감정 조합, 적 AI 고도화(투사체·원거리·보스)·세이브/로드·컨트롤러
지원, 사운드 에셋(인터페이스만 두고 클립은 비워둠 — 단, 실제 `Core/AudioManager.cs`는
`AudioClip`을 직접 주입받는 크로스페이드 재생기로 구현되어 있다. [Core.md](docs/code/Core.md) 참고).

## 3. 모듈 구조

```
HiddenWeight/Assets/Scripts/
├── Core/       게임 상태·씬 흐름·진행도·체크포인트·오디오
├── Data/       ScriptableObject 데이터 테이블
├── Player/     이동·전투·체력·애니메이션·입력
├── World/      룸·게이트·되감기 대상·장애물·파편·상호작용 인터페이스
├── Emotions/   감정 스킬 3종(되감기·숨죽이기·예지) + 자각
├── Enemies/    적 본체·순찰·접촉 피해
├── Ending/     2단 엔딩 시퀀스
├── UI/         HUD·일시정지·파편 로그·타이틀·화면 페이드
└── Editor/     프로젝트 세팅·프리팹/데이터/씬 자동 생성·빌드 검증
```

네임스페이스는 `HiddenWeight.<모듈>`을 쓴다. `Editor` 모듈만 `HiddenWeight.EditorTools`.

각 모듈 폴더에는 해당 기능을 설명하는 `README.md`가 있고, `docs/code/`에는 파일 단위
상세 문서가 있다.

| 모듈 | README | 상세 문서 | 기획서 대응 |
|---|---|---|---|
| Core | [Core/README.md](HiddenWeight/Assets/Scripts/Core/README.md) | [docs/code/Core.md](docs/code/Core.md) | 2.1, 5.6, 5.7 |
| Data | [Data/README.md](HiddenWeight/Assets/Scripts/Data/README.md) | [docs/code/Data.md](docs/code/Data.md) | 6장, 6.1절 |
| Player | [Player/README.md](HiddenWeight/Assets/Scripts/Player/README.md) | [docs/code/Player.md](docs/code/Player.md) | 5.1, 5.4 |
| World | [World/README.md](HiddenWeight/Assets/Scripts/World/README.md) | [docs/code/World.md](docs/code/World.md) | 5.5 |
| Emotions | [Emotions/README.md](HiddenWeight/Assets/Scripts/Emotions/README.md) | [docs/code/Emotions.md](docs/code/Emotions.md) | 5.2, 5.3 |
| Enemies | [Enemies/README.md](HiddenWeight/Assets/Scripts/Enemies/README.md) | [docs/code/Enemies.md](docs/code/Enemies.md) | 5.4 |
| Ending | [Ending/README.md](HiddenWeight/Assets/Scripts/Ending/README.md) | [docs/code/Ending.md](docs/code/Ending.md) | 5.7, 3.4절 |
| UI | [UI/README.md](HiddenWeight/Assets/Scripts/UI/README.md) | [docs/code/UI.md](docs/code/UI.md) | HUD·연출 UI 전반 |
| Editor | [Editor/README.md](HiddenWeight/Assets/Scripts/Editor/README.md) | [docs/code/Editor.md](docs/code/Editor.md) | 8장, 9장 |

코드 문서 전체 색인은 [docs/code/README.md](docs/code/README.md)를 본다.

### 의존 방향

```
Editor    ──▶ (전부)
UI        ──▶ Core, Emotions, Player
Ending    ──▶ Core, Player, UI, World
Enemies   ──▶ Data, Player, World
Emotions  ──▶ Core, Data, Player, World
World     ──▶ Core, Data, Player  (+ Emotions 예외 1건, 아래 참고)
Player    ──▶ Core, Data  (+ World 예외 1건, 아래 참고)
Data      ──▶ (없음)
Core      ──▶ Data
```

순환 참조가 없는 단방향 구조다. 두 가지 예외와 세 가지 정적 훅으로 원래 설계 문서의
의존표(3.1절)보다 실제 구현이 더 세밀해졌다.

**예외 1 — `World/Interactions.cs`는 순수 계약 파일**: 어떤 모듈에도 의존하지 않는
인터페이스(`IRewindable`, `IForeseeable`, `IAwarenessReactive`, `IDamageable`)와
`AwarenessRegistry`만 담는다. 의존표에 없는 모듈이 이 파일 하나만 참조하는 것은 허용한다.
`Player/PlayerAttack.cs`가 `Enemies.Enemy`가 아니라 `World.IDamageable`만 참조하는 것이
이 경우다 — `Player → Enemies` 순환을 막기 위한 결정.

**예외 2 — `World/StoryFragment.cs → Emotions`**: 파편을 모으면
`EmotionSkillController.Instance?.RefreshActive()`를 호출해 현재 활성 스킬을 다시
계산시킨다. 설계 문서가 명시적으로 허용한 유일한 `World → Emotions` 참조다.

**정적 훅 3곳** (역방향 호출이 필요한 자리를 뒤집는다. UI가 스스로 등록하고, Core/World는
UI를 참조하지 않는다):

| 훅 | 정의 위치 | 등록 주체 | 호출 주체 |
|---|---|---|---|
| `SceneFlow.FadeLoader` | Core | UI `ScreenFader` | Core `SceneFlow.LoadWithFade`, World `ZoneTrigger` |
| `GameManager.FragmentPresenter` | Core | UI `FragmentLog` | World `StoryFragment` |
| `AwarenessRegistry` | World (`Interactions.cs`) | World `IAwarenessReactive` 구현체(`HiddenFragment` 등) | Emotions `AwarenessSystem` |

추가로 `GameManager.RespawnRequested`(이벤트)는 Core가 Player를 참조하지 않기 위한 훅이다
— `GameManager.RespawnPlayer()`가 이 이벤트를 발화하면 Player `PlayerHealth`가 구독해서
리스폰을 실행한다.

## 4. 전체 게임 흐름과 모듈 연결

```text
GameManager(Core, DontDestroyOnLoad, Bootstrap 씬)
  ├─ Awake: ProgressState 생성, SceneManager.sceneLoaded 구독
  │    (지역 씬이 로드되면 BalanceData.GetZoneByScene()으로 자동 EnterZone)
  ├─ autoLoadTitle=true(Bootstrap 인스턴스만) → Start()에서 SceneFlow.Load(Title)
  ├─ Title(UI) → "시작" → SceneFlow → Zone_Prologue
  ├─ Zone_Prologue: 이동·점프·대시·벽점프 튜토리얼 (감정 스킬 없음)
  ├─ Zone_Residue(잔재·과거·죄책감): StoryFragment로 되감기(Rewind) 획득
  │    → Gate(requiredSkill=Rewind) 통과 → 출구 ZoneTrigger.LoadWithFade
  ├─ Zone_Gaze(응시·현재·수치심): StoryFragment로 숨죽이기(Hush) + 자각 동시 획득
  │    → GazeHazard는 PlayerHushed 레이어 무시 → 출구
  ├─ Zone_Fracture(균열·미래·불안, awarenessStable=false): 예지(Foresight) 획득
  │    → 출구 ZoneTrigger(marksFractureCleared=true) → ProgressState.HasClearedFracture=true
  ├─ (백트래킹) Zone_Residue 재진입: Rewind && hasAwareness && HasClearedFracture
  │    3중 조건을 만족하면 HiddenFragment(백트래킹 전용) 획득 가능
  └─ Ending 씬: PlayerInput.Enabled=false, AwarenessHeld(L)만 입력
       ├─ 1단계 "거짓 깨어남": AnomalyObject 3종을 L 홀드로 드러냄.
       │    끊김 없이 유지해야 누적되는 홀드 시간이 완료 조건을 채움
       └─ 2단계 "진짜 각성": AnomalyObject 전부 비활성 → 페이드 아웃 → Title

HP 0(PlayerHealth) → GameManager.RespawnPlayer() → RespawnRequested 이벤트 →
마지막 Checkpoint 위치로 리스폰, HP 회복. 게임오버 화면 없음.
```

## 5. 조작표

| 입력 | 기능 |
|---|---|
| `A` / `D` (←/→, `Horizontal` 축) | 좌우 이동 |
| `Shift` (홀드) | 달리기 |
| `Space` | 점프 / 벽점프 (누른 순간 + 홀드 모두 사용) |
| `Left Ctrl` | 대시 |
| `J` | 공격 |
| `K` | 감정 스킬 (지역에 따라 되감기/숨죽이기/예지로 자동 전환. 되감기·숨죽이기는 홀드, 예지는 탭) |
| `L` (홀드) | 자각 |
| `Esc` | 일시정지 / 일시정지 해제 |

`PausePressed`와 `AwarenessHeld`(`Player/PlayerInput.cs`)는 `PlayerInput.Enabled = false`
상태에서도 항상 동작한다 — 일시정지 화면에서 Esc로 해제할 수 있어야 하고, 엔딩 시퀀스는
이동·공격을 잠근 채로 자각 입력만 받아야 하기 때문이다.

## 6. 배치모드 명령 모음

전부 저장소 루트(`/Users/ksh/Desktop/NHN HACKERton`)에서 실행한다. Unity 실행 파일 경로는
`/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity`로 고정한다.

### 컴파일 검증

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -nographics \
  -projectPath "$PWD/HiddenWeight" \
  -logFile "$PWD/.unity-logs/compile.log" \
  -executeMethod HiddenWeight.EditorTools.BuildScript.Compile
echo "exit=$?"
grep -c "error CS" .unity-logs/compile.log || echo "컴파일 에러 0건"
```

기대 결과: `exit=0`, 컴파일 에러 0건.

### EditMode 테스트

`ProgressState`(순수 C# 진행도·게이팅 클래스)에만 테스트를 붙인다. MonoBehaviour·물리·연출은
비용 대비 얻는 것이 없어 테스트하지 않는다.

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -runTests \
  -projectPath "$PWD/HiddenWeight" \
  -testPlatform EditMode \
  -testResults "$PWD/.unity-logs/tests-results.xml" \
  -logFile "$PWD/.unity-logs/tests.log"
echo "exit=$?"
```

`-runTests`는 테스트 어셈블리 컴파일이 성공해야만 통과하므로, 통과 자체가 컴파일 성공의
증거이기도 하다.

### 빌드 검증 (macOS 스탠드얼론)

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" \
  -logFile "$PWD/.unity-logs/build.log" \
  -executeMethod HiddenWeight.EditorTools.BuildScript.BuildMac
echo "exit=$?"
ls -d HiddenWeight/Builds/macOS/HiddenWeight.app
```

기대 결과: `exit=0`, `HiddenWeight/Builds/macOS/HiddenWeight.app` 번들 존재.

### 프로젝트 최초 생성 (참고용 — 이미 생성돼 있으면 다시 실행하지 않는다)

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -nographics \
  -createProject "$PWD/HiddenWeight" \
  -logFile "$PWD/.unity-logs/create.log"
echo "exit=$?"
cat HiddenWeight/ProjectSettings/ProjectVersion.txt
```

### 프로젝트 세팅 (레이어·태그)

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" \
  -logFile "$PWD/.unity-logs/setup.log" \
  -executeMethod HiddenWeight.EditorTools.ProjectSetup.Run
echo "exit=$?"
grep -E "Ground|PlayerHushed" HiddenWeight/ProjectSettings/TagManager.asset
```

### 씬 7개 생성

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" \
  -logFile "$PWD/.unity-logs/scenes.log" \
  -executeMethod HiddenWeight.EditorTools.ZoneSceneBuilder.Run
echo "exit=$?"
ls HiddenWeight/Assets/Scenes/*.unity
```

기대 결과: `exit=0`, 씬 7개(`Bootstrap`, `Title`, `Zone_Prologue`, `Zone_Residue`,
`Zone_Gaze`, `Zone_Fracture`, `Ending`).

### 재설계 전체 지역 씬 생성 (지역별 15룸)

`ZoneSceneBuilder.Run()`이 만드는 4룸짜리 MVP 씬과 별개로, 지역마다 주 동선 12룸 +
비밀방 3룸을 갖춘 `*_Full` 씬을 따로 짓는다. 아직 게임 흐름(`ZoneData.nextSceneName`)에는
연결하지 않았고, 검증을 통과한 뒤 교체한다. 셋 다 같은 형태로 호출한다.

| 지역 | 진입점 | 결과 씬 | 제작 기준 문서 |
|---|---|---|---|
| 잔재 | `ZoneSceneBuilder.RunResidueZone` | `Zone_Residue_Full` | `docs/RESIDUE_ROOM_IMPLEMENTATION.md` |
| 응시 | `ZoneSceneBuilder.RunGazeZone` | `Zone_Gaze_Full` | `docs/GAZE_LEVEL_DESIGN.md` |
| 균열 | `ZoneSceneBuilder.RunFractureZone` | `Zone_Fracture_Full` | `docs/FRACTURE_LEVEL_DESIGN.md` |

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" \
  -logFile "$PWD/.unity-logs/gaze-zone.log" \
  -executeMethod HiddenWeight.EditorTools.ZoneSceneBuilder.RunGazeZone
```

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" \
  -logFile "$PWD/.unity-logs/fracture-zone.log" \
  -executeMethod HiddenWeight.EditorTools.ZoneSceneBuilder.RunFractureZone
```

생성된 씬은 `Assets/Tests/PlayMode/GazeFractureZoneTests.cs`가 명세에 대고 검사한다 —
방 15개와 크기, 체크포인트 3·숏컷 3, 주 동선 12룸이 실제로 걸어서 이어지는지, 숨죽이기
게이트가 규격(통과 높이 0.84~1.4, 틈 폭 0.48~0.8) 안에 있는지, 균열에 자각으로 여는
문이 없는지, 균열의 붕괴 발판이 전부 스스로 되살아나는지.
