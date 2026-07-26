# Ending 모듈 — 정적 침실에서 진행되는 2단계 엔딩 시퀀스 연출/판정

> 기획서 5.7(엔딩), 3.4절 대응.
> 스크롤 없는 고정 침실 씬에서 이동을 막고 자각(L) 홀드 입력만으로 진행되는 컷신형 시퀀스. `EndingSequence`가 페이드/암전/몽타주/BGM/씬 전환을 모두 지휘하고, `AnomalyObject`는 그 지시를 받아 자신의 이상 상태만 표현한다.

## 파일

| 파일 | 역할 | 기획서 대응 |
|---|---|---|
| `AnomalyObject.cs` | 침실에 배치되는 "이상" 오브젝트 1개. `IAwarenessReactive`를 구현하지만 실제 갱신은 자각 신호가 아니라 `EndingSequence`가 매 프레임 직접 호출하는 `OnAwarenessChanged`로 이루어진다. 종류(뒤집힌 촛불/어긋난 그림자/떨리는 벽)별로 다른 비주얼 트릭을 적용한다. | 5.7 "3개의 이상 오브젝트" |
| `EndingSequence.cs` | 엔딩 씬의 감독 코루틴. `PlayerInput.Enabled = false`로 이동을 차단하고 `PlayerInput.AwarenessHeld`만 직접 읽어 1단계(거짓 깨어남) → 2단계(진짜 각성) → 타이틀 복귀까지 상태를 진행시킨다. | 5.7 전체, 3.4 씬 전환 |

## 핵심 규칙 구현

- **1단계 "거짓 깨어남"**
  - `FadeIn`: `ScreenFader`로 검은 화면(alpha 1) → 침실(alpha 0)까지 3초(`fadeInSeconds`) 페이드, 무음.
  - `Stillness`: 4초(`stillnessSeconds`) 동안 아무 입력도 처리하지 않고 대기.
  - `FalseAwakeningInput`: 이때부터 `Update()`가 매 프레임 `PlayerInput.AwarenessHeld`를 확인.
    - 누르는 동안: `_hold += Time.deltaTime`, 동시에 `anomalies` 3개 전부 `OnAwarenessChanged(true)`로 즉시 전원 노출(한꺼번에 드러남, 순차 아님).
    - 떼는 순간: `_hold = 0`으로 완전히 리셋되고 `OnAwarenessChanged(false)`로 즉시 재은폐 — 즉 **끊김 없는 연속 홀드만 인정**하며, 중간에 한 프레임이라도 놓으면 누적치가 0으로 돌아간다.
    - `_hold >= holdToAdvance(2.5초)` 달성 시 `TransitionToRealAwakening()` 코루틴 시작.
  - 완료 연출: 코루틴 진입 즉시 이상 재은폐 + `ScreenFader.SetAlpha(1f)`로 **페이드 없는 즉시 암전**, `blackoutSeconds(1.5초)` 유지 → `PlayMontage()`로 `montageFrames` 3장(잔재/응시/균열, 각 `montageFrameSeconds`=0.8초, 페이드 없는 하드 컷 전환)을 순서대로 표시 후 몽타주 이미지 비활성화, 화면은 다시 alpha 0(침실)으로 즉시 복귀.
- **2단계 "진짜 각성"**
  - `RunTrueAwakening()` 진입 시 `anomalies` 전원의 `Enabled = false`로 내려 이후 어떤 `OnAwarenessChanged` 호출도 무시되게 만듦(이상 오브젝트 자체는 파괴/비활성화되지 않고 로직만 잠김).
  - `AudioManager.Instance.PlayBgm(endingBgm, 2f)`로 이 시퀀스에서 처음이자 유일하게 BGM 재생 시작(2초 페이드인).
  - `trueAwakeningSeconds(8초)`가 지나거나, 그 전에 플레이어가 자각 홀드를 놓는 순간(둘 중 먼저 오는 조건) 종료 — while 루프 조건이 `t < 8 && PlayerInput.AwarenessHeld`이므로 홀드를 계속 유지해도 8초에서 강제 종료된다.
  - `fadeOutSeconds(3초)` 페이드 아웃 → `PlayerInput.Enabled = true` 복구 → `GameManager.SetState(GameState.Title)` → `SceneFlow.LoadWithFade(SceneFlow.Title)`로 타이틀 전환.
