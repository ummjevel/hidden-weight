# World 모듈

`HiddenWeight.World`는 룸/카메라, 발판, 게이트, 시선 기믹, 파편, 지역 전환 트리거 등 지역 배치물을 담당한다. `Interactions.cs`는 이 모듈이 다른 모듈에 내주는 계약(`IRewindable`/`IForeseeable`/`IAwarenessReactive`/`IDamageable`/`AwarenessRegistry`)만 모아둔, `HiddenWeight.*`를 전혀 참조하지 않는 의존성 없는 파일이다. World는 `Core`/`Data`/`Player`를 참조하며, `StoryFragment.cs` 한 곳만 예외적으로 `Emotions`를 참조한다.

## AwarenessUnlockMoment.cs (2026-07-26 신규)

- **역할**: 자각 해금 지점(기획서 EMOTION_SYSTEM 2.4절 — 응시 지역 후반부, 가장 큰 "눈" 오브제 앞). 대사 없이 입력을 잠그고 거대 눈이 플레이어를 향해 커졌다가 가라앉는 연출 뒤 자각을 부여한다. (이전에는 응시 Room2의 `StoryFragment`가 숨죽이기와 자각을 동시에 부여했다 — 이 컴포넌트로 해금 위치가 기획대로 후반부로 이동했다.)
- **상속/의존**: `MonoBehaviour`, `[RequireComponent(typeof(Collider2D))]`. `HiddenWeight.Core`(`GameManager`), `HiddenWeight.Data`(`PlayerLayers`), `HiddenWeight.Player`(`PlayerInput`).
- **주요 멤버**: `[SerializeField] SpriteRenderer eyeVisual`(거대 눈 오브제), `buildUpSeconds = 1.5f`, `holdSeconds = 1f`, `fragmentText`(해금 순간 화면에 띄울 한 줄), `fragmentId = "gaze_awareness"`.
- **동작**: `OnTriggerEnter2D`에서 플레이어 최초 1회만 `UnlockRoutine` 시작. 코루틴은 `PlayerInput.Enabled = false`로 잠근 뒤 눈을 1.35배로 키우며 밝히고(1.5초) → 유지(1초) → 다시 가라앉힌다(1.5초, "정면으로 마주본 뒤에야 눈이 가라앉는다" 연출). 끝나면 `Progress.GrantAwareness()` + `CollectFragment(fragmentId)` + `GameManager.FragmentPresenter?.Invoke(fragmentText)` 후 입력 복구. `OnDestroy`에서 연출 도중 씬이 내려가도 입력이 잠긴 채 남지 않도록 방어적으로 복구한다.

## CrumblingPlatform.cs

- **역할**: 플레이어가 위에서 밟으면 잠시 흔들리다 무너져 사라지는 발판. 되감기로 원상 복구되고, 예지로 무너진 뒤(사라진) 미래 모습을 미리 볼 수 있다.
- **상속/의존**: `MonoBehaviour`, `IRewindable`, `IForeseeable`. `[RequireComponent(typeof(Collider2D))]`, `[RequireComponent(typeof(SpriteRenderer))]`.
- **주요 멤버**:
  - `[SerializeField] float crumbleDelay = 0.6f` — 밟은 뒤 무너지기까지 걸리는 시간.
  - `[SerializeField] float respawnDelay = 0f` — 0이면 되감기로만 복구되고, 0보다 크면 무너진 뒤 그 시간 뒤에 스스로 `Rewind()`한다.
  - `bool HasCrumbled { get; private set; }` — 무너졌는지 여부.
  - `Transform Transform => transform`, `bool CanRewind => HasCrumbled`, `Sprite CurrentSprite => _sprite.sprite`.
  - `void CaptureInitial()` — 빈 구현(위치를 바꾸지 않는 발판이라 되돌릴 상태가 `HasCrumbled` 하나뿐이므로 초기 캡처가 불필요).
  - `void Rewind()` — 진행 중이던 무너짐 코루틴을 멈추고 콜라이더/스프라이트를 다시 켜서 `HasCrumbled = false`로 되돌린다.
  - `Vector3 PredictPosition(float leadSeconds) => transform.position` — 움직이지 않으므로 항상 현재 위치.
  - `bool PredictActive(float leadSeconds)` — 무너지는 타이머(`_crumbleTimer`)가 `leadSeconds` 이내에 끝나거나 이미 무너졌으면 `false`.
