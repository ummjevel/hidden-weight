# Emotions 모듈

`HiddenWeight.Emotions`는 `K` 단일 키에 물리는 세 감정 스킬(되감기·숨죽이기·예지)과 `L` 홀드 자각을 구현한다. `HiddenWeight.Core`(`GameManager`), `HiddenWeight.Data`(`EmotionData`/`EmotionId`/`SkillInput`), `HiddenWeight.Player`(`PlayerController`/`PlayerInput`/`PlayerAttack`)에 의존하며, World와는 `IRewindable`/`IForeseeable`/`IAwarenessReactive`/`AwarenessRegistry`(모두 `World/Interactions.cs` 선언) 인터페이스로만 연결된다 — World의 구체 클래스(예: `HiddenFragment`, `Enemy`)는 어디서도 참조하지 않는다.

## AwarenessSystem.cs
- **역할**: `L` 홀드 자각. 채도를 잃는 URP `Volume` 연출, 이동 감속, 씬 안 `IAwarenessReactive` 대상에 대한 On/Off 방송을 담당하는 단일 `MonoBehaviour`. 균열 지역(`awarenessStable == false`)에서는 자각이 켜져 있는 동안 대상을 주기적으로 깜빡인다.
- **상속/의존**: `MonoBehaviour`. `UnityEngine.Rendering.Volume`, `HiddenWeight.Core.GameManager`(상태·`Balance`·`CurrentZoneData` 조회), `HiddenWeight.Player.PlayerController`(`ExternalSpeedMultiplier`), `HiddenWeight.World.AwarenessRegistry`/`IAwarenessReactive`.
- **주요 멤버**:
  - `static AwarenessSystem Instance { get; private set; }`
  - `bool IsActive { get; private set; }` — 자각 On/Off 상태.
  - `bool IsStable => GameManager.Instance.CurrentZoneData?.awarenessStable ?? true` — 현재 지역이 균열(불안정)인지.
  - `event System.Action<bool> AwarenessChanged` — UI 등 외부 구독용(내부 `Broadcast`에서도 발행).
  - `[SerializeField] float slowMultiplier = 0.6f` — 자각 중 이동속도 배율.
  - `[SerializeField] float volumeRampTime = 0.25f` — Volume `weight`가 0→1(또는 1→0)로 도달하는 시간.
  - `Volume _volume` — `Awake`에서 `gameObject.AddComponent<Volume>()`로 직접 생성, `profile = GameManager.Instance.Balance.awarenessProfile`, `priority = 10f`, 초기 `weight = 0f`.
