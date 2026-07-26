# Editor 모듈

`HiddenWeight.EditorTools`(다른 모듈과 달리 네임스페이스가 `Editor`가 아니라 `EditorTools`)는 프로젝트 세팅·데이터 에셋·플레이스홀더 아트·프리팹·씬을 코드로 생성하고 배치모드 컴파일/빌드를 검증하는 에디터 전용 도구 모음이다. 여기 실린 클래스는 전부 정적 메서드이며 Unity CLI의 `-executeMethod HiddenWeight.EditorTools.<Class>.<Method>`로 배치모드에서 직접 호출된다. 설계 문서 3.1절의 의존 방향 표에서 `Editor ──▶ (전부)`로 표시된 유일한 모듈로, `Core`/`Data`/`Player`/`Emotions`/`Enemies`/`World`/`UI`/`Ending` 전체를 `using`한다.

## BuildScript.cs

- **역할**: 배치모드 컴파일 통과 여부를 확정하는 게이트(`Compile`)와 macOS 스탠드얼론 빌드(`BuildMac`).
- **상속/의존**: 정적 클래스, MonoBehaviour 아님. `UnityEditor`, `UnityEditor.Build.Reporting`(`BuildReport`)에 의존. 다른 `HiddenWeight.*` 모듈은 참조하지 않는다.
- **주요 멤버**:
  - `static void Compile()` — 인자 없음, 반환값 없음.
  - `static void BuildMac()` — 인자 없음, 반환값 없음. 내부에 씬 7개 경로(`Assets/Scenes/Bootstrap.unity` ~ `Assets/Scenes/Ending.unity`)를 하드코딩한 `string[]` 배열을 갖는다.
- **동작**:
  - `Compile()`: `EditorUtility.scriptCompilationFailed`가 `true`면 `Debug.LogError("[BuildScript] 스크립트 컴파일 실패")` 후 `EditorApplication.Exit(1)`. 아니면 `Debug.Log("[BuildScript] 컴파일 통과")` 후 `EditorApplication.Exit(0)`. `-executeMethod` 자체가 컴파일 실패 시 실행되지 않으므로, 이 메서드가 돌아 exit 코드를 남겼다는 사실 자체가 "컴파일 통과"의 증거라는 주석이 코드에 있다.
  - `BuildMac()`: `BuildPipeline.BuildPlayer(scenes, "Builds/macOS/HiddenWeight.app", BuildTarget.StandaloneOSX, BuildOptions.None)`을 호출한다. `report.summary.result != BuildResult.Succeeded`면 에러 개수(`report.summary.totalErrors`)와 함께 로그 후 `Exit(1)`, 성공하면 `report.summary.outputPath`와 함께 로그 후 `Exit(0)`.

## DataAssetBuilder.cs

- **역할**: `Assets/ScriptableObjects/`에 밸런스 데이터 에셋 12종(PlayerData 1, EmotionData 3, EnemyData 3, ZoneData 4, BalanceData 1)을 생성한다.
- **상속/의존**: 정적 클래스. `UnityEditor`, `HiddenWeight.Data`(`PlayerData`, `EmotionData`, `EmotionId`, `SkillInput`, `EnemyData`, `ZoneData`, `ZoneId`, `BalanceData`)에 의존.
- **주요 멤버**:
  - `const string Folder = "Assets/ScriptableObjects"`, `const string SettingsFolder = "Assets/Settings"`.
  - `static void Run()` — 유일한 공개 진입점.
  - `static VolumeProfile LoadVolume(string zoneName)` — `Assets/Settings/Volume_{zoneName}.asset`을 경로로 로드.
  - `static void EnsureFolder()` — `Assets/ScriptableObjects` 폴더 없으면 생성.
  - `static T LoadOrCreate<T>(string path, System.Action<T> configure) where T : ScriptableObject` — 경로에 이미 에셋이 있으면 그 값을 그대로 반환(덮어쓰지 않음), 없으면 `ScriptableObject.CreateInstance<T>()` 후 `configure` 실행, `AssetDatabase.CreateAsset(asset, path)`로 저장.
