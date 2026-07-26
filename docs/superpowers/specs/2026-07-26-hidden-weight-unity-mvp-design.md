# Hidden Weight — Unity MVP 구현 설계

> 대상 기획: [../../GAME_DESIGN.md](../../GAME_DESIGN.md) (4차 초안, 2026-07-23)
> 작성일: 2026-07-26
> 상태: 승인됨. 이 문서를 기준으로 구현 계획을 작성한다.

---

## 1. 목표와 범위

기획서 10.1절 MVP를 Unity로 **플레이 가능한 상태**까지 구현한다. 3개 지역(잔재·응시·균열)을
얇게 전부 만들고, 감정 스킬 3종 + 자각 + 2단 엔딩까지 한 줄기로 이어지게 한다.

산출물은 세 가지다.

1. `HiddenWeight/` — Unity 6000.5.4f1 2D URP 프로젝트 (스크립트·프리팹·데이터·씬 포함)
2. 모듈별 `README.md` + `docs/code/<모듈>.md` — 파일 단위 개발 문서
3. `PROJECT_STRUCTURE.md` — 모듈 개요와 최초 세팅 안내

### 1.1 이 문서에서 확정한 것

기획서에 비어 있던 항목을 아래와 같이 결정했다.

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

### 1.2 범위 밖 (구현하지 않음)

- 기쁨·분노 확장 지역 (기획서 5.4절)
- 자각의 감정별 차등 감지 고도화
- 다회 백트래킹, 여러 엔딩 분기, 감정 조합
- 적 AI 고도화(투사체·원거리·보스), 세이브/로드, 컨트롤러 지원
- 사운드 에셋 (`AudioManager` 인터페이스만 두고 클립은 비워둔다)

---

## 2. Unity 프로젝트 설정

| 항목 | 값 |
|---|---|
| 엔진 | Unity 6000.5.4f1 |
| 경로 | 저장소 루트의 `HiddenWeight/` |
| 렌더링 | URP 17.5.0, 2D Renderer |
| 물리 | Physics2D |
| 입력 | Legacy `Input` (`Input.GetAxisRaw` / `GetKey`) |
| 카메라 | Orthographic, 세로 크기 6 유닛 |
| 픽셀 | Pixels Per Unit 32 |
| 대상 해상도 | 1920×1080 |

패키지는 `com.unity.render-pipelines.universal`, `com.unity.2d.tilemap`, `com.unity.2d.sprite`,
`com.unity.ugui`를 사용한다. Input System 패키지는 설치하되 Legacy 입력 모드로 둔다.

### 2.1 레이어와 태그

| 레이어 | 용도 |
|---|---|
| `Ground` | 타일맵 지형, 발판 |
| `Wall` | 벽잡기 판정 대상 (지형과 별도 판정) |
| `Player` | 플레이어 |
| `PlayerHushed` | 숨죽이기 상태. `GazeHazard`가 무시한다 |
| `Enemy` | 적 |
| `Hazard` | 접촉 피해 영역 |
| `Interactable` | 파편·게이트·되감기 대상 |

---

## 3. 모듈 구조

```
HiddenWeight/Assets/Scripts/
├── Core/       게임 상태·씬 흐름·진행도·체크포인트·오디오
├── Data/       ScriptableObject 데이터 테이블
├── Player/     이동·전투·체력·애니메이션
├── Emotions/   감정 스킬 3종 + 자각
├── World/      룸·게이트·되감기 대상·장애물·파편
├── Enemies/    적 본체·순찰·접촉 피해
├── Ending/     2단 엔딩 시퀀스
├── UI/         HUD·일시정지·파편 로그·타이틀
└── Editor/     프로젝트 세팅·프리팹/데이터/씬 자동 생성·빌드 검증
```

네임스페이스는 `HiddenWeight.<모듈>`을 쓴다. `Editor` 모듈만 `HiddenWeight.EditorTools`.

### 3.1 의존 방향

