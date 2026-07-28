# Editor 모듈 — 프로젝트 세팅·데이터/아트/프리팹/씬 자동 생성 + 빌드 검증

> 기획서 8장(구현 순서), 9장(검증 방법) 대응.
> `Editor`(네임스페이스는 `HiddenWeight.EditorTools`)는 다른 모든 모듈(Core, Data, Player, World,
> Emotions, Enemies, Ending, UI)을 참조하는 유일한 모듈이다. 여기 실린 클래스는 전부 배치모드에서
> `-executeMethod`로 호출되는 정적 진입점이며, 사람이 에디터를 열어 손으로 조작하는 도구가 아니라
> 프로젝트를 코드만으로 재현 가능한 상태로 만드는 1회성(재실행 안전) 생성기다.

## 파일

| 파일 | 역할 | 기획서 대응 |
|---|---|---|
| `ProjectSetup.cs` | 레이어 7종(Ground/Wall/Player/PlayerHushed/Enemy/Hazard/Interactable) 등록, 레이어 충돌 행렬, URP 2D 파이프라인 생성, 지역별 Volume 프로파일 4종 + 자각 전용 Volume 1종, PlayerSettings | 2.1 레이어, 8.1 프로젝트 생성 |
| `DataAssetBuilder.cs` | ScriptableObject 데이터 에셋 12종(PlayerData 1 + EmotionData 3 + EnemyData 3 + ZoneData 4 + BalanceData 1) 생성. 이미 있는 에셋은 값을 덮어쓰지 않고 그대로 반환 | 8.2 Data 단계, 밸런스 수치 |
| `PlaceholderArtBuilder.cs` | 단색 PNG 스프라이트 10종을 코드로 생성하고 Sprite/PPU 32/Point 필터로 임포트 설정 | 2절 PPU 기준, 8.10 플레이스홀더 |
| `PrefabBuilder.cs` | 프리팹 13종(GameManager 포함) 생성, 다른 모듈의 컴포넌트를 `AddComponent`로 조립. 2026-07-26: Player에 `VoidRespawn`, RewindableBlock·CrumblingPlatform에 `RewindHighlight` 추가 | 8.10 프리팹 자동 생성 |
| `ZoneSceneBuilder.cs` | 씬 7개(Bootstrap/Title/Zone_Prologue/Zone_Residue/Zone_Gaze/Zone_Fracture/Ending) 생성, 룸·타일맵·프리팹 배치, `EditorBuildSettings.scenes` 등록. 2026-07-26: 좌우 경계벽(`BuildBoundary`, 낙하 소프트락 방지)·프롤로그 굴뚝 공중 부양 벽(입구 개방)·회전형 눈 3개(위상차 0/120/240도)·자각 해금 지점(`BuildAwarenessUnlock`, 응시 Room3 끝)·연출 데코(`BuildDecor` — 새장/무너진 탑/거울 방)·튜토리얼 힌트 7곳(`BuildTutorialHint`)·균열 파편을 보이는 `StoryFragment`로 교체(자각 무력화 대응) | 4장 씬 구성, 5.3 백트래킹, 8.10 씬 자동 생성 |
| `ResidueZoneBuilder.cs` | `ZoneSceneBuilder`의 partial. 잔재 재설계 전체 지역(주 동선 12룸 + 비밀 3룸)을 `Zone_Residue_Full` 씬으로 짓는다. 방 로컬 좌표를 전역으로 옮기는 `RoomCtx`와 조우·보상·숏컷 조립 헬퍼도 여기 있다 | RESIDUE_ROOM_IMPLEMENTATION.md |
| `GazeZoneBuilder.cs` | 같은 partial. 응시 전체 지역(G01~G12 + GS1~GS3)을 `Zone_Gaze_Full` 씬으로 짓는다. 시선 배치·숨죽이기 게이트 규격·재판관 연결·눈꺼풀 벽을 담당 | GAZE_LEVEL_DESIGN.md |
| `FractureZoneBuilder.cs` | 같은 partial. 균열 전체 지역(F01~F12 + FS1~FS3)을 `Zone_Fracture_Full` 씬으로 짓는다. 자각으로 여는 문을 하나도 두지 않고, 붕괴 발판은 전부 자동 복구로 만든다 | FRACTURE_LEVEL_DESIGN.md |
| `BuildScript.cs` | 배치모드 컴파일 게이트(`Compile`) + macOS 스탠드얼론 빌드(`BuildMac`) | 8.11, 9.1~9.2 검증 방법 |

