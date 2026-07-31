# 잔재 방 포탈 씬 분리 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 잔재 지역의 방 15개를 각각 독립 씬으로 분리하고, 방 사이 복도를 양방향 포탈 문으로 대체한다.

**Architecture:** `Zone_Residue.unity`가 플레이어·카메라·HUD·`RoomLoader`만 갖는 셸이 되고, 방마다 자기 로컬 `(0,0)` 기준의 씬을 갖는다. 동시에 한 방만 additive 로드한다. 연결은 `RoomLinkTable` ScriptableObject 하나가 소유하며, 링크 하나가 양쪽 방에 문 두 개를 굽는다.

**Tech Stack:** Unity 6000.5.4f1, C#, NUnit (Unity Test Framework), `EditorSceneManager` 기반 씬 생성.

## Global Constraints

- 설계 원본: `docs/superpowers/specs/2026-07-31-room-portal-scenes-design.md`
- Unity 에디터 경로: `/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity`
- 프로젝트 경로: `/Users/ksh/Desktop/NHN HACKERton/HiddenWeight`
- **문에 잠금 조건을 넣지 않는다.** QA가 전 구간을 자유롭게 왕복해야 한다.
- `Side` enum 이름은 `LEVEL_01_STANDARD.md` §1.3의 표기와 정확히 같아야 한다: `W, E, NW, NE, SW, SE, U, D, S`
- 방 씬 이름 규칙: `Room_Residue_<방이름>.unity` (예: `Room_Residue_R01.unity`)
- doorId 형식: `<linkId>:<side>` (예: `residue_R01_R02:E`)
- 새 코드는 기존 파일과 같은 스타일을 따른다 — 주석은 한국어, "왜"를 적고 "무엇"은 적지 않는다.
- 커밋 메시지 본문은 한국어, 제목은 Conventional Commits 영어 접두사.

### 테스트 실행 명령

EditMode:

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && "/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -runTests -projectPath "$PWD/HiddenWeight" -testPlatform EditMode -testResults "$PWD/.unity-logs/tests-results.xml" -logFile "$PWD/.unity-logs/tests.log"; echo "exit=$?"
```

PlayMode (`-testPlatform PlayMode`로 바꾼다):

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && "/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -runTests -projectPath "$PWD/HiddenWeight" -testPlatform PlayMode -testResults "$PWD/.unity-logs/play-results.xml" -logFile "$PWD/.unity-logs/play.log"; echo "exit=$?"
```

결과 확인:

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && head -3 .unity-logs/tests-results.xml | grep -oE '(total|passed|failed)="[0-9]+"' | head -3; grep -oE 'methodname="[^"]*"[^>]*result="Failed"' .unity-logs/tests-results.xml | head
```

**중요:** Unity batchmode는 실행 중 `.unity-logs is not a valid directory name` 같은 무해한 경고를 stderr로 뱉는다. 무시하고 결과 XML만 본다.

---

## 파일 구조

| 파일 | 책임 |
| --- | --- |
| `Assets/Scripts/Data/RoomLink.cs` | `Side` enum, `RoomLink` 구조체, doorId 조립 규칙 |
| `Assets/Scripts/Data/RoomLinkTable.cs` | 지역 하나의 링크 목록을 담는 ScriptableObject |
| `Assets/Scripts/World/RoomDoor.cs` | 방 씬의 문. 트리거에 닿으면 `RoomLoader`에 요청만 한다 |
| `Assets/Scripts/World/RoomStart.cs` | 문을 거치지 않고 방에 들어올 때의 위치 마커 |
| `Assets/Scripts/World/RoomLoader.cs` | 셸에 상주. 언로드·로드·배치·암전을 소유하는 유일한 지점 |
| `Assets/Scripts/Editor/ResidueRoomLinks.cs` | 잔재 링크 14개 원본 + 에셋 생성 메뉴 |
| `Assets/Scripts/Editor/ResidueZoneBuilder.cs` | (수정) 방별 씬 출력, 복도 코드 삭제 |
| `Assets/Tests/EditMode/RoomLinkTableTests.cs` | 링크 데이터 정합성 |
| `Assets/Tests/EditMode/RoomDoorTests.cs` | doorId 조립, 문 무장 상태 |
| `Assets/Tests/PlayMode/RoomTransitionTests.cs` | 실제 전환 동작 |
| `Assets/Tests/PlayMode/RoomTestHarness.cs` | `EnterRoom(zone, room)` 헬퍼 |

---

## Task 1: 링크 데이터 모델

**Files:**
- Create: `HiddenWeight/Assets/Scripts/Data/RoomLink.cs`
- Create: `HiddenWeight/Assets/Scripts/Data/RoomLinkTable.cs`
- Test: `HiddenWeight/Assets/Tests/EditMode/RoomLinkTableTests.cs`

**Interfaces:**
- Consumes: 없음 (첫 태스크)
- Produces:
  - `HiddenWeight.Data.Side` — enum `{ W, E, NW, NE, SW, SE, U, D, S }`
  - `HiddenWeight.Data.RoomLink` — `struct`, 필드 `linkId, fromRoom, toRoom, fromSide, toSide, fromAnchor, toAnchor`
  - `RoomLink.DoorId(string linkId, Side side)` → `string` (`"<linkId>:<side>"`)
  - `RoomLink.Opposite(Side side)` → `Side`
  - `HiddenWeight.Data.RoomLinkTable` — `ScriptableObject`, 필드 `zone` (`ZoneId`), `links` (`RoomLink[]`)
  - `RoomLinkTable.FromDoorId(string doorId)` → `string linkId`

- [ ] **Step 1: 실패하는 테스트를 쓴다**

`HiddenWeight/Assets/Tests/EditMode/RoomLinkTableTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.Tests
{
    public class RoomLinkTableTests
    {
        [Test]
        public void DoorId_JoinsLinkIdAndSide()
        {
            Assert.That(RoomLink.DoorId("residue_R01_R02", Side.E), Is.EqualTo("residue_R01_R02:E"));
        }

        [Test]
        public void Opposite_PairsHorizontalAndVertical()
        {
            Assert.That(RoomLink.Opposite(Side.W), Is.EqualTo(Side.E));
            Assert.That(RoomLink.Opposite(Side.E), Is.EqualTo(Side.W));
            Assert.That(RoomLink.Opposite(Side.U), Is.EqualTo(Side.D));
            Assert.That(RoomLink.Opposite(Side.D), Is.EqualTo(Side.U));
            Assert.That(RoomLink.Opposite(Side.NW), Is.EqualTo(Side.SE));
            Assert.That(RoomLink.Opposite(Side.NE), Is.EqualTo(Side.SW));
        }

        // 비밀 연결은 방향이 아니라 연결의 성격이라 마주 보는 짝이 없다.
        [Test]
        public void Opposite_ReturnsSecretUnchanged()
        {
            Assert.That(RoomLink.Opposite(Side.S), Is.EqualTo(Side.S));
        }

        [Test]
        public void FromDoorId_ExtractsLinkId()
        {
            Assert.That(RoomLinkTable.FromDoorId("residue_R01_R02:E"), Is.EqualTo("residue_R01_R02"));
        }

        [Test]
        public void FromDoorId_ReturnsNullWhenMalformed()
        {
            Assert.That(RoomLinkTable.FromDoorId("residue_R01_R02"), Is.Null);
            Assert.That(RoomLinkTable.FromDoorId(null), Is.Null);
        }
    }
}
```

- [ ] **Step 2: 실패를 확인한다**

EditMode 테스트를 돌린다. 컴파일 에러(`Side`, `RoomLink`, `RoomLinkTable` 없음)로 실패해야 한다.

확인: `grep -c "error CS" .unity-logs/tests.log`가 0보다 크다.

- [ ] **Step 3: 최소 구현을 쓴다**

`HiddenWeight/Assets/Scripts/Data/RoomLink.cs`:

```csharp
using System;
using UnityEngine;

namespace HiddenWeight.Data
{
    // 방 출입구 방향. LEVEL_01_STANDARD.md 1.3의 표기를 그대로 옮긴 것이라
    // 맵 문서에 E라고 적힌 출구는 코드에서도 Side.E다. 이름을 바꾸면 문서와 어긋난다.
    public enum Side { W, E, NW, NE, SW, SE, U, D, S }

