# 방 단위 씬 분리와 포탈 문 — 설계

> 작성일: 2026-07-31
>
> 범위: 잔재 지역 1단계. 응시·균열은 같은 구조를 뒤따른다.
>
> 상태: 설계 승인 대기

---

## 1. 배경

지금 한 지역은 씬 하나다. 잔재는 `Zone_Residue_Full.unity` 한 파일에 방 15개가 전부
구워져 있고, 방 사이는 `CorridorGap = 4` 만큼 띄운 뒤 `BuildCorridor`가 바닥 한 줄과
천장을 깔아 잇는다. 방은 걸어서 지나간다.

이 구조에는 세 가지 문제가 있다.

**런타임에 방 개념이 거의 없다.** `Room`은 카메라 클램프용 경계일 뿐이다. 방과 방의 연결은
`ResidueZoneBuilder.BuildConnections()`의 `links` 배열에만 있고, 그것도 타일맵을 굽고
나면 사라진다. 어느 방이 어느 방과 무슨 방향으로 이어지는지 게임이 실행 중에 알 방법이
없다. 지도, 방문 기록, 잠긴 길 표시가 전부 여기서 막힌다.

**문서의 규약이 코드에 없다.** `LEVEL_01_STANDARD.md` §1.3은 출입구 방향을
`W/E/NW/NE/SW/SE/U/D/S`로 표기하기로 정해 뒀고, §1.1은 "출구 좌표는 연결 포털의 중심
좌표"라고 쓴다. 포탈이라는 단어까지 나와 있지만 구현에는 대응물이 없다. 같은 §1.1의 "각
방의 왼쪽 아래를 `(0, 0)`으로 둔다"도 지금은 지켜지지 않는다 — 방마다 전역 오프셋
(`R01 = (0,0)`, `R02 = (30,0)` …)을 받아 한 월드에 나란히 놓인다.

**연결 데이터가 지역 수만큼 복제돼 있다.** 잔재·응시·균열 빌더가 각각 1,100~1,400줄이고
각자 자기 `links` 배열과 복도 코드를 들고 있다. 연결에 인자를 하나 추가하려면 세 곳을
고쳐야 한다.

## 2. 목표와 비목표

### 목표

- 방 하나를 씬 하나로 분리하고, 각 방이 자기 로컬 좌표 `(0, 0)`을 갖는다.
- 방과 방을 포탈 문으로 잇고, 연결을 런타임이 읽을 수 있는 데이터로 만든다.
- 문은 양방향이다. 되돌아올 수 없는 연결이 구조적으로 생기지 않게 한다.
- 문서의 방향 표기를 코드가 같은 단어로 쓴다.

### 비목표

- **문 자체의 잠금 조건은 이번에 구현하지 않는다.** QA가 전 구간을 자유롭게 왕복할 수
  있어야 한다. 설계만 §9에 남긴다.
- 이웃 방 미리 로드(스트리밍)는 하지 않는다. 암전이 전환을 가리므로 필요 없다.
- 응시·균열은 1단계 범위 밖이다.
- 지도 UI는 이번 범위가 아니다. 이 작업은 지도가 나중에 읽을 데이터를 만들어 둘 뿐이다.

## 3. 씬 구조

```text
Zone_Residue.unity              셸. Player, RoomCamera, HUD, RoomLoader, GameManager.
                                지형·적·보상 없음.

Room_Residue_R01.unity          방 하나 = 씬 하나. 모두 로컬 (0,0) 기준.
Room_Residue_R02.unity
...
Room_Residue_R12.unity
Room_Residue_S1.unity           비밀방
Room_Residue_S2.unity
Room_Residue_S3.unity
```

방 씬 15개 + 셸 1개.

**동시에 로드되는 방은 항상 하나다.** 이것이 이 설계를 단순하게 만드는 핵심 제약이다. 모든
방이 `(0, 0)` 기준이어도 겹칠 일이 없고, 좌표 충돌을 피하려고 로드 시점에 오프셋을 계산할
필요도 없다. 전환은 암전 뒤에서 일어나므로 이웃 방을 미리 띄워 둘 이유가 없다.

