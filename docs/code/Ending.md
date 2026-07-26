# Ending 모듈

`HiddenWeight.Ending`은 스크롤 없는 고정 침실 씬에서 진행되는 2단계 엔딩 시퀀스를 담당한다. 기획서 3.1절은 `Ending ──▶ Core, Emotions`로 명시하지만 실제 코드는 `HiddenWeight.Emotions`를 전혀 참조하지 않고, 대신 `HiddenWeight.Core`(GameManager/AudioManager/SceneFlow), `HiddenWeight.Player`(PlayerInput), `HiddenWeight.UI`(ScreenFader), `HiddenWeight.World`(AwarenessRegistry/IAwarenessReactive)를 사용한다 — `EndingSequence`가 이동을 잠근 뒤 자각 홀드 입력과 이상 오브젝트 노출을 직접 지휘하기 때문이다.

## AnomalyObject.cs

- **역할**: 엔딩 침실에 배치되는 "이상" 오브젝트 1개(뒤집힌 촛불 / 어긋난 그림자 / 떨리는 벽 중 하나). `IAwarenessReactive`를 구현하지만 실제로는 자각 시스템이 아니라 `EndingSequence`가 매 프레임 직접 호출하는 `OnAwarenessChanged(bool)`로 노출 여부가 결정된다.
- **상속/의존**: `MonoBehaviour`, `HiddenWeight.World.IAwarenessReactive`. `HiddenWeight.World.AwarenessRegistry`에 `OnEnable`/`OnDisable`에서 자신을 등록/해제한다.
- **주요 멤버**:
  - `enum Kind { InvertedCandle, MismatchedShadow, TremblingWall }` — 이상 종류를 구분하는 전용 열거형(범용 타입 아님).
  - `[SerializeField] Kind type` / `public Kind Type => type` — 이 인스턴스의 종류.
  - `[SerializeField] SpriteRenderer visual` — 이상 상태를 표현하는 스프라이트.
  - `public bool IsRevealed { get; private set; }` — 현재 노출 여부.
  - `public bool Enabled { get; set; } = true` — 2단계에서 `false`로 내려지면 이후 자각 신호를 완전히 무시.
  - `void OnAwarenessChanged(bool active)` — `IAwarenessReactive` 구현. 노출/은폐를 실제로 적용하는 진입점.
- **동작**:
  - `Awake`에서 `visual`의 원래 로컬 위치/회전/`flipY`를 캐시해 두고, 이후 은폐 시 이 값으로 복귀시킨다.
  - `Enabled == false`이면 `OnAwarenessChanged`가 즉시 리턴 — 2단계에서 자각을 걸어도 아무 반응이 없는 이유.
  - `Kind.InvertedCandle`: 노출 중 `visual.flipY = true`, 해제 시 원래 flipY로 복귀.
  - `Kind.MismatchedShadow`: 노출 중 로컬 회전을 `Quaternion.Euler(0,0,90)`으로, 해제 시 원래 회전으로 복귀.
  - `Kind.TremblingWall`: 노출 중에는 `Update()`가 매 프레임 `Mathf.Sin(Time.time * 40f) * 0.02f`만큼 x축에 흔들림을 더해 계속 떨리게 하고, 해제되는 순간 원래 위치로 즉시 스냅한다(흔들림 자체는 `OnAwarenessChanged`가 아니라 `Update`가 담당).

## EndingSequence.cs