    // 방 두 개를 잇는 연결 하나. 빌더가 이걸 읽어 양쪽 방에 문을 하나씩 굽는다.
    [Serializable]
    public struct RoomLink
    {
        public string linkId;
        public string fromRoom;
        public string toRoom;
        public Side fromSide;
        public Side toSide;

        // 문의 중심 좌표(LEVEL_01_STANDARD.md 1.1). 각 방의 로컬 좌표다.
        public Vector2 fromAnchor;
        public Vector2 toAnchor;

        public static string DoorId(string linkId, Side side) => linkId + ":" + side;

        public string FromDoorId => DoorId(linkId, fromSide);
        public string ToDoorId => DoorId(linkId, toSide);

        public static Side Opposite(Side side) => side switch
        {
            Side.W => Side.E,
            Side.E => Side.W,
            Side.U => Side.D,
            Side.D => Side.U,
            Side.NW => Side.SE,
            Side.SE => Side.NW,
            Side.NE => Side.SW,
            Side.SW => Side.NE,
            _ => Side.S, // 비밀 연결은 마주 보는 방향이 없다
        };
    }
}
```

`HiddenWeight/Assets/Scripts/Data/RoomLinkTable.cs`:

```csharp
using UnityEngine;

namespace HiddenWeight.Data
{
    // 지역 하나의 방 연결 목록. 링크 하나가 문 두 개를 만들기 때문에
    // 되돌아올 수 없는 연결을 만들려면 이 데이터를 일부러 망가뜨려야 한다.
    [CreateAssetMenu(fileName = "RoomLinks", menuName = "HiddenWeight/Room Link Table")]
    public class RoomLinkTable : ScriptableObject
    {
        public ZoneId zone;
        public RoomLink[] links;

        public static string FromDoorId(string doorId)
        {
            if (string.IsNullOrEmpty(doorId)) return null;
            int colon = doorId.LastIndexOf(':');
            return colon <= 0 ? null : doorId.Substring(0, colon);
        }

        public bool TryFind(string linkId, out RoomLink link)
        {
            foreach (var candidate in links)
            {
                if (candidate.linkId != linkId) continue;
                link = candidate;
                return true;
            }

            link = default;
            return false;
        }
    }
}
```

- [ ] **Step 4: 통과를 확인한다**

EditMode 테스트를 돌린다. 컴파일 에러 0건, `RoomLinkTableTests` 5개 통과.

- [ ] **Step 5: 커밋**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && git add HiddenWeight/Assets/Scripts/Data/RoomLink.cs HiddenWeight/Assets/Scripts/Data/RoomLinkTable.cs HiddenWeight/Assets/Tests/EditMode/RoomLinkTableTests.cs && git add -A HiddenWeight/Assets/Scripts/Data HiddenWeight/Assets/Tests/EditMode && git commit -m "feat(world): add room link data model

방 연결을 런타임이 읽을 수 있는 데이터로 만든다. LEVEL_01_STANDARD의
방향 표기를 Side enum으로 옮겨 문서와 코드가 같은 단어를 쓰게 한다.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 2: 잔재 링크 테이블 에셋

**Files:**
- Create: `HiddenWeight/Assets/Scripts/Editor/ResidueRoomLinks.cs`
- Test: `HiddenWeight/Assets/Tests/EditMode/ResidueRoomLinkTests.cs`

**Interfaces:**
- Consumes: `RoomLink`, `RoomLinkTable`, `Side` (Task 1)
- Produces:
  - `HiddenWeight.EditorTools.ResidueRoomLinks.Links` → `RoomLink[]` (14개)
  - `HiddenWeight.EditorTools.ResidueRoomLinks.BuildAsset()` — `Assets/ScriptableObjects/RoomLinks_Residue.asset` 생성/갱신
  - 메뉴: `Hidden Weight/Build Residue Room Links`
  - 방 이름 목록 `ResidueRoomLinks.RoomNames` → `string[]` (15개)

- [ ] **Step 1: 실패하는 테스트를 쓴다**

`HiddenWeight/Assets/Tests/EditMode/ResidueRoomLinkTests.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using HiddenWeight.Data;
using HiddenWeight.EditorTools;

namespace HiddenWeight.Tests
{
    public class ResidueRoomLinkTests
    {
        [Test]
        public void HasFourteenLinks()
        {
            Assert.That(ResidueRoomLinks.Links.Length, Is.EqualTo(14));
        }

        [Test]
        public void HasFifteenRooms()
        {
            Assert.That(ResidueRoomLinks.RoomNames.Length, Is.EqualTo(15));
        }

        [Test]
        public void LinkIdsAreUnique()
        {
            var ids = ResidueRoomLinks.Links.Select(l => l.linkId).ToArray();
            Assert.That(ids, Is.Unique);
        }

        [Test]
        public void EverySideFacesItsPair()
        {
            foreach (var link in ResidueRoomLinks.Links)
                Assert.That(link.toSide, Is.EqualTo(RoomLink.Opposite(link.fromSide)),
                    link.linkId + " 의 두 방향이 마주 보지 않는다.");
        }

        [Test]
        public void EveryRoomNameIsKnown()
        {
            var known = new HashSet<string>(ResidueRoomLinks.RoomNames);
            foreach (var link in ResidueRoomLinks.Links)
            {
                Assert.That(known, Does.Contain(link.fromRoom), link.linkId + " fromRoom");
                Assert.That(known, Does.Contain(link.toRoom), link.linkId + " toRoom");
            }
        }

        // 문 28개가 서로 다른 id를 가져야 대상 문을 유일하게 찾을 수 있다.
        [Test]
        public void EveryDoorIdIsUnique()
        {
            var doorIds = ResidueRoomLinks.Links
                .SelectMany(l => new[] { l.FromDoorId, l.ToDoorId })
                .ToArray();

            Assert.That(doorIds.Length, Is.EqualTo(28));
            Assert.That(doorIds, Is.Unique);
        }

