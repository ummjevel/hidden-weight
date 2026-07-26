# Hidden Weight Unity MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 기획서 10.1절 MVP — 잔재·응시·균열 3개 지역을 감정 스킬 3종 + 자각 + 2단 엔딩으로 잇는 2D 횡스크롤 메트로배니아를 Unity에서 플레이 가능한 상태로 만든다.

**Architecture:** 지역 하나가 씬 하나이고, 지역 내부는 `Room` 컴포넌트가 정의한 사각 경계 단위로 카메라가 전환된다. 수치는 전부 ScriptableObject로 빼고, `GameManager`가 `BalanceData`와 `ProgressState`를 들고 씬을 넘나든다. 감정 스킬과 월드 오브젝트는 구체 클래스가 아니라 `IRewindable` / `IAwarenessReactive` / `IForeseeable` 세 인터페이스로만 만난다.

**Tech Stack:** Unity 6000.5.4f1, URP 17.5.0 (2D Renderer), Physics2D, Legacy Input, Unity Test Framework (EditMode 한정)

## Global Constraints

- Unity 버전은 **6000.5.4f1** 고정. `HiddenWeight/ProjectSettings/ProjectVersion.txt`가 이 값이어야 한다
- 프로젝트 경로는 저장소 루트의 `HiddenWeight/`
- 네임스페이스는 `HiddenWeight.<모듈>`. Editor 스크립트만 `HiddenWeight.EditorTools`
- 모듈 의존 방향은 단방향이다: `Core → Data`, `Player → Core, Data`, `World → Core, Data, Player`, `Emotions → Core, Data, Player, World`, `Enemies → Core, Data, Player`, `UI → Core, Data, Player, Emotions`, `Ending → Core, Emotions`. **역방향 참조를 만들지 않는다**
- 수치는 코드에 하드코딩하지 않는다. 전부 `Data` 모듈의 ScriptableObject 필드로 뺀다
- 주석과 커밋 메시지는 한국어로 쓴다 (기존 저장소 관습)
- 각 태스크는 batchmode 컴파일 0 에러를 확인한 뒤 커밋한다
- 게임오버 화면은 만들지 않는다. HP 0이면 마지막 체크포인트로 되돌린다

### 반복 사용하는 명령

**컴파일 검증** (모든 태스크의 검증 단계에서 이 명령을 쓴다):

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
mkdir -p .unity-logs
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -nographics \
  -projectPath "$PWD/HiddenWeight" \
  -logFile "$PWD/.unity-logs/compile.log" \
  -executeMethod HiddenWeight.EditorTools.BuildScript.Compile
echo "exit=$?"
grep -c "error CS" .unity-logs/compile.log || echo "컴파일 에러 0건"
```

기대 결과: `exit=0`, `error CS` 매칭 0건.

**EditMode 테스트 실행:**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -runTests \
  -projectPath "$PWD/HiddenWeight" \
  -testPlatform EditMode \
  -testResults "$PWD/.unity-logs/results.xml" \
  -logFile "$PWD/.unity-logs/tests.log"
echo "exit=$?"
grep -o 'result="[A-Za-z]*"' .unity-logs/results.xml | sort | uniq -c
```

기대 결과: `exit=0`, `result="Passed"` 만 나오고 `Failed`가 없음.

---

## 파일 구조

### 작성할 파일 전체 목록

```
HiddenWeight/
├── Packages/manifest.json
├── ProjectSettings/            (Unity 자동 생성 + ProjectSetup이 수정)
└── Assets/
    ├── Settings/               URP 에셋·2D Renderer·Volume 프로파일 4종
    ├── Art/Placeholder/        Editor가 생성한 단색 스프라이트
    ├── ScriptableObjects/      Editor가 생성한 데이터 에셋
    ├── Prefabs/                Editor가 생성한 프리팹
    ├── Scenes/                 Editor가 생성한 씬 7개
    ├── Tests/EditMode/
    │   ├── HiddenWeight.Tests.EditMode.asmdef
    │   └── ProgressStateTests.cs
    └── Scripts/
        ├── HiddenWeight.asmdef
        ├── Data/
        │   ├── Enums.cs                EmotionId, ZoneId, SkillInput
        │   ├── PlayerData.cs
        │   ├── EmotionData.cs
        │   ├── EnemyData.cs
        │   ├── ZoneData.cs
        │   └── BalanceData.cs
        ├── Core/
        │   ├── GameState.cs
        │   ├── ProgressState.cs        순수 C#. 테스트 대상
        │   ├── GameManager.cs
        │   ├── SceneFlow.cs
        │   ├── Checkpoint.cs
        │   └── AudioManager.cs
        ├── Player/
        │   ├── PlayerInput.cs          키 입력 단일 창구
        │   ├── PlayerState.cs
        │   ├── PlayerController.cs
        │   ├── PlayerHealth.cs
        │   ├── PlayerAttack.cs
        │   └── PlayerAnimator.cs
        ├── World/
        │   ├── Interactions.cs         인터페이스 3종
        │   ├── Room.cs
        │   ├── RoomCamera.cs
        │   ├── Rewindable.cs
        │   ├── CrumblingPlatform.cs
        │   ├── MovingPlatform.cs
        │   ├── GazeHazard.cs
        │   ├── Gate.cs
        │   ├── StoryFragment.cs
        │   ├── HiddenFragment.cs
        │   └── ZoneTrigger.cs
        ├── Emotions/
        │   ├── EmotionSkill.cs         추상 베이스
        │   ├── EmotionSkillController.cs
        │   ├── RewindSkill.cs
        │   ├── HushSkill.cs
        │   ├── ForesightSkill.cs
        │   └── AwarenessSystem.cs
        ├── Enemies/
        │   ├── Enemy.cs
        │   ├── EnemyPatrol.cs
        │   └── ContactDamage.cs
        ├── Ending/
        │   ├── AnomalyObject.cs
        │   └── EndingSequence.cs
        ├── UI/
        │   ├── ScreenFader.cs
        │   ├── HUD.cs
        │   ├── FragmentLog.cs
        │   ├── PauseMenu.cs
        │   └── TitleScreen.cs
        └── Editor/
            ├── HiddenWeight.Editor.asmdef
            ├── BuildScript.cs
            ├── ProjectSetup.cs
            ├── PlaceholderArtBuilder.cs
            ├── DataAssetBuilder.cs
            ├── PrefabBuilder.cs
            └── ZoneSceneBuilder.cs
```

### 저장소 루트 문서

```
PROJECT_STRUCTURE.md
docs/code/README.md
docs/code/{Core,Data,Player,World,Emotions,Enemies,Ending,UI,Editor}.md
```

모듈 폴더마다 `README.md`도 함께 둔다.

---

## Task 1: Unity 프로젝트 생성과 컴파일 검증 파이프라인

가장 먼저 "컴파일이 통과했는지 확인하는 수단"을 만든다. 이후 모든 태스크가 이것에 의존한다.

**Files:**
- Create: `HiddenWeight/` (Unity가 생성)
- Create: `HiddenWeight/Packages/manifest.json`
- Create: `HiddenWeight/Assets/Scripts/HiddenWeight.asmdef`
- Create: `HiddenWeight/Assets/Scripts/Editor/HiddenWeight.Editor.asmdef`
- Create: `HiddenWeight/Assets/Scripts/Editor/BuildScript.cs`
- Create: `HiddenWeight/Assets/Scripts/Editor/ProjectSetup.cs`

**Interfaces:**
- Consumes: 없음
- Produces: `HiddenWeight.EditorTools.BuildScript.Compile()`, `HiddenWeight.EditorTools.BuildScript.BuildMac()`, `HiddenWeight.EditorTools.ProjectSetup.Run()`

- [ ] **Step 1: 프로젝트 생성**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -nographics \
  -createProject "$PWD/HiddenWeight" \
  -logFile "$PWD/.unity-logs/create.log"
echo "exit=$?"
cat HiddenWeight/ProjectSettings/ProjectVersion.txt
```

기대 결과: `exit=0`, `m_EditorVersion: 6000.5.4f1`

- [ ] **Step 2: manifest.json 교체**

`HiddenWeight/Packages/manifest.json`의 `dependencies`에 아래 4개가 반드시 들어가야 한다. 나머지 `com.unity.modules.*`는 Unity가 만든 그대로 둔다.

```json
"com.unity.render-pipelines.universal": "17.5.0",
"com.unity.2d.tilemap": "1.0.0",
"com.unity.2d.sprite": "1.0.0",
"com.unity.ugui": "2.5.0",
"com.unity.test-framework": "1.7.0",
```

- [ ] **Step 3: asmdef 2개 작성**

`HiddenWeight/Assets/Scripts/HiddenWeight.asmdef`:

```json
{
  "name": "HiddenWeight",
  "rootNamespace": "HiddenWeight",
  "references": ["Unity.RenderPipelines.Universal.Runtime"],
  "includePlatforms": [],
  "allowUnsafeCode": false,
  "autoReferenced": true
}
```

`HiddenWeight/Assets/Scripts/Editor/HiddenWeight.Editor.asmdef`:

```json
{
  "name": "HiddenWeight.Editor",
  "rootNamespace": "HiddenWeight.EditorTools",
  "references": ["HiddenWeight", "Unity.RenderPipelines.Universal.Runtime", "Unity.RenderPipelines.Universal.Editor"],
  "includePlatforms": ["Editor"],
  "autoReferenced": true
}
```

- [ ] **Step 4: BuildScript.cs 작성**

```csharp
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HiddenWeight.EditorTools
{
    // 배치모드 검증용. 컴파일이 깨지면 -executeMethod 자체가 실행되지 않으므로,
    // 이 메서드가 돌아서 exit 0을 남겼다는 것이 곧 "컴파일 통과"의 증거다.
    public static class BuildScript
    {
        public static void Compile()
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                Debug.LogError("[BuildScript] 스크립트 컴파일 실패");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("[BuildScript] 컴파일 통과");
            EditorApplication.Exit(0);
        }

        public static void BuildMac()
        {
            var scenes = new[]
            {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/Title.unity",
                "Assets/Scenes/Zone_Prologue.unity",
                "Assets/Scenes/Zone_Residue.unity",
                "Assets/Scenes/Zone_Gaze.unity",
                "Assets/Scenes/Zone_Fracture.unity",
                "Assets/Scenes/Ending.unity",
            };

            var report = BuildPipeline.BuildPlayer(
                scenes, "Builds/macOS/HiddenWeight.app", BuildTarget.StandaloneOSX, BuildOptions.None);

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildScript] 빌드 실패: {report.summary.result}, 에러 {report.summary.totalErrors}개");
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log($"[BuildScript] 빌드 성공: {report.summary.outputPath}");
            EditorApplication.Exit(0);
        }
    }
}
```

- [ ] **Step 5: ProjectSetup.cs 작성**

`ProjectSetup.Run()`이 하는 일:

1. **레이어 등록** — `TagManager.asset`을 `SerializedObject`로 열어 8~15번 슬롯에 `Ground`, `Wall`, `Player`, `PlayerHushed`, `Enemy`, `Hazard`, `Interactable`을 넣는다
2. **레이어 충돌 행렬** — `Physics2D.IgnoreLayerCollision`으로 `PlayerHushed`↔`Enemy` 충돌을 끈다
3. **URP 에셋 생성** — `Assets/Settings/`에 `UniversalRenderPipelineAsset`과 `Renderer2DData`를 만들고 `GraphicsSettings.defaultRenderPipeline`과 모든 Quality 레벨에 연결한다
4. **Volume 프로파일 4종 생성** — `Assets/Settings/Volume_{Prologue,Residue,Gaze,Fracture}.asset`. 각각 `ColorAdjustments` 추가 후 설계 문서 6.1절 수치를 넣는다. 추가로 `Assets/Settings/Volume_Awareness.asset`에 채도 -80 + `Vignette`
5. **PlayerSettings** — 회사명 `NHN Hackerton`, 제품명 `Hidden Weight`, 기본 해상도 1920×1080, 전체화면 아님

`EditorApplication.Exit` 호출은 하지 않는다 (다른 빌더에서 재사용하기 위함).

- [ ] **Step 6: ProjectSetup 실행**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" \
  -logFile "$PWD/.unity-logs/setup.log" \
  -executeMethod HiddenWeight.EditorTools.ProjectSetup.Run
echo "exit=$?"
grep -E "Ground|PlayerHushed" HiddenWeight/ProjectSettings/TagManager.asset
```

기대 결과: `exit=0`, `TagManager.asset`에 `Ground`와 `PlayerHushed`가 보임

- [ ] **Step 7: 컴파일 검증**

Global Constraints의 컴파일 검증 명령 실행. `exit=0`, 에러 0건.

