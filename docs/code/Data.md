# Data 모듈

`HiddenWeight.Data` 네임스페이스는 게임 수치를 담는 ScriptableObject 테이블과 전역 식별자 enum으로만
구성된다. 로직이 있는 곳은 `BalanceData`의 조회 메서드뿐이고 나머지는 순수 데이터 컨테이너다.
`HiddenWeight.*`에 대한 의존성이 전혀 없어 다른 모든 모듈의 최하위 리프 노드 역할을 한다.

## BalanceData.cs

- **역할**: 전체 밸런스 데이터의 진입점. 플레이어/감정/적/지역 데이터를 한데 모아 Id 또는 씬 이름으로
  조회하는 기능을 제공한다. `GameManager`가 이 에셋 하나만 참조로 들고 다닌다.
- **상속/의존**: `ScriptableObject` 상속. `PlayerData`, `EmotionData[]`, `EnemyData[]`, `ZoneData[]`,
  `UnityEngine.Rendering.VolumeProfile`을 참조한다. `[CreateAssetMenu(fileName = "BalanceData", menuName = "HiddenWeight/Balance Data")]`.
- **주요 멤버**:
  - `public PlayerData player`
  - `public EmotionData[] emotions`
  - `public EnemyData[] enemies`
  - `public ZoneData[] zones`
  - `public VolumeProfile awarenessProfile`
  - `public EmotionData GetEmotion(EmotionId id)`
  - `public ZoneData GetZone(ZoneId id)`
  - `public ZoneData GetZoneByScene(string sceneName)`
- **동작**:
  - 세 조회 메서드 모두 대상 배열이 `null`이면 즉시 `null`을 반환한다.
  - 이후 배열을 `foreach`로 선형 탐색하며, 원소가 `null`이 아니고 `id`(또는 `sceneName`)가 일치하는
    첫 원소를 반환한다. 못 찾으면 `null`.
  - 배열 원소가 최대 4개(감정 3종, 지역 4종)뿐이라 이진 탐색이나 딕셔너리 캐싱 없이 선형 탐색으로 충분하다는
    설계 의도가 주석으로 명시되어 있다.

## EmotionData.cs

- **역할**: 감정 스킬(되감기/숨죽이기/예지) 1종의 수치를 담는 ScriptableObject.
- **상속/의존**: `ScriptableObject` 상속. `EmotionId`, `SkillInput` enum에 의존.
  `[CreateAssetMenu(fileName = "EmotionData", menuName = "HiddenWeight/Emotion Data")]`.
- **주요 멤버** (필드 기본값 없음 — 감정마다 값이 크게 달라 클래스 차원의 공용 기본값을 두지 않고 인스턴스별로 채운다):
  - `public EmotionId id`
  - `public string displayName` — "되감기" / "숨죽이기" / "예지"
  - `public SkillInput inputMode` — 되감기·숨죽이기는 Hold, 예지는 Tap
  - `public float channelTime` — 채널링 시간(되감기 1.0, 나머지 0)
  - `public float cooldown` — 쿨타임(되감기 2, 예지 3, 숨죽이기 0)
  - `public float range` — 사거리(되감기 6, 예지 8)
  - `public float moveSpeedMultiplier` — 사용 중 이동 배율(되감기 0=이동 불가, 숨죽이기 0.45, 예지 1)
  - `public float effectDuration` — 예지 고스트 표시 시간(1.5)
  - `public float previewLeadTime` — 예지가 내다보는 미래 초(2.0)
  - `public float hushScale` — 숨죽이기 스케일 배수(0.6)
- **동작**: 값만 정의하며 로직 없음. 실제 감정별 수치는 `Editor/DataAssetBuilder.cs`가 에셋 생성 시 채워 넣는다.

## EnemyData.cs

- **역할**: 적 1종의 수치를 담는 ScriptableObject.
- **상속/의존**: `ScriptableObject` 상속. `UnityEngine.Color`만 사용, `HiddenWeight.Data` 내 다른 타입에는 의존하지 않음.
  `[CreateAssetMenu(fileName = "EnemyData", menuName = "HiddenWeight/Enemy Data")]`.
- **주요 멤버** (필드 기본값 포함):
  - `public int maxHealth = 2`
  - `public float moveSpeed = 1.5f`
  - `public int contactDamage = 1`
  - `public Color tint = Color.white`
  - `public float knockbackForce = 6f`
  - `public float wobbleAmplitude = 0f` — 균열 지역 적만 0.2로 오버라이드
  - `public float wobbleFrequency = 3f`