        // 주 동선 12방이 R01부터 R12까지 끊기지 않아야 완주가 가능하다.
        [Test]
        public void MainRouteConnectsR01ToR12()
        {
            var byFrom = ResidueRoomLinks.Links.ToLookup(l => l.fromRoom);
            string current = "R01";

            for (int i = 1; i < 12; i++)
            {
                string expected = "R" + (i + 1).ToString("00");
                var link = byFrom[current].FirstOrDefault(l => l.toRoom == expected);
                Assert.That(link.linkId, Is.Not.Null, current + " 에서 " + expected + " 로 가는 링크가 없다.");
                current = expected;
            }

            Assert.That(current, Is.EqualTo("R12"));
        }
    }
}
```

- [ ] **Step 2: 실패를 확인한다**

EditMode 테스트를 돌린다. `ResidueRoomLinks` 없음으로 컴파일 실패해야 한다.

- [ ] **Step 3: 최소 구현을 쓴다**

`HiddenWeight/Assets/Scripts/Editor/ResidueRoomLinks.cs`:

```csharp
using UnityEditor;
using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.EditorTools
{
    // 잔재 방 연결의 원본. 좌표는 예전 ResidueZoneBuilder.BuildConnections()의 links
    // 배열과 샤프트 3개에서 그대로 옮겨 왔다 — 복도를 문으로 바꾸는 것이지 동선을
    // 다시 설계하는 것이 아니다.
    public static class ResidueRoomLinks
    {
        const string AssetPath = "Assets/ScriptableObjects/RoomLinks_Residue.asset";

        public static readonly string[] RoomNames =
        {
            "R01", "R02", "R03", "R04", "R05", "R06", "R07", "R08",
            "R09", "R10", "R11", "R12", "S1", "S2", "S3"
        };

        public static readonly RoomLink[] Links =
        {
            Link("residue_R01_R02", "R01", Side.E, new Vector2(26, 2), "R02", new Vector2(0, 2)),
            Link("residue_R02_R03", "R02", Side.E, new Vector2(28, 3), "R03", new Vector2(0, 2)),
            Link("residue_R03_R04", "R03", Side.E, new Vector2(27, 1), "R04", new Vector2(2, 20)),
            Link("residue_R04_R05", "R04", Side.E, new Vector2(22, 2), "R05", new Vector2(0, 2)),
            Link("residue_R05_R06", "R05", Side.E, new Vector2(26, 2), "R06", new Vector2(0, 2)),
            Link("residue_R06_R07", "R06", Side.E, new Vector2(32, 5), "R07", new Vector2(0, 3)),
            Link("residue_R07_R08", "R07", Side.E, new Vector2(30, 8), "R08", new Vector2(2, 2)),
            Link("residue_R08_R09", "R08", Side.E, new Vector2(22, 26), "R09", new Vector2(0, 3)),
            Link("residue_R09_R10", "R09", Side.E, new Vector2(32, 4), "R10", new Vector2(0, 3)),
            Link("residue_R10_R11", "R10", Side.E, new Vector2(24, 7), "R11", new Vector2(0, 3)),
            Link("residue_R11_R12", "R11", Side.E, new Vector2(28, 4), "R12", new Vector2(0, 3)),

            // 비밀방 3곳. 예전에는 수직 샤프트였다.
            Link("residue_R04_S1", "R04", Side.D, new Vector2(8, 6), "S1", new Vector2(8, 14)),
            Link("residue_R06_S2", "R06", Side.D, new Vector2(20, 1), "S2", new Vector2(20, 18)),
            Link("residue_R11_S3", "R11", Side.U, new Vector2(14, 10), "S3", new Vector2(14, 0)),
        };

        static RoomLink Link(string id, string from, Side fromSide, Vector2 fromAnchor,
            string to, Vector2 toAnchor) => new RoomLink
            {
                linkId = id,
                fromRoom = from,
                toRoom = to,
                fromSide = fromSide,
                toSide = RoomLink.Opposite(fromSide),
                fromAnchor = fromAnchor,
                toAnchor = toAnchor,
            };

        [MenuItem("Hidden Weight/Build Residue Room Links")]
        public static void BuildAsset()
        {
            var table = AssetDatabase.LoadAssetAtPath<RoomLinkTable>(AssetPath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<RoomLinkTable>();
                AssetDatabase.CreateAsset(table, AssetPath);
            }

            table.zone = ZoneId.Residue;
            table.links = Links;
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ResidueRoomLinks] 링크 {Links.Length}개를 {AssetPath} 에 저장했다.");
        }
    }
}
```

- [ ] **Step 4: 통과를 확인한다**

EditMode 테스트를 돌린다. `ResidueRoomLinkTests` 7개 통과.

- [ ] **Step 5: 에셋을 생성한다**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && "/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" -logFile "$PWD/.unity-logs/links.log" -executeMethod HiddenWeight.EditorTools.ResidueRoomLinks.BuildAsset; echo "exit=$?"; ls -l HiddenWeight/Assets/ScriptableObjects/RoomLinks_Residue.asset
```

기대: 에셋 파일이 존재한다.

- [ ] **Step 6: 커밋**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && git add -A HiddenWeight/Assets/Scripts/Editor HiddenWeight/Assets/Tests/EditMode HiddenWeight/Assets/ScriptableObjects && git commit -m "feat(world): define residue room links

기존 BuildConnections의 복도 테이블과 샤프트 3개를 좌표 그대로 링크
14개로 옮긴다. 동선을 바꾸지 않고 표현만 데이터로 승격한 것이다.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 3: 문과 시작점 컴포넌트

**Files:**
- Create: `HiddenWeight/Assets/Scripts/World/RoomDoor.cs`
- Create: `HiddenWeight/Assets/Scripts/World/RoomStart.cs`
- Test: `HiddenWeight/Assets/Tests/EditMode/RoomDoorTests.cs`

**Interfaces:**
- Consumes: `Side`, `RoomLink` (Task 1), `PlayerLayers.IsPlayer(GameObject)` (기존)
- Produces:
  - `HiddenWeight.World.RoomDoor` — 필드 `doorId, side, targetRoom, targetDoorId, arrivalOffset`
  - `RoomDoor.Configure(string doorId, Side side, string targetRoom, string targetDoorId, Vector2 arrivalOffset)`
  - `RoomDoor.ArrivalPosition` → `Vector2` (`transform.position + arrivalOffset`)
  - `RoomDoor.Armed` → `bool`, `RoomDoor.Disarm()`
  - `RoomDoor.DefaultArrivalOffset(Side side)` → `Vector2`
  - `HiddenWeight.World.RoomStart` — 필드 없음

- [ ] **Step 1: 실패하는 테스트를 쓴다**

`HiddenWeight/Assets/Tests/EditMode/RoomDoorTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    public class RoomDoorTests
    {
        GameObject _go;
        RoomDoor _door;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Door", typeof(BoxCollider2D));
            _door = _go.AddComponent<RoomDoor>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        // 방으로 들어가는 쪽으로 밀어내지 않으면 도착하자마자 문에 낀다.
        [Test]
        public void DefaultArrivalOffset_PushesIntoRoom()
        {
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.W), Is.EqualTo(new Vector2(1.5f, 0f)));
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.E), Is.EqualTo(new Vector2(-1.5f, 0f)));
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.U), Is.EqualTo(new Vector2(0f, -1.5f)));
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.D), Is.EqualTo(new Vector2(0f, 1.5f)));
        }

        // 대각은 가로 성분만 따른다. 실제로 쓰는 지역이 나오면 그때 다시 정한다.
        [Test]
        public void DefaultArrivalOffset_TreatsDiagonalsAsHorizontal()
        {
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.NW), Is.EqualTo(new Vector2(1.5f, 0f)));
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.SW), Is.EqualTo(new Vector2(1.5f, 0f)));
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.NE), Is.EqualTo(new Vector2(-1.5f, 0f)));
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.SE), Is.EqualTo(new Vector2(-1.5f, 0f)));
        }

        [Test]
        public void DefaultArrivalOffset_LeavesSecretAtZero()
        {
            Assert.That(RoomDoor.DefaultArrivalOffset(Side.S), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Configure_StoresLinkFields()
        {
            _door.Configure("residue_R01_R02:E", Side.E, "R02", "residue_R01_R02:W", new Vector2(-1.5f, 0f));

            Assert.That(_door.DoorId, Is.EqualTo("residue_R01_R02:E"));
            Assert.That(_door.Side, Is.EqualTo(Side.E));
            Assert.That(_door.TargetRoom, Is.EqualTo("R02"));
            Assert.That(_door.TargetDoorId, Is.EqualTo("residue_R01_R02:W"));
        }

        [Test]
        public void ArrivalPosition_AddsOffsetToTransform()
        {
            _go.transform.position = new Vector3(10f, 4f, 0f);
            _door.Configure("d", Side.W, "R02", "e", new Vector2(1.5f, 0f));

            Assert.That(_door.ArrivalPosition, Is.EqualTo(new Vector2(11.5f, 4f)));
        }

        // 도착한 문 위에 서 있는 상태로 시작하므로, 벗어나기 전까지 발동하면 안 된다.
        [Test]
        public void Door_StartsArmedAndCanBeDisarmed()
        {
            Assert.That(_door.Armed, Is.True);

            _door.Disarm();

            Assert.That(_door.Armed, Is.False);
        }

        [Test]
        public void Collider_IsTrigger()
        {
            _door.Configure("d", Side.E, "R02", "e", Vector2.zero);

            Assert.That(_go.GetComponent<BoxCollider2D>().isTrigger, Is.True);
        }
    }
}
```

- [ ] **Step 2: 실패를 확인한다**

EditMode 테스트를 돌린다. `RoomDoor` 없음으로 컴파일 실패해야 한다.

- [ ] **Step 3: 최소 구현을 쓴다**

`HiddenWeight/Assets/Scripts/World/RoomDoor.cs`:

```csharp
using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    // 방 씬에 놓이는 포탈 문. 트리거에 닿으면 RoomLoader에 전환을 요청하는 것이 전부다.
    // 씬을 로드하는 일도 플레이어를 옮기는 일도 문의 책임이 아니다 — 그건 RoomLoader가
    // 혼자 소유해야 전환 도중의 상태를 한곳에서 관리할 수 있다.
    [RequireComponent(typeof(Collider2D))]
    public class RoomDoor : MonoBehaviour
    {
        [SerializeField] string doorId;
        [SerializeField] Side side;
        [SerializeField] string targetRoom;
        [SerializeField] string targetDoorId;
        [SerializeField] Vector2 arrivalOffset;

        public string DoorId => doorId;
        public Side Side => side;
        public string TargetRoom => targetRoom;
        public string TargetDoorId => targetDoorId;
        public Vector2 ArrivalPosition => (Vector2)transform.position + arrivalOffset;

        // 도착한 문은 플레이어가 자기 트리거를 벗어날 때까지 발동하지 않는다.
        // 시간 쿨다운이 아니라 상태로 막아야 로드가 느려도, 문 앞에서 머뭇거려도 안전하다.
        public bool Armed { get; private set; } = true;

        public void Disarm() => Armed = false;

        public void Configure(string id, Side doorSide, string room, string targetDoor, Vector2 offset)
        {
            doorId = id;
            side = doorSide;
            targetRoom = room;
            targetDoorId = targetDoor;
            arrivalOffset = offset;

            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        public static Vector2 DefaultArrivalOffset(Side side) => side switch
        {
            Side.W or Side.NW or Side.SW => new Vector2(1.5f, 0f),
            Side.E or Side.NE or Side.SE => new Vector2(-1.5f, 0f),
            Side.U => new Vector2(0f, -1.5f),
            Side.D => new Vector2(0f, 1.5f),
            _ => Vector2.zero,
        };

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!Armed || !PlayerLayers.IsPlayer(other.gameObject)) return;
            RoomLoader.Instance?.RequestTransition(this);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;
            Armed = true;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.6f);
        }
    }
}
```

`HiddenWeight/Assets/Scripts/World/RoomStart.cs`:

```csharp
using UnityEngine;

namespace HiddenWeight.World
{
    // 문을 거치지 않고 이 방에 들어올 때 플레이어가 서는 자리.
    // 지역 첫 진입, 체크포인트 복귀, 테스트가 특정 방을 바로 띄울 때 쓴다.
    // 위치만 있으면 되므로 필드가 없다.
    public class RoomStart : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.8f, 1.4f, 0f));
        }
    }
}
```

- [ ] **Step 4: 통과를 확인한다**

EditMode 테스트를 돌린다. `RoomDoorTests` 7개 통과. `RoomLoader`는 아직 없지만 `RoomLoader.Instance?`는 다음 태스크에서 만들 때까지 컴파일되지 않는다 — **Task 4를 먼저 만들거나**, 임시로 그 줄을 주석 처리하지 말고 Task 4의 `RoomLoader` 골격을 이 태스크에서 함께 만든다.

> **구현자 주의:** `RoomDoor`가 `RoomLoader`를 참조하므로 순환을 피하려면 이 스텝에서
> `RoomLoader`의 빈 골격(아래)을 함께 만든다. 본 구현은 Task 4에서 채운다.
>
> ```csharp
> using UnityEngine;
>
> namespace HiddenWeight.World
> {
>     public class RoomLoader : MonoBehaviour
>     {
>         public static RoomLoader Instance { get; private set; }
>         void Awake() => Instance = this;
>         public void RequestTransition(RoomDoor from) { }
>     }
> }
> ```

- [ ] **Step 5: 커밋**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && git add -A HiddenWeight/Assets/Scripts/World HiddenWeight/Assets/Tests/EditMode && git commit -m "feat(world): add room door and start marker

문은 트리거 감지와 도착 좌표 계산만 맡는다. 되튕김은 시간 쿨다운이
아니라 트리거를 벗어났는지로 막아, 로드가 느려도 문 앞에서 머뭇거려도
안전하게 만든다.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 4: RoomLoader 전환 오케스트레이션

**Files:**
- Modify: `HiddenWeight/Assets/Scripts/World/RoomLoader.cs` (Task 3의 골격을 채운다)
- Test: `HiddenWeight/Assets/Tests/EditMode/RoomLoaderTests.cs`

**Interfaces:**
- Consumes: `RoomDoor` (Task 3), `PlayerController.TeleportTo(Vector3)`, `PlayerInput.Enabled`, `ScreenFader.Instance.FadeTo(float, float)`, `RoomCamera.Instance.SetRoom(Room)` / `SnapToPlayer()` (전부 기존)
- Produces:
  - `RoomLoader.Instance` → `RoomLoader`
  - `RoomLoader.CurrentRoom` → `string`
  - `RoomLoader.IsTransitioning` → `bool`
  - `RoomLoader.EntryProtectedUntil` → `float` (`Time.time` 기준)
  - `RoomLoader.RoomLoaded` → `event Action<string>`
  - `RoomLoader.RequestTransition(RoomDoor from)`
  - `RoomLoader.LoadRoom(string roomName, string arriveAtDoorId)` → `Coroutine`
  - `RoomLoader.SceneNameFor(string roomName)` → `string`

- [ ] **Step 1: 실패하는 테스트를 쓴다**

`HiddenWeight/Assets/Tests/EditMode/RoomLoaderTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    public class RoomLoaderTests
    {
        GameObject _root;
        RoomLoader _loader;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("RoomLoader");
            _loader = _root.AddComponent<RoomLoader>();
            _loader.ConfigureForTests("Room_Residue_");
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        [Test]
        public void SceneNameFor_PrefixesRoomName()
        {
            Assert.That(_loader.SceneNameFor("R01"), Is.EqualTo("Room_Residue_R01"));
            Assert.That(_loader.SceneNameFor("S3"), Is.EqualTo("Room_Residue_S3"));
        }

        [Test]
        public void StartsIdle()
        {
            Assert.That(_loader.IsTransitioning, Is.False);
            Assert.That(_loader.CurrentRoom, Is.Null);
        }

        // 전환 중 다른 문이 발동하면 두 전환이 겹쳐 플레이어가 사라진다.
        [Test]
        public void RequestTransition_IgnoredWhileTransitioning()
        {
            var doorGo = new GameObject("Door", typeof(BoxCollider2D));
            var door = doorGo.AddComponent<RoomDoor>();
            door.Configure("a:E", Side.E, "R02", "a:W", Vector2.zero);

            _loader.SetTransitioningForTests(true);
            _loader.RequestTransition(door);

            Assert.That(_loader.CurrentRoom, Is.Null);

            Object.DestroyImmediate(doorGo);
        }

        // 문을 통과하면 그 문은 무장 해제된다 — 돌아왔을 때 즉시 되튕기지 않게.
        [Test]
        public void RequestTransition_DisarmsSourceDoor()
        {
            var doorGo = new GameObject("Door", typeof(BoxCollider2D));
            var door = doorGo.AddComponent<RoomDoor>();
            door.Configure("a:E", Side.E, "R02", "a:W", Vector2.zero);

            _loader.RequestTransition(door);

            Assert.That(door.Armed, Is.False);

            Object.DestroyImmediate(doorGo);
        }
    }
}
```

- [ ] **Step 2: 실패를 확인한다**

EditMode 테스트를 돌린다. `ConfigureForTests`, `SetTransitioningForTests`, `SceneNameFor` 없음으로 컴파일 실패해야 한다.

- [ ] **Step 3: 구현을 쓴다**

`HiddenWeight/Assets/Scripts/World/RoomLoader.cs` 전체를 아래로 교체한다:

```csharp
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using HiddenWeight.Player;
using HiddenWeight.UI;

namespace HiddenWeight.World
{
    // 방 전환 전체를 소유하는 유일한 지점. 문은 요청만 하고, 언로드·로드·플레이어 배치·
    // 암전·입력 잠금은 전부 여기서 일어난다. 실패해도 반드시 입력과 화면을 되돌린다 —
    // 검은 화면에서 조작이 막힌 채 멈추는 것이 최악이다.
    public class RoomLoader : MonoBehaviour
    {
        [SerializeField] string scenePrefix = "Room_Residue_";
        [SerializeField] float fadeSeconds = 0.2f;

        // 전환 직후 적의 선제공격을 막는 시간(LEVEL_01_STANDARD.md 1.2 진입 보호).
        [SerializeField] float entryProtectionSeconds = 1.5f;

        public static RoomLoader Instance { get; private set; }

        public string CurrentRoom { get; private set; }
        public bool IsTransitioning { get; private set; }
        public float EntryProtectedUntil { get; private set; }

        public event Action<string> RoomLoaded;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public string SceneNameFor(string roomName) => scenePrefix + roomName;

        public void RequestTransition(RoomDoor from)
        {
            if (IsTransitioning || from == null) return;

            // 돌아왔을 때 이 문이 즉시 다시 발동하지 않도록 미리 무장을 푼다.
            from.Disarm();
            StartCoroutine(Transition(from.TargetRoom, from.TargetDoorId));
        }

        public Coroutine LoadRoom(string roomName, string arriveAtDoorId)
            => StartCoroutine(Transition(roomName, arriveAtDoorId));

        IEnumerator Transition(string roomName, string arriveAtDoorId)
        {
            if (IsTransitioning) yield break;

            IsTransitioning = true;
            bool inputWasEnabled = PlayerInput.Enabled;
            PlayerInput.Enabled = false;

            var fader = ScreenFader.Instance;
            if (fader != null) yield return fader.FadeTo(1f, fadeSeconds);

            string sceneName = SceneNameFor(roomName);
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[RoomLoader] 씬 {sceneName} 을 빌드 세팅에서 찾을 수 없다. 전환을 취소한다.");
                yield return Restore(fader, inputWasEnabled);
                yield break;
            }

            string previous = CurrentRoom;
            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            var loaded = SceneManager.GetSceneByName(sceneName);
            if (loaded.IsValid()) SceneManager.SetActiveScene(loaded);

            if (!string.IsNullOrEmpty(previous))
            {
                var old = SceneManager.GetSceneByName(SceneNameFor(previous));
                if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
            }

            CurrentRoom = roomName;
            PlacePlayer(loaded, roomName, arriveAtDoorId);
            SyncCamera(loaded);

            EntryProtectedUntil = Time.time + entryProtectionSeconds;
            RoomLoaded?.Invoke(roomName);

            yield return Restore(fader, inputWasEnabled);
        }

        IEnumerator Restore(ScreenFader fader, bool inputWasEnabled)
        {
            if (fader != null) yield return fader.FadeTo(0f, fadeSeconds);
            PlayerInput.Enabled = inputWasEnabled;
            IsTransitioning = false;
        }

        void PlacePlayer(Scene scene, string roomName, string arriveAtDoorId)
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            Vector2 target;

            if (!string.IsNullOrEmpty(arriveAtDoorId)
                && TryFindDoor(scene, arriveAtDoorId, out var door))
            {
                target = door.ArrivalPosition;
                // 도착한 문 위에 서 있으므로, 벗어나기 전까지 발동하면 안 된다.
                door.Disarm();
            }
            else
            {
                if (!string.IsNullOrEmpty(arriveAtDoorId))
                    Debug.LogError($"[RoomLoader] {roomName} 에서 문 {arriveAtDoorId} 을 찾지 못했다. RoomStart로 대신 배치한다.");

                var start = FindInScene<RoomStart>(scene);
                if (start != null)
                {
                    target = start.transform.position;
                }
                else
                {
                    Debug.LogError($"[RoomLoader] {roomName} 에 RoomStart가 없다. (0,0)에 배치한다.");
                    target = Vector2.zero;
                }
            }

            // 걷던 방향은 건드리지 않는다. 연속으로 방을 지날 때 방향이 뒤집히면
            // 매번 다시 잡아야 해서 재돌파가 답답해진다.
            player.TeleportTo(new Vector3(target.x, target.y, player.transform.position.z));
        }

        void SyncCamera(Scene scene)
        {
            var camera = RoomCamera.Instance;
            if (camera == null) return;

            var room = FindInScene<Room>(scene);
            if (room != null) camera.SetRoom(room);
            camera.SnapToPlayer();
        }

        static bool TryFindDoor(Scene scene, string doorId, out RoomDoor found)
        {
            found = null;
            if (!scene.IsValid()) return false;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var door in root.GetComponentsInChildren<RoomDoor>(true))
                {
                    if (door.DoorId != doorId) continue;
                    found = door;
                    return true;
                }
            }

            return false;
        }

        static T FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid()) return null;

            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }

            return null;
        }

        // --- 테스트 전용 ---

        public void ConfigureForTests(string prefix) => scenePrefix = prefix;

        public void SetTransitioningForTests(bool value) => IsTransitioning = value;
    }
}
```

- [ ] **Step 4: 통과를 확인한다**

EditMode 테스트를 돌린다. `RoomLoaderTests` 4개 통과, 컴파일 에러 0건.

- [ ] **Step 5: 커밋**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && git add -A HiddenWeight/Assets/Scripts/World HiddenWeight/Assets/Tests/EditMode && git commit -m "feat(world): orchestrate room transitions

전환의 모든 단계를 RoomLoader 하나가 소유한다. 씬을 못 찾거나 대상 문이
없어도 입력 잠금과 암전을 반드시 되돌려, 검은 화면에서 멈추는 상태를
만들지 않는다.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 5: 잔재 빌더를 방별 씬 출력으로 전환

**Files:**
- Modify: `HiddenWeight/Assets/Scripts/Editor/ResidueZoneBuilder.cs:742-790` (`BuildConnections` 삭제)
- Modify: `HiddenWeight/Assets/Scripts/Editor/ResidueZoneBuilder.cs:792-860` (`BuildResidueZone` → 방별 씬 + 셸)
- Modify: `HiddenWeight/Assets/Scripts/Editor/ZoneSceneBuilder.cs:1274` (`RegisterBuildSettings`에 방 씬 15개 추가)

**Interfaces:**
- Consumes: `ResidueRoomLinks.Links` / `.RoomNames` (Task 2), `RoomDoor.Configure` / `.DefaultArrivalOffset` (Task 3), `RoomLoader` (Task 4), 기존 `NewScene()` / `SaveScene(Scene, string)` / `RoomCtx`
- Produces:
  - `Room_Residue_R01` ~ `Room_Residue_S3` 씬 15개
  - `Zone_Residue` 셸 씬
  - 메뉴 `Hidden Weight/Build Residue Zone (Rooms)`

- [ ] **Step 1: 복도 코드를 삭제한다**

`ResidueZoneBuilder.cs`에서 다음을 지운다:

- `BuildConnections(RoomCtx c)` 전체 (742~790행). **단 마지막 `BuildGate` 호출은 Step 3에서 R11 씬으로 옮긴다 — 지우기 전에 그 줄을 따로 적어 둔다.**
- `BuildCorridor(...)` (593행 근처)
- `BuildShaft(...)` (606행 근처)
- 상수 `CorridorGap` (26행)
- 방별 전역 오프셋 상수 `R01`~`S3` (28~42행) — 방이 각자 `(0,0)`을 쓰므로 필요 없다

`BuildResidueZone()`에서 `BuildConnections(ctx)` 호출도 지운다.

- [ ] **Step 2: 방마다 자기 씬을 굽도록 바꾼다**

`BuildResidueZone()`을 아래로 교체한다. 기존 방 빌드 함수(`BuildR01`~`BuildS3`)는 그대로 두고, 호출하는 쪽만 바꾼다.