셸 씬은 `Zone_Residue`라는 기존 이름을 그대로 쓴다. `SceneFlow.Residue` 상수와
`ZoneData.sceneName`이 이미 그 이름을 가리키고 있어 지역 진입 경로를 건드리지 않아도 된다.
현재의 `Zone_Residue_Full.unity`는 이 작업이 끝나면 삭제한다.

## 4. 데이터 모델

### 4.1 Side

`LEVEL_01_STANDARD.md` §1.3의 표기를 그대로 옮긴다.

```csharp
public enum Side { W, E, NW, NE, SW, SE, U, D, S }
```

문서와 코드가 같은 단어를 쓰는 것이 목적이다. 맵 문서에 `E`라고 적힌 출구는 코드에서도
`Side.E`다.

### 4.2 RoomLink

```csharp
[Serializable]
public struct RoomLink
{
    public string linkId;      // "residue_R01_R02"
    public string fromRoom;    // "R01"
    public string toRoom;      // "R02"
    public Side   fromSide;    // E
    public Side   toSide;      // W
    public Vector2 fromAnchor; // (26, 2) — fromRoom 로컬 좌표
    public Vector2 toAnchor;   // (0, 2)  — toRoom 로컬 좌표
}
```

앵커는 문의 중심 좌표다 (`LEVEL_01_STANDARD.md` §1.1). 플레이어가 실제로 서는 위치는
앵커에 도착 오프셋을 더해 정한다 (§6.3).

### 4.3 RoomLinkTable

```csharp
[CreateAssetMenu(menuName = "HiddenWeight/Room Link Table")]
public class RoomLinkTable : ScriptableObject
{
    public ZoneId zone;
    public RoomLink[] links;
}
```

지역당 에셋 하나. 잔재는 `Assets/Data/RoomLinks_Residue.asset`.

**링크 하나가 문 두 개를 만든다.** 빌더가 `links`를 훑으며 `fromRoom`에 문 하나,
`toRoom`에 짝이 되는 문 하나를 굽는다. 한쪽만 만들어 못 돌아오는 연결은 이 구조에서
만들어질 수 없다.

### 4.4 잔재 링크 목록

기존 `BuildConnections()`의 `links` 배열과 샤프트 3개를 그대로 옮긴다.

| linkId | from | side | to | side | fromAnchor | toAnchor |
| --- | --- | --- | --- | --- | --- | --- |
| residue_R01_R02 | R01 | E | R02 | W | (26, 2) | (0, 2) |
| residue_R02_R03 | R02 | E | R03 | W | (28, 3) | (0, 2) |
| residue_R03_R04 | R03 | E | R04 | W | (27, 1) | (2, 20) |
| residue_R04_R05 | R04 | E | R05 | W | (22, 2) | (0, 2) |
| residue_R05_R06 | R05 | E | R06 | W | (26, 2) | (0, 2) |
| residue_R06_R07 | R06 | E | R07 | W | (32, 5) | (0, 3) |
| residue_R07_R08 | R07 | E | R08 | W | (30, 8) | (2, 2) |
| residue_R08_R09 | R08 | E | R09 | W | (22, 26) | (0, 3) |
| residue_R09_R10 | R09 | E | R10 | W | (32, 4) | (0, 3) |
| residue_R10_R11 | R10 | E | R11 | W | (24, 7) | (0, 3) |
| residue_R11_R12 | R11 | E | R12 | W | (28, 4) | (0, 3) |
| residue_R04_S1 | R04 | D | S1 | U | (8, 6) | (8, 14) |
| residue_R06_S2 | R06 | D | S2 | U | (20, 1) | (20, 18) |
| residue_R11_S3 | R11 | U | S3 | D | (14, 10) | (14, 0) |

링크 14개 → 문 28개.