- [ ] **Step 8: 커밋**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
git add HiddenWeight
git commit -m "[feat] Unity 6000.5.4f1 2D URP 프로젝트 생성 + 배치모드 검증 파이프라인"
```

---

## Task 2: Data 모듈 — ScriptableObject 데이터 테이블

**Files:**
- Create: `HiddenWeight/Assets/Scripts/Data/Enums.cs`
- Create: `HiddenWeight/Assets/Scripts/Data/PlayerData.cs`
- Create: `HiddenWeight/Assets/Scripts/Data/EmotionData.cs`
- Create: `HiddenWeight/Assets/Scripts/Data/EnemyData.cs`
- Create: `HiddenWeight/Assets/Scripts/Data/ZoneData.cs`
- Create: `HiddenWeight/Assets/Scripts/Data/BalanceData.cs`

**Interfaces:**
- Consumes: 없음
- Produces: 아래 타입 전부. 이후 모든 태스크가 이 이름과 필드명을 그대로 쓴다

```csharp
namespace HiddenWeight.Data
{
    public enum EmotionId { None = 0, Rewind = 1, Hush = 2, Foresight = 3 }
    public enum ZoneId { Prologue = 0, Residue = 1, Gaze = 2, Fracture = 3 }
    public enum SkillInput { Hold = 0, Tap = 1 }
}
```

- [ ] **Step 1: Enums.cs 작성**

위 코드 그대로. 명시적 숫자를 붙이는 이유는 인스펙터에서 값이 저장된 뒤 순서를 바꿔도 에셋이 깨지지 않게 하기 위함이다.

- [ ] **Step 2: PlayerData.cs 작성**

`[CreateAssetMenu(fileName = "PlayerData", menuName = "HiddenWeight/Player Data")]`

| 필드 | 타입 | 기본값 | 설명 |
|---|---|---|---|
| `walkSpeed` | float | 6 | 걷기 |
| `runSpeed` | float | 9 | Shift 홀드 |
| `jumpVelocity` | float | 14 | 점프 초기 속도 |
| `gravityScale` | float | 3.5 | Rigidbody2D 기본 중력 |
| `fallGravityMultiplier` | float | 1.6 | 하강 중 중력 배수 |
| `coyoteTime` | float | 0.1 | 발판 이탈 후 점프 허용 시간 |
| `jumpBufferTime` | float | 0.1 | 착지 전 입력 기억 시간 |
| `variableJumpCut` | float | 0.5 | 상승 중 키를 떼면 곱할 속도 배수 |
| `dashDistance` | float | 4 | |
| `dashDuration` | float | 0.15 | |
| `dashCooldown` | float | 0.8 | |
| `wallSlideSpeed` | float | 2 | 벽잡기 하강 속도 |
| `wallJumpVelocity` | Vector2 | (9, 13) | 벽 반대 방향 x, 위쪽 y |
| `wallJumpLockTime` | float | 0.15 | 벽점프 직후 좌우 입력 무시 시간 |
| `maxHealth` | int | 3 | |
| `invulnerableTime` | float | 0.8 | |
| `knockbackForce` | float | 8 | 피격 넉백 |
| `attackRadius` | float | 1.2 | |
| `attackAngle` | float | 90 | 부채꼴 각도(도) |
| `attackActiveTime` | float | 0.1 | 히트박스 유지 시간 |
| `attackCooldown` | float | 0.35 | |
| `attackDamage` | int | 1 | |

- [ ] **Step 3: EmotionData.cs 작성**

`[CreateAssetMenu(fileName = "EmotionData", menuName = "HiddenWeight/Emotion Data")]`

| 필드 | 타입 | 설명 |
|---|---|---|
| `id` | EmotionId | |
| `displayName` | string | "되감기" / "숨죽이기" / "예지" |
| `inputMode` | SkillInput | Rewind·Hush = Hold, Foresight = Tap |
| `channelTime` | float | 되감기 채널링 1.0. 나머지 0 |
| `cooldown` | float | 되감기 2, 예지 3, 숨죽이기 0 |
| `range` | float | 되감기 6, 예지 8 |
| `moveSpeedMultiplier` | float | 되감기 0(이동 불가), 숨죽이기 0.45, 예지 1 |
| `effectDuration` | float | 예지 고스트 표시 시간 1.5 |
| `previewLeadTime` | float | 예지가 내다보는 미래 초 2.0 |
| `hushScale` | float | 숨죽이기 스케일 배수 0.6 |

- [ ] **Step 4: EnemyData.cs 작성**

`[CreateAssetMenu(fileName = "EnemyData", menuName = "HiddenWeight/Enemy Data")]`

필드: `maxHealth`(int, 2), `moveSpeed`(float, 1.5), `contactDamage`(int, 1), `tint`(Color, white), `knockbackForce`(float, 6), `wobbleAmplitude`(float, 0 — 균열 지역만 0.2), `wobbleFrequency`(float, 3)

- [ ] **Step 5: ZoneData.cs 작성**

`[CreateAssetMenu(fileName = "ZoneData", menuName = "HiddenWeight/Zone Data")]`

| 필드 | 타입 | 설명 |
|---|---|---|
| `id` | ZoneId | |
| `displayName` | string | "몽환의 우주" / "잔재" / "응시" / "균열" |
| `sceneName` | string | `Zone_Prologue` 등 |
| `nextSceneName` | string | 클리어 시 넘어갈 씬 |
| `grantedSkill` | EmotionId | 이 지역에서 얻는 스킬. 프롤로그는 None |
| `grantsAwareness` | bool | 응시만 true |
| `awarenessStable` | bool | 균열만 false |
| `volumeProfile` | VolumeProfile | 지역 색보정 |
| `bgm` | AudioClip | MVP에서는 비워둔다 |

`using UnityEngine.Rendering;` 필요.

- [ ] **Step 6: BalanceData.cs 작성**

`[CreateAssetMenu(fileName = "BalanceData", menuName = "HiddenWeight/Balance Data")]`

```csharp
public PlayerData player;
public EmotionData[] emotions;
public EnemyData[] enemies;
public ZoneData[] zones;
public VolumeProfile awarenessProfile;

public EmotionData GetEmotion(EmotionId id);   // 없으면 null
public ZoneData GetZone(ZoneId id);            // 없으면 null
public ZoneData GetZoneByScene(string sceneName);
```

조회는 배열 선형 탐색으로 충분하다. 원소가 4개 이하다.

- [ ] **Step 7: 컴파일 검증**

Global Constraints의 컴파일 검증 명령. 에러 0건.

- [ ] **Step 8: 커밋**

```bash
git add HiddenWeight/Assets/Scripts/Data
git commit -m "[feat] Data 모듈: 플레이어·감정·적·지역 ScriptableObject 정의"
```

---

## Task 3: Core 모듈 — 진행 상태와 게임 매니저 (TDD)

`ProgressState`는 순수 C#이고 게이팅 규칙이 전부 여기 모인다. 여기만 테스트를 쓴다.

**Files:**
- Create: `HiddenWeight/Assets/Tests/EditMode/HiddenWeight.Tests.EditMode.asmdef`
- Create: `HiddenWeight/Assets/Tests/EditMode/ProgressStateTests.cs`
- Create: `HiddenWeight/Assets/Scripts/Core/ProgressState.cs`
- Create: `HiddenWeight/Assets/Scripts/Core/GameState.cs`
- Create: `HiddenWeight/Assets/Scripts/Core/SceneFlow.cs`
- Create: `HiddenWeight/Assets/Scripts/Core/GameManager.cs`
- Create: `HiddenWeight/Assets/Scripts/Core/Checkpoint.cs`
- Create: `HiddenWeight/Assets/Scripts/Core/AudioManager.cs`

**Interfaces:**
- Consumes: `HiddenWeight.Data.{EmotionId, ZoneId, BalanceData, ZoneData}`
- Produces:

```csharp
namespace HiddenWeight.Core
{
    public enum GameState { Boot, Title, Playing, Paused, Ending }

    public class ProgressState
    {
        public bool HasAwareness { get; }
        public bool HasClearedFracture { get; }
        public ZoneId CurrentZone { get; set; }
        public Vector3 LastCheckpoint { get; set; }
        public int FragmentCount { get; }