- **동작**:
  - `OnCollisionEnter2D`에서 이미 무너졌거나 무너지는 중이면 무시하고, `"Player"` 레이어가 아니면 무시하며, 접촉면 법선(`contact.normal.y > 0.5f`)으로 "위에서 밟았는지"만 확인해 통과하면 `CrumbleRoutine`을 시작한다.
  - `CrumbleRoutine`은 `crumbleDelay` 동안 매 프레임 `Random.Range(-0.05f, 0.05f)`로 x축을 흔들다가, 시간이 다 되면 원위치로 되돌리고 콜라이더/스프라이트를 끄고 `HasCrumbled = true`로 만든다. `respawnDelay > 0f`면 `RespawnRoutine`을 추가로 시작해 일정 시간 뒤 자동으로 `Rewind()`를 호출한다.

## Gate.cs

- **역할**: 특정 감정 스킬(`EmotionId`)을 보유해야 통과할 수 있는 문. 잔재 백트래킹 최종 파편용 게이트도 같은 컴포넌트로 표현한다.
- **상속/의존**: `MonoBehaviour`. `HiddenWeight.Core`(`GameManager`), `HiddenWeight.Data`(`EmotionId`)에 의존.
- **주요 멤버**:
  - `[SerializeField] EmotionId requiredSkill` — 통과에 필요한 스킬. `public EmotionId RequiredSkill => requiredSkill`로 읽기 전용 노출.
  - `[SerializeField] bool requiresFinalCondition` — true면 `requiredSkill` 대신 최종 조건(균열 클리어 뒤 잔재 백트래킹)을 검사한다.
  - `[SerializeField] GameObject blocker` — 실제로 길을 막는 콜라이더 오브젝트(없어도 동작).
  - `[SerializeField] SpriteRenderer hintIcon` — 필요 스킬 아이콘(없으면 무시).
  - `bool IsOpen` — `GameManager.Instance.Progress`를 읽어, `requiresFinalCondition`이면 `p.CanOpenFinalGate()`를, 아니면 `p.CanOpenGate(requiredSkill)`를 그대로 돌려주는 읽기 전용 프로퍼티.
- **동작**:
  - `Update`에서 매 프레임 `IsOpen`을 재평가해 `blocker`를 열림 여부의 반대로 `SetActive`하고, `hintIcon`은 닫혀 있을 때만(`!open`) 활성화한다. 상태를 캐시하지 않고 매 프레임 재계산하므로, 스킬을 해금하는 즉시(다음 프레임) 문이 자동으로 열린다.

## GazeHazard.cs

- **역할**: 응시 지역의 "시선" 기믹. 원뿔형 시야 안에 플레이어가 있고 시야가 지형에 가려지지 않으면 **경보 단계(눈 확대·적색화) 0.5초 뒤부터** 주기적으로 피해를 준다 (2026-07-26 추가 — 기획서 EMOTION_SYSTEM 2.3절 "경보 이펙트 → 0.5초 후 위협 활성화", 즉사 대신 데미지 방식은 2.6절 권장안).
- **상속/의존**: `MonoBehaviour`. `HiddenWeight.Player`(`PlayerHealth`)에 의존.
- **주요 멤버**:
  - `[SerializeField] float viewRadius = 6f`, `[SerializeField] float viewAngle = 60f` — 시야 반경과 각도.
  - `[SerializeField] float damageInterval = 1f` — 피해 사이 간격.
  - `[SerializeField] float alarmDelay = 0.5f`, `[SerializeField] float alarmScale = 1.4f` (2026-07-26 추가) — 경보 유지 시간과 경보 시 눈 확대 배율.
  - `[SerializeField] LayerMask playerMask` — 인스펙터에서 `Player` 레이어만 지정하고 `PlayerHushed`는 넣지 않는다(아래 참고).
  - `[SerializeField] LayerMask groundMask` — 시야 차단(가림) 판정용 레이어.
  - `bool IsPlayerSeen { get; private set; }` — 현재 프레임에 플레이어가 보이는지.
  - `bool IsAlarmed => _seenTime >= alarmDelay` (2026-07-26 추가) — 연속 감지 시간이 경보 시간을 넘어 피해 단계에 들어갔는지.