- **동작**:
  - `Run()`은 `EnsureFolder()` 후 `LoadOrCreate`를 12번 호출한다: `PlayerData.asset`(기본값 그대로 사용, configure는 빈 람다), `Emotion_Rewind.asset`(`SkillInput.Hold`, `channelTime=1.0`, `cooldown=2`, `range=6`, `moveSpeedMultiplier=0`), `Emotion_Hush.asset`(`SkillInput.Hold`, `cooldown=0`, `moveSpeedMultiplier=0.45`, `hushScale=0.6`), `Emotion_Foresight.asset`(`SkillInput.Tap`, `cooldown=3`, `range=8`, `moveSpeedMultiplier=1`, `effectDuration=1.5`, `previewLeadTime=2`), `Enemy_Residue.asset`(`moveSpeed=1.2`, tint `#6B5D52`), `Enemy_Gaze.asset`(`moveSpeed=2.0`, tint `#7B5EA7`), `Enemy_Fracture.asset`(`moveSpeed=1.6`, tint `#8FD9C4`, `wobbleAmplitude=0.2`), `Zone_Prologue/Residue/Gaze/Fracture.asset`(각각 `sceneName`/`nextSceneName`/`grantedSkill`/`grantsAwareness`/`awarenessStable`/`volumeProfile` 설정, `Zone_Fracture`만 `nextSceneName="Zone_Residue"`로 되돌아가고 `awarenessStable=false`), 마지막으로 `BalanceData.asset`(앞서 만든 참조들을 배열로 묶고 `awarenessProfile`을 `Volume_Awareness.asset`에서 로드).
  - 이미 있는 에셋은 `configure` 람다가 아예 실행되지 않으므로(`LoadOrCreate`가 `existing != null`이면 즉시 반환), 재실행해도 수동으로 조정한 값이 보존된다.
  - `LoadVolume`은 GUID가 아니라 경로 기반 조회라, `ProjectSetup`을 재실행해 URP/Volume 에셋의 GUID가 바뀌어도 참조가 깨지지 않는다.
  - 마지막에 `AssetDatabase.SaveAssets()` + `AssetDatabase.Refresh()` + 완료 로그.

## PlaceholderArtBuilder.cs

- **역할**: `Assets/Art/Placeholder/`에 단색 PNG 스프라이트 10장을 코드로 생성하고 스프라이트 임포트 설정을 적용한다. 실제 아트가 준비되기 전까지 게임을 플레이 가능한 상태로 만드는 자리표시자.
- **상속/의존**: 정적 클래스. `System.IO`, `UnityEditor`(`TextureImporter` 등)에 의존. 다른 `HiddenWeight.*` 모듈은 참조하지 않는다.
- **주요 멤버**:
  - `const string Folder = "Assets/Art/Placeholder"`.
  - `static readonly (string name, int width, int height, string hex)[] Sprites` — 10개 항목: `Player`(32×48, `#C8B8E8`), `Enemy`(32×32, `#FFFFFF`), `Tile`(32×32, `#808080`), `Platform`(96×16, `#A0A0A0`), `Fragment`(16×16, `#FFFFFF`), `Gate`(32×96, `#303048`), `Eye`(48×48, `#9060C0`), `Candle`(16×32, `#E8A050`), `Bed`(192×64, `#685878`), `Wall`(256×192, `#484058`).
  - `static void Run()` — 유일한 공개 진입점.
  - `static void WritePng(string name, int width, int height, string hex)` — `Texture2D(width, height, TextureFormat.RGBA32, false)`를 만들어 전체 픽셀을 단색으로 채우고 `EncodeToPNG()` 결과를 `File.WriteAllBytes`로 저장.
  - `static void ConfigureImporter(string path)` — `TextureImporterType.Sprite`, `SpriteImportMode.Single`, `spritePixelsPerUnit = 32`, `filterMode = FilterMode.Point`, `textureCompression = TextureImporterCompression.Uncompressed`.