        public void UnlockSkill(EmotionId id);
        public bool HasSkill(EmotionId id);
        public void GrantAwareness();
        public void MarkFractureCleared();
        public bool CollectFragment(string id);   // 처음 수집이면 true
        public bool HasFragment(string id);
        public bool CanOpenGate(EmotionId required);
        public bool CanOpenFinalGate();
        public void ResetAll();
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; }
        public BalanceData Balance { get; }
        public ProgressState Progress { get; }
        public GameState State { get; }
        public ZoneData CurrentZoneData { get; }
        public event System.Action<GameState> StateChanged;
        public void SetState(GameState next);
        public void EnterZone(ZoneId id);
        public void RespawnPlayer();
    }

    public static class SceneFlow
    {
        public const string Bootstrap = "Bootstrap";
        public const string Title = "Title";
        public const string Prologue = "Zone_Prologue";
        public const string Residue = "Zone_Residue";
        public const string Gaze = "Zone_Gaze";
        public const string Fracture = "Zone_Fracture";
        public const string Ending = "Ending";
        public static void Load(string sceneName);
        public static void LoadWithFade(string sceneName, float fadeSeconds = 0.5f);
    }

    public class Checkpoint : MonoBehaviour { }

    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; }
        public void PlayBgm(AudioClip clip, float fadeSeconds = 1f);
        public void StopBgm(float fadeSeconds = 1f);
        public void PlaySfx(AudioClip clip, float volume = 1f);
    }
}
```

- [ ] **Step 1: 테스트 asmdef 작성**

`HiddenWeight/Assets/Tests/EditMode/HiddenWeight.Tests.EditMode.asmdef`:

```json
{
  "name": "HiddenWeight.Tests.EditMode",
  "rootNamespace": "HiddenWeight.Tests",
  "references": ["HiddenWeight", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": ["Editor"],
  "overrideReferences": true,
  "precompiledReferences": ["nunit.framework.dll"],
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "autoReferenced": false
}
```

- [ ] **Step 2: 실패하는 테스트 작성**

`ProgressStateTests.cs`:

```csharp
using NUnit.Framework;
using HiddenWeight.Core;
using HiddenWeight.Data;

namespace HiddenWeight.Tests
{
    public class ProgressStateTests
    {
        ProgressState _p;

        [SetUp]
        public void SetUp() => _p = new ProgressState();

        [Test]
        public void 시작할때는_아무_스킬도_없다()
        {
            Assert.IsFalse(_p.HasSkill(EmotionId.Rewind));
            Assert.IsFalse(_p.HasSkill(EmotionId.Hush));
            Assert.IsFalse(_p.HasSkill(EmotionId.Foresight));
            Assert.IsFalse(_p.HasAwareness);
        }

        [Test]
        public void 스킬을_해금하면_보유로_바뀐다()
        {
            _p.UnlockSkill(EmotionId.Rewind);
            Assert.IsTrue(_p.HasSkill(EmotionId.Rewind));
            Assert.IsFalse(_p.HasSkill(EmotionId.Hush));
        }

        [Test]
        public void None_게이트는_스킬_없이도_열린다()
        {
            Assert.IsTrue(_p.CanOpenGate(EmotionId.None));
        }

        [Test]
        public void 필요스킬이_없으면_게이트가_닫혀있다()
        {
            Assert.IsFalse(_p.CanOpenGate(EmotionId.Hush));
            _p.UnlockSkill(EmotionId.Hush);
            Assert.IsTrue(_p.CanOpenGate(EmotionId.Hush));
        }

        [Test]
        public void 최종게이트는_세_조건이_전부_충족돼야_열린다()
        {
            Assert.IsFalse(_p.CanOpenFinalGate());

            _p.UnlockSkill(EmotionId.Rewind);
            Assert.IsFalse(_p.CanOpenFinalGate(), "자각과 균열 클리어가 아직 없다");

            _p.GrantAwareness();
            Assert.IsFalse(_p.CanOpenFinalGate(), "균열 클리어가 아직 없다");

            _p.MarkFractureCleared();
            Assert.IsTrue(_p.CanOpenFinalGate());
        }

        [Test]
        public void 파편은_처음_수집할때만_true를_돌려준다()
        {
            Assert.IsTrue(_p.CollectFragment("residue_01"));
            Assert.IsFalse(_p.CollectFragment("residue_01"));
            Assert.AreEqual(1, _p.FragmentCount);
            Assert.IsTrue(_p.HasFragment("residue_01"));
        }

        [Test]
        public void 같은_스킬을_두번_해금해도_상태가_그대로다()
        {
            _p.UnlockSkill(EmotionId.Rewind);
            _p.UnlockSkill(EmotionId.Rewind);
            Assert.IsTrue(_p.HasSkill(EmotionId.Rewind));
        }

        [Test]
        public void ResetAll은_모든_진행도를_지운다()
        {
            _p.UnlockSkill(EmotionId.Rewind);
            _p.GrantAwareness();
            _p.MarkFractureCleared();
            _p.CollectFragment("a");

            _p.ResetAll();

            Assert.IsFalse(_p.HasSkill(EmotionId.Rewind));
            Assert.IsFalse(_p.HasAwareness);
            Assert.IsFalse(_p.HasClearedFracture);
            Assert.AreEqual(0, _p.FragmentCount);
        }
    }
}
```

- [ ] **Step 3: 테스트가 실패하는지 확인**

Global Constraints의 EditMode 테스트 명령 실행.
기대 결과: 컴파일 에러 — `ProgressState`가 아직 없다. 로그에 `error CS0246` 또는 `The type or namespace name 'ProgressState' could not be found`.

- [ ] **Step 4: ProgressState.cs 구현**

```csharp
using System.Collections.Generic;
using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.Core
{
    // 지역을 넘나들며 유지되는 진행도. MonoBehaviour가 아니라 GameManager가 들고 다니는 순수 C# 객체다.
    public class ProgressState
    {
        readonly HashSet<EmotionId> _skills = new HashSet<EmotionId>();
        readonly HashSet<string> _fragments = new HashSet<string>();

        public bool HasAwareness { get; private set; }
        public bool HasClearedFracture { get; private set; }
        public ZoneId CurrentZone { get; set; } = ZoneId.Prologue;
        public Vector3 LastCheckpoint { get; set; }
        public int FragmentCount => _fragments.Count;

        public void UnlockSkill(EmotionId id)
        {
            if (id != EmotionId.None) _skills.Add(id);
        }

        public bool HasSkill(EmotionId id) => _skills.Contains(id);

        public void GrantAwareness() => HasAwareness = true;

        public void MarkFractureCleared() => HasClearedFracture = true;

        public bool CollectFragment(string id) => _fragments.Add(id);

        public bool HasFragment(string id) => _fragments.Contains(id);

        // 게이트가 요구하는 스킬이 None이면 조건 없이 열린다.
        public bool CanOpenGate(EmotionId required)
            => required == EmotionId.None || _skills.Contains(required);

        // 기획서 5.3절: 균열 클리어 후 자각을 갖춘 채 잔재로 백트래킹해야 열리는 최종 파편.
        public bool CanOpenFinalGate()
            => _skills.Contains(EmotionId.Rewind) && HasAwareness && HasClearedFracture;

        public void ResetAll()
        {
            _skills.Clear();
            _fragments.Clear();
            HasAwareness = false;
            HasClearedFracture = false;
            CurrentZone = ZoneId.Prologue;
            LastCheckpoint = Vector3.zero;
        }
    }
}
```

- [ ] **Step 5: 테스트 통과 확인**

Global Constraints의 EditMode 테스트 명령.
기대 결과: `exit=0`, 8개 전부 `result="Passed"`, `Failed` 0건.

- [ ] **Step 6: GameState.cs, SceneFlow.cs 작성**

`GameState`는 위 Interfaces의 enum 그대로.

`SceneFlow.Load`는 `UnityEngine.SceneManagement.SceneManager.LoadScene`을 감싼다.
`LoadWithFade`는 `ScreenFader.Instance`가 있으면 페이드 아웃 후 로드, 없으면 즉시 로드한다.
Task 10에서 `ScreenFader`가 생기기 전까지는 널 체크로 넘어간다.

- [ ] **Step 7: GameManager.cs 작성**

- `Awake`: 이미 `Instance`가 있으면 자기 자신을 파괴, 없으면 `Instance = this` + `DontDestroyOnLoad`
- `[SerializeField] BalanceData balance` — 인스펙터로 연결
- `Progress`는 `Awake`에서 `new ProgressState()`
- `SetState(next)`: 값이 같으면 무시. 다르면 저장하고 `StateChanged` 발행. `Paused`면 `Time.timeScale = 0`, 그 외 1
- `EnterZone(id)`: `Progress.CurrentZone = id`, `CurrentZoneData = balance.GetZone(id)`. `grantedSkill`이 `None`이 아니면 해금하지 않는다 — 해금은 지역 안의 스킬 획득 지점(`ZoneTrigger`가 아니라 별도 픽업)에서 한다
- `RespawnPlayer()`: `PlayerController.Instance`를 `Progress.LastCheckpoint`로 옮기고 `PlayerHealth.RestoreFull()` 호출

`RespawnPlayer`가 `Player` 모듈을 참조하는데, 의존 방향은 `Player → Core`다. 역참조를 피하기 위해 `GameManager`는 `System.Action<Vector3>` 이벤트 `RespawnRequested`를 발행하고, `PlayerHealth`가 구독하도록 한다.

`RespawnPlayer()`의 실제 시그니처는 다음과 같다:

```csharp
public event System.Action<Vector3> RespawnRequested;
public void RespawnPlayer() => RespawnRequested?.Invoke(Progress.LastCheckpoint);
```

- [ ] **Step 8: Checkpoint.cs 작성**

`[RequireComponent(typeof(Collider2D))]`. `OnTriggerEnter2D`에서 상대가 `Player` 레이어면
`GameManager.Instance.Progress.LastCheckpoint = transform.position`.
중복 갱신을 막기 위해 `bool _used` 플래그를 둔다.

- [ ] **Step 9: AudioManager.cs 작성**

`DontDestroyOnLoad` 싱글턴. `AudioSource` 2개(BGM 루프용, SFX 원샷용)를 코드로 붙인다.
`PlayBgm`은 코루틴으로 현재 클립을 페이드 아웃하고 새 클립을 페이드 인한다.
클립이 `null`이면 아무것도 하지 않는다 (MVP에서 사운드 에셋이 없다).

- [ ] **Step 10: 컴파일 + 테스트 검증**

컴파일 검증 명령과 EditMode 테스트 명령을 모두 실행. 둘 다 통과해야 한다.

- [ ] **Step 11: 커밋**

```bash
git add HiddenWeight/Assets/Scripts/Core HiddenWeight/Assets/Tests
git commit -m "[feat] Core 모듈: 진행도·게임매니저·씬 흐름·체크포인트 (ProgressState 테스트 8건)"
```

---

## Task 4: Player 모듈 — 이동·전투·체력

**Files:**
- Create: `HiddenWeight/Assets/Scripts/Player/PlayerInput.cs`
- Create: `HiddenWeight/Assets/Scripts/Player/PlayerState.cs`
- Create: `HiddenWeight/Assets/Scripts/Player/PlayerController.cs`
- Create: `HiddenWeight/Assets/Scripts/Player/PlayerHealth.cs`
- Create: `HiddenWeight/Assets/Scripts/Player/PlayerAttack.cs`
- Create: `HiddenWeight/Assets/Scripts/Player/PlayerAnimator.cs`

**Interfaces:**
- Consumes: `HiddenWeight.Data.PlayerData`, `HiddenWeight.Core.GameManager`
- Produces:

```csharp
namespace HiddenWeight.Player
{
    public static class PlayerInput
    {
        public static bool Enabled { get; set; }        // 기본 true
        public static float Horizontal { get; }         // -1 / 0 / 1
        public static bool RunHeld { get; }             // LeftShift
        public static bool JumpPressed { get; }         // Space down
        public static bool JumpHeld { get; }            // Space
        public static bool DashPressed { get; }         // LeftControl down
        public static bool AttackPressed { get; }       // J down
        public static bool SkillPressed { get; }        // K down
        public static bool SkillHeld { get; }           // K
        public static bool AwarenessHeld { get; }       // L
        public static bool PausePressed { get; }        // Escape down
    }

    public enum PlayerState
    {
        Idle, Walk, Run, Jump, AirMove, Fall, Land, Attack, Dash, WallCling, WallJump
    }

    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; }
        public PlayerState State { get; }
        public bool IsGrounded { get; }
        public bool IsOnWall { get; }
        public int Facing { get; }                             // -1 또는 1
        public float ExternalSpeedMultiplier { get; set; }     // 기본 1. 감정 스킬이 감속시킬 때 사용
        public bool MovementLocked { get; set; }               // 되감기 채널링 중 true
        public event System.Action<PlayerState> StateChanged;
        public void ApplyKnockback(Vector2 direction, float force);
        public void TeleportTo(Vector3 position);
    }

    public class PlayerHealth : MonoBehaviour
    {
        public int Current { get; }
        public int Max { get; }
        public bool IsInvulnerable { get; }
        public event System.Action<int, int> HealthChanged;    // (현재, 최대)
        public void TakeDamage(int amount, Vector2 sourcePosition);
        public void RestoreFull();
    }

    public class PlayerAttack : MonoBehaviour
    {
        public bool CanAttack { get; set; }                    // 숨죽이기 중 false
        public event System.Action Attacked;
    }

    public class PlayerAnimator : MonoBehaviour { }
}
```

- [ ] **Step 1: PlayerInput.cs 작성**

`Enabled`가 false면 모든 프로퍼티가 0 / false를 돌려준다. 키 상수는 이 파일에만 둔다.

```csharp
public static float Horizontal
    => Enabled ? Input.GetAxisRaw("Horizontal") : 0f;
public static bool JumpPressed
    => Enabled && Input.GetKeyDown(KeyCode.Space);
```

나머지도 같은 형태. `RunHeld`는 `KeyCode.LeftShift`, `DashPressed`는 `KeyCode.LeftControl`,
`AttackPressed`는 `KeyCode.J`, `SkillPressed`/`SkillHeld`는 `KeyCode.K`,
`AwarenessHeld`는 `KeyCode.L`, `PausePressed`는 `KeyCode.Escape`.

`PausePressed`만 `Enabled`와 무관하게 항상 동작해야 한다 (일시정지 해제용).

- [ ] **Step 2: PlayerState.cs 작성**

위 enum 그대로. `PlayerAnimator`가 이 값을 그대로 애니메이터 정수 파라미터로 넘긴다.

- [ ] **Step 3: PlayerController.cs 작성**

`[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]`

**접지·벽 판정** — `Physics2D.OverlapBox`를 쓴다. 인스펙터에 `groundCheck`, `wallCheck` Transform과
`groundLayer`(Ground), `wallLayer`(Wall) LayerMask를 노출한다.

**FixedUpdate 순서:**

1. 접지·벽 판정 갱신
2. 코요테 타이머와 점프 버퍼 타이머 갱신

```csharp
_coyoteTimer = IsGrounded ? _data.coyoteTime : _coyoteTimer - Time.fixedDeltaTime;
_jumpBufferTimer = PlayerInput.JumpPressed ? _data.jumpBufferTime : _jumpBufferTimer - Time.fixedDeltaTime;
```

3. `MovementLocked`면 수평 속도를 0으로 만들고 상태를 `Idle`로 두고 나머지를 건너뛴다
4. 대시 처리 — `_dashTimer > 0`이면 `Facing` 방향으로 `dashDistance / dashDuration` 속도 고정, 중력 0
5. 벽점프 잠금 처리 — `_wallJumpLockTimer > 0`이면 좌우 입력을 무시
6. 수평 이동 — `속도 = (RunHeld ? runSpeed : walkSpeed) * ExternalSpeedMultiplier`
7. 벽잡기 — 공중이고 `IsOnWall`이고 벽 방향으로 입력 중이면 y속도를 `-wallSlideSpeed`로 고정
8. 점프 — `_jumpBufferTimer > 0`이고 (`_coyoteTimer > 0` 또는 벽잡기 중)이면 발동. 벽잡기 중이면 벽점프
9. 가변 점프 — 상승 중(`velocity.y > 0`)에 `JumpHeld`가 false면 `velocity.y *= variableJumpCut`
10. 하강 중력 — `velocity.y < 0`이면 `gravityScale * fallGravityMultiplier`
11. 상태 결정 후 `StateChanged` 발행

**상태 결정 규칙** (우선순위 순):

| 조건 | 상태 |
|---|---|
| `_attackTimer > 0` | `Attack` |
| `_dashTimer > 0` | `Dash` |
| `_wallJumpLockTimer > 0` | `WallJump` |
| 공중 + 벽잡기 중 | `WallCling` |
| 착지 직후 0.12초 | `Land` |
| 공중 + `velocity.y > 0` + 수평 입력 있음 | `AirMove` |
| 공중 + `velocity.y > 0` | `Jump` |
| 공중 + `velocity.y <= 0` | `Fall` |
| 지상 + 수평 입력 있음 + `RunHeld` | `Run` |
| 지상 + 수평 입력 있음 | `Walk` |
| 그 외 | `Idle` |

`Facing`은 수평 입력이 0이 아닐 때만 갱신한다.

`ApplyKnockback(dir, force)`은 `Rigidbody2D.linearVelocity`를 `dir.normalized * force`로 덮어쓰고,
0.2초간 수평 입력을 무시하는 타이머를 건다.

`TeleportTo(pos)`는 위치를 옮기고 속도를 0으로 만든다.

- [ ] **Step 4: PlayerHealth.cs 작성**

- `Awake`에서 `GameManager.Instance.Balance.player`로 `maxHealth`, `invulnerableTime`, `knockbackForce`를 읽는다
- `TakeDamage`: `IsInvulnerable`이면 무시. 아니면 HP 감소, `HealthChanged` 발행,
  `PlayerController.ApplyKnockback((transform.position - sourcePosition).normalized, knockbackForce)`,
  무적 타이머 시작 + 스프라이트 0.1초 간격 점멸 코루틴
- HP가 0 이하가 되면 `GameManager.Instance.RespawnPlayer()` 호출. 게임오버 화면은 없다
- `OnEnable`에서 `GameManager.Instance.RespawnRequested += HandleRespawn` 구독,
  `OnDisable`에서 해제. `HandleRespawn(pos)`는 `PlayerController.TeleportTo(pos)` + `RestoreFull()`

- [ ] **Step 5: PlayerAttack.cs 작성**

- `J` 입력 + 쿨타임 종료 + `CanAttack`이면 발동
- `Physics2D.OverlapCircleAll(transform.position, attackRadius, enemyLayer)`로 후보를 모으고,
  각 후보의 방향 벡터와 `Facing` 벡터의 각도가 `attackAngle / 2` 이내인 것만 남긴다

```csharp
var toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
var facingVec = new Vector2(_controller.Facing, 0f);
if (Vector2.Angle(facingVec, toTarget) <= _data.attackAngle * 0.5f) { /* 명중 */ }
```

- 명중한 각 대상의 `Enemy.TakeDamage(_data.attackDamage, transform.position)` 호출.
  `Enemy`는 Task 9에서 만든다. **이 태스크에서는 `Enemies` 네임스페이스를 참조하지 않고**,
  `Attacked` 이벤트만 발행하고 실제 피해 적용은 Task 9에서 이 파일에 추가한다
- `Attacked` 이벤트 발행, `attackActiveTime`동안 `PlayerState.Attack` 유지

- [ ] **Step 6: PlayerAnimator.cs 작성**

`PlayerController.StateChanged`를 구독해 `Animator.SetInteger("State", (int)state)`를 호출하고,
`SpriteRenderer.flipX = Facing < 0`을 매 프레임 갱신한다.
`Animator` 컴포넌트가 없으면(플레이스홀더 단계) 아무것도 하지 않고 `flipX`만 처리한다.

- [ ] **Step 7: 컴파일 검증**

컴파일 검증 명령. 에러 0건.

- [ ] **Step 8: 커밋**

```bash
git add HiddenWeight/Assets/Scripts/Player
git commit -m "[feat] Player 모듈: 이동·점프·대시·벽점프 상태머신 + 체력·근접공격"
```

---

## Task 5: World 모듈 A — 인터페이스와 카메라 룸

**Files:**
- Create: `HiddenWeight/Assets/Scripts/World/Interactions.cs`
- Create: `HiddenWeight/Assets/Scripts/World/Room.cs`
- Create: `HiddenWeight/Assets/Scripts/World/RoomCamera.cs`

**Interfaces:**
- Consumes: `HiddenWeight.Player.PlayerController`
- Produces:

```csharp
namespace HiddenWeight.World
{
    // 되감기(잔재) 대상. 전체 시간이 아니라 오브젝트 단위로 되돌린다 (기획서 4.2절).
    public interface IRewindable
    {
        Transform Transform { get; }
        bool CanRewind { get; }        // 이미 초기 상태면 false
        void CaptureInitial();         // Start에서 1회
        void Rewind();
    }