- **동작**:
  - `Update`에서 `Physics2D.OverlapCircle(transform.position, viewRadius, playerMask)`로 원 안의 대상을 찾고, `Vector2.Angle(transform.right, toPlayer)`가 `viewAngle * 0.5f` 이내인지 확인한 뒤, `Physics2D.Linecast(transform.position, target.transform.position, groundMask)`로 지형에 가려졌는지 확인해 가려지지 않았을 때만 `IsPlayerSeen = true`로 만든다.
  - 안 보이면 데미지 타이머·`_seenTime`을 리셋하고 시각도 원상 복구. 보이면 `_seenTime`을 누적하며 `UpdateAlarmVisual()`로 경보 진행도에 따라 눈을 `alarmScale`배까지 키우고 붉게 물들인다 — 별도 UI 없이 "들켰다"를 몸으로 알린다. `IsAlarmed` 전에는 피해가 없어 벗어날 기회가 있고, 이후에는 `damageInterval`마다 `target.GetComponent<PlayerHealth>().TakeDamage(1, transform.position)`을 호출한다.
  - **`PlayerHushed` 레이어 처리**: 코드 주석에 명시된 대로, 인스펙터의 `playerMask`에는 `Player` 레이어만 넣고 `PlayerHushed`는 넣지 않는다. 숨죽이기 스킬이 활성화되면 플레이어 게임오브젝트의 레이어 자체가 `PlayerHushed`로 바뀌므로(Emotions의 `HushSkill`), `GazeHazard`의 `OverlapCircle`이 애초에 그 오브젝트를 감지하지 못한다 — World가 Emotions를 코드로 참조하지 않고도 숨죽이기와 시선 기믹이 맞물리는 이유다. `OnDrawGizmosSelected`로 씬 뷰에 시야 반경(빨간 원)과 좌우 시야각 경계선을 그려 배치를 돕는다.

## GazeRotator.cs (2026-07-26 신규)

- **역할**: 회전형 "시선"(기획서 EMOTION_SYSTEM 2.3절 — 고정형/회전형 두 종류로 난이도 조절). `GazeHazard`가 `transform.right`를 시야 방향으로 쓰는 구조를 그대로 이용해, Z축 회전만 시켜 감지 로직 수정 없이 시야각이 통로를 훑게 한다.
- **상속/의존**: `MonoBehaviour`. 다른 모듈 참조 없음(같은 GameObject의 `GazeHazard`와 조합될 뿐 코드로는 서로 모른다).
- **주요 멤버**: `[SerializeField] float degreesPerSecond = 60f`.
- **동작**: `Update`에서 `transform.Rotate(0, 0, degreesPerSecond * Time.deltaTime)`. 응시 Room3(감시자의 회랑)의 눈 3개가 위상차(초기 회전 0/120/240도)를 두고 회전하도록 `ZoneSceneBuilder`가 배치한다.

## HiddenFragment.cs

- **역할**: 자각(L 홀드) 중에만 보이고 만질 수 있는 파편. `StoryFragment`의 특수화.
- **상속/의존**: `StoryFragment` 상속, `IAwarenessReactive` 구현.
- **주요 멤버**:
  - `[SerializeField] SpriteRenderer visual` — 자각 활성화 시 켜지는 시각 표현.
  - `bool _revealed` (private) — 현재 자각 활성 여부를 그대로 반영.
  - `protected override bool IsCollectable => _revealed` — `StoryFragment.Collect()`의 수집 가능 조건을 오버라이드해, 자각이 켜져 있을 때만 수집되게 한다.
  - `void OnAwarenessChanged(bool active)` — `IAwarenessReactive` 구현. `_revealed`를 갱신하고 `visual.enabled`를 그대로 맞춘다.
- **동작**:
  - `OnEnable`/`OnDisable`에서 각각 `AwarenessRegistry.Register(this)`/`Unregister(this)`를 호출해 스스로 등록소에 등록·해제한다. 실제로 `OnAwarenessChanged`를 호출하는 주체(Emotions의 AwarenessSystem)는 이 클래스가 전혀 알지 못한다 — `AwarenessRegistry`를 통한 역방향 훅.

