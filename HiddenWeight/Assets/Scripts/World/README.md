# World 모듈 — 룸·발판·게이트 등 지역 배치물과 되감기/예지/자각 계약 제공

> 기획서 5.5(월드) 대응.
> World는 `HiddenWeight.Core`, `HiddenWeight.Data`, `HiddenWeight.Player`를 참조하지만, `HiddenWeight.Emotions`나 `HiddenWeight.UI`는 원칙적으로 참조하지 않는다. `Interactions.cs`는 이 모듈이 다른 모듈에 내주는 계약(인터페이스)만 모아둔 파일로, 그 자체는 `HiddenWeight.*`를 전혀 참조하지 않는 중립 지대다.

## 파일

| 파일 | 역할 | 기획서 대응 |
|---|---|---|
| `Interactions.cs` | `IRewindable`/`IForeseeable`/`IAwarenessReactive`/`IDamageable` 인터페이스와 `AwarenessRegistry` 정적 등록소를 선언하는 의존성 없는 계약 파일 | 3.1 의존 방향, 4.2 되감기/예지/자각 |
| `Rewindable.cs` | `IRewindable`의 기본 구현. 초기 Transform·활성 상태·스프라이트를 저장했다가 복원 | 4.2 되감기 |
| `CrumblingPlatform.cs` | 밟으면 무너지는 발판. `IRewindable`(되감기로 복구) + `IForeseeable`(예지로 무너진 뒤 사라진 모습을 미리 표시) | 5.5 월드, 4.2 되감기/예지 |
| `MovingPlatform.cs` | 왕복 이동 발판. 위치를 시간 기반 순수 함수로 계산해 `IForeseeable`이 미래 위치를 정확히 예측하게 함 | 5.5 월드, 4.2 예지 |
| `Gate.cs` | 필요 스킬(`EmotionId`)을 지정하고 `GameManager.Progress`로 통과 가능 여부를 매 프레임 확인, 미보유 시 차단+힌트 표시 | 5.5 월드, 5.6 진행 상태 |
| `GazeHazard.cs` | 응시 지역의 원뿔 시야 기믹. 시야 안에 플레이어가 있으면 주기적으로 피해 | 5.5 월드 |
| `HiddenFragment.cs` | `StoryFragment`를 상속하고 `IAwarenessReactive`를 구현. 자각(L 홀드) 중에만 보이고 수집 가능 | 5.5 월드, 5.6 진행 상태(자각) |
| `StoryFragment.cs` | 이야기 파편 수집 오브젝트. 스킬 해금·자각 해금 지점으로도 쓰인다 | 5.5 월드 |
| `ZoneTrigger.cs` | 지역 클리어 지점. 다음 씬으로 전환하고, 균열 클리어/백트래킹 엔딩 분기를 처리 | 5.5 월드, 5.3 백트래킹 |
| `Room.cs` | 룸 단위 카메라 경계. 플레이어 진입 시 `RoomCamera`에 자신을 등록 | 5.5 월드(룸) |
| `RoomCamera.cs` | 현재 룸 경계 안에서 플레이어를 부드럽게 따라가는 카메라 | 5.5 월드(룸 카메라) |

## 핵심 규칙 구현

- **되감기(`IRewindable`)**: `CaptureInitial()`로 초기 상태를 저장하고 `Rewind()`로 되돌린다. `Rewindable`은 위치/회전/활성/스프라이트를, `CrumblingPlatform`은 `HasCrumbled` 플래그 하나만 되돌린다(위치를 바꾸지 않는 발판이라 `CaptureInitial()`은 빈 구현). `CanRewind`는 "이미 초기 상태면 false"를 보장해 불필요한 되감기를 막는다.
- **예지(`IForeseeable`)**: `PredictPosition(leadSeconds)`/`PredictActive(leadSeconds)`/`CurrentSprite`로 `leadSeconds` 뒤의 위치·존재 여부·고스트 스프라이트를 알려준다. `MovingPlatform`은 시간 기반 순수 함수(`PositionAt`)라 미래 위치를 그대로 계산하면 되고, `CrumblingPlatform`은 무너지는 타이머가 `leadSeconds` 안에 끝나면 `PredictActive`가 `false`를 돌려준다.
- **자각(`IAwarenessReactive`)**: `OnAwarenessChanged(bool active)` 한 메서드뿐이다. 구현체는 `OnEnable`/`OnDisable`에서 스스로 `AwarenessRegistry.Register`/`Unregister`로 등록/해제한다(예: `HiddenFragment`). World는 누가 이 목록을 순회해서 호출하는지 몰라도 되며, 실제 호출자는 Emotions의 AwarenessSystem이다.
- **게이트 규칙**: 일반 게이트는 `ProgressState.CanOpenGate(requiredSkill)`(요구 스킬이 `None`이면 무조건 통과, 아니면 보유 여부만 확인)로 열리고, `requiresFinalCondition = true`인 게이트(잔재 백트래킹 최종 파편용)만 `ProgressState.CanOpenFinalGate()`(되감기 보유 && 자각 && 균열 클리어)를 대신 확인한다. 두 조건은 배타적으로, `Gate` 하나가 둘 다 검사하지는 않는다.
- **지역 전환 흐름**: `ZoneTrigger`는 `GameManager.CurrentZoneData.nextSceneName`(없으면 `SceneFlow.Title`)을 기본 목적지로 삼되, `marksFractureCleared = true`인 트리거를 통과할 때 `Progress.MarkFractureCleared()`를 먼저 호출한다. 이후 "잔재(Residue) 지역이면서 이미 균열을 클리어한 상태"에서 잔재 출구를 다시 타면 목적지를 강제로 `SceneFlow.Ending`으로 덮어써 백트래킹 엔딩 분기를 만든다. 실제 전환은 항상 `SceneFlow.LoadWithFade(next)`로 수행한다.