## 핵심 규칙 구현

정적 진입점(모두 `HiddenWeight.EditorTools.<Class>.<Method>` 형태로 `-executeMethod`에 넘긴다):

- `ProjectSetup.Run()`
- `DataAssetBuilder.Run()`
- `PlaceholderArtBuilder.Run()`
- `PrefabBuilder.Run()`
- `ZoneSceneBuilder.Run()`
- `BuildScript.Compile()` — `EditorUtility.scriptCompilationFailed`가 `true`면 에러 로그 후 `EditorApplication.Exit(1)`, 아니면 `EditorApplication.Exit(0)`. `-executeMethod` 자체가 컴파일 실패 시 실행되지 못하므로, 이 메서드가 돌아 exit 코드를 남겼다는 사실 자체가 "컴파일 통과"의 증거다.
- `BuildScript.BuildMac()` — 씬 7개를 하드코딩된 배열로 나열해 `BuildPipeline.BuildPlayer(scenes, "Builds/macOS/HiddenWeight.app", BuildTarget.StandaloneOSX, BuildOptions.None)` 호출. `report.summary.result != BuildResult.Succeeded`면 `Exit(1)`, 성공하면 `Exit(0)`.

기타 규칙:

- `DataAssetBuilder`, `PrefabBuilder`, `ProjectSetup`의 URP 에셋 재생성 로직은 전부 "이미 있으면 건너뛴다"(`LoadOrCreate` 패턴) 또는 "참조를 비우고 지운 뒤 다시 만든다"(URP 렌더 파이프라인 교체 시 `GraphicsSettings`/`QualitySettings` 참조를 먼저 null로 비워야 "Default Renderer is missing" 에러가 안 남) 식으로 재실행 안전성을 갖는다.
- `ZoneSceneBuilder`는 `ProjectSetup.Run()`을 절대 다시 호출하지 않는다 — URP 에셋이 새 GUID로 재생성되면 `ZoneData.volumeProfile` 참조가 깨지기 때문이다. 대신 `DataAssetBuilder`가 이미 `Assets/Settings/Volume_{zone}.asset` 경로로 읽어 각 `ZoneData`에 박아 둔 참조를 그대로 쓴다.
- `PrefabBuilder`는 `GameManager`(+`BalanceData` 참조)를 가장 먼저 만들어 씬에 살려 두고 가장 마지막에 저장·파괴한다 — 다른 컴포넌트의 `Awake()`가 `GameManager.Instance.Balance.*`를 참조하므로, 프리팹을 짓는 동안 `NullReferenceException` 콘솔 노이즈를 막기 위함이다.

## 씬 배치

이 모듈 자체는 씬에 배치되는 컴포넌트가 없다 — 반대로 씬/프리팹/에셋을 **만들어내는** 배치모드 도구다. 실제 의존 관계(코드에서 확인)에 따른 1회성 실행 순서는 다음과 같다.

1. `ProjectSetup.Run()` — 레이어·URP·Volume 프로파일·PlayerSettings 준비
2. `DataAssetBuilder.Run()` — `ZoneData.volumeProfile`이 1번이 만든 `Volume_*.asset`을 경로로 읽어야 하므로 반드시 1번 다음
3. `PlaceholderArtBuilder.Run()` — 스프라이트 PNG 생성 (2번과는 독립적이지만 4/5번보다 먼저 필요)
4. `PrefabBuilder.Run()` — 2번의 `BalanceData`/`EnemyData`와 3번의 스프라이트를 `LoadData`/`LoadSprite`로 참조
5. `ZoneSceneBuilder.Run()` — 4번의 프리팹을 `Spawn()`으로 인스턴스화하고, 2번의 `ZoneData`/`EnemyData`, 3번의 스프라이트(Ending 배경, GroundTile)를 함께 참조. `EditorBuildSettings.scenes`도 이 단계에서 등록
6. `BuildScript.Compile()` / `BuildScript.BuildMac()` — 위 5단계가 끝난 상태에서 컴파일·빌드 검증

## 다른 모듈과의 연결