- **동작**:
  - `Run()`은 `EnsureFolder()` → `Sprites` 전체에 대해 `WritePng` 실행 → `AssetDatabase.SaveAssets()`/`Refresh()`(PNG가 디스크에 먼저 보이도록 임포트를 강제) → `Sprites` 전체에 대해 `ConfigureImporter` 실행 → 다시 `SaveAssets()`/`Refresh()`.
  - PPU 32는 설계 문서 2절의 Pixels Per Unit 기준과 일치한다는 주석이 있다.
  - 상대 경로(`$"{Folder}/{name}.png"`)는 에디터 스크립트에서 프로젝트 루트(Assets의 부모) 기준으로 풀린다.

## PrefabBuilder.cs

- **역할**: `Assets/Prefabs/`에 프리팹 13종(`GameManager`, `Player`, `Enemy`, `MovingPlatform`, `CrumblingPlatform`, `RewindableBlock`, `GazeHazard`, `Gate`, `StoryFragment`, `HiddenFragment`, `Checkpoint`, `MainCamera`, `HUD`)을 생성한다.
- **상속/의존**: 정적 클래스. `HiddenWeight.Core`(`GameManager`, `AudioManager`, `ScreenFader`), `HiddenWeight.Data`(`BalanceData`, `EnemyData`), `HiddenWeight.Player`(`PlayerController`, `PlayerAttack`, `PlayerHealth`, `PlayerAnimator`), `HiddenWeight.Emotions`(`EmotionSkillController`, `RewindSkill`, `HushSkill`, `ForesightSkill`, `AwarenessSystem`), `HiddenWeight.Enemies`(`Enemy`, `EnemyPatrol`, `ContactDamage`), `HiddenWeight.World`(`MovingPlatform`, `CrumblingPlatform`, `Rewindable`, `GazeHazard`, `Gate`, `StoryFragment`, `HiddenFragment`, `Checkpoint`, `RoomCamera`), `HiddenWeight.UI`(`HUD`, `FragmentLog`)에 의존.
- **주요 멤버**:
  - `const string Folder = "Assets/Prefabs"`, `const string ArtFolder = "Assets/Art/Placeholder"`, `const string DataFolder = "Assets/ScriptableObjects"`.
  - `static void Run()` — 유일한 공개 진입점.
  - `static Sprite LoadSprite(string name)`, `static T LoadData<T>(string name) where T : UnityEngine.Object` — 각각 `ArtFolder`/`DataFolder` 경로 로더.
  - `static void SavePrefab(GameObject root, string name)` — `PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/{name}.prefab")` 후 `Object.DestroyImmediate(root)`로 씬의 임시 오브젝트 제거.
  - `static GameObject NewChild(Transform parent, string name)` — 이름만 있는 자식 오브젝트 생성.
  - 프리팹별 빌더: `BuildGameManagerRoot()`, `BuildPlayer()`, `BuildEnemy()`, `BuildMovingPlatform()`, `BuildCrumblingPlatform()`, `BuildRewindableBlock()`, `BuildGazeHazard()`, `BuildGate()`, `BuildStoryFragment()`, `BuildHiddenFragment()`, `BuildCheckpoint()`, `BuildMainCamera()`, `BuildHUD()`.