```
Editor ──▶ (전부)
UI ──▶ Core, Data, Player, Emotions
Ending ──▶ Core, Emotions
Enemies ──▶ Core, Data, Player
Emotions ──▶ Core, Data, Player, World
World ──▶ Core, Data, Player
Player ──▶ Core, Data
Data ──▶ (없음)
Core ──▶ Data
```

순환 참조가 없는 단방향 구조다. 특히 `World`는 `Emotions`를 **참조하지 않는다** —
`Gate`가 필요로 하는 `EmotionId`는 `Data`에 있고, `GazeHazard`는 숨죽이기 여부를
`Emotions`에 묻지 않고 플레이어의 레이어(`PlayerHushed`)로만 판단한다.

반대로 `Emotions`는 `World`의 구체 클래스를 알 필요가 없다. 상호작용은
`IRewindable` / `IAwarenessReactive` / `IForeseeable` 인터페이스로만 연결하며,
인터페이스 정의는 `World/Interactions.cs`에 두고 `Emotions`가 소비한다.

---

## 4. 씬 구성

| # | 씬 | 내용 |
|---|---|---|
| 0 | `Bootstrap` | `GameManager` 생성 후 `DontDestroyOnLoad`, `Title`로 넘어감 |
| 1 | `Title` | 타이틀·시작·종료 |
| 2 | `Zone_Prologue` | 몽환의 우주. 이동·점프·대시·벽점프 튜토리얼 |
| 3 | `Zone_Residue` | 잔재(과거·죄책감). 되감기 획득 |
| 4 | `Zone_Gaze` | 응시(현재·수치심). 숨죽이기 + 자각 획득 |
| 5 | `Zone_Fracture` | 균열(미래·불안). 예지 획득 |
| 6 | `Ending` | 거짓 깨어남 → 진짜 각성 |

백트래킹은 균열 클리어 후 `Zone_Residue`를 다시 로드하는 방식이다. `ProgressState.HasClearedFracture`가
true면 잔재 지역의 최종 파편 게이트가 열린다.

각 지역 씬 안에서 카메라는 `Room` 컴포넌트가 정의한 사각 경계 안에서만 움직이고,
플레이어가 룸 경계를 넘으면 다음 룸으로 전환된다.

---

## 5. 핵심 시스템

### 5.1 PlayerController

상태 기반 2D 플랫포머 컨트롤러. 상태는 `Idle / Walk / Run / Jump / AirMove / Fall / Land /
Attack / Dash / WallCling / WallJump`로, 레퍼런스 스프라이트 시트와 1:1 대응한다.

| 입력 | 기능 |
|---|---|
| `A` / `D` (또는 ←/→) | 좌우 이동 |
| `Shift` (홀드) | 달리기 |
| `Space` | 점프 / 벽점프 |
| `LeftControl` | 대시 |
| `J` | 공격 |
| `K` | 감정 스킬 |
| `L` (홀드) | 자각 |
| `Esc` | 일시정지 |

기획서 6.2절 조작표에 대시·달리기 키를 추가한 형태다.

느낌을 위해 세 가지를 넣는다.

- **코요테 타임** 0.1초 — 발판에서 떨어진 직후에도 점프 허용
- **점프 버퍼** 0.1초 — 착지 직전 입력을 기억
- **가변 점프 높이** — 상승 중 키를 떼면 상승 속도를 절반으로 깎음

벽잡기는 `Wall` 레이어에 공중에서 접촉하고 벽 방향 입력이 있을 때 진입하며, 천천히 미끄러진다.
벽점프는 벽 반대 방향으로 튕겨 나가고 짧은 입력 잠금(0.15초)을 건다.

### 5.2 감정 스킬 (`K` 단일 키)

`EmotionSkillController`가 현재 지역의 `EmotionData`를 보고 보유한 스킬 하나에 위임한다.
기획서 6.2절의 "지역에 따라 자동 전환"을 그대로 구현한 것이다.
모든 스킬은 `EmotionSkill` 추상 클래스를 상속하고 `CanUse` / `OnBegin` / `OnTick` / `OnEnd`를 채운다.