```csharp
[MenuItem("Hidden Weight/Build Residue Zone (Rooms)")]
public static void BuildResidueRooms()
{
    EnsureScenesFolder();

    var builders = new (string room, Action<RoomCtx> build)[]
    {
        ("R01", BuildR01), ("R02", BuildR02), ("R03", BuildR03), ("R04", BuildR04),
        ("S1",  BuildS1),  ("R05", BuildR05), ("R06", BuildR06), ("S2",  BuildS2),
        ("R07", BuildR07), ("R08", BuildR08), ("R09", BuildR09), ("R10", BuildR10),
        ("R11", BuildR11), ("S3",  BuildS3),  ("R12", BuildR12),
    };

    foreach (var (room, build) in builders)
    {
        var scene = NewScene();
        var ctx = NewRoomCtx();

        // 방마다 자기 로컬 (0,0)을 쓴다 — LEVEL_01_STANDARD 1.1.
        // 전역 오프셋이 사라졌으므로 방 빌드 함수 안의 c.O 대입은 전부 지워야 한다.
        ctx.O = Vector2Int.zero;
        build(ctx);

        BuildRoomStart(ctx);
        BuildDoorsFor(ctx, room);
        SaveScene(scene, "Room_Residue_" + room);
    }

    BuildResidueShell();
    RegisterBuildSettings();
    Debug.Log("[ResidueZoneBuilder] 방 씬 15개와 셸을 구웠다.");
}

// 링크 테이블을 훑어 이 방에 속한 문을 전부 세운다. 링크 하나가 양쪽에 문을
// 하나씩 만들기 때문에, 한쪽만 만들어 못 돌아오는 연결이 생길 수 없다.
static void BuildDoorsFor(RoomCtx c, string room)
{
    foreach (var link in ResidueRoomLinks.Links)
    {
        if (link.fromRoom == room)
            SpawnDoor(c, link.FromDoorId, link.fromSide, link.fromAnchor, link.toRoom, link.ToDoorId);

        if (link.toRoom == room)
            SpawnDoor(c, link.ToDoorId, link.toSide, link.toAnchor, link.fromRoom, link.FromDoorId);
    }
}

static void SpawnDoor(RoomCtx c, string doorId, Side side, Vector2 anchor,
    string targetRoom, string targetDoorId)
{
    var go = new GameObject("Door_" + doorId.Replace(':', '_'));
    go.transform.SetParent(c.Root.transform, false);
    go.transform.position = new Vector3(anchor.x, anchor.y, 0f);

    var col = go.AddComponent<BoxCollider2D>();
    col.isTrigger = true;
    col.size = side == Side.U || side == Side.D
        ? new Vector2(3f, 1.2f)
        : new Vector2(1.2f, 3.5f);

    var door = go.AddComponent<RoomDoor>();
    door.Configure(doorId, side, targetRoom, targetDoorId, RoomDoor.DefaultArrivalOffset(side));
}

static void BuildRoomStart(RoomCtx c)
{
    var go = new GameObject("RoomStart");
    go.transform.SetParent(c.Root.transform, false);
    go.transform.position = new Vector3(3f, 3f, 0f);
    go.AddComponent<RoomStart>();
}

// 셸에는 지형이 없다. 플레이어·카메라·HUD·RoomLoader만 두고 R01을 로드시킨다.
static void BuildResidueShell()
{
    var scene = NewScene();
    var root = new GameObject("Zone_Residue");

    // 씬이 어느 지역인지 직접 선언한다. GameManager가 이 표식으로 ZoneData를 고른다.
    var marker = new GameObject("ZoneMarker");
    marker.transform.SetParent(root.transform, false);
    var zoneMarker = marker.AddComponent<HiddenWeight.Core.ZoneMarker>();
    SetField(zoneMarker, "zone", p => p.enumValueIndex = (int)ZoneId.Residue);

    // 플레이어 시작 좌표는 의미가 없다. 진입점이 R01을 로드하면서 RoomStart로 옮긴다.
    PlacePlayerAndCamera(root, new Vector3(3f, 3f, 0f));

    // RoomLoader는 씬 루트로 남긴다. Zone 루트의 자식으로 붙이면 방 씬을 언로드할 때
    // 함께 사라질 위험이 있고, 싱글턴이 중간에 죽으면 전환이 통째로 멈춘다.
    var loaderGo = new GameObject("RoomLoader");
    loaderGo.AddComponent<RoomLoader>();
    loaderGo.AddComponent<ResidueEntryPoint>();

    SaveScene(scene, "Zone_Residue");
}
```

`NewRoomCtx()`는 기존 `BuildResidueZone()`(802~822행)의 ctx 생성부를 그대로 옮긴 것이다.
`FloorArt`는 `false`다 — 잔재는 4K 방 배경을 쓰므로 바닥 아트를 따로 깔지 않는다.

```csharp
static RoomCtx NewRoomCtx()
{
    var tilemap = BuildZoneRoot("Residue", out var root);

    var rooms = new GameObject("Rooms");
    rooms.transform.SetParent(root.transform, true);

    return new RoomCtx
    {
        Map = tilemap,
        Root = root,
        Rooms = rooms.transform,
        O = Vector2Int.zero,
        FloorArt = false,
    };
}

// 방 씬 하나를 마무리한다. 예전에는 지역 씬 끝에서 한 번만 하던 일들인데,
// 방이 씬으로 갈라졌으므로 방마다 해야 한다.
static void FinishRoomScene(RoomCtx c)
{
    // 충돌 연출과 공격체 발사대는 씬마다 하나씩 필요하다 —
    // 다른 씬에 있는 인스턴스는 이 방의 적이 쓸 수 없다.
    BuildImpactVFX(c.Root.transform, ResidueImpacts);
    BuildProjectileSpawner(c.Root.transform, ResidueProjectiles);

    // 숏컷은 방 빌드 함수가 만들어 정적 필드에 남긴다. 그 방이 만든 것만 붙인다.
    if (_shortcutA != null) AttachSealAnimator(_shortcutA, new Vector2(4f, 2f));
    if (_shortcutB != null) AttachSealAnimator(_shortcutB, new Vector2(3f, 2.5f));
    if (_shortcutC != null) AttachSealAnimator(_shortcutC, new Vector2(4f, 2f));
    _shortcutA = _shortcutB = _shortcutC = null;

    foreach (var room in Object.FindObjectsByType<Room>(FindObjectsSortMode.None))
        SingleRoomBackgroundBuilder.Build(room, "Assets/Art/Residue");

    ClotheCollisionPlaceholderRenderers(c.Root);
}
```

`BuildResidueRooms()`의 루프에서 `SaveScene` 직전에 `FinishRoomScene(ctx);`를 부르고,
루프 시작 전에 `UseArtRoot("Assets/Art/Residue");`를 한 번 부른다. 루프가 끝나면
`AssetDatabase.SaveAssets();`를 부른다.

- [ ] **Step 3: 방 빌드 함수에서 전역 오프셋 대입을 지운다**

`BuildR01`~`BuildS3` 각 함수 첫 줄의 `c.O = R01;` 같은 대입을 전부 삭제한다. 방이 자기
로컬 좌표를 쓰므로 `RoomCtx.O`는 `(0,0)`으로 남아야 한다.

`BuildR11`에는 S3 게이트를 추가한다. 예전 `BuildConnections` 마지막 줄에 있던
`BuildGate(parent, new Vector2(R11.x + 14, R11.y + 11), EmotionId.Rewind, true)`를 R11
로컬 좌표로 옮긴 것이다. **이 게이트가 "균열 클리어 후 잔재 재방문"을 성립시키므로
없애면 안 된다** (`LEVEL_00_INDEX.md` §0).

`BuildR11` 끝부분에 넣는다:

```csharp
// S3 비밀방 문 앞을 막는 되감기 게이트. 문의 파라미터가 아니라 별개 블로커라
// RoomDoor에 잠금이 없어도 그대로 산다(설계 9.1).
BuildGate(c.Root.transform, c.P(14f, 11f), EmotionId.Rewind, true);
```

- [ ] **Step 4: 진입점을 만든다**

`HiddenWeight/Assets/Scripts/World/ResidueEntryPoint.cs`:

```csharp
using UnityEngine;

namespace HiddenWeight.World
{
    // 셸 씬에는 지형이 없다. 지역에 들어오면 첫 방을 로드해야 게임이 시작된다.
    public class ResidueEntryPoint : MonoBehaviour
    {
        [SerializeField] string firstRoom = "R01";

        void Start()
        {
            var loader = RoomLoader.Instance;
            if (loader == null)
            {
                Debug.LogError("[ResidueEntryPoint] RoomLoader가 없다. 첫 방을 로드할 수 없다.");
                return;
            }

            if (loader.CurrentRoom == null) loader.LoadRoom(firstRoom, null);
        }
    }
}
```