- **동작**:
  - `Run()`은 `BuildGameManagerRoot()`를 가장 먼저 호출해 씬에 살려 두고(GameObject만 만들고 저장은 나중에), 나머지 12개 프리팹을 순서대로 만든 뒤 마지막에 `SavePrefab(gameManagerRoot, "GameManager")`를 호출한다. 다른 컴포넌트들의 `Awake()`(에디터에서도 `AddComponent` 시점에 즉시 실행됨)가 `GameManager.Instance.Balance.*`를 참조하므로, `GameManager`가 씬에 없으면 프리팹을 짓는 동안 `NullReferenceException` 콘솔 노이즈가 나기 때문이라는 주석이 있다.
  - `BuildPlayer()`: `SpriteRenderer`+`Rigidbody2D`(회전 고정)+`CapsuleCollider2D`(0.8×1.4)+`GroundCheck`/`WallCheck` 자식 + `PlayerController`/`PlayerAttack`/`PlayerHealth`/**`VoidRespawn`(2026-07-26 추가 — 맵 밖 무한 낙하 소프트락 방지)**/`PlayerAnimator`/`EmotionSkillController`/`RewindSkill`/`HushSkill`/`ForesightSkill`/`AwarenessSystem`을 붙이고, `SerializedObject`로 `groundCheck`/`wallCheck`/`groundLayer`(`Ground`)/`wallLayer`(`Wall`)/`enemyLayer`(`Enemy`)/`interactableMask`(`Interactable`+`Ground`)를 채운다. 레이어는 `Player`.
  - `BuildEnemy()`: `SpriteRenderer`+`Rigidbody2D`+`BoxCollider2D`(0.9×0.9)+`Enemy`(기본 `data`=`Enemy_Residue`)+`EdgeCheck` 자식+`EnemyPatrol`(`edgeCheck`, `groundMask=Ground`)+`ContactDamage`. 레이어는 `Enemy`.
  - `BuildMovingPlatform()`/`BuildCrumblingPlatform()`: `Rigidbody2D`(Kinematic)+`BoxCollider2D`(3×0.5, `Platform.png` 96×16px/32ppu)+각각 `MovingPlatform`/`CrumblingPlatform`. `CrumblingPlatform`에는 `RewindHighlight`(2026-07-26 추가 — 무너져 되감기 가능해지면 골드 아웃라인)도 붙는다. 레이어는 `Ground`.
  - `BuildRewindableBlock()`: `Rigidbody2D`(기본 Dynamic 그대로 — 되감기로 밀린 위치를 되돌려야 하므로)+`BoxCollider2D`(1×1)+`Rewindable`+`RewindHighlight`(2026-07-26 추가). 레이어는 `Ground`.
  - `BuildGazeHazard()`: `SpriteRenderer`(`Eye`)+`GazeHazard`(`playerMask=Player`만, `PlayerHushed`는 제외 — 숨죽이기 중엔 레이어가 바뀌어 시선에서 벗어남; `groundMask=Ground`). 레이어는 `Hazard`.
  - `BuildGate()`: `Blocker` 자식(레이어 `Ground`, `BoxCollider2D` 1×3)+`Hint` 자식(위쪽에 배치)+`Gate`(`blocker`/`hintIcon` 참조).
  - `BuildStoryFragment()`/`BuildHiddenFragment()`: `CircleCollider2D`(반지름 0.25, 트리거)+해당 컴포넌트. `HiddenFragment`는 `SpriteRenderer.enabled = false`로 시작(자각/L 홀드로만 드러남), `visual` 참조를 채운다. 레이어는 `Interactable`.
  - `BuildCheckpoint()`: `BoxCollider2D`(트리거)+`Checkpoint`.
  - `BuildMainCamera()`: 태그 `MainCamera`, `Camera`(orthographic, size 6, `SolidColor`/검정)+`RoomCamera`.
  - `BuildHUD()`: `HUD`+`FragmentLog`만 얹는다 — 두 컴포넌트가 `Awake()`에서 스스로 Canvas 계층을 만드는 self-building 컴포넌트라는 주석이 있다.

## ProjectSetup.cs