`R03_R04`의 `fromAnchor.y = 1`과 `toAnchor.y = 20`처럼 두 앵커의 높이가 다른 것은
정상이다. 지금은 두 방이 한 월드에 놓여 있어 전역 높이가 같아야 했지만(빌더가 불일치를
경고로 잡는다), 방이 분리되면 각자 자기 로컬 좌표를 쓰므로 그 제약이 사라진다.

## 5. 컴포넌트

### 5.1 RoomDoor — 방 씬

```csharp
public class RoomDoor : MonoBehaviour
{
    public string doorId;        // "residue_R01_R02:E"
    public Side   side;
    public string targetRoom;    // "R02"
    public string targetDoorId;  // "residue_R01_R02:W"
    public Vector2 arrivalOffset;
}
```

`Collider2D`(트리거)를 요구한다. 플레이어가 닿으면 `RoomLoader`에 전환을 요청하고 그
이상은 하지 않는다. 로드도 배치도 문의 일이 아니다.

문은 자기가 어느 방에 있는지 모른다. 그건 자기가 속한 씬이 이미 답이다.

### 5.2 RoomLoader — 셸 씬

전환 전체를 소유하는 유일한 지점이다.

```csharp
public class RoomLoader : MonoBehaviour
{
    public static RoomLoader Instance { get; }

    public string CurrentRoom { get; }
    public bool   IsTransitioning { get; }

    public event Action<string> RoomLoaded;

    public void RequestTransition(RoomDoor from);
    public Coroutine LoadRoom(string roomName, string arriveAtDoorId);
}
```

`LoadRoom`은 테스트와 첫 진입이 함께 쓴다. 지역에 처음 들어올 때는
`LoadRoom("R01", null)`로 시작하고, `arriveAtDoorId`가 비면 방 씬이 들고 있는 기본
시작점에 놓는다.

### 5.3 RoomStart — 방 씬

```csharp
public class RoomStart : MonoBehaviour { }
```

문을 거치지 않고 그 방에 들어올 때의 위치. 각 방 씬에 하나. 첫 진입(R01), 체크포인트
복귀, 테스트에서 특정 방을 바로 띄울 때 쓴다. 위치만 갖는 마커라 필드가 없다.

## 6. 전환 흐름

### 6.1 순서

1. 플레이어가 `RoomDoor` 트리거에 진입한다.
2. `RoomLoader`가 `IsTransitioning`을 세우고 플레이어 입력을 잠근다.
3. `ScreenFader`로 0.2초 암전한다.
4. 현재 방 씬을 언로드하고 대상 방 씬을 `LoadSceneMode.Additive`로 로드한다.
5. 대상 씬에서 `targetDoorId`와 일치하는 `RoomDoor`를 찾아 플레이어를 그 앵커 +
   `arrivalOffset`으로 옮긴다.
6. `RoomCamera.SetRoom()` + `SnapToPlayer()`.
7. 0.2초 밝아지고 입력을 푼다.
8. 1.5초 동안 적의 선제공격을 막는다 (`LEVEL_01_STANDARD.md` §1.2 진입 보호).

암전 총 0.4초에 로드 시간이 더해진다. 방 하나가 작아 로드는 짧을 것으로 보지만, 실제
측정 뒤 암전 길이를 조정한다.

### 6.2 진행 방향 유지

문을 통과한 뒤 플레이어는 **걷던 방향을 유지한다**. 동쪽 문으로 나가면 다음 방에서
동쪽을 보고 서 있는다. 방향이 뒤집히면 연속으로 방을 지날 때 매번 방향을 다시 잡아야 해서
재돌파가 답답해진다.

수직 문(`U`/`D`)은 좌우 방향을 건드리지 않고 그대로 둔다.

### 6.3 도착 오프셋

앵커는 문의 중심이라 그 자리에 그대로 놓으면 문 안에 낀다. 방향별 기본값:

| side | arrivalOffset |
| --- | --- |
| W | (+1.5, 0) |
| E | (-1.5, 0) |
| U | (0, -1.5) |
| D | (0, +1.5) |

방으로 **들어가는 쪽**으로 밀어낸다. 빌더가 `toSide`를 보고 기본값을 넣고, 필요하면 방별로
덮어쓴다.