| 스킬 | 지역 | 입력 | 사용 중 이동 | 동작 |
|---|---|---|---|---|
| 되감기 | 잔재 | 홀드 | **불가** | 조준선 안의 가장 가까운 `IRewindable`을 1.0초 채널링해 초기 상태로 복원. 쿨타임 2초 |
| 숨죽이기 | 응시 | 홀드 | 감속(0.45배), 공격 불가 | 스케일 0.6배 + 레이어를 `PlayerHushed`로 전환. `GazeHazard`가 무시 |
| 예지 | 균열 | 탭 | 가능 | 반경 8유닛 내 `IForeseeable`의 2초 뒤 상태를 반투명 고스트로 1.5초간 표시. 쿨타임 3초 |

되감기는 전체 시간 되감기가 아니라 **오브젝트 단위**다(기획서 4.2절). `IRewindable`을 구현한
오브젝트가 각자 자기 초기 상태(위치·회전·활성 여부·부서짐 단계)를 들고 있다가 복원한다.

채널링 중 이동 불가는 기획서가 명시한 제약이므로 그대로 지킨다 — "멈춰서 되돌려야 하는,
위험을 감수하는 스킬".

### 5.3 자각 (`L` 홀드)

별도 시스템이 아니라 **화면 효과 + 인터페이스 브로드캐스트**다.

- 홀드하는 동안 URP `Volume`의 가중치를 0.25초에 걸쳐 1까지 올린다. 프로파일에는
  `ColorAdjustments`(채도 -80)와 `Vignette`가 들어간다 → 기획서 7.2절 "채도를 잃는 연출"
- 이동은 가능하되 속도 0.6배 (기획서 4.3절 권장안)
- 씬 안의 모든 `IAwarenessReactive`에게 `OnAwarenessChanged(bool active)`를 알린다.
  숨겨진 파편·잔상·이상 오브젝트가 이때만 드러난다
- 지속시간 제한과 자원 게이지는 두지 않는다 (기획서 4.3절 MVP 권장안)

**균열 지역의 불안정성**: `ZoneData.awarenessStable = false`인 지역에서는 `AwarenessSystem`이
반응 대상 중 일부를 무작위로 누락시키고, 0.3~0.8초 간격으로 표시를 깜빡인다.
"믿을 수 있는 예지와 믿을 수 없는 자각이 같은 공간에 있는" 인지 부조화(기획서 4.2절)를
메커닉으로 만든다.

### 5.4 전투

기획서에 설계가 없어 새로 정한다. 원칙은 "탐험을 방해하지 않는 최소한".

**공격** — `J` 키. 전방 부채꼴(반경 1.2유닛, 90도) 히트박스를 0.1초 켠다. 쿨타임 0.35초.
숨죽이기 중에는 사용 불가.

**체력** — HP 3. 피격 시 무적 0.8초 + 넉백 + 스프라이트 점멸. 0이 되면 마지막 체크포인트로
되돌리고 HP를 채운다. 게임오버·잔기·리트라이 화면은 없다.

**적 1종** — 지형 위를 왕복 순찰하는 `Enemy`. 낭떠러지·벽을 만나면 방향을 바꾼다.
접촉 시 피해 1. HP 2, 피격 시 넉백. 지역별로 `EnemyData` 에셋만 바꿔 색과 속도를 달리한다.

| 지역 | 색 | 속도 | 성격 |
|---|---|---|---|
| 잔재 | 탁한 회갈색 | 1.2 | 느리고 무겁게 |
| 응시 | 보라 | 2.0 | 시선 기믹 근처에 배치 |
| 균열 | 파스텔 민트 | 1.6 | 순찰 경로가 미세하게 어긋남 (0.2유닛 진동) |

### 5.5 월드