## Interactions.cs

`HiddenWeight.*`를 전혀 참조하지 않는 순수 계약 파일. 여러 인터페이스와 정적 등록소 하나를 담고 있으며, World 자신이 이들을 구현/소비하는 것 외에 Emotions/Player/Enemies가 이 파일만 보고 World의 구체 타입을 몰라도 되게 하는 역할을 한다.

- **`IRewindable`** — 되감기(잔재) 대상 계약. 전체 시간이 아니라 오브젝트 단위로 되돌린다(기획서 4.2절).
  - `Transform Transform { get; }`
  - `bool CanRewind { get; }` — 이미 초기 상태면 `false`.
  - `void CaptureInitial()` — `Start`에서 1회 호출되는 초기 상태 캡처.
  - `void Rewind()` — 초기 상태로 복원.
  - 구현체: `Rewindable`(기본 구현), `CrumblingPlatform`. 소비자: Emotions의 `RewindSkill`.
- **`IAwarenessReactive`** — 자각(L 홀드) 중에만 반응하는 오브젝트 계약.
  - `void OnAwarenessChanged(bool active)` — 유일한 멤버.
  - 구현체: `HiddenFragment`. 소비자: Emotions의 AwarenessSystem(`AwarenessRegistry.Items`를 순회하며 호출).
- **`IForeseeable`** — 예지(균열) 대상 계약. `leadSeconds` 뒤의 상태를 예측해 돌려준다.
  - `Transform Transform { get; }`
  - `Vector3 PredictPosition(float leadSeconds)` — 미래 예상 위치.
  - `bool PredictActive(float leadSeconds)` — `false`면 그때는 사라져 있음.
  - `Sprite CurrentSprite { get; }` — 고스트 표시에 그대로 쓰는 현재 스프라이트.
  - 구현체: `CrumblingPlatform`, `MovingPlatform`. 소비자: Emotions의 `ForesightSkill`.
- **`IDamageable`** — 피해를 받을 수 있는 대상 계약. 기획서 3.1절에 명시된 Player→Enemies 역참조 회피용 계약(Task 9): Enemies의 `Enemy`가 이를 구현하고, Player의 `PlayerAttack`은 이 인터페이스만 참조하며 `Enemy` 구현 타입은 어디서도 참조하지 않는다.
  - `bool IsAlive { get; }`
  - `void TakeDamage(int amount, Vector2 sourcePosition)`
- **`AwarenessRegistry`** (정적 클래스) — 자각 반응 오브젝트의 등록소. World가 Emotions를 참조하지 않고도 Emotions가 World를 관찰할 수 있게 하는 역방향 훅.
  - `static readonly List<IAwarenessReactive> _items` (private) — 내부 저장소.
  - `static IReadOnlyList<IAwarenessReactive> Items => _items` — 읽기 전용 전체 목록. Emotions의 AwarenessSystem이 이를 순회하며 `OnAwarenessChanged`를 호출한다.
  - `static event System.Action<IAwarenessReactive> Added` — 새 항목이 등록될 때 발행.
  - `static void Register(IAwarenessReactive r)` — `null`이 아니고 아직 목록에 없으면 추가한 뒤 `Added` 이벤트를 발행.
  - `static void Unregister(IAwarenessReactive r)` — 목록에서 제거.

## MovingPlatform.cs

- **역할**: 왕복 이동하는 발판. 위치를 시간 기반 순수 함수로 계산해, 예지(균열)가 미래 위치를 정확히 예측할 수 있게 한다.
- **상속/의존**: `MonoBehaviour`, `IForeseeable`. `[RequireComponent(typeof(Rigidbody2D))]`.
- **주요 멤버**:
  - `[SerializeField] Vector2 offset = new Vector2(6, 0)` — 시작점 기준 왕복 끝점.
  - `[SerializeField] float period = 4f` — 왕복 1회 주기.
  - `Transform Transform => transform`, `Sprite CurrentSprite => _sprite != null ? _sprite.sprite : null`.
  - `Vector3 PositionAt(float time)` (private) — `Mathf.PingPong(time / (period * 0.5f), 1f)`를 `Mathf.SmoothStep`으로 보간해 `_origin + offset * t`를 계산하는 순수 함수. 이 함수 하나가 현재 위치 계산과 예지 예측 모두의 기반이 된다.
  - `Vector3 PredictPosition(float lead) => PositionAt(Time.time + lead)` — `PositionAt`에 미래 시각을 넣기만 하면 되므로 예측이 정확하다.
  - `bool PredictActive(float lead) => true` — 이 발판은 사라지지 않으므로 항상 `true`.