- **동작**: 값만 정의하며 로직 없음.

## Enums.cs

- **역할**: 모듈 전역에서 쓰는 식별자 enum 3종을 정의한다.
- **상속/의존**: 의존성 없음(순수 enum 선언).
- **주요 멤버**:
  - `public enum EmotionId { None = 0, Rewind = 1, Hush = 2, Foresight = 3 }`
  - `public enum ZoneId { Prologue = 0, Residue = 1, Gaze = 2, Fracture = 3 }`
  - `public enum SkillInput { Hold = 0, Tap = 1 }`
- **동작**: 값만 정의하며 로직 없음. 모든 값에 명시적 정수를 부여해, 인스펙터에 직렬화된 값이 enum
  선언 순서 변경으로 깨지지 않도록 한다.

## PlayerData.cs

- **역할**: 플레이어 이동/전투 밸런스 수치. 코드에 하드코딩하지 않고 전부 이 ScriptableObject의
  직렬화된 필드로 관리한다.
- **상속/의존**: `ScriptableObject` 상속. `UnityEngine.Vector2`만 사용.
  `[CreateAssetMenu(fileName = "PlayerData", menuName = "HiddenWeight/Player Data")]`.
- **주요 멤버** (`[Header]`로 5개 그룹, 필드 기본값 포함):
  - 이동: `walkSpeed = 6f`, `runSpeed = 9f`, `jumpVelocity = 14f`, `gravityScale = 3.5f`,
    `fallGravityMultiplier = 1.6f`, `coyoteTime = 0.1f`, `jumpBufferTime = 0.1f`, `variableJumpCut = 0.5f`
  - 대시: `dashDistance = 4f`, `dashDuration = 0.15f`, `dashCooldown = 0.8f`
  - 벽: `wallSlideSpeed = 2f`, `wallJumpVelocity = new Vector2(9f, 13f)`, `wallJumpLockTime = 0.15f`, `wallCoyoteTime = 0.1f`(2026-07-26 추가 — 벽에서 떨어진 뒤에도 벽점프를 허용하는 유예 시간. 기존 `PlayerData.asset`에는 이 필드가 직렬화되어 있지 않아 C# 필드 초기값 0.1이 그대로 쓰인다)
  - 생존: `maxHealth = 3`, `invulnerableTime = 0.8f`, `blinkInterval = 0.1f`(무적 시간 중 스프라이트
    점멸 간격), `knockbackForce = 8f`
  - 공격: `attackRadius = 1.2f`, `attackAngle = 90f`(부채꼴 각도, 도), `attackActiveTime = 0.1f`,
    `attackCooldown = 0.35f`, `attackDamage = 1`
- **동작**: 값만 정의하며 로직 없음. `blinkInterval`은 하드코딩 상수 대신 이 에셋 필드로 직접 추가된
  값으로, 필드 자체의 기본값(`0.1f`)이 곧 실제 사용값이다(`DataAssetBuilder`가 별도로 덮어쓰지 않음).

## ZoneData.cs

- **역할**: 지역(프롤로그/잔재/응시/균열) 1개의 설정을 담는 ScriptableObject.
- **상속/의존**: `ScriptableObject` 상속. `ZoneId`, `EmotionId` enum과
  `UnityEngine.Rendering.VolumeProfile`, `UnityEngine.AudioClip`에 의존.
  `[CreateAssetMenu(fileName = "ZoneData", menuName = "HiddenWeight/Zone Data")]`.
- **주요 멤버**:
  - `public ZoneId id`
  - `public string displayName` — "몽환의 우주" / "잔재" / "응시" / "균열"
  - `public string sceneName` — 예: `Zone_Prologue`
  - `public string nextSceneName` — 클리어 시 넘어갈 씬
  - `public EmotionId grantedSkill` — 이 지역에서 얻는 스킬(프롤로그는 `None`)
  - `public bool grantsAwareness` — 응시 지역만 `true`
  - `public bool awarenessStable = true` — 균열 지역만 `false`
  - `public VolumeProfile volumeProfile` — 지역별 색보정
  - `public AudioClip bgm` — MVP에서는 비워둔다
- **동작**: 값만 정의하며 로직 없음. `BalanceData.GetZone`/`GetZoneByScene`이 이 클래스의 `id`/`sceneName`
  필드를 키로 배열을 탐색한다.