- `Editor`는 `Core`, `Data`, `Player`, `Emotions`, `Enemies`, `World`, `UI`, `Ending`을 모두 `using`한다(설계 문서 3.1절 `Editor ──▶ (전부)`). 다른 모듈은 서로 단방향 참조만 허용되지만, Editor는 배치모드에서 프리팹·씬을 조립하려면 각 모듈의 구체 컴포넌트 타입(`PlayerController`, `Enemy`, `Room`, `HUD`, `EndingSequence` 등)에 직접 `AddComponent`/`GetComponent`로 접근해야 하므로 예외적으로 전체 참조가 허용된다.
- `PrefabBuilder`는 `HiddenWeight.Player`(`PlayerController`, `PlayerAttack`, `PlayerHealth`, `PlayerAnimator`), `HiddenWeight.Emotions`(`EmotionSkillController`, `RewindSkill`, `HushSkill`, `ForesightSkill`, `AwarenessSystem`), `HiddenWeight.Enemies`(`Enemy`, `EnemyPatrol`, `ContactDamage`), `HiddenWeight.World`(`MovingPlatform`, `CrumblingPlatform`, `Rewindable`, `GazeHazard`, `Gate`, `StoryFragment`, `HiddenFragment`, `Checkpoint`, `RoomCamera`), `HiddenWeight.UI`(`HUD`, `FragmentLog`), `HiddenWeight.Core`(`GameManager`, `AudioManager`, `ScreenFader`)를 조립한다.
- `ZoneSceneBuilder`는 여기에 더해 `HiddenWeight.World.Room`/`ZoneTrigger`, `HiddenWeight.UI.PauseMenu`/`TitleScreen`, `HiddenWeight.Ending.EndingSequence`/`AnomalyObject`까지 다룬다.
- `DataAssetBuilder`는 `HiddenWeight.Data`(`PlayerData`, `EmotionData`, `EnemyData`, `ZoneData`, `BalanceData`, `EmotionId`, `ZoneId`, `SkillInput`)만 참조한다.

## 의존성 주의

- 배치모드 호출은 아래 패턴을 그대로 쓴다. `<Class>.<Method>` 자리만 바꾼다(예: `ProjectSetup.Run`, `DataAssetBuilder.Run`, `PlaceholderArtBuilder.Run`, `PrefabBuilder.Run`, `ZoneSceneBuilder.Run`, `BuildScript.Compile`, `BuildScript.BuildMac`).

```bash
cd "/Users/ksh/Desktop/NHN HACKERton"
"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" \
  -logFile "$PWD/.unity-logs/<name>.log" \
  -executeMethod HiddenWeight.EditorTools.<Class>.<Method>
```

- 순서를 어기면(예: `PrefabBuilder`를 `DataAssetBuilder`/`PlaceholderArtBuilder`보다 먼저 실행) `LoadData<T>`/`LoadSprite`가 `null`을 반환하고, `SerializedObject.FindProperty(...).objectReferenceValue = null`이 조용히 적용되어 콘솔 에러 없이 참조 누락 상태로 저장된다 — 실행 순서를 반드시 지킬 것.
- `ZoneSceneBuilder.Run()`은 `ProjectSetup.Run()`을 호출하지 않는다. 만약 URP 에셋을 다시 만들어야 한다면 `ProjectSetup.Run()`을 다시 돌린 뒤 `DataAssetBuilder.Run()`도 함께 재실행해 `ZoneData.volumeProfile`이 새 경로를 다시 읽게 해야 한다(경로 기반 조회라 GUID가 바뀌어도 깨지지는 않는다).
- `DataAssetBuilder`/`ProjectSetup`은 재실행해도 기존 에셋 값을 덮어쓰지 않으므로, 수동으로 조정한 밸런스 수치는 안전하게 보존된다. 반대로 `PlaceholderArtBuilder`/`PrefabBuilder`/`ZoneSceneBuilder`는 재실행 시 동일 경로에 덮어쓰거나(스프라이트 PNG, 프리팹) 씬을 통째로 새로 만들므로, 씬/프리팹에 손으로 가한 수정은 재실행하면 사라진다.
- `BuildScript.BuildMac()`의 씬 목록은 `ZoneSceneBuilder.RegisterBuildSettings()`가 만드는 `EditorBuildSettings.scenes`와 별개로 하드코딩되어 있다 — 씬 이름/개수가 바뀌면 두 곳을 모두 고쳐야 한다.