| 컴포넌트 | 역할 |
|---|---|
| `Room` | 사각 경계 정의. 플레이어 진입 시 카메라 경계를 이 룸으로 전환 |
| `RoomCamera` | 현재 룸 경계 안에서 플레이어를 부드럽게 따라감 |
| `Gate` | 필요 스킬(`EmotionId`)을 지정. 미보유 시 통과 차단 + 힌트 표시 |
| `Rewindable` | `IRewindable` 기본 구현. 초기 Transform·활성 상태를 저장했다가 복원 |
| `CrumblingPlatform` | 밟으면 무너지는 발판. `IRewindable`(되감기로 복구) + `IForeseeable`(예지로 무너진 형태 미리 보기) |
| `MovingPlatform` | 왕복 발판. `IForeseeable`(2초 뒤 위치 표시) |
| `GazeHazard` | 시선 기믹. 원뿔 시야 안에 플레이어가 있으면 피해. `PlayerHushed` 레이어는 무시 |
| `StoryFragment` | 수집 파편. 접촉 시 `ProgressState`에 기록하고 화면 하단에 텍스트 한 줄 표시 |
| `HiddenFragment` | `IAwarenessReactive`. 자각 중에만 나타나는 파편 |
| `ZoneTrigger` | 지역 클리어 지점. 다음 씬 로드 |
| `Checkpoint` | 통과 시 리스폰 지점 갱신 |

### 5.6 진행 상태

`ProgressState`는 `GameManager`가 들고 씬을 넘나드는 순수 C# 클래스다.

```csharp
HashSet<EmotionId> unlockedSkills;   // Rewind, Hush, Foresight
bool hasAwareness;                    // 응시 지역에서 true
HashSet<string> collectedFragments;   // 파편 id
ZoneId currentZone;
bool hasClearedFracture;              // 백트래킹 게이트 개방 조건
Vector3 lastCheckpoint;
```

게이팅은 `Gate.requiredSkill`을 `unlockedSkills`와 대조하는 것이 전부다.
잔재 백트래킹의 최종 파편만 예외적으로 `되감기 보유 && hasAwareness && hasClearedFracture`
세 조건을 모두 본다 (기획서 5.3절).

### 5.7 엔딩

`Ending` 씬. 횡스크롤을 버리고 정적 1인칭 침실을 스프라이트 레이어로 구성한다.
플레이어 이동은 없고 `L`(자각)만 받는다.

**1단계 — 거짓 깨어남**
1. 검은 화면에서 3초에 걸쳐 침실이 페이드 인. 무음
2. 4초간 아무 일도 일어나지 않는다 (안도감 확보 구간)
3. `AnomalyObject` 3종이 씬에 숨어 있다 — 거꾸로 타는 촛불, 광원과 어긋난 그림자, 미세하게 떠는 벽
4. 플레이어가 `L`을 홀드하면 셋이 동시에 드러난다. 시스템은 먼저 알려주지 않는다
5. **1단계 종료 조건**: 자각 홀드를 끊김 없이 2.5초 유지한다. 홀드를 놓으면 누적 시간이
   초기화되고 이상 오브젝트도 다시 숨는다 — 플레이어가 스스로 "확신이 설 때까지" 들여다보게 만든다
6. 조건 충족 시 1~2초 암전 → 잔재·응시·균열의 단편 프레임 몽타주 2~3초 → 2단계
   (기획서 3.4절 권장안, 총 5~8초)

**2단계 — 진짜 각성**
1. 같은 침실. 이번엔 `AnomalyObject`가 전부 비활성
2. `L`을 홀드해도 아무것도 드러나지 않는다
3. 옅은 음악이 처음으로 들어온다 (기획서 8.1절)
4. 8초 후 또는 플레이어가 자각을 놓으면 타이틀로 페이드 아웃

---

## 6. 데이터 (ScriptableObject)

수치는 전부 에셋으로 뺀다. 코드 재컴파일 없이 밸런싱하기 위함이다.