- **동작**:
  - `Awake`에서 `Rigidbody2D.bodyType`을 `Kinematic`으로 강제하고 시작 위치를 `_origin`으로 저장한다.
  - `FixedUpdate`에서 `PositionAt(Time.time)`으로 다음 위치를 계산해 `_rb.MovePosition`으로 이동하고, 이동량(`delta`)만큼 위에 타고 있는 `_riderOnTop`도 같이 옮긴다.
  - 발판 위 플레이어 추적은 `transform.SetParent` 대신 이동량을 더하는 방식이다(주석: 부모 변경은 스케일 오염을 일으킨다). `OnCollisionEnter2D`에서 위에서 접촉(`contact.normal.y > 0.5f`)하면 `_riderOnTop`을 설정하고, `OnCollisionExit2D`에서 같은 대상이면 `null`로 해제한다.

## Rewindable.cs

- **역할**: `IRewindable`의 범용 기본 구현. 부서지거나 옮겨진 오브젝트를 원래 자리로 되돌린다(기획서 4.2절). **(2026-07-26 추가)** 되감기는 영구다(EMOTION_SYSTEM 1.2절) — 되돌린 사실을 `ProgressState`에 `persistentId`로 기록하고, 씬이 다시 로드되면 `Start`에서 곧바로 복원 상태로 시작한다.
- **상속/의존**: `MonoBehaviour`, `IRewindable`. `HiddenWeight.Core`(`GameManager.Progress`)에 의존(2026-07-26 추가).
- **주요 멤버**:
  - `[SerializeField] string persistentId` (2026-07-26 추가) — 비워두면 `Start`에서 `"{씬이름}:{이름}:{초기좌표}"`로 자동 생성(같은 이름의 블록 여러 개도 좌표로 구분됨).
  - `Transform Transform => transform`.
  - `bool CanRewind` — `Vector3.SqrMagnitude(transform.position - _initialPosition) > 0.0001f`, 즉 초기 위치와 위치 차이가 있을 때만 `true`(위치 변화만 비교하며 회전/활성 상태는 비교하지 않음).
  - `void CaptureInitial()` — 현재 위치/회전/활성 상태/스프라이트(있으면)를 저장.
  - `void Rewind()` — 저장된 값으로 위치/회전/활성 상태/스프라이트를 복원하고 `Freeze()` + `Progress.MarkRewound(persistentId)` 기록 후 `BounceRoutine`을 재생.
- **동작**:
  - `Start`에서 `SpriteRenderer`/`Rigidbody2D`를 캐시하고 `CaptureInitial()`을 1회 호출한 뒤, `Progress.IsRewound(persistentId)`면 `Freeze()`로 복원 상태 그대로 시작한다(2026-07-26 추가).
  - `Freeze()` (2026-07-26 추가): `Rigidbody2D`가 있으면 속도 0 + `bodyType = Static`으로 고정 — 복원된 다리 블록이 중력으로 **다시 무너지면 "영구"가 아니게 되는** 기존 잠재 버그도 함께 잡는다.
  - `Rewind()`가 값 복원 후 진행 중이던 바운스 코루틴이 있으면 멈추고 `BounceRoutine`을 새로 시작한다. `BounceRoutine`은 0.3초에 걸쳐 `localScale`을 0.8에서 1.0으로 `Mathf.Lerp`로 튕겨 보이는 되감기 연출이다.
  - `CrumblingPlatform`에는 이 영구 규칙을 적용하지 않는다 — 밟으면 다시 무너지는 반복 기믹이 의도이기 때문.

## RewindHighlight.cs (2026-07-26 신규)

