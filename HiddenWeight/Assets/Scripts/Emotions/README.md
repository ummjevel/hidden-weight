# Emotions 모듈 — 감정 스킬(K)과 자각(L)을 담당하는 플레이어 능력 레이어

> 기획서 5.2(감정 스킬), 5.3(자각) 대응.
> 지역별로 자동 전환되는 세 감정 스킬(되감기·숨죽이기·예지)과, 별도 스킬이 아닌 화면 효과 + 인터페이스 브로드캐스트로 구현된 자각(L 홀드)을 담는다. World의 구체 클래스를 몰라도 동작하도록 `World/Interactions.cs`의 인터페이스에만 의존한다.

## 파일

| 파일 | 역할 | 기획서 대응 |
|---|---|---|
| `EmotionSkill.cs` | 세 스킬의 공통 추상 베이스. 쿨타임 진행, 이동속도 배율 적용/복원을 공통 처리하고 `CanUse`/`OnBegin`/`OnTick`/`OnEnd`를 하위 클래스에 위임 | 5.2 |
| `EmotionSkillController.cs` | `K` 키 단일 디스패처. 현재 지역이 부여하고 보유한 스킬 하나를 활성화하고 Hold/Tap 입력을 라우팅. `Instance`/`RefreshActive()` 노출 | 5.2, 6.2 |
| `RewindSkill.cs` | 되감기(잔재). 홀드 채널링 1.0초, 조준 반경 6유닛 내 가장 가까운 `IRewindable` 복원, 쿨타임 2초, 채널링 중 이동 불가 | 5.2, 4.2 |
| `HushSkill.cs` | 숨죽이기(응시). 홀드 중 속도 0.45배 + 스케일 0.6배 + `PlayerHushed` 레이어 전환 + 공격 비활성 | 5.2 |
| `ForesightSkill.cs` | 예지(균열). 탭 1회, 반경 8유닛 내 `IForeseeable`의 2.0초 뒤 상태를 반투명 고스트로 1.5초 표시, 쿨타임 3초 | 5.2 |
| `AwarenessSystem.cs` | 자각(L 홀드). URP Volume weight 0.25초 램프(채도 -80 + Vignette), 이동속도 0.6배, `AwarenessRegistry`를 통한 `IAwarenessReactive` 방송, 균열 지역 깜빡임 | 5.3 |

## 핵심 규칙 구현

- **되감기(RewindSkill)**: `Data.channelTime`(1.0초) 동안 채널링해야 `IRewindable.Rewind()`가 호출된다. 채널링 중 `_target.CanRewind`가 거짓이 되면 즉시 취소. **대상 없이 시작한 경우에만** `SkipCooldown = true`로 쿨타임 없이 취소되고, 그 외 모든 종료(성공 포함)는 `Data.cooldown`(2초)이 정상 적용된다. 이동 불가는 `RewindSkill` 자체 코드가 아니라 `EmotionSkill` 베이스가 `Data.moveSpeedMultiplier == 0f`를 보고 `PlayerController.MovementLocked`를 자동으로 켜고 끄는 것으로 구현된다.
- **숨죽이기(HushSkill)**: `PlayerAttack.CanAttack`은 스스로 스킬 상태를 조회하지 않는다 — `HushSkill.OnBegin`이 `GetComponent<PlayerAttack>().CanAttack = false`로 직접 끄고, `OnEnd`가 `true`로 복원하는 능동적 제어다. 레이어를 `PlayerHushed`로 바꿔 시선 판정(`GazeHazard` 등)에서 벗어나며, `localScale *= hushScale`(0.6배)만 적용하고 콜라이더 크기 조정 코드는 별도로 없다(스케일 변경이 `CapsuleCollider2D`에 자동 반영된다고 전제).
- **예지(ForesightSkill)**: Tap 입력이라 `EmotionSkillController`가 `End()`를 걸어주지 않으므로, `OnTick`에서 `_timer`(effectDuration=1.5초)가 0 이하가 되면 스킬이 **스스로** `End()`를 호출해 종료한다. `IForeseeable.PredictActive(previewLeadTime)`이 `false`(미래에 사라짐)면 고스트를 아예 생성하지 않는 방식으로 "사라질 예정"을 표현한다.
- **자각(AwarenessSystem)**: `AwarenessRegistry.Items`(World 소유 등록소)를 순회해 방송하며, `FindObjectsOfType` 등 씬 전수 탐색을 쓰지 않는다. `AwarenessRegistry.Added` 이벤트를 구독해, 자각이 켜진 도중 새로 등록되는 오브젝트도 즉시 동기화한다. 균열 지역(`ZoneData.awarenessStable == false`)에서는 `UnstableFlicker` 코루틴이 0.3~0.8초 간격으로 반복해 등록 항목의 **절반(`Random.Range` 무작위 선택, 최소 1개)**을 `OnAwarenessChanged(false)`로 껐다가 0.15초 뒤 전체를 다시 켠다 — 이 절반 비율과 0.15초 복구 지연은 기획서에 없는, 구현 단계에서 정한 구체 수치다. 그 외 수치(램프 0.25초, 속도 0.6배, 채도 -80, 깜빡임 간격 0.3~0.8초)는 기획서 5.3절과 정확히 일치한다.