    // 자각(L 홀드) 중에만 반응하는 오브젝트.
    public interface IAwarenessReactive
    {
        void OnAwarenessChanged(bool active);
    }

    // 예지(균열) 대상. leadSeconds 뒤의 상태를 예측해 돌려준다.
    public interface IForeseeable
    {
        Transform Transform { get; }
        Vector3 PredictPosition(float leadSeconds);
        bool PredictActive(float leadSeconds);   // false면 그때는 사라져 있다
        Sprite CurrentSprite { get; }            // 고스트에 그대로 쓴다
    }

    public class Room : MonoBehaviour
    {
        public Bounds WorldBounds { get; }
        public bool Contains(Vector3 point);
    }

    public class RoomCamera : MonoBehaviour
    {
        public static RoomCamera Instance { get; }
        public Room CurrentRoom { get; }
        public void SetRoom(Room room);
        public void SnapToPlayer();
    }
}
```

- [ ] **Step 1: Interactions.cs 작성**

위 인터페이스 3개를 한 파일에 담는다. `using UnityEngine;` 필요.
세 인터페이스가 항상 함께 읽히므로 파일을 나누지 않는다.

- [ ] **Step 2: Room.cs 작성**

- `[SerializeField] Vector2 size = new Vector2(24, 14)` — 룸 크기(월드 유닛)
- `WorldBounds`는 `new Bounds(transform.position, new Vector3(size.x, size.y, 1f))`
- `Contains(point)`는 x·y만 비교한다 (z 무시)
- `OnDrawGizmos`로 씬 뷰에 노란 와이어 사각형을 그린다 — 레벨 배치 때 필수
- `OnTriggerEnter2D`에서 상대가 `Player` 레이어면 `RoomCamera.Instance.SetRoom(this)`.
  이를 위해 `BoxCollider2D`를 `isTrigger = true`로 두고 크기를 `size`와 동기화한다
  (`OnValidate`에서 자동 동기화)

- [ ] **Step 3: RoomCamera.cs 작성**

- `Camera`가 붙은 오브젝트에 부착. `Awake`에서 `Instance = this`
- `LateUpdate`에서 플레이어를 따라가되, 결과 위치를 `CurrentRoom.WorldBounds` 안으로 클램프한다

```csharp
var half = new Vector2(_cam.orthographicSize * _cam.aspect, _cam.orthographicSize);
var b = CurrentRoom.WorldBounds;
// 룸이 화면보다 작으면 룸 중심에 고정한다.
float x = b.size.x <= half.x * 2f
    ? b.center.x
    : Mathf.Clamp(target.x, b.min.x + half.x, b.max.x - half.x);
float y = b.size.y <= half.y * 2f
    ? b.center.y
    : Mathf.Clamp(target.y, b.min.y + half.y, b.max.y - half.y);
transform.position = Vector3.Lerp(transform.position, new Vector3(x, y, transform.position.z), followLerp * Time.deltaTime);
```

- `CurrentRoom`이 null이면 플레이어를 그냥 따라간다
- `SetRoom(room)`은 `CurrentRoom`을 바꾸기만 한다. 급전환을 막기 위해 Lerp가 자연히 처리한다
- `SnapToPlayer()`는 Lerp 없이 즉시 이동 (씬 진입·리스폰용)

- [ ] **Step 4: 컴파일 검증**

컴파일 검증 명령. 에러 0건.

- [ ] **Step 5: 커밋**

```bash
git add HiddenWeight/Assets/Scripts/World
git commit -m "[feat] World 모듈: 상호작용 인터페이스 3종 + 룸 단위 카메라"
```

---

## Task 6: World 모듈 B — 장애물·게이트·파편

**Files:**
- Create: `HiddenWeight/Assets/Scripts/World/Rewindable.cs`
- Create: `HiddenWeight/Assets/Scripts/World/CrumblingPlatform.cs`
- Create: `HiddenWeight/Assets/Scripts/World/MovingPlatform.cs`
- Create: `HiddenWeight/Assets/Scripts/World/GazeHazard.cs`
- Create: `HiddenWeight/Assets/Scripts/World/Gate.cs`
- Create: `HiddenWeight/Assets/Scripts/World/StoryFragment.cs`
- Create: `HiddenWeight/Assets/Scripts/World/HiddenFragment.cs`
- Create: `HiddenWeight/Assets/Scripts/World/ZoneTrigger.cs`

**Interfaces:**
- Consumes: `IRewindable`, `IForeseeable`, `IAwarenessReactive` (Task 5), `HiddenWeight.Core.GameManager`, `HiddenWeight.Data.EmotionId`
- Produces:

```csharp
namespace HiddenWeight.World
{
    public class Rewindable : MonoBehaviour, IRewindable { }

    public class CrumblingPlatform : MonoBehaviour, IRewindable, IForeseeable
    {
        public bool HasCrumbled { get; }
    }

    public class MovingPlatform : MonoBehaviour, IForeseeable { }

    public class GazeHazard : MonoBehaviour
    {
        public bool IsPlayerSeen { get; }
    }

    public class Gate : MonoBehaviour
    {
        public bool IsOpen { get; }
        public EmotionId RequiredSkill { get; }
    }

    public class StoryFragment : MonoBehaviour
    {
        public string FragmentId { get; }
        protected virtual bool IsCollectable { get; }
        public virtual void Collect();
    }

    public class HiddenFragment : StoryFragment, IAwarenessReactive { }

    public class ZoneTrigger : MonoBehaviour { }
}
```

- [ ] **Step 1: Rewindable.cs 작성**

`IRewindable`의 기본 구현. 부서지거나 옮겨진 오브젝트를 원래 자리로 되돌린다.

- `Start`에서 `CaptureInitial()` — `transform.position`, `transform.rotation`,
  `gameObject.activeSelf`, `SpriteRenderer.sprite`를 저장
- `CanRewind`는 현재 상태가 초기 상태와 다를 때 true.
  위치 비교는 `Vector3.SqrMagnitude(현재 - 초기) > 0.0001f`
- `Rewind()`는 저장한 값을 복원하고, `Rigidbody2D`가 있으면 속도를 0으로 만든 뒤
  0.3초에 걸쳐 스케일을 0.8 → 1.0으로 튕기는 연출 코루틴을 돌린다
- `Transform`은 `=> transform`

- [ ] **Step 2: CrumblingPlatform.cs 작성**

`IRewindable`과 `IForeseeable`을 둘 다 구현한다. 되감기로 복구되고, 예지로 무너진 뒤를 미리 본다.

- `[SerializeField] float crumbleDelay = 0.6f` — 밟은 뒤 무너지기까지
- `[SerializeField] float respawnDelay = 0f` — 0이면 되감기로만 복구된다
- `OnCollisionEnter2D`에서 플레이어가 위에서 닿으면 흔들림 시작 → `crumbleDelay` 후
  `Collider2D.enabled = false`, `SpriteRenderer.enabled = false`, `HasCrumbled = true`
- `CanRewind`는 `HasCrumbled`
- `Rewind()`는 콜라이더·렌더러를 되살리고 `HasCrumbled = false`
- `PredictActive(lead)`는 `_crumbleTimer > 0 && _crumbleTimer <= lead`면 false, 그 외 `!HasCrumbled`
- `PredictPosition(lead)`는 `transform.position` (움직이지 않는다)

- [ ] **Step 3: MovingPlatform.cs 작성**

`IForeseeable`만 구현한다.

- `[SerializeField] Vector2 offset = new Vector2(6, 0)` — 시작점 기준 왕복 끝점
- `[SerializeField] float period = 4f` — 왕복 1회 주기
- 위치는 시간 기반 순수 함수로 계산한다. 예지가 미래를 정확히 예측하려면 이래야 한다

```csharp
Vector3 PositionAt(float time)
{
    float t = Mathf.PingPong(time / (period * 0.5f), 1f);
    return _origin + (Vector3)offset * Mathf.SmoothStep(0f, 1f, t);
}

void FixedUpdate() => _rb.MovePosition(PositionAt(Time.time));
public Vector3 PredictPosition(float lead) => PositionAt(Time.time + lead);
public bool PredictActive(float lead) => true;
```

- `Rigidbody2D`는 `Kinematic`으로 두고, 플레이어를 함께 옮기기 위해 발판 위 플레이어를
  `transform.SetParent`로 붙였다가 떼는 대신 **플랫폼 이동량을 플레이어 위치에 더하는 방식**을 쓴다
  (부모 변경은 스케일 오염을 일으킨다)

```csharp
void FixedUpdate()
{
    var next = PositionAt(Time.time);
    var delta = next - transform.position;
    _rb.MovePosition(next);
    if (_riderOnTop != null) _riderOnTop.position += delta;
}
```

`_riderOnTop`은 `OnCollisionEnter2D` / `OnCollisionExit2D`로 갱신한다.

- [ ] **Step 4: GazeHazard.cs 작성**

응시 지역의 "시선" 기믹. 원뿔 시야 안에 플레이어가 있으면 피해를 준다.

- `[SerializeField] float viewRadius = 6f`, `float viewAngle = 60f`, `float damageInterval = 1f`
- `[SerializeField] LayerMask playerMask` — 인스펙터에서 `Player`만 지정한다.
  **`PlayerHushed`는 넣지 않는다.** 숨죽이기 중 플레이어는 레이어가 바뀌므로 자동으로 무해화된다
  (기획서 4.2절). 이것이 `World`가 `Emotions`를 참조하지 않고도 동작하는 이유다
- `Update`에서 `Physics2D.OverlapCircle(transform.position, viewRadius, playerMask)`로 찾고,
  각도가 `viewAngle / 2` 이내이고 `Physics2D.Linecast`로 `Ground`에 가리지 않으면 명중
- 명중 시 `damageInterval` 간격으로 `PlayerHealth.TakeDamage(1, transform.position)`
- `OnDrawGizmosSelected`로 시야 원뿔을 그린다

- [ ] **Step 5: Gate.cs 작성**

- `[SerializeField] EmotionId requiredSkill`
- `[SerializeField] bool requiresFinalCondition` — 잔재 백트래킹 최종 파편용
- `[SerializeField] GameObject blocker` — 실제로 길을 막는 콜라이더 오브젝트
- `[SerializeField] SpriteRenderer hintIcon` — 필요 스킬 아이콘. 없으면 무시
- `Update`에서 조건을 확인하고 `blocker.SetActive(!IsOpen)`

```csharp
public bool IsOpen
{
    get
    {
        var p = GameManager.Instance.Progress;
        return requiresFinalCondition ? p.CanOpenFinalGate() : p.CanOpenGate(requiredSkill);
    }
}
```

- [ ] **Step 6: StoryFragment.cs 작성**

- `[SerializeField] string fragmentId` — 지역별 고유 문자열 (`residue_01` 등)
- `[SerializeField, TextArea(2, 4)] string text` — 화면에 뜰 한 줄
- `[SerializeField] EmotionId grantsSkill = EmotionId.None` — 스킬 획득 지점으로도 쓴다
- `[SerializeField] bool grantsAwareness` — 응시 지역의 자각 해금 지점
- `protected virtual bool IsCollectable => true`
- `OnTriggerEnter2D`에서 플레이어면 `Collect()`

```csharp
public virtual void Collect()
{
    if (!IsCollectable) return;
    var p = GameManager.Instance.Progress;
    if (!p.CollectFragment(fragmentId)) return;      // 이미 먹은 것
    if (grantsSkill != EmotionId.None) p.UnlockSkill(grantsSkill);
    if (grantsAwareness) p.GrantAwareness();
    FragmentLog.Instance?.Show(text);                 // Task 10에서 생김. 그때까지 null 체크로 넘어간다
    gameObject.SetActive(false);
}
```

`FragmentLog`가 아직 없으므로 이 태스크에서는 해당 줄을 `Debug.Log(text)`로 두고,
Task 10에서 교체한다. **교체를 잊지 않도록 `// TODO(Task 10)` 대신 실제로 Task 10 Step에 명시한다.**

- [ ] **Step 7: HiddenFragment.cs 작성**

```csharp
public class HiddenFragment : StoryFragment, IAwarenessReactive
{
    [SerializeField] SpriteRenderer visual;
    bool _revealed;

    protected override bool IsCollectable => _revealed;

    void OnEnable() => AwarenessSystem.Register(this);   // Task 8에서 생김
    void OnDisable() => AwarenessSystem.Unregister(this);

    public void OnAwarenessChanged(bool active)
    {
        _revealed = active;
        if (visual != null) visual.enabled = active;
    }
}
```

`AwarenessSystem`은 Task 8에서 만든다. 이 태스크에서는 `OnEnable`/`OnDisable`의 등록 줄을
비워두고 Task 8에서 채운다 — 컴파일 순서 때문이다.

- [ ] **Step 8: ZoneTrigger.cs 작성**

지역 클리어 지점. 플레이어가 닿으면 다음 씬으로 넘어간다.