- **역할**: 되감기 가능한 오브젝트의 골드빛 아웃라인(기획서 EMOTION_SYSTEM 1.3절 — 자각 없이도 식별 가능해야 진행이 막히지 않는다). 같은 GameObject의 `IRewindable`을 관찰만 하고, 되감을 것이 있을 때(`CanRewind`)만 표시한다.
- **상속/의존**: `MonoBehaviour`. World 내부 계약(`IRewindable`)만 사용. `PrefabBuilder`가 `RewindableBlock`/`CrumblingPlatform` 프리팹에 부착한다.
- **주요 멤버**: `[SerializeField] Color outlineColor`(옅은 골드~앰버 `(1, 0.82, 0.35)`), `outlineScale = 1.2f`, `pulseSpeed = 2.5f`.
- **동작**: `Start`에서 자식 `RewindOutline` GameObject를 만들어 본체보다 1.2배 큰 `SpriteRenderer`를 `sortingOrder - 1`(본체 바로 뒤)로 깐다. `Update`에서 `CanRewind`일 때만 켜고, 본체 스프라이트를 복사하되 `enabled`는 따라가지 않는다 — 무너져 스프라이트가 꺼진 발판에서도 아웃라인은 자리를 표시해야 하기 때문. 알파를 0.35~0.8로 `PingPong` 맥동시켜 눈에 띄게 한다.

## Room.cs

- **역할**: 룸 단위 카메라 전환용 사각 경계. 레벨 배치 시 씬에 배치하고 `size`로 룸 크기를 지정한다.
- **상속/의존**: `MonoBehaviour`. `[RequireComponent(typeof(BoxCollider2D))]`. `RoomCamera.Instance`를 직접 호출.
- **주요 멤버**:
  - `[SerializeField] Vector2 size = new Vector2(24, 14)` — 룸 크기.
  - `Bounds WorldBounds => new Bounds(transform.position, new Vector3(size.x, size.y, 1f))` — 월드 좌표 기준 경계.
  - `bool Contains(Vector3 point)` — 점이 `WorldBounds` 안에 있는지(x/y 각각 min/max 비교).
- **동작**:
  - `OnValidate`에서 `BoxCollider2D`를 `isTrigger = true`로 강제하고 `size`/`offset`을 필드 값에 자동 동기화한다.
  - `OnTriggerEnter2D`에서 `"Player"` 레이어가 아니면 무시하고, 맞으면 `RoomCamera.Instance.SetRoom(this)`를 호출해 카메라 경계를 이 룸으로 전환한다.
  - `OnDrawGizmos`로 씬 뷰에 노란색 와이어큐브를 그려 룸 경계를 시각화한다.

## RoomCamera.cs

- **역할**: 룸 단위로 플레이어를 따라가는 카메라. 목표 위치를 현재 룸 경계 안으로 클램프한다.
- **상속/의존**: `MonoBehaviour`. `[RequireComponent(typeof(Camera))]`. `HiddenWeight.Player`(`PlayerController.Instance`)에 의존.
- **주요 멤버**:
  - `[SerializeField] float followLerp = 8f` — 카메라 추적 부드러움 계수(밸런스 수치가 아니라 연출용 값이라 인스펙터 필드로 둠).
  - `static RoomCamera Instance { get; private set; }` — 전역 접근점.
  - `Room CurrentRoom { get; private set; }` — 현재 클램프 기준이 되는 룸.
  - `Vector2 ComputeClampedTarget(Vector2 target)` (private) — `CurrentRoom`이 없으면 원래 목표(플레이어 위치)를 그대로 돌려주고, 있으면 카메라의 절반 크기(`orthographicSize`, `aspect` 기준)와 룸 경계를 비교해 룸이 화면보다 작으면 룸 중심에 고정하고, 크면 `Mathf.Clamp`로 경계 안에 묶는다.
  - `void SetRoom(Room room)` — `CurrentRoom`만 바꾼다. 급격한 전환은 `LateUpdate`의 `Lerp`가 자연히 흡수한다.
  - `void SnapToPlayer()` — `Lerp` 없이 즉시 플레이어 위치(클램프 적용)로 이동. 씬 진입·리스폰용으로 별도 호출이 필요하다(자동 호출되지 않음).
- **동작**:
  - `Awake`에서 `Instance = this`, `Camera` 컴포넌트를 캐시.
  - `LateUpdate`에서 `PlayerController.Instance`가 없으면(씬에 플레이어가 아직 없을 수 있음) 아무 것도 하지 않고, 있으면 `ComputeClampedTarget`으로 계산한 목표를 향해 `Vector3.Lerp(..., followLerp * Time.deltaTime)`로 부드럽게 이동한다.