대각(`NW`/`NE`/`SW`/`SE`)과 비밀 연결(`S`)은 잔재 링크 14개에 등장하지 않는다. 기본값은
가로 성분만 적용하고(`NW`/`SW`는 W와, `NE`/`SE`는 E와 같게), 실제로 쓰는 지역이 나오면
그때 정한다. `S`는 방향이 아니라 연결의 성격이라 오프셋을 갖지 않으며, 반드시 방별로
지정해야 한다.

### 6.4 되튕김 방지

도착한 문은 플레이어가 그 위에 서 있는 상태로 시작한다. 그대로 두면 트리거가 즉시 다시
발동해 왔던 방으로 돌아간다. `ShortcutPassage`가 이미 같은 문제를 `_nextUseTime` 정적
쿨다운으로 막고 있다.

여기서는 더 확실한 방법을 쓴다. 도착한 문은 **플레이어가 자기 트리거에서 한 번 벗어날
때까지 비활성**이다 (`OnTriggerExit2D`에서 다시 켠다). 시간이 아니라 상태에 기반하므로
로드가 느려도 안전하고, 문 앞에 서서 머뭇거려도 튕기지 않는다.

## 7. 에러 처리

전부 조용히 실패하지 않고 로그를 남긴다. 맵 데이터 오류는 QA 중에 즉시 드러나야 한다.

| 상황 | 처리 |
| --- | --- |
| `targetRoom` 씬이 빌드 세팅에 없다 | `LogError`, 전환 취소, 입력 복구. 플레이어는 원래 방에 남는다 |
| 대상 씬에 `targetDoorId`가 없다 | `LogError`, 그 방의 `RoomStart`로 대신 배치 |
| 대상 씬에 `RoomStart`도 없다 | `LogError`, `(0, 0)` 배치 — 최소한 게임은 계속된다 |
| 전환 중에 다른 문이 발동 | `IsTransitioning`이 서 있으면 무시 |
| 링크 테이블에 짝 없는 문 | **빌드 시점**에 잡는다 (§8.1) |

전환이 어떤 이유로든 실패하면 입력 잠금과 암전은 반드시 되돌린다. 검은 화면에서 조작이
막힌 채 멈추는 것이 최악이다.

## 8. 테스트

### 8.1 EditMode — 링크 테이블 검증

빌드 전에 데이터만으로 잡을 수 있는 것들.

- 모든 `linkId`가 유일하다.
- 모든 `fromRoom`/`toRoom`이 실제 방 이름이다.
- 링크마다 문 두 개가 짝으로 생성되고, 서로를 `targetDoorId`로 가리킨다.
- 방향이 마주 본다 (`E`↔`W`, `U`↔`D`). 어긋나면 실패.
- 잔재 링크가 정확히 14개다.

### 8.2 PlayMode — 전환 동작

- R01에서 동쪽 문에 닿으면 R02가 로드되고 R01이 언로드된다.
- R02의 서쪽 문으로 되돌아가면 R01로 돌아오고, 출발했던 문 근처에 선다.
- 왕복해도 되튕기지 않는다 (§6.4).
- 전환 뒤 `RoomCamera.CurrentRoom`이 새 방을 가리킨다.
- 없는 방을 요청하면 플레이어가 원래 방에 남고 입력이 복구된다.
- R01부터 R12까지 문만 따라 완주한다.

### 8.3 기존 테스트 마이그레이션

`Zone_*_Full` 씬을 `LoadSceneMode.Single`로 띄우고 오브젝트를 찾는 PlayMode 테스트가
17개 파일, 69개 있다. 방이 셸에서 빠지면 전부 아무것도 찾지 못한다.

테스트 헬퍼를 하나 만들어 기계적으로 옮긴다.

```csharp
// 셸을 띄우고 지정한 방을 로드한다. RoomLoader.LoadRoom과 이름이 겹치지 않게 둔다.
public static IEnumerator EnterRoom(string zone, string room);
```