- **역할**: 프로젝트 최초 세팅(레이어, 레이어 충돌 행렬, URP 파이프라인, 지역별/자각용 Volume 프로파일, PlayerSettings)을 코드로 재현 가능하게 남긴다.
- **상속/의존**: 정적 클래스. `UnityEngine.Rendering`, `UnityEngine.Rendering.Universal`(`Renderer2DData`, `UniversalRenderPipelineAsset`, `ColorAdjustments`, `Vignette`, `VolumeProfile`)에 의존. 다른 `HiddenWeight.*` 모듈은 참조하지 않는다.
- **주요 멤버**:
  - `static readonly string[] LayerNames = { "Ground", "Wall", "Player", "PlayerHushed", "Enemy", "Hazard", "Interactable" }` — 사용자 레이어 슬롯 8~14번에 순서대로 채워짐.
  - `static readonly (string zoneName, float exposure, float saturation, float hue, float contrast)[] ZoneColorGrading` — `Prologue`(0,0,0,0), `Residue`(-1.2,-45,-15,0), `Gaze`(-0.6,-20,25,-15), `Fracture`(0.8,30,40,0).
  - `static void Run()` — 유일한 공개 진입점. `EditorApplication.Exit`는 호출하지 않는다(다른 배치모드 도구가 이 메서드를 그대로 재사용할 수 있도록 하기 위한 설계라는 주석이 있음).
  - private 단계 메서드: `RegisterLayers()`, `SetupLayerCollisionMatrix()`, `SetupUniversalRenderPipeline()`, `DeleteAssetIfExists(string path)`, `CreateVolumeProfiles()`, `ConfigurePlayerSettings()`, `EnsureSettingsFolder()`.
- **동작**:
  - `RegisterLayers()`: `ProjectSettings/TagManager.asset`을 `SerializedObject`로 열어 `layers` 배열의 8번 인덱스부터 `LayerNames` 7개를 순서대로 채운다.
  - `SetupLayerCollisionMatrix()`: `Physics2D.IgnoreLayerCollision(PlayerHushed, Enemy, true)` — 숨죽이기 상태는 적과 충돌하지 않는다.
  - `SetupUniversalRenderPipeline()`: 재실행 시 "Default Renderer is missing" 에러를 피하기 위해, 먼저 `GraphicsSettings.defaultRenderPipeline = null` + 모든 Quality 레벨의 `renderPipeline = null`로 참조를 비우고, 기존 `HiddenWeight_URP.asset`/`HiddenWeight_Renderer2D.asset`을 지운 뒤, `Renderer2DData`와 `UniversalRenderPipelineAsset.Create(rendererData)`를 새로 만들어 `GraphicsSettings`와 모든 Quality 레벨에 다시 연결한다. 원래 Quality 레벨 인덱스로 복원까지 한다.
  - `CreateVolumeProfiles()`: `ZoneColorGrading` 4종 각각에 대해 `VolumeProfile` 에셋(`Assets/Settings/Volume_{zoneName}.asset`)을 만들고 `ColorAdjustments`(postExposure/saturation/hueShift/contrast)를 서브에셋(`AddObjectToAsset`)으로 붙인다. 추가로 자각 전용 `Volume_Awareness.asset`을 만들어 `ColorAdjustments.saturation = -80`과 `Vignette.intensity = 0.4`를 붙인다("채도를 잃는 연출", 기획서 7.2절).
  - `ConfigurePlayerSettings()`: `companyName = "NHN Hackerton"`, `productName = "Hidden Weight"`, 기본 해상도 1920×1080, 창모드(`defaultIsFullScreen = false`).
  - `Run()` 마지막에 `AssetDatabase.SaveAssets()` + `AssetDatabase.Refresh()`.

## ZoneSceneBuilder.cs