- **AnomalyObject 표현 방식**(`Kind` enum: `InvertedCandle` / `MismatchedShadow` / `TremblingWall`)
  - `InvertedCandle`: 자각 중 `visual.flipY = true`, 해제 시 원래 flipY로 복귀.
  - `MismatchedShadow`: 자각 중 로컬 회전을 90도로 틀고, 해제 시 원래 회전으로 복귀.
  - `TremblingWall`: 자각 중 `Update()`에서 매 프레임 `Mathf.Sin(Time.time * 40f) * 0.02f`만큼 x축 흔들림을 적용, 해제 시 원위치로 즉시 스냅.
  - `Enabled = false`(2단계)이면 `OnAwarenessChanged`가 아무것도 하지 않고 즉시 반환 — 촛불/그림자/벽 모두 평범한 상태로 고정.

## 씬 배치

- `RoomCamera`(플레이 중 스크롤 카메라)를 사용하지 않는 **고정(static) 카메라** 구성 — 이동 자체가 `PlayerInput.Enabled = false`로 막혀 있으므로 카메라도 따라갈 이유가 없다.
- 배경(침실) → 침대 등 정적 배경 스프라이트 → `AnomalyObject` 3개(촛불/그림자/벽, 각자 `SpriteRenderer` 보유)를 배경보다 위 레이어에 배치.
- `Canvas` 하위에 전체 화면 몽타주용 `Image`(`montageImage`)를 배치하고 `montageFrames`(잔재/응시/균열 스프라이트 3장)를 인스펙터에 연결. 평상시 비활성화 상태로 시작(`Awake()`에서 `SetActive(false)`).
- `ScreenFader` 싱글턴이 씬(또는 상위 영속 씬)에 존재해야 페이드/암전 연출이 동작한다.

## 다른 모듈과의 연결

- **실제 의존성은 Core, Player, UI, World이다.** `EndingSequence.cs`는 `HiddenWeight.Core`(`GameManager`/`GameState`/`AudioManager`/`SceneFlow`), `HiddenWeight.Player`(`PlayerInput`), `HiddenWeight.UI`(`ScreenFader`, `UnityEngine.UI.Image`)를 사용하고, `AnomalyObject.cs`는 `HiddenWeight.World`(`IAwarenessReactive`, `AwarenessRegistry`)를 사용한다.
- **기획서 3.1절과의 편차(문서화된 이탈)**: 기획서 3.1절 의존 관계도는 `Ending ──▶ Core, Emotions`라고 명시하지만, 실제 구현은 **Core + Player + UI + World**를 사용하며 `HiddenWeight.Emotions`는 어디에도 참조되지 않는다. 이는 엔딩 씬에 이동·볼륨·불안정 연출이 필요 없어 `AwarenessSystem`(Emotions)을 두지 않고, `EndingSequence`가 `PlayerInput.Enabled = false`로 이동만 잠근 뒤 `PlayerInput.AwarenessHeld`를 직접 폴링하며 `AnomalyObject`들을 코드로 직접 제어하기 때문이다(`AwarenessRegistry`/`IAwarenessReactive`는 World 쪽 등록 인터페이스로만 남아 있고, 실제 노출 판정과 트리거는 Emotions 모듈이 아니라 이 시퀀스가 전담한다).

## 의존성 주의

- 씬 진입 전/후 `PlayerInput.Enabled`는 `EndingSequence`가 `Start()`에서 false로, 종료 시(`RunTrueAwakening()` 끝) 및 `OnDestroy()`(방어적 복구)에서 true로 되돌린다 — 다른 코드가 이 사이에 `PlayerInput.Enabled`를 건드리면 안 된다.
- `PlayerInput.AwarenessHeld`는 `Enabled` 게이트를 우회해 항상 읽히도록 설계돼 있어야 하며, 엔딩 씬은 이 특성에 의존한다.
- 씬에 `ScreenFader.Instance`, `GameManager.Instance`, `AudioManager.Instance`가 준비돼 있지 않아도 각 호출은 null 체크로 스킵되지만, 그 경우 페이드/BGM/상태 전환 연출이 통째로 빠지므로 통합 시 반드시 세 싱글턴을 씬(또는 영속 씬)에 등록해야 한다.
- `montageImage`/`montageFrames`(3장)를 인스펙터에 연결하지 않으면 몽타주 구간이 그냥 스킵된다(대기 시간 없이 통과).
- `anomalies` 배열에는 정확히 3개의 `AnomalyObject`(촛불/그림자/벽 각 1개)를 연결해야 기획서 5.7의 연출과 일치한다.