잔재 테스트(`ResidueZoneTests`, `ResiduePlacementTests`, `ResidueArtTests`,
`ResidueVerificationTests`, `ResidueCompletionArtTests`, `ResidueLoopCompletionTests`)를
1단계에서 옮긴다. 지역 무관 테스트(`JumpSanityTests`, `AttackSanityTests`,
`HangTimeSanityTests` 등)는 잔재 R01을 기본 무대로 쓰게 바꾼다.

응시·균열 테스트는 2단계에서 같이 옮긴다. 그때까지는 `Zone_Gaze_Full`,
`Zone_Fracture_Full`이 남아 있으므로 계속 통과한다.

## 9. 2단계 — 문 잠금 (이번 구현 범위 아님)

QA가 전 구간을 왕복해야 해서 1단계 문에는 잠금이 없다. 아래는 나중에 붙일 설계다.

`RoomDoor`에 조건 필드를 더한다. 비어 있으면 지금처럼 항상 열린다.

```csharp
public EmotionId requiredSkill;      // None이면 조건 없음
public string    requiredShortcutId; // 비면 조건 없음
public string    requiredEncounterId;// 비면 조건 없음
public bool      oneWayUntilOpened;  // 반대편에서만 열리는 숏컷 문
```

세 조건은 이미 코드에 대응물이 있다 — `Gate`가 `EmotionId`를, `Shortcut`이 개방 상태를,
`ZoneTrigger`가 `requiredEncounterId`를 본다. 새 개념을 만들지 않고 문이 그것들을
읽기만 하면 된다.

`oneWayUntilOpened`는 할로우 나이트식 숏컷 문이다. 처음에는 반대편에서만 열 수 있고, 한 번
열면 `ProgressState`에 기록돼 다음 방문에도 열려 있다.

**닫힌 문은 보이되 지나갈 수 없어야 한다.** 조건을 못 채운 문은 사라지지 않는다. 길이 있다는
것과 지금은 못 간다는 것을 함께 읽어야 나중에 돌아올 이유가 생긴다
(`GAME_DESIGN.md`의 랜드마크 원칙).

### 9.1 지금 있는 잠금 하나

S3 비밀방 입구에는 이미 잠금이 있다. `ResidueZoneBuilder.BuildConnections()` 마지막 줄의
`BuildGate(..., EmotionId.Rewind, true)`이며, "균열 클리어 후 잔재 재방문"
(`LEVEL_00_INDEX.md` §0)을 성립시키는 장치다.

이건 문의 파라미터가 아니라 **문 앞에 서 있는 별개의 블로커**다. 1단계에서 `RoomDoor`에
잠금을 넣지 않아도 `Gate` 오브젝트를 R11 씬의 S3 문 앞에 그대로 두면 보존된다. 없애면
안 된다.

## 10. 단계

| 단계 | 내용 | 끝나면 확인되는 것 |
| --- | --- | --- |
| 1 | `Side`, `RoomLink`, `RoomLinkTable` + EditMode 검증 | 데이터가 스스로 모순을 잡는다 |
| 2 | `RoomDoor`, `RoomStart`, `RoomLoader` | 전환 로직이 씬 없이 단위 검증된다 |
| 3 | 잔재 빌더를 방별 씬 출력으로 전환, 복도 코드 삭제 | 방 15개 씬이 생긴다 |
| 4 | 잔재 테스트 마이그레이션 | 기존 검증이 새 구조에서 산다 |
| 5 | 손으로 R01→R12 왕복 QA | 실제로 게임이 된다 |

2단계(응시·균열)와 3단계(문 잠금)는 별도 계획으로 다룬다.

## 11. 문서 갱신

- `LEVEL_01_STANDARD.md` §1.3에 이 문서를 가리키는 줄을 넣는다. 방향 표기가 이제
  `Side` enum으로 코드에 존재한다는 사실을 맵 문서 쪽에서도 알 수 있어야 한다.
- `PROJECT_STRUCTURE.md`에 방 씬 명명 규칙(`Room_<지역>_<방>.unity`)을 추가한다.
