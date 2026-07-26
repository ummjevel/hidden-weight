# Data 모듈 — 게임 수치의 단일 진실 공급원(ScriptableObject 테이블)

> 기획서 6장(데이터) 대응.
> 수치를 코드 재컴파일 없이 밸런싱하기 위한 ScriptableObject 테이블. 이동속도, 감정 스킬 값,
> 적 스펙, 지역 설정을 전부 에셋 필드로 빼서 에디터에서 직접 조정할 수 있게 한다.

## 파일

| 파일 | 역할 | 기획서 대응 |
|---|---|---|
| `PlayerData.cs` | 플레이어 이동·대시·벽점프·생존·공격 수치 1세트. `wallCoyoteTime`(0.1s, 2026-07-26 추가 — 벽에서 떨어진 뒤에도 벽점프를 허용하는 유예. 기존 에셋에는 미직렬화라 C# 초기값이 쓰임) 포함 | 6장, 2장(플레이어 조작) |
| `EmotionData.cs` | 감정 스킬(되감기/숨죽이기/예지) 1종의 입력 방식·채널링·쿨타임 등 | 6장, 4장(감정 스킬) |
| `EnemyData.cs` | 적 1종의 HP·이동속도·접촉 피해·넉백 등 | 6장, 5장(적) |
| `Enums.cs` | `EmotionId`, `ZoneId`, `SkillInput` 열거형 정의 | 전역 식별자 |
| `ZoneData.cs` | 지역(프롤로그/잔재/응시/균열) 1개의 씬 연결·색보정·부여 스킬 등 | 6장, 7.1절(지역별 색보정) |
| `BalanceData.cs` | 위 데이터 에셋들을 한데 모아 `GameManager`가 들고 다니는 루트 에셋. Id/씬 이름으로 조회 | 6장 |

## 핵심 규칙 구현

- `EmotionId`, `ZoneId`는 인스펙터에 저장된 값이 enum 순서 변경으로 깨지지 않도록 **명시적 숫자**를 붙였다(`None = 0` 포함).
- `PlayerData.blinkInterval`(float, 기본값 `0.1f`)은 무적 시간 중 스프라이트 점멸 간격을 제어하며, 코드에 하드코딩된 상수가 아니라 이 에셋의 직렬화 필드로 관리된다.
- `EnemyData`, `PlayerData`는 클래스 필드 자체에 기본값을 갖지만, `EmotionData`, `ZoneData`, `BalanceData`(참조 배열)는 감정/지역마다 값이 크게 달라 필드 기본값이 없고 인스턴스별로 채워진다.
- `BalanceData`는 `player`(단일), `emotions`/`enemies`/`zones`(배열), `awarenessProfile`(`VolumeProfile`)을 들고 있으며, `GetEmotion(EmotionId)`, `GetZone(ZoneId)`, `GetZoneByScene(string)` 3개의 선형 탐색 조회 메서드를 제공한다. 배열이 `null`이거나 대상을 못 찾으면 `null`을 반환한다(배열 원소가 최대 4개라 선형 탐색으로 충분).
- `GameManager`는 `BalanceData` 단 하나(`[SerializeField] balance`)만 참조로 들고, 씬 로드 시 `GetZoneByScene`으로, 이후 `GetZone`으로 현재 지역 데이터를 조회한다.

## 씬 배치

- 이 모듈 자체는 씬에 배치되는 MonoBehaviour가 없다 — 전부 프로젝트 에셋(ScriptableObject)이다.
- 에셋은 Editor 모듈의 `DataAssetBuilder`(`Assets/Scripts/Editor/DataAssetBuilder.cs`)가 `Assets/ScriptableObjects/` 폴더에 12종을 생성한다. 이미 존재하는 에셋은 값을 덮어쓰지 않고 그대로 재사용한다 — 재실행 한 번으로 수동 밸런싱 결과가 날아가지 않도록 하기 위함.

## 다른 모듈과의 연결

- Core(`GameManager`), Player, Emotions, Enemies, World, UI 등 사실상 모든 모듈이 `HiddenWeight.Data`의 타입(에셋 또는 enum)을 읽기만 한다.
- `Data`는 `HiddenWeight.*` 네임스페이스에 대한 의존성이 전혀 없다(grep으로 확인) — 이 모듈은 항상 트리의 리프(leaf)이며, 다른 모듈을 참조하는 순간 순환 의존이 생긴다.

## 의존성 주의

- 밸런스 조정자는 `Assets/ScriptableObjects/*.asset` 인스펙터 값을 직접 바꾸면 되고, 코드 재컴파일이 필요 없다.
- `EmotionId`/`ZoneId`에 새 값을 추가할 때는 반드시 명시적 정수를 지정할 것 — 순서 변경만으로 기존 에셋의 참조가 깨지는 것을 막기 위한 규칙이다.
- `BalanceData.GetEmotion`/`GetZone`/`GetZoneByScene`은 배열이 비어 있거나 `null`인 원소가 있어도 예외 없이 `null`을 반환하므로, 호출부는 반환값의 `null` 체크를 반드시 해야 한다(`GameManager`가 실제로 이렇게 방어적으로 호출한다).