- `[SerializeField] bool marksFractureCleared` — 균열 지역 출구만 true
- `OnTriggerEnter2D`에서 플레이어면:

```csharp
var gm = GameManager.Instance;
if (marksFractureCleared) gm.Progress.MarkFractureCleared();
var next = gm.CurrentZoneData != null ? gm.CurrentZoneData.nextSceneName : SceneFlow.Title;
SceneFlow.LoadWithFade(next);
```

- [ ] **Step 9: 컴파일 검증**

컴파일 검증 명령. 에러 0건.

- [ ] **Step 10: 커밋**

```bash
git add HiddenWeight/Assets/Scripts/World
git commit -m "[feat] World 모듈: 되감기 대상·무너지는 발판·이동 발판·시선 기믹·게이트·파편"
```

---

## Task 7: Emotions 모듈 — 감정 스킬 3종

**Files:**
- Create: `HiddenWeight/Assets/Scripts/Emotions/EmotionSkill.cs`
- Create: `HiddenWeight/Assets/Scripts/Emotions/RewindSkill.cs`
- Create: `HiddenWeight/Assets/Scripts/Emotions/HushSkill.cs`
- Create: `HiddenWeight/Assets/Scripts/Emotions/ForesightSkill.cs`
- Create: `HiddenWeight/Assets/Scripts/Emotions/EmotionSkillController.cs`

**Interfaces:**
- Consumes: `HiddenWeight.World.{IRewindable, IForeseeable}`, `HiddenWeight.Player.{PlayerInput, PlayerController, PlayerAttack}`, `HiddenWeight.Data.EmotionData`, `HiddenWeight.Core.GameManager`
- Produces:

```csharp
namespace HiddenWeight.Emotions
{
    public abstract class EmotionSkill : MonoBehaviour
    {
        public abstract EmotionId Id { get; }
        public EmotionData Data { get; set; }
        public bool IsActive { get; protected set; }
        public float CooldownRemaining { get; protected set; }
        public virtual bool CanUse { get; }
        protected PlayerController Player { get; }

        public void Begin();          // 컨트롤러가 호출. 내부에서 OnBegin
        public void Tick(float dt);   // 내부에서 OnTick
        public void End();            // 내부에서 OnEnd

        protected abstract void OnBegin();
        protected abstract void OnTick(float dt);
        protected abstract void OnEnd();
    }

    public class RewindSkill : EmotionSkill
    {
        public float ChannelProgress { get; }   // 0~1. HUD가 읽는다
    }

    public class HushSkill : EmotionSkill { }

    public class ForesightSkill : EmotionSkill { }

    public class EmotionSkillController : MonoBehaviour
    {
        public static EmotionSkillController Instance { get; }
        public EmotionSkill Active { get; }
        public EmotionId CurrentEmotion { get; }
        public event System.Action<EmotionId> EmotionChanged;
    }
}
```

- [ ] **Step 1: EmotionSkill.cs 작성**

- `Awake`에서 `Player = PlayerController.Instance` 캐시
- `CanUse`는 기본적으로 `CooldownRemaining <= 0 && !IsActive`
- `Update`에서 `CooldownRemaining`을 감소시킨다
- `Begin()`은 `CanUse`가 false면 무시. true면 `IsActive = true` 후 `OnBegin()`
- `End()`는 `IsActive`가 false면 무시. true면 `OnEnd()` 후 `IsActive = false`,
  `CooldownRemaining = Data.cooldown`
- `Data.moveSpeedMultiplier`를 스킬 활성/비활성 시 `Player.ExternalSpeedMultiplier`에
  적용/복구하는 공통 처리를 여기서 한다. `moveSpeedMultiplier`가 0이면
  `Player.MovementLocked = true`로 바꾼다 (되감기)

- [ ] **Step 2: RewindSkill.cs 작성**

홀드하는 동안 채널링하고, `channelTime`을 채우면 대상이 되감긴다. 채널링 중 이동 불가.

```csharp
public override EmotionId Id => EmotionId.Rewind;

IRewindable _target;
float _channel;
public float ChannelProgress => Data.channelTime <= 0f ? 1f : _channel / Data.channelTime;

protected override void OnBegin()
{
    _target = FindNearestTarget();
    _channel = 0f;
    if (_target == null) { End(); return; }   // 대상이 없으면 즉시 취소, 쿨타임 없음
}

protected override void OnTick(float dt)
{
    if (_target == null || !_target.CanRewind) { End(); return; }
    _channel += dt;
    if (_channel >= Data.channelTime)
    {
        _target.Rewind();
        End();
    }
}

protected override void OnEnd()
{
    _target = null;
    _channel = 0f;
}

IRewindable FindNearestTarget()
{
    var hits = Physics2D.OverlapCircleAll(Player.transform.position, Data.range, interactableMask);
    IRewindable best = null;
    float bestSqr = float.MaxValue;
    foreach (var h in hits)
    {
        var r = h.GetComponentInParent<IRewindable>();
        if (r == null || !r.CanRewind) continue;
        float sqr = ((Vector2)r.Transform.position - (Vector2)Player.transform.position).sqrMagnitude;
        if (sqr < bestSqr) { bestSqr = sqr; best = r; }
    }
    return best;
}
```

대상 없이 취소된 경우 쿨타임을 걸지 않도록, `OnEnd`에서 `_channel == 0 && _target == null`이면
베이스의 쿨타임 설정을 건너뛴다. 이를 위해 `EmotionSkill`에 `protected bool SkipCooldown` 필드를 둔다.

- [ ] **Step 3: HushSkill.cs 작성**

홀드 중 축소·은신. 레이어를 `PlayerHushed`로 바꿔 `GazeHazard`의 시야에서 벗어난다.

```csharp
public override EmotionId Id => EmotionId.Hush;

int _originalLayer;
Vector3 _originalScale;

protected override void OnBegin()
{
    _originalLayer = Player.gameObject.layer;
    _originalScale = Player.transform.localScale;
    Player.gameObject.layer = LayerMask.NameToLayer("PlayerHushed");
    Player.transform.localScale = _originalScale * Data.hushScale;
    var atk = Player.GetComponent<PlayerAttack>();
    if (atk != null) atk.CanAttack = false;
}

protected override void OnTick(float dt) { }

protected override void OnEnd()
{
    Player.gameObject.layer = _originalLayer;
    Player.transform.localScale = _originalScale;
    var atk = Player.GetComponent<PlayerAttack>();
    if (atk != null) atk.CanAttack = true;
}
```

축소 상태에서 좁은 틈을 지나려면 콜라이더도 줄어야 한다. `localScale` 변경이 `CapsuleCollider2D`에
자동 반영되므로 별도 처리는 필요 없다.

- [ ] **Step 4: ForesightSkill.cs 작성**

탭 입력. 반경 안 `IForeseeable`들의 `previewLeadTime` 뒤 상태를 반투명 고스트로 `effectDuration`동안 보여준다.

```csharp
public override EmotionId Id => EmotionId.Foresight;

readonly List<GameObject> _ghosts = new List<GameObject>();
float _timer;

protected override void OnBegin()
{
    _timer = Data.effectDuration;
    var hits = Physics2D.OverlapCircleAll(Player.transform.position, Data.range);
    foreach (var h in hits)
    {
        var f = h.GetComponentInParent<IForeseeable>();
        if (f == null) continue;
        if (!f.PredictActive(Data.previewLeadTime)) continue;   // 그때는 사라져 있다 → 고스트 없음
        SpawnGhost(f);
    }
}

void SpawnGhost(IForeseeable f)
{
    var go = new GameObject("ForesightGhost");
    go.transform.position = f.PredictPosition(Data.previewLeadTime);
    go.transform.localScale = f.Transform.localScale;
    var sr = go.AddComponent<SpriteRenderer>();
    sr.sprite = f.CurrentSprite;
    sr.color = new Color(1f, 1f, 1f, 0.35f);
    sr.sortingOrder = 50;
    _ghosts.Add(go);
}

protected override void OnTick(float dt)
{
    _timer -= dt;
    if (_timer <= 0f) End();
}

protected override void OnEnd()
{
    foreach (var g in _ghosts) if (g != null) Destroy(g);
    _ghosts.Clear();
}
```

무너질 발판이 "무너진 뒤의 형태"를 보여줘야 하는데, `PredictActive`가 false면 고스트를
띄우지 않는 것으로 대신한다 — 발판이 있어야 할 자리에 아무것도 안 보이는 것이 곧 경고다.

- [ ] **Step 5: EmotionSkillController.cs 작성**

플레이어에 붙는다. 세 스킬 컴포넌트를 전부 들고 있다가, 현재 지역의 `grantedSkill` 중
**보유한** 것 하나를 활성으로 지정한다.

```csharp
void RefreshActive()
{
    var gm = GameManager.Instance;
    var zone = gm.CurrentZoneData;
    var wanted = zone != null ? zone.grantedSkill : EmotionId.None;

    // 지역이 주는 스킬을 아직 못 얻었으면 활성 스킬이 없다.
    if (wanted == EmotionId.None || !gm.Progress.HasSkill(wanted))
    {
        SetActive(null);
        return;
    }
    SetActive(_skills.Find(s => s.Id == wanted));
}
```

`Update`:

```csharp
if (Active == null) { RefreshActive(); return; }

if (Active.Data.inputMode == SkillInput.Hold)
{
    if (PlayerInput.SkillHeld && !Active.IsActive) Active.Begin();
    else if (!PlayerInput.SkillHeld && Active.IsActive) Active.End();
}
else // Tap
{
    if (PlayerInput.SkillPressed && !Active.IsActive) Active.Begin();
}

if (Active.IsActive) Active.Tick(Time.deltaTime);
```

`RefreshActive`는 매 프레임이 아니라 지역 진입 시와 파편 수집 시에만 부르면 되지만,
MVP에서는 `Update` 시작 부분에서 `Active == null`일 때만 호출해 비용을 줄인다.
스킬 획득 직후 반영되도록 `StoryFragment.Collect()`가
`EmotionSkillController.Instance?.RefreshActive()`를 호출하게 한다.
`RefreshActive`를 `public`으로 노출한다.

- [ ] **Step 6: StoryFragment에 스킬 갱신 호출 추가**

Task 6에서 만든 `StoryFragment.Collect()` 끝에 아래 한 줄을 넣는다.

```csharp
EmotionSkillController.Instance?.RefreshActive();
```

- [ ] **Step 7: 컴파일 검증**

컴파일 검증 명령. 에러 0건.

- [ ] **Step 8: 커밋**

```bash
git add HiddenWeight/Assets/Scripts/Emotions HiddenWeight/Assets/Scripts/World/StoryFragment.cs
git commit -m "[feat] Emotions 모듈: 되감기·숨죽이기·예지 + K 단일키 자동 전환"
```

---

## Task 8: 자각 시스템

**Files:**
- Create: `HiddenWeight/Assets/Scripts/Emotions/AwarenessSystem.cs`
- Modify: `HiddenWeight/Assets/Scripts/World/HiddenFragment.cs` (등록 줄 채우기)

**Interfaces:**
- Consumes: `HiddenWeight.World.IAwarenessReactive`, `HiddenWeight.Player.PlayerInput`, `HiddenWeight.Core.GameManager`
- Produces:

```csharp
namespace HiddenWeight.Emotions
{
    public class AwarenessSystem : MonoBehaviour
    {
        public static AwarenessSystem Instance { get; }
        public bool IsActive { get; }
        public bool IsStable { get; }          // 균열 지역에서 false
        public event System.Action<bool> AwarenessChanged;

        public static void Register(IAwarenessReactive reactive);
        public static void Unregister(IAwarenessReactive reactive);
    }
}
```

`Register`/`Unregister`는 static이며, `Instance`가 아직 없어도 안전해야 한다
(씬 로드 순서에 따라 `HiddenFragment.OnEnable`이 먼저 돌 수 있다).
static 리스트에 담아두고 `Instance`가 생기면 그때 동기화한다.

- [ ] **Step 1: AwarenessSystem.cs 작성**

```csharp
static readonly List<IAwarenessReactive> _reactives = new List<IAwarenessReactive>();

public static void Register(IAwarenessReactive r)
{
    if (r != null && !_reactives.Contains(r)) _reactives.Add(r);
    // 이미 자각이 켜져 있는 상태에서 새 오브젝트가 들어오면 즉시 동기화한다.
    if (Instance != null && Instance.IsActive) r?.OnAwarenessChanged(true);
}

public static void Unregister(IAwarenessReactive r) => _reactives.Remove(r);
```

**활성 조건** — `GameManager.Instance.Progress.HasAwareness`가 true이고 `PlayerInput.AwarenessHeld`.

**Update 흐름:**

1. 원하는 상태를 계산하고, 직전 상태와 다르면 전환한다
2. 켜질 때: `Player.ExternalSpeedMultiplier = 0.6f`, 볼륨 가중치를 0.25초에 걸쳐 1로 올린다,
   모든 리스너에 `OnAwarenessChanged(true)`
3. 꺼질 때: 배율 복구, 가중치를 0으로, 리스너에 `OnAwarenessChanged(false)`

**불안정 처리 (균열 지역)** — `IsStable`은 `GameManager.Instance.CurrentZoneData?.awarenessStable ?? true`.
false면 자각이 켜져 있는 동안 `0.3~0.8초` 간격으로 리스너 중 **절반을 무작위로 골라**
`OnAwarenessChanged(false)`를 보냈다가 다시 `true`로 되돌린다.