- **동작**:
  - `Awake`: 싱글턴 등록 + Volume 컴포넌트 생성/설정.
  - `OnEnable`/`OnDisable`에서 `AwarenessRegistry.Added`를 구독/해제한다. `HandleAdded(IAwarenessReactive r)`는 자각이 이미 켜진 상태에서 새로 등록된 오브젝트(씬 로드 순서상 `HiddenFragment.OnEnable`이 이 시점 이후에 도는 경우 대비)에게 즉시 `OnAwarenessChanged(true)`를 호출해 동기화한다.
  - `Update`: `GameManager.State != Playing`이면 켜져 있던 자각을 강제로 끄고 리턴(단, `PlayerInput.AwarenessHeld`는 `Enabled`와 무관하게 항상 값을 반환하므로, Ending 시퀀스에서의 오동작을 막기 위해 여기서 직접 막는다). 그 외에는 `wanted = Progress.HasAwareness && PlayerInput.AwarenessHeld`를 매 프레임 계산해 `IsActive`와 다르면 `Activate()`/`Deactivate()`를 호출한다. 자각을 유지한 채 안정 지역에서 균열 지역으로 넘어간 경우를 대비해, `IsActive && !IsStable && _flickerRoutine == null`이면 `UnstableFlicker` 코루틴을 새로 시작한다(반대 방향인 균열→안정 전환은 `UnstableFlicker`의 `while` 조건이 스스로 종료해 처리).
  - `Activate()`: `PlayerController.Instance.ExternalSpeedMultiplier = slowMultiplier`(0.6), `StartWeightRamp(1f)`로 Volume weight를 `volumeRampTime`(0.25초)에 걸쳐 1까지 램프, `Broadcast(true)`로 등록된 모든 `IAwarenessReactive`에 On 전달, 불안정 지역이면 `UnstableFlicker` 코루틴 시작.
  - `Deactivate()`: 속도 배율을 1로 복원, weight를 0으로 램프, `Broadcast(false)`, 진행 중이던 flicker 코루틴을 중단.
  - `Broadcast(bool active)`: `AwarenessRegistry.Items`(등록소 전체 목록)를 순회하며 각 항목의 `OnAwarenessChanged(active)`를 호출한 뒤 `AwarenessChanged` 이벤트를 발행한다. `FindObjectsOfType` 등 씬 전수 탐색을 쓰지 않고, World가 소유한 `AwarenessRegistry`를 그대로 읽는 방식으로 구현되어 있다.
  - `RampWeight(float target)`: `volumeRampTime` 동안 `Time.deltaTime` 누적으로 `Mathf.Lerp(start, target, t/volumeRampTime)`을 매 프레임 적용, 종료 시 정확히 `target`으로 스냅.
  - `UnstableFlicker()`: `IsActive && !IsStable`인 동안 반복. 매 반복마다 `Random.Range(0.3f, 0.8f)`초 대기 후, `AwarenessRegistry.Items`가 비어 있지 않으면 `n = Mathf.Max(1, items.Count / 2)`개(전체의 절반, 최소 1개)를 무작위로 골라(중복 뽑기 가능) `OnAwarenessChanged(false)`로 끄고, `0.15f`초 뒤 전체 항목에 `OnAwarenessChanged(true)`를 다시 걸어 복구한다. 코루틴 종료 시 `_flickerRoutine = null`.
  - 색보정(`ColorAdjustments` 채도 -80, `Vignette`)은 이 스크립트가 아니라 `Editor/ProjectSetup.cs`가 에디터 스크립트로 `awarenessProfile`에 미리 구워 넣는다 — `AwarenessSystem`은 완성된 `VolumeProfile` 에셋의 `weight`만 제어한다.
  - 기획서 5.3절의 램프 시간(0.25초), 속도 배율(0.6배), 채도(-80), 균열 지역 깜빡임 간격(0.3~0.8초)은 코드와 정확히 일치한다. 다만 "절반을 무작위로 끈 뒤 0.15초 후 전부 복구"라는 구체적 드롭 비율·복구 지연은 기획서에 명시된 수치가 아니라 구현에서 정한 값이다.

## EmotionSkill.cs
- **역할**: 되감기/숨죽이기/예지 세 스킬의 공통 베이스 추상 클래스. 쿨타임 진행과 이동속도 배율 적용/복원을 한 곳에서 처리하고, 각 하위 스킬은 `OnBegin`/`OnTick`/`OnEnd`만 구현한다.
- **상속/의존**: `MonoBehaviour`를 상속하는 `abstract class`. `HiddenWeight.Data.EmotionData`, `HiddenWeight.Player.PlayerController`에 의존.
- **주요 멤버**:
  - `abstract EmotionId Id { get; }` — 하위 클래스가 자신의 감정 종류를 밝힌다.
  - `EmotionData Data { get; set; }` — `EmotionSkillController.Awake`가 밸런스 데이터에서 채워준다.
  - `bool IsActive { get; protected set; }`
  - `float CooldownRemaining { get; protected set; }`
  - `virtual bool CanUse => CooldownRemaining <= 0f && !IsActive` — 하위 클래스가 필요하면 오버라이드 가능.
  - `protected PlayerController Player { get; private set; }` — `Awake`에서 `PlayerController.Instance`로 캐시.
  - `protected bool SkipCooldown` — `OnEnd`에서 `true`로 설정하면 이번 `End()` 호출에서는 쿨타임을 걸지 않는다(대상 없이 취소된 되감기처럼, 실패 케이스에 쿨타임을 물리지 않기 위한 훅). `RewindSkill.OnEnd`가 "채널링 0초 + 대상 없음" 조건일 때만 이를 `true`로 설정해 사용한다.
  - `void Begin()` / `void Tick(float dt)` / `void End()` — `EmotionSkillController`가 호출하는 공개 진입점.
  - `abstract void OnBegin()` / `abstract void OnTick(float dt)` / `abstract void OnEnd()` — 하위 클래스 구현 지점.