- **역할**: 씬 7개(`Bootstrap`, `Title`, `Zone_Prologue`, `Zone_Residue`, `Zone_Gaze`, `Zone_Fracture`, `Ending`)를 코드로 조립하고 `EditorBuildSettings.scenes`에 등록한다. 6개 파일 중 가장 크고, 룸 배치·타일맵·프리팹 인스턴스화·Volume까지 전부 이 파일에서 처리한다.
- **상속/의존**: 정적 클래스. `UnityEditor.SceneManagement`, `UnityEngine.SceneManagement`, `UnityEngine.Tilemaps`, `UnityEngine.Rendering`(`Volume`), `UnityEngine.UI`, `UnityEngine.EventSystems`, `HiddenWeight.Core`(`GameManager`), `HiddenWeight.Data`(`ZoneData`, `EnemyData`, `EmotionId`), `HiddenWeight.World`(`Room`, `ZoneTrigger`, `StoryFragment`, `HiddenFragment`, `Gate`, `Enemy`... 등 World측 타입), `HiddenWeight.Enemies`(`Enemy`, `MovingPlatform`은 World 소속), `HiddenWeight.UI`(`PauseMenu`, `TitleScreen`), `HiddenWeight.Ending`(`EndingSequence`, `AnomalyObject`)에 의존. `ProjectSetup.Run()`은 절대 호출하지 않는다(URP 에셋 GUID 재생성 시 참조가 깨지므로, Volume 프로파일은 항상 기존 `ZoneData` 에셋의 참조를 그대로 읽어 쓴다는 주석이 있음).
- **주요 멤버**:
  - `const string ScenesFolder = "Assets/Scenes"`, `PrefabFolder = "Assets/Prefabs"`, `DataFolder = "Assets/ScriptableObjects"`, `ArtFolder = "Assets/Art/Placeholder"`.
  - `static void Run()` — 유일한 공개 진입점.
  - 공통 헬퍼: `static Scene NewScene()`(`EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)`), `static void SaveScene(Scene scene, string name)`(`EditorSceneManager.SaveScene(scene, $"{ScenesFolder}/{name}.unity")`), `static Sprite LoadSprite(string name)`, `static T LoadData<T>(string name) where T : Object`, `static GameObject Spawn(string prefabName, Vector3 position)`(프리팹을 `PrefabUtility.InstantiatePrefab`로 인스턴스화 후 위치 지정), `static void SetField(Object target, string propertyName, System.Action<SerializedProperty> apply)`(SerializedObject 필드 세팅 공통 래퍼).
  - 타일 헬퍼: `static Tile GroundTile()`(1회 생성 캐시, `Assets/Art/Placeholder/GroundTile.asset`), `static void PlaceTiles(Tilemap tilemap, TileBase tile, int xMin, int xMax, int yMin, int yMax)`(사각형 영역 채우기), `static void Floor(Tilemap tilemap, int xMin, int xMax, int topY, int depth = 6)`(표면 높이 `topY`, 깊이 `depth`의 solid 바닥 구간), `static Tilemap BuildGroundGrid(out GameObject gridGO)`(Grid+Tilemap+TilemapCollider2D+CompositeCollider2D 구성, 레이어 `Ground`).
  - 배치 헬퍼: `BuildRoom`, `BuildZoneVolume`, `BuildSolidBlock`, `BuildSafePlatform`, `BuildZoneTrigger`, `BuildStoryFragment`, `BuildHiddenFragment`, `BuildGate`, `BuildEnemy`, `BuildGazeHazard`(2026-07-26: `rotateSpeed` 인자 추가 — 0이 아니면 `GazeRotator`를 붙여 회전형), `BuildMovingPlatform`, `BuildCrumblingPlatform`, `BuildRewindableBlock`, `BuildCheckpoint`, `BuildEventSystem`, `BuildPauseMenu`, `BuildZoneCommon(string zoneAssetName, Vector3 playerSpawn, out GameObject root)`(GameManager+MainCamera+Player+HUD+PauseMenu+EventSystem+Grid/Tilemap+지역 Volume 공통 킷).
  - 2026-07-26 신규 헬퍼: `BuildDecor`(충돌 없는 배경 연출 스프라이트 — 새장·무너진 탑·거울 기둥 등), `BuildTutorialHint`(UI `TutorialHint` 배치), `BuildAwarenessUnlock`(거대 눈 오브제+트리거로 `AwarenessUnlockMoment` 배치), `BuildBoundary`(보이지 않는 지역 경계벽 — `Ground` 레이어라 벽잡기 불가, 스프라이트 비활성). 왼쪽 경계는 `BuildZoneRoot`가 모든 지역에 공통으로 세우고(x=-2), 오른쪽 경계는 폭이 달라 각 지역 빌더가 세운다(프롤로그 73 / 잔재·응시 97 / 균열 109) — 이전 지역 방향으로 되돌아가면 허공 무한 낙하로 소프트락되던 문제의 1차 방어선.
  - 씬별 빌더: `BuildBootstrap()`, `BuildTitle()`, `BuildZonePrologue()`, `BuildZoneResidue()`, `BuildZoneGaze()`, `BuildZoneFracture()`, `BuildEnding()`, `BuildAnomaly(...)`(Ending 전용), `RegisterBuildSettings()`.