```csharp
IEnumerator UnstableFlicker()
{
    while (IsActive && !IsStable)
    {
        yield return new WaitForSeconds(Random.Range(0.3f, 0.8f));
        if (_reactives.Count == 0) continue;
        int n = Mathf.Max(1, _reactives.Count / 2);
        for (int i = 0; i < n; i++)
        {
            var pick = _reactives[Random.Range(0, _reactives.Count)];
            pick?.OnAwarenessChanged(false);
        }
        yield return new WaitForSeconds(0.15f);
        foreach (var r in _reactives) r?.OnAwarenessChanged(true);
    }
}
```

**볼륨 처리** — `Awake`에서 자기 GameObject에 `Volume` 컴포넌트를 붙이고
`profile = GameManager.Instance.Balance.awarenessProfile`, `weight = 0`, `priority = 10`으로 둔다.
`using UnityEngine.Rendering;` 필요.

- [ ] **Step 2: HiddenFragment 등록 줄 채우기**

Task 6 Step 7에서 비워둔 `OnEnable`/`OnDisable`을 채운다.

```csharp
void OnEnable() => AwarenessSystem.Register(this);
void OnDisable() => AwarenessSystem.Unregister(this);
```

`World`가 `Emotions`를 참조하게 되어 의존 방향이 뒤집힌다. 이를 막기 위해
**`AwarenessSystem`을 `Emotions`가 아니라 `World` 네임스페이스에 두지 않고**,
등록 창구만 `World/Interactions.cs`에 static 클래스로 분리한다.

```csharp
// World/Interactions.cs 에 추가
namespace HiddenWeight.World
{
    public static class AwarenessRegistry
    {
        static readonly List<IAwarenessReactive> _items = new List<IAwarenessReactive>();
        public static IReadOnlyList<IAwarenessReactive> Items => _items;
        public static event System.Action<IAwarenessReactive> Added;

        public static void Register(IAwarenessReactive r)
        {
            if (r != null && !_items.Contains(r)) { _items.Add(r); Added?.Invoke(r); }
        }
        public static void Unregister(IAwarenessReactive r) => _items.Remove(r);
    }
}
```

`HiddenFragment`는 `AwarenessRegistry.Register(this)`를 부르고,
`AwarenessSystem`(Emotions)이 `AwarenessRegistry.Items`를 읽는다. 의존 방향이 지켜진다.
Step 1의 `AwarenessSystem.Register`/`Unregister` static 메서드는 만들지 않는다.

- [ ] **Step 3: Step 1의 AwarenessSystem을 AwarenessRegistry 기반으로 수정**

`_reactives`를 자체 리스트가 아니라 `AwarenessRegistry.Items`로 바꾼다.
자각이 켜진 상태에서 새로 등록된 오브젝트를 동기화하기 위해 `AwarenessRegistry.Added`를 구독한다.

```csharp
void OnEnable() => AwarenessRegistry.Added += HandleAdded;
void OnDisable() => AwarenessRegistry.Added -= HandleAdded;
void HandleAdded(IAwarenessReactive r) { if (IsActive) r.OnAwarenessChanged(true); }
```

- [ ] **Step 4: 컴파일 검증**

컴파일 검증 명령. 에러 0건.

- [ ] **Step 5: 커밋**

```bash
git add HiddenWeight/Assets/Scripts/Emotions HiddenWeight/Assets/Scripts/World
git commit -m "[feat] 자각 시스템: 채도 상실 볼륨 + 반응 오브젝트 브로드캐스트 + 균열 지역 불안정화"
```

---

## Task 9: Enemies 모듈

**Files:**
- Create: `HiddenWeight/Assets/Scripts/Enemies/Enemy.cs`
- Create: `HiddenWeight/Assets/Scripts/Enemies/EnemyPatrol.cs`
- Create: `HiddenWeight/Assets/Scripts/Enemies/ContactDamage.cs`
- Modify: `HiddenWeight/Assets/Scripts/Player/PlayerAttack.cs` (피해 적용 연결)

**Interfaces:**
- Consumes: `HiddenWeight.Data.EnemyData`, `HiddenWeight.Player.{PlayerController, PlayerHealth}`
- Produces:

```csharp
namespace HiddenWeight.Enemies
{
    public class Enemy : MonoBehaviour
    {
        public static IReadOnlyList<Enemy> All { get; }
        public EnemyData Data { get; }
        public int Health { get; }
        public bool IsAlive { get; }
        public event System.Action<Enemy> Died;
        public void TakeDamage(int amount, Vector2 sourcePosition);
    }

    public class EnemyPatrol : MonoBehaviour { }

    public class ContactDamage : MonoBehaviour { }
}
```

- [ ] **Step 1: Enemy.cs 작성**

- `[SerializeField] EnemyData data` — 인스펙터로 지역별 에셋 연결
- `Awake`에서 `Health = data.maxHealth`, `SpriteRenderer.color = data.tint`
- static `_all` 리스트에 `OnEnable`에서 추가, `OnDisable`에서 제거
- `TakeDamage(amount, src)`: HP 감소, 넉백(`Rigidbody2D.linearVelocity` 덮어쓰기),
  0.1초 흰색 점멸. HP 0 이하면 `Died` 발행 후 `Destroy(gameObject)`

- [ ] **Step 2: EnemyPatrol.cs 작성**

지형 위 왕복. 낭떠러지·벽에서 방향을 바꾼다.

- `[SerializeField] Transform edgeCheck` — 발끝 앞쪽에 둔 빈 오브젝트
- `[SerializeField] LayerMask groundMask`
- `FixedUpdate`:

```csharp
bool groundAhead = Physics2D.OverlapCircle(edgeCheck.position, 0.1f, groundMask);
bool wallAhead = Physics2D.Raycast(transform.position, Vector2.right * _dir, 0.5f, groundMask);
if (!groundAhead || wallAhead) Flip();

float wobble = _data.wobbleAmplitude <= 0f
    ? 0f
    : Mathf.Sin(Time.time * _data.wobbleFrequency) * _data.wobbleAmplitude;

_rb.linearVelocity = new Vector2(_dir * _data.moveSpeed, _rb.linearVelocity.y + wobble * Time.fixedDeltaTime);
```

`wobbleAmplitude`는 균열 지역 적만 0.2로 설정해, "순찰 경로가 미세하게 어긋난" 느낌을 준다.

- [ ] **Step 3: ContactDamage.cs 작성**

- `[SerializeField] int damage = 1` (0이면 `Enemy.Data.contactDamage`를 쓴다)
- `OnCollisionStay2D` / `OnTriggerStay2D`에서 상대가 `PlayerHealth`를 가지면
  `TakeDamage(damage, transform.position)`. 무적 처리는 `PlayerHealth`가 알아서 한다

- [ ] **Step 4: PlayerAttack에 피해 적용 연결**

Task 4 Step 5에서 비워둔 자리를 채운다. `PlayerAttack.cs` 상단에 `using HiddenWeight.Enemies;`를
추가하고, 부채꼴 판정을 통과한 대상에 대해:

```csharp
var enemy = hit.GetComponentInParent<Enemy>();
if (enemy != null && enemy.IsAlive)
    enemy.TakeDamage(_data.attackDamage, transform.position);
```

의존 방향은 `Enemies → Player`인데 여기서 `Player → Enemies`가 생긴다.
이를 피하려면 `Enemy`가 아니라 인터페이스를 두어야 하지만, 적이 1종뿐인 MVP에서는
`World/Interactions.cs`에 `IDamageable`을 추가하고 `Enemy`가 이를 구현하는 쪽이 깔끔하다.

```csharp
// World/Interactions.cs 에 추가
public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(int amount, Vector2 sourcePosition);
}
```

`PlayerAttack`은 `IDamageable`만 참조한다. `Player → World` 의존이 생기는데,
Global Constraints의 의존 방향표에는 `World → Player`만 있다. **의존표를 다음과 같이 고친다:**

- `World`의 인터페이스 파일(`Interactions.cs`)은 어떤 모듈에도 의존하지 않는 순수 계약이므로,
  `Player`가 이를 참조하는 것을 허용한다
- 설계 문서 3.1절과 `PROJECT_STRUCTURE.md`에도 이 예외를 명시한다

- [ ] **Step 5: 컴파일 검증**

컴파일 검증 명령. 에러 0건.

- [ ] **Step 6: 커밋**

```bash
git add HiddenWeight/Assets/Scripts/Enemies HiddenWeight/Assets/Scripts/Player HiddenWeight/Assets/Scripts/World
git commit -m "[feat] Enemies 모듈: 순찰형 적 1종 + 접촉 피해 + 플레이어 공격 연결"
```

---

## Task 10: UI 모듈

**Files:**
- Create: `HiddenWeight/Assets/Scripts/UI/ScreenFader.cs`
- Create: `HiddenWeight/Assets/Scripts/UI/HUD.cs`
- Create: `HiddenWeight/Assets/Scripts/UI/FragmentLog.cs`
- Create: `HiddenWeight/Assets/Scripts/UI/PauseMenu.cs`
- Create: `HiddenWeight/Assets/Scripts/UI/TitleScreen.cs`
- Modify: `HiddenWeight/Assets/Scripts/World/StoryFragment.cs` (`Debug.Log` → `FragmentLog`)
- Modify: `HiddenWeight/Assets/Scripts/Core/SceneFlow.cs` (페이드 연결)

**Interfaces:**
- Consumes: `HiddenWeight.Core.{GameManager, GameState, SceneFlow}`, `HiddenWeight.Player.{PlayerInput, PlayerHealth}`, `HiddenWeight.Emotions.{EmotionSkillController, RewindSkill}`
- Produces:

```csharp
namespace HiddenWeight.UI
{
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance { get; }
        public void SetAlpha(float alpha);
        public IEnumerator FadeTo(float alpha, float seconds);
        public void FadeAndLoad(string sceneName, float seconds = 0.5f);
    }

    public class HUD : MonoBehaviour { }

    public class FragmentLog : MonoBehaviour
    {
        public static FragmentLog Instance { get; }
        public void Show(string text, float seconds = 4f);
    }

    public class PauseMenu : MonoBehaviour { }

    public class TitleScreen : MonoBehaviour { }
}
```

- [ ] **Step 1: ScreenFader.cs 작성**

`DontDestroyOnLoad` 싱글턴. `Canvas`(sortingOrder 999) + 전체 화면 검은 `Image`를 코드로 만든다.
`FadeAndLoad`는 페이드 아웃 → `SceneManager.LoadScene` → 페이드 인 코루틴이다.

- [ ] **Step 2: SceneFlow에 페이드 연결**

Task 3 Step 6에서 널 체크로 남겨둔 `LoadWithFade`를 채운다.

```csharp
public static void LoadWithFade(string sceneName, float fadeSeconds = 0.5f)
{
    if (ScreenFader.Instance != null) ScreenFader.Instance.FadeAndLoad(sceneName, fadeSeconds);
    else Load(sceneName);
}
```

`Core → UI` 참조가 생긴다. 이를 피하기 위해 `SceneFlow`에 정적 훅을 둔다.

```csharp
// Core/SceneFlow.cs
public static System.Action<string, float> FadeLoader;   // UI가 채운다

public static void LoadWithFade(string sceneName, float fadeSeconds = 0.5f)
{
    if (FadeLoader != null) FadeLoader(sceneName, fadeSeconds);
    else Load(sceneName);
}
```

`ScreenFader.Awake`에서 `SceneFlow.FadeLoader = FadeAndLoad;`로 자기를 등록한다.
의존 방향이 `UI → Core` 한쪽으로만 유지된다.

- [ ] **Step 3: FragmentLog.cs 작성**

화면 하단 중앙에 텍스트 한 줄을 페이드 인 → 유지 → 페이드 아웃으로 띄운다.
`TextMeshProUGUI`가 아니라 uGUI `Text`를 쓴다 (TMP 패키지 추가를 피하기 위함).
연달아 호출되면 앞의 것을 즉시 끝내고 새 것을 띄운다.

- [ ] **Step 4: StoryFragment의 Debug.Log 교체**

Task 6 Step 6에서 임시로 둔 줄을 바꾼다.

```csharp
// 변경 전
Debug.Log(text);
// 변경 후
FragmentLog.Instance?.Show(text);
```

`World → UI` 참조가 생긴다. Step 2와 같은 방식으로 `StoryFragment`가 아니라
`Core`에 정적 훅을 두고 UI가 채우게 한다.

```csharp
// Core/GameManager.cs 에 추가
public static System.Action<string> FragmentPresenter;
```

`StoryFragment`는 `GameManager.FragmentPresenter?.Invoke(text)`를 부르고,
`FragmentLog.Awake`가 `GameManager.FragmentPresenter = s => Show(s);`로 등록한다.

- [ ] **Step 5: HUD.cs 작성**

표시 항목:
- HP — `PlayerHealth.HealthChanged` 구독. 하트 아이콘 3개를 켜고 끈다
- 현재 감정 스킬 이름과 쿨타임 — `EmotionSkillController.Active`에서 읽는다.
  `Active`가 null이면 감춘다
- 되감기 채널링 게이지 — `Active`가 `RewindSkill`이고 `IsActive`일 때만
  `RewindSkill.ChannelProgress`로 채운다
- 수집 파편 수 — `GameManager.Instance.Progress.FragmentCount`

`GameManager.State`가 `Playing`이 아니면 캔버스를 감춘다.

- [ ] **Step 6: PauseMenu.cs 작성**

`PlayerInput.PausePressed`로 토글. 열릴 때 `GameManager.SetState(GameState.Paused)`,
닫힐 때 `Playing`. 버튼은 "계속하기" / "타이틀로". 타이틀로 갈 때
`GameManager.Instance.Progress.ResetAll()`을 부르고 `SceneFlow.LoadWithFade(SceneFlow.Title)`.

- [ ] **Step 7: TitleScreen.cs 작성**