- **동작**:
  - `Update`: `CooldownRemaining > 0f`이면 `Time.deltaTime`만큼 계속 차감(스킬이 켜져 있지 않아도 항상 진행).
  - `Begin()`: `CanUse`가 아니면 무시. 통과하면 `IsActive = true` → `ApplySpeedMultiplier()`(속도 배율 적용이 `OnBegin()`보다 먼저 실행됨에 유의 — `RewindSkill`처럼 `OnBegin` 내부에서 대상이 없어 즉시 `End()`를 호출하는 경우에도, `End()`의 복구 로직이 그대로 되돌려주므로 안전) → `OnBegin()`.
  - `End()`: `IsActive`가 아니면 무시. `SkipCooldown = false`로 리셋 → `OnEnd()` 실행(이 안에서 `SkipCooldown`을 다시 `true`로 설정할 수 있음) → `RestoreSpeedMultiplier()` → `IsActive = false` → `SkipCooldown`이 여전히 `false`일 때만 `CooldownRemaining = Data.cooldown` 적용 → 마지막으로 `SkipCooldown = false`로 다시 리셋(다음 사용을 위한 정리).
  - `ApplySpeedMultiplier()`: `Player.ExternalSpeedMultiplier = Data.moveSpeedMultiplier`로 설정하고, `Data.moveSpeedMultiplier == 0f`(되감기)일 때만 추가로 `Player.MovementLocked = true`를 켠다.
  - `RestoreSpeedMultiplier()`: 속도 배율을 1로 되돌리고, `moveSpeedMultiplier == 0f`였을 때만 `MovementLocked = false`로 해제.

## EmotionSkillController.cs
- **역할**: 플레이어에 붙는 싱글턴 디스패처. 세 `EmotionSkill` 컴포넌트를 모두 들고 있다가 현재 지역이 부여하고 플레이어가 보유한 스킬 하나를 활성 스킬로 지정하고, `K` 키 입력(Hold/Tap)을 활성 스킬로 라우팅한다.
- **상속/의존**: `MonoBehaviour`. `HiddenWeight.Core.GameManager`, `HiddenWeight.Data`(`EmotionId`, `SkillInput`), `HiddenWeight.Player.PlayerInput`. `GetComponents<EmotionSkill>()`으로 같은 오브젝트에 붙은 세 스킬 컴포넌트를 수집한다.
- **주요 멤버**:
  - `static EmotionSkillController Instance { get; private set; }` — World의 `StoryFragment.Collect()`가 `EmotionSkillController.Instance?.RefreshActive()`로 호출하는 유일한 역참조 지점.
  - `EmotionSkill Active => _active` — 현재 활성 스킬 인스턴스(없으면 `null`).
  - `EmotionId CurrentEmotion { get; private set; } = EmotionId.None`
  - `event System.Action<EmotionId> EmotionChanged` — 활성 스킬이 바뀔 때 발행(UI 등에서 구독).
  - `void RefreshActive()` — 인자 없음. 현재 지역(`GameManager.Instance.CurrentZoneData`)의 `grantedSkill`을 확인해, 지역이 없거나 `grantedSkill == EmotionId.None`이거나 아직 `Progress.HasSkill(wanted)`가 아니면 활성 스킬을 `null`로 비운다. 조건을 만족하면 `_skills.Find(s => s.Id == wanted)`로 찾은 컴포넌트를 활성으로 지정한다.