## StoryFragment.cs

- **역할**: 지역 곳곳의 이야기 파편. 스킬 해금 지점, 자각 해금 지점으로도 쓰인다.
- **상속/의존**: `MonoBehaviour`. `[RequireComponent(typeof(Collider2D))]`. `HiddenWeight.Core`(`GameManager`), `HiddenWeight.Data`(`EmotionId`), 그리고 **`HiddenWeight.Emotions`(`EmotionSkillController`)** — World가 Emotions를 참조하는 유일한 파일.
- **주요 멤버**:
  - `[SerializeField] string fragmentId` — 지역별 고유 문자열(`residue_01` 등).
  - `[SerializeField, TextArea(2, 4)] string text` — 화면에 뜰 한 줄.
  - `[SerializeField] EmotionId grantsSkill = EmotionId.None` — 스킬 획득 지점으로도 쓰임.
  - `[SerializeField] bool grantsAwareness` — 응시 지역의 자각 해금 지점.
  - `string FragmentId => fragmentId`.
  - `protected virtual bool IsCollectable => true` — 기본은 항상 수집 가능. `HiddenFragment`가 자각 조건으로 오버라이드.
  - `void Collect()` — `virtual`. 수집 로직 전체를 담당.
- **동작**:
  - `OnTriggerEnter2D`에서 `"Player"` 레이어일 때만 `Collect()`를 호출.
  - `Collect()`는 `IsCollectable`이 `false`면 즉시 리턴하고, `GameManager.Instance.Progress.CollectFragment(fragmentId)`가 `false`(이미 먹은 것)여도 리턴한다. 통과하면 `grantsSkill != EmotionId.None`일 때 `p.UnlockSkill(grantsSkill)`, `grantsAwareness`면 `p.GrantAwareness()`를 호출하고, `GameManager.FragmentPresenter?.Invoke(text)`로 UI에 텍스트를 띄운 뒤(Core의 정적 훅, World가 UI를 직접 참조하지 않기 위함) `gameObject.SetActive(false)`로 자신을 비활성화한다.
  - **Emotions 예외**: 마지막 줄 `EmotionSkillController.Instance?.RefreshActive()`가 스킬 지급 직후 현재 활성 스킬 컨트롤러를 재평가하도록 호출한다. 이는 기획서 3.1절에 명시된 계획적 단일 예외로, "새로 해금한 스킬이 현재 지역에서 즉시 활성화되어야 한다"는 요구를 만족시키기 위해 의도적으로 허용되었다. World의 다른 어떤 파일도 `HiddenWeight.Emotions`를 참조하지 않는다.

## ZoneTrigger.cs

- **역할**: 지역 클리어 지점. 플레이어가 닿으면 다음 씬으로 넘어간다.
- **상속/의존**: `MonoBehaviour`. `[RequireComponent(typeof(Collider2D))]`. `HiddenWeight.Core`(`GameManager`, `SceneFlow`), `HiddenWeight.Data`(`ZoneId`)에 의존.
- **주요 멤버**:
  - `[SerializeField] bool marksFractureCleared` — 균열 지역 출구에서만 `true`로 설정하는 플래그.
- **동작**:
  - `OnTriggerEnter2D`에서 `"Player"` 레이어가 아니면 무시.
  - `marksFractureCleared`가 `true`면 `gm.Progress.MarkFractureCleared()`를 먼저 호출한다.
  - 기본 목적지는 `gm.CurrentZoneData?.nextSceneName`(없으면 `SceneFlow.Title`)이다.
  - 백트래킹 규칙: `gm.Progress.CurrentZone == ZoneId.Residue && gm.Progress.HasClearedFracture`(균열을 클리어한 뒤 잔재로 되돌아온 경우)면 목적지를 `SceneFlow.Ending`으로 덮어쓴다.
  - 최종적으로 `SceneFlow.LoadWithFade(next)`를 호출해 페이드 전환한다(UI의 `ScreenFader`가 `SceneFlow.FadeLoader`를 등록해뒀으면 페이드가 걸리고, 없으면 즉시 전환으로 폴백).