게임 제목 "Hidden Weight"와 부제 "눈뜨는 꿈", 버튼 2개("시작하기", "종료").
시작하면 `Progress.ResetAll()` 후 `SceneFlow.LoadWithFade(SceneFlow.Prologue)`.

- [ ] **Step 8: 컴파일 검증**

컴파일 검증 명령. 에러 0건.

- [ ] **Step 9: 커밋**

```bash
git add HiddenWeight/Assets/Scripts/UI HiddenWeight/Assets/Scripts/Core HiddenWeight/Assets/Scripts/World
git commit -m "[feat] UI 모듈: HUD·파편 로그·일시정지·타이틀·화면 페이드"
```

---

## Task 11: Ending 모듈

**Files:**
- Create: `HiddenWeight/Assets/Scripts/Ending/AnomalyObject.cs`
- Create: `HiddenWeight/Assets/Scripts/Ending/EndingSequence.cs`

**Interfaces:**
- Consumes: `HiddenWeight.World.{IAwarenessReactive, AwarenessRegistry}`, `HiddenWeight.Player.PlayerInput`, `HiddenWeight.Core.{GameManager, SceneFlow}`, `HiddenWeight.UI.ScreenFader`
- Produces:

```csharp
namespace HiddenWeight.Ending
{
    public class AnomalyObject : MonoBehaviour, IAwarenessReactive
    {
        public enum Kind { InvertedCandle, MismatchedShadow, TremblingWall }
        public Kind Type { get; }
        public bool IsRevealed { get; }
        public bool Enabled { get; set; }     // 2단계에서 false
    }

    public class EndingSequence : MonoBehaviour { }
}
```

- [ ] **Step 1: AnomalyObject.cs 작성**

- `[SerializeField] Kind type`
- `[SerializeField] SpriteRenderer visual` — 이상 상태를 보여주는 스프라이트
- `Enabled`가 false면 `OnAwarenessChanged`가 와도 아무것도 하지 않는다 (2단계용)
- `OnAwarenessChanged(true)`일 때 종류별 연출:

| 종류 | 연출 |
|---|---|
| `InvertedCandle` | `visual.flipY = true` + 불꽃 스프라이트를 위아래 뒤집는다 |
| `MismatchedShadow` | 그림자 스프라이트의 `localRotation`을 광원 반대편이 아닌 90도로 돌린다 |
| `TremblingWall` | `localPosition`에 `Mathf.Sin(Time.time * 40f) * 0.02f`를 더한다 |

- `OnEnable`/`OnDisable`에서 `AwarenessRegistry.Register`/`Unregister`

- [ ] **Step 2: EndingSequence.cs 작성**

`Ending` 씬의 감독. `PlayerInput.Enabled = false`로 두고 자각(L)만 받는다.

```
1단계
  0.0s  검은 화면. ScreenFader.SetAlpha(1)
  0.0s  3초에 걸쳐 페이드 인 (무음)
  3.0s  4초 정적. 아무 일도 일어나지 않는다
  7.0s  이후 자각 입력을 받기 시작한다
        L 홀드가 끊기지 않고 2.5초 누적되면 1단계 종료
        홀드를 놓으면 누적 0으로 초기화 + 모든 AnomalyObject 숨김
  종료  1.5초 암전 → 몽타주 2.5초 → 2단계
2단계
  0.0s  같은 침실. 모든 AnomalyObject.Enabled = false
  0.0s  AudioManager.PlayBgm(endingBgm, 2f)  (클립이 null이면 무음)
  8.0s  또는 자각을 놓는 즉시, 3초 페이드 아웃 → Title
```

**자각 홀드 누적 로직:**

```csharp
void Update()
{
    if (_phase != Phase.FalseAwakeningInput) return;

    if (PlayerInput.AwarenessHeld)
    {
        _hold += Time.deltaTime;
        SetAnomaliesRevealed(true);
        if (_hold >= holdToAdvance) StartCoroutine(TransitionToRealAwakening());
    }
    else
    {
        _hold = 0f;
        SetAnomaliesRevealed(false);
    }
}
```

**몽타주** — `[SerializeField] Sprite[] montageFrames` (잔재·응시·균열 각 1장, 총 3장).
전체 화면 `Image`에 0.8초씩 순서대로 띄운다. 완전한 장면이 아니라 단편적 프레임이므로
페이드 없이 딱딱 끊어 보여준다 (기획서 3.4절 "짧게 처리").

`AwarenessSystem`은 `Ending` 씬에 두지 않는다. `EndingSequence`가 `AnomalyObject`를
직접 제어한다 — 이동·볼륨·불안정 로직이 필요 없기 때문이다.

- [ ] **Step 3: 컴파일 검증**

컴파일 검증 명령. 에러 0건.

- [ ] **Step 4: 커밋**

```bash
git add HiddenWeight/Assets/Scripts/Ending
git commit -m "[feat] Ending 모듈: 거짓 깨어남 → 잔상 몽타주 → 진짜 각성 2단 시퀀스"
```

---

## Task 12: Editor 빌더 — 플레이스홀더 아트·데이터 에셋·프리팹

**Files:**
- Create: `HiddenWeight/Assets/Scripts/Editor/PlaceholderArtBuilder.cs`
- Create: `HiddenWeight/Assets/Scripts/Editor/DataAssetBuilder.cs`
- Create: `HiddenWeight/Assets/Scripts/Editor/PrefabBuilder.cs`

**Interfaces:**
- Consumes: 앞선 모든 모듈
- Produces: `PlaceholderArtBuilder.Run()`, `DataAssetBuilder.Run()`, `PrefabBuilder.Run()`

- [ ] **Step 1: PlaceholderArtBuilder.cs 작성**

`Assets/Art/Placeholder/`에 단색 PNG를 코드로 생성하고 `TextureImporter`를 스프라이트로 설정한다.

생성할 스프라이트:

| 파일 | 크기(px) | 색 | 용도 |
|---|---|---|---|
| `Player.png` | 32×48 | 연보라 #C8B8E8 | 플레이어 |
| `Enemy.png` | 32×32 | 흰색 (런타임에 tint) | 적 |
| `Tile.png` | 32×32 | 회색 #808080 | 지형 타일 |
| `Platform.png` | 96×16 | 밝은 회색 #A0A0A0 | 이동·무너지는 발판 |
| `Fragment.png` | 16×16 | 흰색 | 스토리 파편 |
| `Gate.png` | 32×96 | 짙은 남색 #303048 | 게이트 blocker |
| `Eye.png` | 48×48 | 보라 #9060C0 | 시선 기믹 |
| `Candle.png` | 16×32 | 주황 #E8A050 | 엔딩 촛불 |
| `Bed.png` | 192×64 | 회보라 #685878 | 엔딩 침대 |
| `Wall.png` | 256×192 | 짙은 회보라 #484058 | 엔딩 벽 |

`TextureImporter` 설정: `spritePixelsPerUnit = 32`, `filterMode = Point`,
`textureCompression = Uncompressed`, `spriteImportMode = Single`.

Pixels Per Unit을 32로 두는 이유는 프로젝트 전체 기준(설계 문서 2절)과 맞추기 위해서다.

- [ ] **Step 2: DataAssetBuilder.cs 작성**

`Assets/ScriptableObjects/`에 에셋을 생성한다. 이미 있으면 값을 덮어쓰지 않고 건너뛴다
(수동 조정한 밸런스를 날리지 않기 위함).

| 에셋 파일 | 내용 |
|---|---|
| `PlayerData.asset` | Task 2 Step 2 표의 기본값 |
| `Emotion_Rewind.asset` | id=Rewind, "되감기", Hold, channelTime 1.0, cooldown 2, range 6, moveSpeedMultiplier 0 |
| `Emotion_Hush.asset` | id=Hush, "숨죽이기", Hold, channelTime 0, cooldown 0, moveSpeedMultiplier 0.45, hushScale 0.6 |
| `Emotion_Foresight.asset` | id=Foresight, "예지", Tap, cooldown 3, range 8, moveSpeedMultiplier 1, effectDuration 1.5, previewLeadTime 2 |
| `Enemy_Residue.asset` | HP 2, speed 1.2, tint #6B5D52, wobble 0 |
| `Enemy_Gaze.asset` | HP 2, speed 2.0, tint #7B5EA7, wobble 0 |
| `Enemy_Fracture.asset` | HP 2, speed 1.6, tint #8FD9C4, wobble 0.2 |
| `Zone_Prologue.asset` | Prologue, "몽환의 우주", scene `Zone_Prologue`, next `Zone_Residue`, grantedSkill None |
| `Zone_Residue.asset` | Residue, "잔재", scene `Zone_Residue`, next `Zone_Gaze`, grantedSkill Rewind |
| `Zone_Gaze.asset` | Gaze, "응시", scene `Zone_Gaze`, next `Zone_Fracture`, grantedSkill Hush, grantsAwareness true |
| `Zone_Fracture.asset` | Fracture, "균열", scene `Zone_Fracture`, next `Zone_Residue`, grantedSkill Foresight, awarenessStable **false** |
| `BalanceData.asset` | 위 전부를 참조로 묶고, `awarenessProfile`에 `Volume_Awareness.asset` 연결 |

균열의 `nextSceneName`이 `Zone_Residue`인 것은 기획서 5.3절 백트래킹 때문이다.
잔재를 두 번째로 방문했을 때 `ZoneTrigger`가 `Ending`으로 보내야 하는데, 이는
`Progress.HasClearedFracture`로 판단한다. `ZoneTrigger`에 다음을 추가한다:

```csharp
// Task 6에서 만든 ZoneTrigger의 씬 결정 부분을 이렇게 고친다
var next = gm.CurrentZoneData != null ? gm.CurrentZoneData.nextSceneName : SceneFlow.Title;
if (gm.Progress.CurrentZone == ZoneId.Residue && gm.Progress.HasClearedFracture)
    next = SceneFlow.Ending;
```

각 `ZoneData`의 `volumeProfile`에는 Task 1 Step 5에서 만든 `Volume_*.asset`을 연결한다.

- [ ] **Step 3: PrefabBuilder.cs 작성**

`Assets/Prefabs/`에 프리팹을 생성한다.

| 프리팹 | 구성 |
|---|---|
| `Player.prefab` | SpriteRenderer(Player.png), Rigidbody2D(freezeRotation), CapsuleCollider2D, `PlayerController`(+ groundCheck·wallCheck 자식), `PlayerHealth`, `PlayerAttack`, `PlayerAnimator`, `EmotionSkillController` + `RewindSkill`·`HushSkill`·`ForesightSkill`, `AwarenessSystem`. layer = `Player` |
| `Enemy.prefab` | SpriteRenderer(Enemy.png), Rigidbody2D, BoxCollider2D, `Enemy`, `EnemyPatrol`(+ edgeCheck 자식), `ContactDamage`. layer = `Enemy` |
| `MovingPlatform.prefab` | SpriteRenderer(Platform.png), Rigidbody2D(Kinematic), BoxCollider2D, `MovingPlatform`. layer = `Ground` |
| `CrumblingPlatform.prefab` | 위와 같되 `CrumblingPlatform`. layer = `Ground` |
| `RewindableBlock.prefab` | SpriteRenderer(Tile.png), Rigidbody2D, BoxCollider2D, `Rewindable`. layer = `Ground` |
| `GazeHazard.prefab` | SpriteRenderer(Eye.png), `GazeHazard`. layer = `Hazard` |
| `Gate.prefab` | 부모에 `Gate`, 자식 `Blocker`(SpriteRenderer Gate.png + BoxCollider2D, layer `Ground`), 자식 `Hint`(SpriteRenderer) |
| `StoryFragment.prefab` | SpriteRenderer(Fragment.png), CircleCollider2D(trigger), `StoryFragment`. layer = `Interactable` |
| `HiddenFragment.prefab` | 위와 같되 `HiddenFragment`, 시작 시 SpriteRenderer 비활성 |
| `Checkpoint.prefab` | BoxCollider2D(trigger), `Checkpoint` |
| `GameManager.prefab` | `GameManager`(BalanceData 연결), `AudioManager`, `ScreenFader` |
| `MainCamera.prefab` | Camera(Orthographic, size 6, 배경 검정), `RoomCamera` |
| `HUD.prefab` | Canvas(ScreenSpaceOverlay) + `HUD` + 하트 3개 + 스킬 라벨 + 게이지 + `FragmentLog` |

`GameObject.AddComponent`로 조립하고 `PrefabUtility.SaveAsPrefabAsset`으로 저장한 뒤
씬의 임시 오브젝트를 `Object.DestroyImmediate`로 지운다.

- [ ] **Step 4: 세 빌더 실행**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
U="/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity"
for M in PlaceholderArtBuilder DataAssetBuilder PrefabBuilder; do
  "$U" -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" \
    -logFile "$PWD/.unity-logs/$M.log" \
    -executeMethod "HiddenWeight.EditorTools.$M.Run"
  echo "$M exit=$?"