- **동작**:
  - `Awake`: 싱글턴 등록 → `GetComponents<EmotionSkill>()`로 `_skills` 구성 → `GameManager.Instance.Balance`에서 각 스킬의 `Data`(`EmotionData`)를 `GetEmotion(s.Id)`로 채워준다.
  - `Update`: `Active == null`이면 매 프레임 `RefreshActive()`를 호출해 지역/보유 상태 변화(스킬을 새로 얻는 순간 등)를 폴링 방식으로 감지하고 즉시 리턴. `Active`가 있으면 `Data.inputMode`에 따라 분기 — `Hold`는 `PlayerInput.SkillHeld`가 눌린 순간 `Begin()`, 떼는 순간 `End()`; `Tap`은 `PlayerInput.SkillPressed`(눌린 프레임 1회)에서만 `Begin()`을 호출하고 별도 `End()` 트리거가 없다(스킬 스스로 `OnTick`에서 타이머를 재고 `End()`를 호출). `Active.IsActive`이면 매 프레임 `Active.Tick(Time.deltaTime)` 호출.
  - `RefreshActive()`: `SetActive(...)`로 위임.
  - `SetActive(EmotionSkill skill)`: 이미 같은 스킬이면 무시. 이전 활성 스킬이 채널링/유지 중이면 먼저 `End()`로 정리한 뒤 교체하고, `CurrentEmotion`을 갱신하며 `EmotionChanged` 이벤트를 발행한다. `RefreshActive()`가 최초 `Update`에서도 매 프레임 호출될 수 있으므로, 이 no-op 가드가 없으면 매 프레임 불필요하게 스킬을 리셋하게 된다.

## ForesightSkill.cs
- **역할**: 예지. 탭 입력 1회로 주변 `IForeseeable` 대상들의 미래 상태를 반투명 고스트로 잠시 보여준다.
- **상속/의존**: `EmotionSkill` 상속. `HiddenWeight.World.IForeseeable`, `Physics2D.OverlapCircleAll`.
- **주요 멤버**:
  - `override EmotionId Id => EmotionId.Foresight`
  - `readonly List<GameObject> _ghosts` — 생성된 고스트 오브젝트 추적(정리용).
  - `float _timer` — 표시 지속시간 카운트다운.
- **동작**:
  - `OnBegin()`: `_timer = Data.effectDuration`(예지 1.5초). `Physics2D.OverlapCircleAll(Player.transform.position, Data.range)`(예지 8유닛)로 반경 내 콜라이더를 모두 검사, 각각에서 `GetComponentInParent<IForeseeable>()`을 찾아 대상이면 `f.PredictActive(Data.previewLeadTime)`(예지 2.0초 뒤)을 확인한다. 미래에 비활성 상태(`false`)면 고스트를 만들지 않는다 — "무너질 발판이 그때는 사라져 있다"는 사실을 "그 자리에 아무것도 안 보인다"로 표현하는 의도적 설계.
  - `SpawnGhost(IForeseeable f)`: 새 `GameObject("ForesightGhost")`를 만들어 `f.PredictPosition(Data.previewLeadTime)` 위치·`f.Transform.localScale` 크기로 배치하고, `SpriteRenderer`를 추가해 `sprite = f.CurrentSprite`, `color = (1,1,1,0.35f)`(반투명), `sortingOrder = 50`으로 그린 뒤 `_ghosts`에 추가.
  - `OnTick(dt)`: `_timer`를 차감, 0 이하가 되면 `End()` 호출(스킬 스스로 종료 — `EmotionSkillController`는 Tap 입력에 대해 `End()`를 트리거하지 않으므로 반드시 자체 종료해야 함).
  - `OnEnd()`: `_ghosts`에 남은 오브젝트를 모두 `Destroy`하고 리스트를 비운다.
  - 쿨타임(3초)은 `EmotionSkill.End()`가 `Data.cooldown`으로 공통 처리하므로 `ForesightSkill`에는 별도 쿨타임 로직이 없다.

## HushSkill.cs
- **역할**: 숨죽이기. 홀드 중 플레이어를 축소·감속시키고 레이어를 바꿔 시선 기반 위협(`GazeHazard` 등)의 인식에서 벗어나며, 공격을 비활성화한다.
- **상속/의존**: `EmotionSkill` 상속. `HiddenWeight.Player.PlayerAttack`(`CanAttack`), `UnityEngine.LayerMask`.
- **주요 멤버**:
  - `override EmotionId Id => EmotionId.Hush`
  - `int _originalLayer` / `Vector3 _originalScale` — 종료 시 복원을 위한 원본 상태 저장.