## 씬 배치

- `Room`은 `[RequireComponent(typeof(BoxCollider2D))]`이며 `OnValidate`에서 콜라이더를 `isTrigger = true`, `size`/`offset`을 자동 동기화한다. 레벨 배치 시 룸 경계마다 하나씩 배치하고 인스펙터의 `size`로 룸 크기를 지정하면 된다. `RoomCamera`는 씬의 메인 카메라(`[RequireComponent(typeof(Camera))]`)에 붙이고 정적 `Instance`로 접근하며, `SnapToPlayer()`는 씬 진입·리스폰 시 Lerp 없이 즉시 스냅하고 싶을 때 별도로 호출해야 한다(자동 호출되지 않음).
- `Gate`의 `blocker`(실제 콜라이더 오브젝트)와 `hintIcon`(필요 스킬 아이콘)은 각각 별도 인스펙터 슬롯이며 둘 다 없어도 동작한다(둘 다 null 체크).
- 레이어 요구사항: `CrumblingPlatform`/`MovingPlatform`은 `"Player"` 레이어와의 `OnCollisionEnter2D`(위에서 밟았는지 `contact.normal.y > 0.5f`로 판정)로 동작하므로 플레이어는 `Player` 레이어, 지형/발판은 `Ground`(또는 `Interactable`) 레이어에 있어야 한다. `GazeHazard`는 인스펙터의 `playerMask`에 `Player` 레이어만 넣고 `PlayerHushed`는 절대 넣지 않는다(핵심 규칙 참고). `groundMask`는 시야 차단 판정(`Physics2D.Linecast`)에 쓰이므로 `Ground`/`Wall` 레이어를 넣는다. `StoryFragment`/`ZoneTrigger`/`Room`은 `"Player"` 레이어와의 트리거 충돌만 보므로 `Interactable` 레이어 + `IsTrigger` 콜라이더로 배치한다.

## 다른 모듈과의 연결

- **`Interactions.cs`는 중립 계약 파일**이다. `using HiddenWeight.*`가 전혀 없고, `IRewindable`/`IForeseeable`/`IAwarenessReactive`/`IDamageable`을 선언만 한다. Emotions의 `RewindSkill`/`ForesightSkill`/`AwarenessSystem`과 Player의 `PlayerAttack`이 각각 이 인터페이스만 참조해서 World의 구체 클래스(`CrumblingPlatform`, `HiddenFragment` 등)나 Enemies의 `Enemy`를 몰라도 되게 한다. 특히 `IDamageable`은 Player → Enemies 직접 참조 순환을 끊기 위한 계약으로, `PlayerAttack`은 `IDamageable`만 참조하고 `Enemy` 타입은 전혀 참조하지 않는다.
- **`AwarenessRegistry`**는 World가 소유한 정적 등록소다(`Items`, `Added` 이벤트, `Register`/`Unregister`). `HiddenFragment` 같은 `IAwarenessReactive` 구현체가 스스로 등록/해제하고, Emotions의 AwarenessSystem은 `AwarenessRegistry.Items`를 순회하며 `OnAwarenessChanged`를 호출한다 — World가 Emotions를 몰라도 Emotions가 World를 관찰할 수 있는 역방향 훅이다.
- **유일한 예외**: `StoryFragment.cs`만 `using HiddenWeight.Emotions;`를 갖는다. `Collect()`에서 스킬을 지급한 직후 `EmotionSkillController.Instance?.RefreshActive()`를 호출해, 현재 활성 스킬 컨트롤러가 새로 해금된 스킬을 즉시 반영하도록 만든다. 이는 기획서 3.1절에 명시된, 계획적으로 허용된 단 한 줄짜리 예외이며 그 외에 World는 Emotions나 UI를 어디서도 참조하지 않는다.
- World가 Core로 나가는 훅: `SceneFlow.LoadWithFade(sceneName)`(`ZoneTrigger`의 지역 전환), `GameManager.FragmentPresenter?.Invoke(text)`(`StoryFragment`가 UI를 직접 참조하지 않고 파편 텍스트를 화면에 띄우는 훅), `GameManager.Instance.Progress`(`Gate`/`StoryFragment`/`ZoneTrigger`가 진행 상태를 읽고 쓰는 공통 통로).

## 의존성 주의

- `Interactions.cs`를 수정할 때는 이 계약을 소비하는 다른 모듈(Emotions의 스킬들, Player의 `PlayerAttack`, Enemies의 `Enemy`)도 함께 확인해야 한다 — 여기서 시그니처가 바뀌면 World 밖 여러 모듈이 동시에 깨진다.
- `StoryFragment`의 Emotions 참조를 다른 파일로 옮기거나 늘리지 말 것. World→Emotions 참조가 늘어나면 기획서 3.1절이 정의한 의존 방향(계획적 예외 1건 한정)이 깨진다.
- `Gate`/`StoryFragment`/`ZoneTrigger`는 모두 `GameManager.Instance`가 존재한다고 가정하고 null 체크 없이 바로 접근한다. Bootstrap 씬을 거치지 않고 지역 씬을 단독 실행하면 `NullReferenceException`이 난다(Core 모듈 README와 동일한 주의).
- `AwarenessRegistry`는 씬 전환 시 정적 리스트가 초기화되지 않는다(`static readonly List`). `HiddenFragment`가 `OnDisable`에서 `Unregister`를 호출하므로 오브젝트가 파괴/비활성화되면 정상적으로 목록에서 빠지지만, `OnDisable` 없이 파괴되는 경로가 생기면 죽은 참조가 남을 수 있다.