- **동작**:
  - `Run()`: `EnsureScenesFolder()` → 7개 씬을 순서대로 빌드(`BuildBootstrap` ~ `BuildEnding`) → `RegisterBuildSettings()`로 `EditorBuildSettings.scenes` 등록 → `SaveAssets()`/`Refresh()`.
  - `BuildBootstrap()`: `GameManager` 프리팹을 스폰하고 `autoLoadTitle=true`로 오버라이드.
  - `BuildTitle()`: `GameManager` 스폰 + `TitleScreen` 컴포넌트를 얹은 오브젝트 + `EventSystem`.
  - `BuildZonePrologue()`(3룸, 튜토리얼): Room1(평지, 좌우 이동), Room2(3단 계단+구덩이, 실패 시 낮은 안전 통로로 낙하), Room3(높이 8 벽 2개 사이 벽점프+상단 발판). `ZoneTrigger(marksFractureCleared=false)`로 종료.
  - `BuildZoneResidue()`(4룸, 되감기): Room1 입구(Checkpoint+StoryFragment로 Rewind 스킬 부여), Room2(RewindableBlock 3개, 무너진 다리), Room3(CrumblingPlatform 4개 연속), Room4(Enemy 2+Gate(Rewind)+출구, 곁가지로 `requiresFinalCondition=true` Gate 뒤에 `residue_hidden_final` HiddenFragment).
  - `BuildZoneGaze()`(4룸, 숨죽이기): Room1(GazeHazard 1개 예고), Room2(StoryFragment로 Hush+자각 부여), Room3(GazeHazard 3개 + 낮은 천장, 숨죽이기 전용 통과), Room4(Enemy_Gaze 2 + HiddenFragment 2 + 출구).
  - `BuildZoneFracture()`(4룸, 예지): Room1(StoryFragment로 Foresight 부여), Room2(MovingPlatform 3개, 주기 3/5/7초), Room3(CrumblingPlatform 3개 + BuildSafePlatform 3개 교대 배치, 예지로 구분), Room4(Enemy_Fracture 2 + HiddenFragment 2 + `ZoneTrigger(marksFractureCleared=true)`로 종료 — 기획서 5.3절 백트래킹의 균열 클리어 마킹).
  - `BuildEnding()`: 횡스크롤 없는 정적 침실 씬. `Wall`/`Bed` 배경 스프라이트 + `AnomalyObject` 3개(`InvertedCandle`, `MismatchedShadow`, `TremblingWall`) + 전체화면 몽타주 Canvas/Image + `EndingSequence`(anomalies 3개, montageImage, montageFrames 3개를 `Tile`/`Eye`/`Gate` 스프라이트로 채움).
  - `RegisterBuildSettings()`: `EditorBuildSettings.scenes`를 7개 씬 경로로 덮어쓴다(전부 `enabled: true`).