- [ ] **Step 5: 빌드 세팅에 방 씬을 등록한다**

`ZoneSceneBuilder.RegisterBuildSettings()`(1274행)에서 씬 목록에 방 15개를 더한다:

```csharp
// 방 씬은 additive로만 로드되지만 빌드 세팅에 없으면 CanStreamedLevelBeLoaded가
// false를 돌려주고 RoomLoader가 전환을 취소한다.
foreach (var room in ResidueRoomLinks.RoomNames)
    scenes.Add(new EditorBuildSettingsScene($"{ScenesFolder}/Room_Residue_{room}.unity", true));
```

기존 배열 리터럴을 `List<EditorBuildSettingsScene>`로 바꾼 뒤 위 루프를 더하고,
마지막에 `EditorBuildSettings.scenes = scenes.ToArray();`로 대입한다.

- [ ] **Step 6: 씬을 굽는다**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && "/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" -batchmode -quit -nographics -projectPath "$PWD/HiddenWeight" -logFile "$PWD/.unity-logs/rooms.log" -executeMethod HiddenWeight.EditorTools.ZoneSceneBuilder.BuildResidueRooms; echo "exit=$?"; ls HiddenWeight/Assets/Scenes/Room_Residue_*.unity | wc -l
```

기대: `exit=0`, 씬 파일 15개.

- [ ] **Step 7: 커밋**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && git add -A HiddenWeight/Assets && git commit -m "feat(world): split residue rooms into scenes

방 15개를 각자 로컬 (0,0) 기준의 씬으로 굽고, 복도와 샤프트를 포탈
문으로 대체한다. Zone_Residue는 플레이어·카메라·RoomLoader만 갖는
셸이 된다.

S3 되감기 게이트는 R11 씬으로 옮겨 그대로 보존했다 — 균열 클리어 후
재방문 설계가 여기 걸려 있다.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 6: PlayMode 전환 테스트

**Files:**
- Create: `HiddenWeight/Assets/Tests/PlayMode/RoomTestHarness.cs`
- Create: `HiddenWeight/Assets/Tests/PlayMode/RoomTransitionTests.cs`

**Interfaces:**
- Consumes: `RoomLoader`, `RoomDoor`, `ResidueRoomLinks` (Task 2·4·5)
- Produces: `RoomTestHarness.EnterRoom(string zone, string room)` → `IEnumerator`

- [ ] **Step 1: 헬퍼를 쓴다**

`HiddenWeight/Assets/Tests/PlayMode/RoomTestHarness.cs`:

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 셸을 띄우고 지정한 방을 로드한다. RoomLoader.LoadRoom과 이름이 겹치지 않게 둔다.
    public static class RoomTestHarness
    {
        public static IEnumerator EnterRoom(string zone, string room)
        {
            yield return SceneManager.LoadSceneAsync("Zone_" + zone, LoadSceneMode.Single);
            yield return null;

            var loader = RoomLoader.Instance;
            Assert(loader != null, "셸에 RoomLoader가 없다.");

            // 진입점이 이미 첫 방을 로드했을 수 있으므로 끝날 때까지 기다린다.
            while (loader.IsTransitioning) yield return null;

            if (loader.CurrentRoom != room)
            {
                yield return loader.LoadRoom(room, null);
                while (loader.IsTransitioning) yield return null;
            }

            yield return null;
        }

        static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception(message);
        }
    }
}
```

- [ ] **Step 2: 실패하는 테스트를 쓴다**

`HiddenWeight/Assets/Tests/PlayMode/RoomTransitionTests.cs`:

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    public class RoomTransitionTests
    {
        [UnityTest]
        public IEnumerator EntryPoint_LoadsFirstRoom()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R01");

            Assert.That(RoomLoader.Instance.CurrentRoom, Is.EqualTo("R01"));
            Assert.That(SceneManager.GetSceneByName("Room_Residue_R01").isLoaded, Is.True);
        }

        [UnityTest]
        public IEnumerator LoadRoom_UnloadsPreviousRoom()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R01");
            yield return RoomTestHarness.EnterRoom("Residue", "R02");

            Assert.That(SceneManager.GetSceneByName("Room_Residue_R02").isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName("Room_Residue_R01").isLoaded, Is.False);
        }

        [UnityTest]
        public IEnumerator Door_CarriesPlayerToPairedDoor()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R01");

            var door = FindDoor("residue_R01_R02:E");
            Assert.That(door, Is.Not.Null, "R01에 동쪽 문이 없다.");

            RoomLoader.Instance.RequestTransition(door);
            while (RoomLoader.Instance.IsTransitioning) yield return null;

            Assert.That(RoomLoader.Instance.CurrentRoom, Is.EqualTo("R02"));

            var arrival = FindDoor("residue_R01_R02:W");
            Assert.That(arrival, Is.Not.Null, "R02에 서쪽 문이 없다.");
            Assert.That(Vector2.Distance(PlayerController.Instance.transform.position, arrival.ArrivalPosition),
                Is.LessThan(0.1f));
        }

        // 도착한 문 위에 서 있으므로 무장이 풀려 있어야 즉시 되돌아가지 않는다.
        [UnityTest]
        public IEnumerator ArrivalDoor_IsDisarmed()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R01");

            RoomLoader.Instance.RequestTransition(FindDoor("residue_R01_R02:E"));
            while (RoomLoader.Instance.IsTransitioning) yield return null;

            Assert.That(FindDoor("residue_R01_R02:W").Armed, Is.False);
        }

        [UnityTest]
        public IEnumerator Transition_SetsCurrentRoomOnCamera()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R02");

            Assert.That(RoomCamera.Instance.CurrentRoom, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator MissingRoom_LeavesPlayerAndRestoresInput()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R01");

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("찾을 수 없다"));
            yield return RoomLoader.Instance.LoadRoom("R99", null);
            while (RoomLoader.Instance.IsTransitioning) yield return null;

            Assert.That(RoomLoader.Instance.CurrentRoom, Is.EqualTo("R01"));
            Assert.That(PlayerInput.Enabled, Is.True);
        }

        // 주 동선을 문만 따라 끝까지 갈 수 있어야 QA가 성립한다.
        [UnityTest]
        public IEnumerator MainRoute_WalksR01ToR12()
        {
            yield return RoomTestHarness.EnterRoom("Residue", "R01");

            for (int i = 1; i < 12; i++)
            {
                string linkId = $"residue_R{i:00}_R{i + 1:00}";
                var door = FindDoor(linkId + ":E");
                Assert.That(door, Is.Not.Null, linkId + " 의 동쪽 문이 없다.");

                RoomLoader.Instance.RequestTransition(door);
                while (RoomLoader.Instance.IsTransitioning) yield return null;
            }

            Assert.That(RoomLoader.Instance.CurrentRoom, Is.EqualTo("R12"));
        }

        static RoomDoor FindDoor(string doorId)
        {
            foreach (var door in Object.FindObjectsByType<RoomDoor>(FindObjectsSortMode.None))
                if (door.DoorId == doorId) return door;

            return null;
        }
    }
}
```

- [ ] **Step 3: 실패를 확인한다**

PlayMode 테스트를 돌린다. 씬이 아직 안 구워졌으면 실패하고, Task 5가 끝났으면 통과해야
한다. 실패하면 `.unity-logs/play.log`에서 원인을 확인한다.

- [ ] **Step 4: 통과를 확인한다**

PlayMode 테스트를 돌린다. `RoomTransitionTests` 7개 통과.

- [ ] **Step 5: 커밋**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && git add -A HiddenWeight/Assets/Tests/PlayMode && git commit -m "test(world): cover room transitions

문 통과, 이전 방 언로드, 도착 문 무장 해제, 없는 방 요청 시 복구,
R01에서 R12까지 완주를 검증한다.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 7: 기존 잔재 테스트 마이그레이션

**Files:**
- Modify: `HiddenWeight/Assets/Tests/PlayMode/ResidueZoneTests.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/ResiduePlacementTests.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/ResidueArtTests.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/ResidueVerificationTests.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/ResidueCompletionArtTests.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/ResidueLoopCompletionTests.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/JumpSanityTests.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/AttackSanityTests.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/HangTimeSanityTests.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/EnemyPersonalitySanityTests.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/EmotionSkillTests.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/PlayerAbilityAnimationTests.cs`
- Modify: `HiddenWeight/Assets/Tests/PlayMode/ZoneBgmTests.cs`

**Interfaces:**
- Consumes: `RoomTestHarness.EnterRoom` (Task 6)
- Produces: 없음 (기존 검증을 새 구조로 옮길 뿐)

- [ ] **Step 1: 현재 실패 목록을 기록한다**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && grep -oE 'methodname="[^"]*"[^>]*result="Failed"' .unity-logs/play-results.xml | sed 's/.*methodname="\([^"]*\)".*/\1/' | sort > /tmp/before.txt; wc -l /tmp/before.txt
```