- **동작**:
  - `OnBegin()`: 현재 레이어·스케일을 저장한 뒤, `Player.gameObject.layer = LayerMask.NameToLayer("PlayerHushed")`로 전환(이 레이어를 `GazeHazard`류 시선 판정이 무시하도록 별도로 구성돼 있을 것으로 전제), `Player.transform.localScale = _originalScale * Data.hushScale`(숨죽이기 스케일 배수 0.6)로 축소. `Player.GetComponent<PlayerAttack>()`을 찾아 있으면 `atk.CanAttack = false`로 직접 꺼서 공격을 막는다 — `PlayerAttack` 스스로 스킬 상태를 조회하는 방식이 아니라, `HushSkill`이 `OnBegin`/`OnEnd`에서 능동적으로 켜고 끄는 방식이다.
  - `OnTick(dt)`: 아무 것도 하지 않음(빈 구현) — 홀드가 유지되는 동안 상태 변화 없이 계속 켜져 있기만 하면 된다.
  - `OnEnd()`: 저장해둔 레이어·스케일을 복원하고, `PlayerAttack`이 있으면 `CanAttack = true`로 되돌린다.
  - 이동속도 배율(0.45)은 `HushSkill`이 아니라 `EmotionSkill` 베이스의 `ApplySpeedMultiplier()`/`RestoreSpeedMultiplier()`가 `Data.moveSpeedMultiplier`를 통해 공통 처리한다. 쿨타임은 `EmotionData.cooldown`이 0으로 설정돼 있어(주석 근거) 사실상 즉시 재사용 가능.
  - 축소 상태에서의 콜라이더 크기 조정은 별도 코드가 없다 — 주석에 따르면 `localScale` 변경이 `CapsuleCollider2D`에 자동 반영되므로 충분하다고 판단.

## RewindSkill.cs
- **역할**: 되감기. 홀드하는 동안 조준선(플레이어 주변) 안 가장 가까운 `IRewindable` 대상 하나를 채널링해, 다 채우면 해당 오브젝트를 자기 자신의 초기 상태로 복원시킨다.
- **상속/의존**: `EmotionSkill` 상속. `HiddenWeight.World.IRewindable`, `Physics2D.OverlapCircleAll` + `[SerializeField] LayerMask interactableMask`.
- **주요 멤버**:
  - `override EmotionId Id => EmotionId.Rewind`
  - `IRewindable _target` — 채널링 대상.
  - `float _channel` — 채널링 경과 시간.
  - `float ChannelProgress => Data.channelTime <= 0f ? 1f : _channel / Data.channelTime` — 0~1 진행률(UI 등에서 조회 가능하도록 공개).
- **동작**:
  - `OnBegin()`: `FindNearestTarget()`으로 대상을 찾고 `_channel = 0f`로 리셋. 대상이 없으면 그 자리에서 바로 `End()`를 호출해 취소한다(홀드했지만 대상이 없어 즉시 끝나는 경우).
  - `FindNearestTarget()`: `Physics2D.OverlapCircleAll(Player.transform.position, Data.range, interactableMask)`(되감기 6유닛, `interactableMask`로 필터링)로 검색된 콜라이더마다 `GetComponentInParent<IRewindable>()`을 찾고, `CanRewind`가 `true`인 것들 중 플레이어와의 `sqrMagnitude`가 가장 작은(가장 가까운) 대상 하나를 반환.
  - `OnTick(dt)`: `_target`이 없거나 `_target.CanRewind`가 `false`가 된 경우(채널링 도중 대상이 이미 되감겨졌거나 상태가 바뀐 경우) 즉시 `End()`. 그 외엔 `_channel += dt`, `_channel >= Data.channelTime`(되감기 1.0초)에 도달하면 `_target.Rewind()`를 호출해 복원시키고 `End()`.
  - `OnEnd()`: `_channel == 0f && _target == null`(대상 없이 즉시 취소된 경우)일 때만 `SkipCooldown = true`로 설정해 쿨타임을 물리지 않는다. 그 외의 모든 종료(채널링 성공, 중도 취소 등)는 `EmotionSkill.End()`가 `Data.cooldown`(2초)을 정상 적용한다. 이후 `_target = null`, `_channel = 0f`로 정리.
  - 이동 불가(`MovementLocked = true`)는 `RewindSkill`이 아니라 `EmotionSkill` 베이스가 `Data.moveSpeedMultiplier == 0f`(되감기 값)를 보고 자동으로 켜고 끈다.