- **역할**: 엔딩 씬의 감독 코루틴. 이동을 차단한 뒤 자각(L) 홀드 입력만으로 진행되는 2단계 엔딩(거짓 깨어남 → 진짜 각성)을 순서대로 실행하고 타이틀로 복귀시킨다.
- **상속/의존**: `MonoBehaviour`. 참조하는 타입 — `HiddenWeight.Core.GameManager`, `HiddenWeight.Core.GameState`, `HiddenWeight.Core.AudioManager`, `HiddenWeight.Core.SceneFlow`(이상 4개는 `HiddenWeight.Core`) / `HiddenWeight.Player.PlayerInput`(`HiddenWeight.Player`) / `HiddenWeight.UI.ScreenFader`, `UnityEngine.UI.Image`(`HiddenWeight.UI` + Unity UI) / 같은 네임스페이스의 `HiddenWeight.Ending.AnomalyObject`.
- **주요 멤버**:
  - `enum Phase { FadeIn, Stillness, FalseAwakeningInput, Blackout, Montage, TrueAwakening, FadeOut }` — 내부 상태 머신.
  - `[SerializeField] AnomalyObject[] anomalies` — 3개(촛불/그림자/벽) 연결.
  - `[SerializeField] Image montageImage` / `Sprite[] montageFrames` — 몽타주용 전체화면 이미지와 프레임(잔재/응시/균열 3장).
  - `[SerializeField] AudioClip endingBgm` — null이면 무음.
  - 타이밍 필드: `fadeInSeconds=3f`, `stillnessSeconds=4f`, `holdToAdvance=2.5f`, `blackoutSeconds=1.5f`, `montageFrameSeconds=0.8f`, `trueAwakeningSeconds=8f`, `fadeOutSeconds=3f`.
  - `float _hold` — 1단계 자각 홀드 누적 타이머(연속 홀드가 끊기면 0으로 리셋).
- **동작** (2단계 상태 머신, 실측 수치 그대로):
  1. `Start()`에서 `GameManager.SetState(GameState.Ending)`, `PlayerInput.Enabled = false`(이동 차단, 자각 홀드는 이 게이트를 우회해 별도로 읽힘)로 설정하고 `RunSequence()` 시작.
  2. `Phase.FadeIn`: `ScreenFader.SetAlpha(1f)`(암전) → `FadeTo(0f, 3초)`로 침실이 무음으로 페이드인.
  3. `Phase.Stillness`: 4초 동안 아무 것도 하지 않고 대기.
  4. `Phase.FalseAwakeningInput`: 이후 `Update()`가 매 프레임 `PlayerInput.AwarenessHeld`를 확인.
     - 누르는 동안: `_hold += Time.deltaTime` 및 `SetAnomaliesRevealed(true)`로 3개 오브젝트를 **한꺼번에** 노출. `_hold >= 2.5f`가 되면 `TransitionToRealAwakening()` 시작.
     - 떼는 즉시: `_hold = 0f`로 완전 리셋, `SetAnomaliesRevealed(false)`로 즉시 재은폐 — 끊김 없는 연속 2.5초 홀드만 인정되고, 중간에 놓으면 누적치가 통째로 사라진다.
  5. `TransitionToRealAwakening()`: 진입 즉시 `Phase.Blackout`으로 전환해 `Update()`의 중복 트리거를 막고, `SetAnomaliesRevealed(false)` + `ScreenFader.SetAlpha(1f)`로 **페이드 없이 즉시 암전**, `1.5초` 대기.
  6. `PlayMontage()`(`Phase.Montage`): `montageImage` 활성화 후 `montageFrames`(잔재/응시/균열, 총 3장)를 각 `0.8초`씩 **페이드 없는 하드 컷**으로 순서대로 표시, 끝나면 `montageImage` 비활성화 및 `ScreenFader.SetAlpha(0f)`로 페이드 없이 곧장 같은 침실로 복귀.
  7. `RunTrueAwakening()`(`Phase.TrueAwakening`): `anomalies` 전원 `Enabled = false`로 잠가 이후 자각을 걸어도 아무 것도 드러나지 않게 함. `AudioManager.PlayBgm(endingBgm, 2f)`로 이 시퀀스 최초의 BGM 재생 시작. `while (t < 8f && PlayerInput.AwarenessHeld) { t += Time.deltaTime; yield return null; }`로 **8초 경과 또는 자각 홀드 해제 중 먼저 오는 조건**에서 루프 종료(계속 누르고 있어도 8초에서 강제 종료됨).
  8. `Phase.FadeOut`: `ScreenFader.FadeTo(1f, 3초)`로 암전 → `PlayerInput.Enabled = true` 복구 → `GameManager.SetState(GameState.Title)` → `SceneFlow.LoadWithFade(SceneFlow.Title)`로 타이틀 전환.
  - `OnDestroy()`에서 시퀀스 도중 씬이 예외적으로 내려가도 `PlayerInput.Enabled = true`로 방어적으로 복구한다.