done
ls HiddenWeight/Assets/Art/Placeholder/*.png | wc -l
ls HiddenWeight/Assets/ScriptableObjects/*.asset | wc -l
ls HiddenWeight/Assets/Prefabs/*.prefab | wc -l
```

기대 결과: 세 번 다 `exit=0`, PNG 10개, 에셋 12개, 프리팹 13개

- [ ] **Step 5: 커밋**

```bash
git add HiddenWeight/Assets
git commit -m "[feat] Editor 빌더: 플레이스홀더 스프라이트·데이터 에셋·프리팹 자동 생성"
```

---

## Task 13: Editor 빌더 — 씬 7개 생성

**Files:**
- Create: `HiddenWeight/Assets/Scripts/Editor/ZoneSceneBuilder.cs`

**Interfaces:**
- Consumes: Task 12의 프리팹과 데이터 에셋
- Produces: `ZoneSceneBuilder.Run()` — 씬 7개 생성 + `EditorBuildSettings.scenes` 등록

- [ ] **Step 1: ZoneSceneBuilder.cs 작성 — 공통 헬퍼**

- `NewScene(name)` — `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)`
- `PlaceTiles(tilemap, rects)` — `Tile` 에셋을 만들어 `Tilemap.SetTile`로 사각형 영역을 채운다
- `Spawn(prefabPath, position)` — `PrefabUtility.InstantiatePrefab`
- `SaveScene(name)` — `Assets/Scenes/<name>.unity`로 저장
- 각 지역 씬 공통 구성: `Grid` + `Tilemap`(+ `TilemapCollider2D`, `CompositeCollider2D`,
  layer `Ground`), `MainCamera` 프리팹, `Player` 프리팹, `HUD` 프리팹, `Room` 3~4개,
  지역 `Volume`(ZoneData의 프로파일, weight 1)

- [ ] **Step 2: Bootstrap·Title 씬 생성**

`Bootstrap`: `GameManager` 프리팹만 두고, 빈 `BootLoader` 컴포넌트 대신
`GameManager.Start`에서 `SceneFlow.Load(SceneFlow.Title)`를 호출하게 한다.
(`GameManager`에 `[SerializeField] bool autoLoadTitle = false`를 추가하고 Bootstrap 인스턴스만 true)

`Title`: Canvas + `TitleScreen` + 제목·부제 텍스트 + 버튼 2개.

- [ ] **Step 3: Zone_Prologue 씬 생성**

몽환의 우주. 튜토리얼 3룸.

| 룸 | 내용 |
|---|---|
| 1 | 평평한 바닥. 좌우 이동만 |
| 2 | 3단 계단 + 폭 4의 구덩이 → 점프·대시 필요 |
| 3 | 높이 8의 수직 벽 2개 → 벽점프로 올라가야 출구 도달 |

출구에 `ZoneTrigger`. 적은 두지 않는다.

- [ ] **Step 4: Zone_Residue 씬 생성**

잔재(과거·죄책감). 4룸.

| 룸 | 내용 |
|---|---|
| 1 | 입구. `Checkpoint`. `StoryFragment`(grantsSkill = Rewind) 배치 → 여기서 되감기 획득 |
| 2 | 무너진 다리 — `RewindableBlock` 3개가 아래로 떨어져 있다. 되감기로 복원해야 건넌다 |
| 3 | `CrumblingPlatform` 4개 연속. 밟으면 무너지고, 되감기로 되살려 되돌아올 수 있다 |
| 4 | `Enemy`(Enemy_Residue) 2마리 + `Gate`(requiredSkill = Rewind) + 출구 `ZoneTrigger`. 추가로 `Gate`(requiresFinalCondition = true) 뒤에 `HiddenFragment` 하나 — 백트래킹 전용 |

- [ ] **Step 5: Zone_Gaze 씬 생성**

응시(현재·수치심). 4룸.

| 룸 | 내용 |
|---|---|
| 1 | 입구 + `Checkpoint`. `GazeHazard` 1개를 멀리 배치해 위험을 미리 보여준다 |
| 2 | `StoryFragment`(grantsSkill = Hush, grantsAwareness = true) → 숨죽이기와 자각을 동시에 획득 |
| 3 | `GazeHazard` 3개가 겹치는 통로 + 높이 1.2의 좁은 틈. 숨죽이기로만 통과 |
| 4 | `Enemy`(Enemy_Gaze) 2마리 + `HiddenFragment` 2개(자각으로만 보임) + 출구 |

- [ ] **Step 6: Zone_Fracture 씬 생성**

균열(미래·불안). 4룸. `ZoneData.awarenessStable = false`라 자각이 깜빡인다.

| 룸 | 내용 |
|---|---|
| 1 | 입구 + `Checkpoint` + `StoryFragment`(grantsSkill = Foresight) |
| 2 | `MovingPlatform` 3개가 서로 다른 주기로 왕복. 예지로 도착 위치를 보고 뛴다 |
| 3 | `CrumblingPlatform` 6개 중 3개만 안전. 예지로 무너질 것을 구분한다 |
| 4 | `Enemy`(Enemy_Fracture) 2마리 + `HiddenFragment` 2개 + 출구 `ZoneTrigger`(marksFractureCleared = true) |

- [ ] **Step 7: Ending 씬 생성**

횡스크롤 없음. 정적 레이어 구성:

- 배경 `Wall.png` (z 0)
- `Bed.png` (z -1)
- `AnomalyObject` 3개: `Candle.png`(InvertedCandle), 그림자용 검은 사각(MismatchedShadow),
  벽 일부 사각(TremblingWall)
- `EndingSequence` + Canvas(몽타주용 전체화면 `Image`)
- `MainCamera`(RoomCamera 없이 고정)

- [ ] **Step 8: EditorBuildSettings 등록**

```csharp
EditorBuildSettings.scenes = new[]
{
    new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity", true),
    new EditorBuildSettingsScene("Assets/Scenes/Title.unity", true),
    new EditorBuildSettingsScene("Assets/Scenes/Zone_Prologue.unity", true),
    new EditorBuildSettingsScene("Assets/Scenes/Zone_Residue.unity", true),
    new EditorBuildSettingsScene("Assets/Scenes/Zone_Gaze.unity", true),
    new EditorBuildSettingsScene("Assets/Scenes/Zone_Fracture.unity", true),
    new EditorBuildSettingsScene("Assets/Scenes/Ending.unity", true),
};
```

- [ ] **Step 9: 씬 빌더 실행**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" \
  -logFile "$PWD/.unity-logs/scenes.log" \
  -executeMethod HiddenWeight.EditorTools.ZoneSceneBuilder.Run
echo "exit=$?"
ls HiddenWeight/Assets/Scenes/*.unity
```

기대 결과: `exit=0`, 씬 7개

- [ ] **Step 10: 빌드 검증**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" \
  -logFile "$PWD/.unity-logs/build.log" \
  -executeMethod HiddenWeight.EditorTools.BuildScript.BuildMac
echo "exit=$?"
ls -d HiddenWeight/Builds/macOS/HiddenWeight.app
```

기대 결과: `exit=0`, `.app` 번들 존재

- [ ] **Step 11: 커밋**

```bash
git add HiddenWeight/Assets
git commit -m "[feat] Editor 빌더: 지역 3곳·프롤로그·타이틀·엔딩 씬 7개 자동 생성 + 빌드 검증"
```

---

## Task 14: 문서

**Files:**
- Create: `PROJECT_STRUCTURE.md`
- Create: `HiddenWeight/Assets/Scripts/{Core,Data,Player,World,Emotions,Enemies,Ending,UI,Editor}/README.md` (9개)
- Create: `docs/code/README.md`
- Create: `docs/code/{Core,Data,Player,World,Emotions,Enemies,Ending,UI,Editor}.md` (9개)

**Interfaces:**
- Consumes: 완성된 코드 전체
- Produces: 문서만. 코드 변경 없음

- [ ] **Step 1: PROJECT_STRUCTURE.md 작성**

이전 프로젝트(`origin/ksh`의 같은 파일)와 같은 구성으로 쓴다.

1. Unity 프로젝트 설정 표 + 최초 세팅 절차
2. 확정한 기본값 (설계 문서 1.1절 표를 옮긴다)
3. 모듈 구조 트리 + 모듈별 문서 링크 표
4. 전체 게임 흐름과 모듈 연결 다이어그램
5. 조작표 (Task 4 Step 1의 키 배치)
6. 배치모드 명령 모음 (컴파일·테스트·빌드)

- [ ] **Step 2: 모듈 README 9개 작성**

각각 다음을 담는다: 모듈이 맡은 책임 한 문단, 파일 목록 표(파일 / 역할 / 기획서 대응 절),
다른 모듈과의 연결 방식, 씬 배치 시 주의점.

- [ ] **Step 3: docs/code/README.md 작성**

색인이다. 게임 한 줄 요약, 씬 흐름 다이어그램, 모듈별 문서 링크 표, 아키텍처 원칙,
조작 요약을 담는다.

- [ ] **Step 4: docs/code/*.md 9개 작성**

파일 단위 상세 문서다. 파일마다 아래 4개 항목을 반드시 채운다.

```markdown
## <파일명>.cs

**역할**: 한두 문장.

**상속/의존**: 기반 클래스, `[RequireComponent]`, 참조하는 다른 모듈의 타입.

**주요 멤버**
- `[SerializeField]` 필드와 기본값
- public 프로퍼티·메서드·이벤트 시그니처
- 의미 있는 내부 상태

**동작**
- 메서드별로 실제 흐름을 서술. 조건 분기와 수치를 명시한다.
```

- [ ] **Step 5: 문서와 코드 일치 확인**

각 `docs/code/*.md`에 적은 public 멤버 이름이 실제 코드와 같은지 대조한다.

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
for m in Core Data Player World Emotions Enemies Ending UI; do
  echo "=== $m ==="
  ls HiddenWeight/Assets/Scripts/$m/*.cs | xargs -n1 basename
  grep -c '^## ' docs/code/$m.md
done
```

기대 결과: 각 모듈의 `.cs` 파일 수와 문서의 `## ` 헤딩 수가 같다.

- [ ] **Step 6: 커밋**

```bash
git add PROJECT_STRUCTURE.md docs/code HiddenWeight/Assets/Scripts
git commit -m "[docs] 프로젝트 구조 + 모듈 README 9종 + 파일별 개발 문서 9종"
```

---

## 자체 점검 결과

계획을 설계 문서와 대조해 확인한 항목과, 그 과정에서 고친 것을 남긴다.

**설계 문서 커버리지** — 전 항목이 태스크에 대응한다.

| 설계 문서 절 | 담당 태스크 |
|---|---|
| 2. Unity 프로젝트 설정 | Task 1 |
| 2.1 레이어와 태그 | Task 1 Step 5 |
| 3. 모듈 구조 | Task 2~11 |
| 4. 씬 구성 | Task 13 |
| 5.1 PlayerController | Task 4 |
| 5.2 감정 스킬 | Task 7 |
| 5.3 자각 | Task 8 |
| 5.4 전투 | Task 4(공격·체력) + Task 9(적) |
| 5.5 월드 | Task 5, 6 |
| 5.6 진행 상태 | Task 3 |
| 5.7 엔딩 | Task 11 |
| 6. 데이터 | Task 2(정의) + Task 12(에셋 생성) |
| 6.1 지역별 색보정 | Task 1 Step 5 |
| 7. 문서 산출물 | Task 14 |
| 9. 검증 방법 | Task 1(컴파일) + Task 13 Step 10(빌드) |

**고친 것 3건**

1. **의존 순환** — `PlayerAttack`이 `Enemy`를 직접 부르면 `Player → Enemies`가 생겨
   설계 문서 3.1절 의존표와 어긋난다. `World/Interactions.cs`에 `IDamageable`을 추가하고
   `PlayerAttack`이 인터페이스만 참조하도록 바꿨다 (Task 9 Step 4). 인터페이스 파일은
   어떤 모듈에도 의존하지 않는 순수 계약이므로 `Player`가 참조하는 것을 허용한다.
   이 예외를 `PROJECT_STRUCTURE.md`에 명시한다
2. **`Core → UI`, `World → UI` 역참조** — `SceneFlow`의 페이드와 `StoryFragment`의 파편 표시가
   UI를 직접 부르면 방향이 뒤집힌다. 정적 훅(`SceneFlow.FadeLoader`,
   `GameManager.FragmentPresenter`)을 두고 UI가 자기를 등록하는 방식으로 바꿨다 (Task 10 Step 2, 4)
3. **`World → Emotions` 역참조** — `HiddenFragment`가 `AwarenessSystem`을 직접 부르면 안 된다.
   등록 창구를 `World/Interactions.cs`의 `AwarenessRegistry`로 분리하고,
   `AwarenessSystem`(Emotions)이 그 목록을 읽는 방향으로 바꿨다 (Task 8 Step 2, 3)

**미리 정한 이름 일관성** — 여러 태스크에 걸쳐 쓰이는 식별자를 여기 모아둔다. 구현 중
이름이 흔들리면 이 표를 기준으로 맞춘다.

| 이름 | 정의 위치 | 소비 위치 |
|---|---|---|
| `ProgressState.CanOpenFinalGate()` | Task 3 | Task 6 `Gate` |
| `ProgressState.CollectFragment(string)` → bool | Task 3 | Task 6 `StoryFragment` |
| `GameManager.RespawnRequested` | Task 3 | Task 4 `PlayerHealth` |
| `GameManager.FragmentPresenter` | Task 10 Step 4 | Task 6 `StoryFragment` |
| `SceneFlow.FadeLoader` | Task 10 Step 2 | Task 6 `ZoneTrigger` |
| `PlayerController.ExternalSpeedMultiplier` | Task 4 | Task 7 `EmotionSkill`, Task 8 `AwarenessSystem` |
| `PlayerController.MovementLocked` | Task 4 | Task 7 `RewindSkill` |
| `PlayerAttack.CanAttack` | Task 4 | Task 7 `HushSkill` |
| `IForeseeable.CurrentSprite` | Task 5 | Task 7 `ForesightSkill` |
| `AwarenessRegistry.Items` / `.Added` | Task 8 Step 2 | Task 8 `AwarenessSystem` |
| `IDamageable` | Task 9 Step 4 | Task 4 `PlayerAttack`, Task 9 `Enemy` |
| `EmotionSkillController.RefreshActive()` | Task 7 | Task 6 `StoryFragment` |
| `EmotionSkill.SkipCooldown` | Task 7 Step 2 | Task 7 `RewindSkill` |