| 에셋 | 필드 |
|---|---|
| `PlayerData` | 이동속도 6 / 달리기 9 / 점프력 14 / 중력 스케일 3.5 / 대시 거리 4·쿨타임 0.8 / 벽 미끄럼 2 / 최대 HP 3 / 무적 0.8 |
| `EmotionData` (3개) | `EmotionId`, 표시명, 입력 방식, 채널링 시간, 쿨타임, 사거리, 사용 중 이동 배율 |
| `EnemyData` (3개) | HP, 이동속도, 접촉 피해, 색상, 넉백 세기 |
| `ZoneData` (4개) | `ZoneId`, 지역명, 색보정 프로파일, BGM, `awarenessStable`, 다음 씬 이름 |
| `BalanceData` | 위 에셋들의 참조 묶음. `GameManager`가 하나만 들고 다닌다 |

### 6.1 지역별 색보정 (기획서 7.1절)

| 지역 | Color Adjustments |
|---|---|
| 프롤로그 | 중립. 채도 0, 노출 0 |
| 잔재 | 노출 -1.2, 채도 -45, 색조 남색 쪽으로 -15 |
| 응시 | 노출 -0.6, 채도 -20, 색조 보라·청록 쪽으로 +25, 대비 -15 |
| 균열 | 노출 +0.8, 채도 +30, 색조 민트·라벤더 쪽으로 +40 |

---

## 7. 문서 산출물

| 문서 | 내용 |
|---|---|
| `PROJECT_STRUCTURE.md` | 모듈 개요, 씬 흐름, Unity 최초 세팅, 조작표 |
| `HiddenWeight/Assets/Scripts/<모듈>/README.md` | 모듈 단위 기능 개요와 파일 목록 |
| `docs/code/README.md` | 코드 문서 색인, 아키텍처 원칙 |
| `docs/code/<모듈>.md` | **파일별** 역할 / 상속·의존 / 주요 멤버 / 동작 |

`docs/code/` 포맷은 이전 프로젝트(Rookie to CEO)에서 쓰던 것을 그대로 따른다.

---

## 8. 구현 순서

1. `HiddenWeight/` Unity 프로젝트 생성, URP 2D 설정, 레이어·태그 등록
2. `Data` — ScriptableObject 클래스와 열거형
3. `Core` — `GameManager`, `GameState`, `SceneFlow`, `ProgressState`, `Checkpoint`, `AudioManager`
4. `Player` — `PlayerController`, `PlayerHealth`, `PlayerAttack`, `PlayerAnimator`
5. `World` — 인터페이스 3종, `Room`, `RoomCamera`, 장애물·파편·게이트
6. `Emotions` — `EmotionSkillController`, 스킬 3종, `AwarenessSystem`
7. `Enemies` — `Enemy`, `EnemyPatrol`, `ContactDamage`
8. `UI` — `HUD`, `PauseMenu`, `FragmentLog`, `TitleScreen`
9. `Ending` — `EndingSequence`, `AnomalyObject`
10. `Editor` — 플레이스홀더 스프라이트·프리팹·데이터 에셋·씬 6개 자동 생성
11. batchmode 컴파일 및 빌드 검증
12. 문서 작성

각 단계는 batchmode 컴파일이 통과해야 다음으로 넘어간다.

---

## 9. 검증 방법

Unity 테스트 프레임워크를 도입하지 않는다. 대신 두 가지로 확인한다.

1. **컴파일 검증** — `Unity -batchmode -quit -projectPath HiddenWeight
   -executeMethod HiddenWeight.EditorTools.BuildScript.Compile`. 에러 0건이어야 한다
2. **빌드 검증** — 같은 방식으로 macOS 스탠드얼론 빌드가 성공해야 한다

플레이 감각(점프 높이, 대시 거리, 되감기 채널링 시간)은 수치를 에셋으로 빼두었으므로
에디터에서 직접 조정한다.