## 씬 배치

- `EmotionSkillController`와 세 `EmotionSkill` 구현체(`RewindSkill`/`HushSkill`/`ForesightSkill`)는 **같은 게임오브젝트**(플레이어 프리팹)에 함께 붙는다 — `EmotionSkillController.Awake`가 `GetComponents<EmotionSkill>()`로 형제 컴포넌트를 수집하기 때문에 반드시 한 오브젝트에 세 스킬 컴포넌트가 모두 있어야 한다.
- `AwarenessSystem`은 플레이어 프리팹 또는 별도 매니저 오브젝트에 배치 가능(코드상 `PlayerController.Instance`만 참조하며 자신의 부모/자식 관계에 의존하지 않음). `Awake`에서 자기 자신에게 `Volume` 컴포넌트를 `AddComponent`로 직접 붙이므로, 인스펙터에 미리 `Volume` 컴포넌트를 추가해둘 필요는 없다.
- `GameManager.Instance.Balance.awarenessProfile`(`VolumeProfile` 에셋)이 반드시 할당돼 있어야 하며, 그 프로파일에는 `ColorAdjustments`(채도 -80)와 `Vignette`가 포함돼야 한다 — 이 두 오버라이드는 `Editor/ProjectSetup.cs`가 에디터 스크립트로 미리 구워 넣고, `AwarenessSystem`은 완성된 프로파일의 `weight`만 제어한다.
- `RewindSkill`의 `[SerializeField] LayerMask interactableMask`는 인스펙터에서 되감기 대상이 속한 레이어로 반드시 설정해야 한다(비워두면 `OverlapCircleAll`이 아무 것도 검출하지 못한다).

## 다른 모듈과의 연결

- **Player**: `PlayerController.ExternalSpeedMultiplier`(모든 스킬 공통 감속 + 자각 감속 0.6배), `PlayerController.MovementLocked`(되감기 채널링 중), `PlayerAttack.CanAttack`(숨죽이기 중 비활성)을 직접 읽고 쓴다. `PlayerInput.SkillHeld`/`SkillPressed`(K)와 `PlayerInput.AwarenessHeld`(L)를 입력으로 읽는다.
- **World**: 구체 클래스를 참조하지 않고 `World/Interactions.cs`의 `IRewindable`/`IForeseeable`/`IAwarenessReactive` 인터페이스와 `AwarenessRegistry`(정적 등록소, `Items`/`Added` 노출)만 소비한다. World가 Emotions를 몰라도 Emotions가 World를 관찰할 수 있게 하는 단방향 관찰 구조다.
- **World → Emotions 역호출(유일한 예외)**: `World/StoryFragment.cs`의 `Collect()`가 스킬을 새로 부여할 때 `EmotionSkillController.Instance?.RefreshActive()`를 호출해, 방금 얻은 스킬을 즉시 활성 스킬로 반영시킨다. `EmotionSkillController.Instance`는 `Update`에서도 `Active == null`일 때 매 프레임 `RefreshActive()`를 자동 호출하므로, 이 역호출이 없어도 다음 프레임에는 결국 동기화되지만 즉시 반영을 위해 명시적으로 호출한다.

## 의존성 주의

- `EmotionSkill.End()`는 `SkipCooldown`을 두 번(호출 시작 시, 그리고 `if (!SkipCooldown) ...` 판정 직후) `false`로 리셋한다 — 하위 클래스의 `OnEnd()`에서 설정한 값이 그 프레임 안에서만 유효하고 다음 사용에 영향을 주지 않도록 하는 장치이니, 새 스킬을 추가할 때 `OnEnd()` 밖에서 `SkipCooldown`을 설정하면 무시된다는 점에 유의.
- `EmotionSkillController.Update`는 `Active == null`일 때 매 프레임 `RefreshActive()`를 호출한다 — 스킬 미보유 구간에서는 사실상 폴링이므로, 씬에 `GameManager`/`Balance`가 준비되지 않은 채 이 컴포넌트가 먼저 `Update`를 타면 `NullReferenceException` 위험이 있다(초기화 순서 보장 필요).
- `AwarenessSystem.Update`는 `GameManager.State != Playing`일 때 자각을 강제로 끄지만, `PlayerInput.AwarenessHeld`는 `PlayerInput.Enabled`와 무관하게 항상 실제 키 입력을 반환하는 특수 필드이므로, Ending 시퀀스 등에서 `PlayerInput.Enabled = false`로 다른 입력을 다 막아도 L 키만은 여기서 별도로 체크해 막아야 한다는 점을 잊지 말 것.
- 새 `IAwarenessReactive`/`IRewindable`/`IForeseeable` 구현체를 추가할 때는 반드시 `AwarenessRegistry.Register`/`Unregister`(자각 대상) 또는 콜라이더 배치(되감기/예지 대상, `interactableMask`/물리 레이어와 무관하게 `OverlapCircleAll`은 기본 레이어 마스크를 쓰므로 예지는 마스크 지정이 없다)를 챙겨야 Emotions 쪽에서 정상적으로 발견한다.