이 목록이 마이그레이션 대상이다. 하나도 빠뜨리지 않기 위한 기준선이다.

- [ ] **Step 2: 로드 방식을 바꾼다**

각 파일에서 아래 패턴을

```csharp
yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
yield return null;
```

이렇게 바꾼다:

```csharp
yield return RoomTestHarness.EnterRoom("Residue", RoomName);
```

`SceneName` 상수는 지우고 `RoomName` 상수를 넣는다. **어느 방인지는 그 테스트가 무엇을
찾는지 보고 정한다** — 예를 들어 `ResiduePlacementTests`가 R01의 체크포인트와 재화를
검사하면 `RoomName = "R01"`이다. 여러 방의 오브젝트를 한 번에 찾는 테스트는 방마다
`[TestCase]`로 쪼갠다.

지역 무관 테스트(`JumpSanityTests`, `AttackSanityTests`, `HangTimeSanityTests`,
`PlayerAbilityAnimationTests`)는 `RoomName = "R01"`을 쓴다. R01은 전투도 위험도 없는
평평한 방이라 이동·공격 검증의 무대로 적합하다(`LEVEL_21_RESIDUE_ROOMS.md`).

- [ ] **Step 3: 전체 오브젝트를 훑는 단언을 방 단위로 좁힌다**

`Object.FindObjectsByType<T>()`로 지역 전체를 세던 단언은 이제 한 방만 본다. 개수를
단언하는 테스트는 기대값을 그 방의 것으로 바꾼다. 지역 전체 합계를 확인하던 테스트는
`ResidueRoomLinks.RoomNames`를 돌며 방마다 로드해 합산하도록 바꾼다.

- [ ] **Step 4: 통과를 확인한다**

PlayMode 테스트를 돌린다.

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && grep -oE 'methodname="[^"]*"[^>]*result="Failed"' .unity-logs/play-results.xml | sed 's/.*methodname="\([^"]*\)".*/\1/' | sort > /tmp/after.txt; diff /tmp/before.txt /tmp/after.txt; head -3 .unity-logs/play-results.xml | grep -oE '(total|passed|failed)="[0-9]+"' | head -3
```

기대: `failed="0"`. 응시·균열 테스트는 `Zone_*_Full`이 아직 남아 있어 계속 통과한다.

- [ ] **Step 5: 커밋**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && git add -A HiddenWeight/Assets/Tests && git commit -m "test(world): migrate residue tests to room scenes

풀 씬 로드 전제를 EnterRoom 헬퍼로 바꾼다. 지역 무관 이동·공격 테스트는
전투도 위험도 없는 R01을 무대로 쓴다.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 8: 낡은 씬 정리와 문서 갱신

**Files:**
- Delete: `HiddenWeight/Assets/Scenes/Zone_Residue_Full.unity` (+ `.meta`)
- Modify: `docs/LEVEL_01_STANDARD.md:52-64`
- Modify: `PROJECT_STRUCTURE.md`

**Interfaces:**
- Consumes: 없음
- Produces: 없음

- [ ] **Step 1: 낡은 씬을 참조하는 곳이 없는지 확인한다**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && grep -rn "Zone_Residue_Full" HiddenWeight/Assets --include='*.cs' | grep -v "\.meta"
```

기대: 결과 없음. `ResidueLoopRuntime.cs:16`이 `Residue_Full`을 검사하고 있으니 그 조건을
`RoomLoader.Instance != null`로 바꾼다.

- [ ] **Step 2: 씬을 지운다**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && git rm HiddenWeight/Assets/Scenes/Zone_Residue_Full.unity HiddenWeight/Assets/Scenes/Zone_Residue_Full.unity.meta
```

- [ ] **Step 3: 문서를 갱신한다**

`docs/LEVEL_01_STANDARD.md` §1.3 표 아래에 넣는다:

```markdown
이 표기는 코드에서 `HiddenWeight.Data.Side` enum으로 존재한다. 방 연결의 런타임 구조는
`docs/superpowers/specs/2026-07-31-room-portal-scenes-design.md`를 따른다.
```

`PROJECT_STRUCTURE.md`의 씬 목록 절에 넣는다:

```markdown
### 방 씬

방 하나는 씬 하나다. 이름은 `Room_<지역>_<방>.unity`이며 모두 로컬 `(0, 0)` 기준으로
만든다. 지역 씬(`Zone_<지역>.unity`)은 플레이어·카메라·HUD·`RoomLoader`만 갖는 셸이고
지형을 포함하지 않는다. 동시에 로드되는 방은 항상 하나다.
```

- [ ] **Step 4: 전체 테스트를 돌린다**

EditMode와 PlayMode를 모두 돌려 `failed="0"`을 확인한다.

- [ ] **Step 5: 커밋**

```bash
cd "/Users/ksh/Desktop/NHN HACKERton" && git add -A && git commit -m "chore(world): retire residue full scene

방 씬이 대체했으므로 Zone_Residue_Full을 지운다. 방향 표기가 Side
enum으로 코드에 있다는 사실을 맵 문서에서도 찾을 수 있게 한다.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

## Task 9: 손으로 하는 QA

**Files:** 없음 (실행만)

- [ ] **Step 1: 게임을 띄운다**

Unity 에디터에서 `Zone_Residue.unity`를 열고 재생한다.

- [ ] **Step 2: 아래를 확인한다**

- [ ] R01에서 시작하고 지형이 보인다.
- [ ] 동쪽 끝 문에 걸어 들어가면 암전 후 R02에 도착한다.
- [ ] 도착 직후 되튕겨 R01로 돌아가지 않는다.
- [ ] 왔던 문으로 되돌아가면 R01로 돌아온다.
- [ ] 문을 통과해도 걷던 방향이 유지된다.
- [ ] R01부터 R12까지 문만 따라 완주된다.
- [ ] R04 아래 문으로 S1 비밀방에 내려갔다 올라온다.
- [ ] R11의 S3 문 앞에 되감기 게이트가 서 있고, 되감기 없이는 막힌다.
- [ ] 방을 나갔다 다시 들어오면 일반 적이 되살아난다 (`CONTENT_SYSTEM.md` §8.2).
- [ ] 암전 길이가 답답하지 않다. 답답하면 `RoomLoader.fadeSeconds`를 조정한다.

- [ ] **Step 3: 발견한 문제를 기록한다**

고칠 것이 있으면 이 계획 아래에 Task 10으로 추가하고 같은 TDD 흐름으로 처리한다.

---

## 이번 범위 밖 (후속 계획으로)

- **응시·균열 지역 적용.** 같은 구조를 `GazeZoneBuilder`, `FractureZoneBuilder`에 반복한다. 그때 `Zone_Gaze_Full`, `Zone_Fracture_Full`을 지우고 해당 테스트를 옮긴다.
- **문 잠금 파라미터.** 설계 §9. `requiredSkill`, `requiredShortcutId`, `requiredEncounterId`, `oneWayUntilOpened`.
- **진입 보호를 적이 존중하게 만들기.** `RoomLoader.EntryProtectedUntil`은 설정되지만 적 AI가 아직 읽지 않는다. 적 쪽 수정이 필요해 별도 작업으로 둔다.
- **지도 UI.** 이번 작업이 만든 링크 데이터를 읽어 이미 방문한 방과 아직 못 간 길을 그린다.
