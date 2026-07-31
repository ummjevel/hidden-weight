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

            // 비밀방 3곳. 예전에는 수직 샤프트였는데, 그 샤프트는 벽만 세우고 바닥 타일을
            // 뚫지 않았다 — 실제 출입은 ResidueLoopRuntime이 심던 텔레포트 쌍이 맡았다.
            // 그래서 샤프트 좌표를 그대로 문으로 쓰면 지형에 파묻히거나 점프로 닿지 않는다.
            // 아래 값은 플레이어 캡슐(0.8x1.4)과 점프 높이 2.72로 다시 계산한 것이다.

            // R04: 부서진 바닥 발판(x 6~9, 윗면 6.25)의 오른쪽 끝. 발판 한가운데(x 7.5) 위에는
            // 지그재그 발판(7,8)이 1.5만 띄우고 덮고 있어 복귀 도착 자리를 낼 수 없다 —
            // 그 발판은 x 8.5에서 끝나므로 x를 9.5로 밀어 머리 위를 비웠다.
            // 도착 (9.5, 8.0)은 아무 콜라이더에도 닿지 않고 하층 안전 바닥으로 떨어진다.
            Link("residue_R04_S1", "R04", Side.D, new Vector2(9.5f, 6.5f), "S1", new Vector2(2f, 3.25f)),

            // R06: 바닥(y=5)을 걸어가는 몸통은 6.4에서 끝난다. 판정을 6.6부터 두어 R07로
            // 지나가는 플레이어를 낚아채지 않고, 그 자리에서 점프해야만 들어가게 한다.
            // S2 쪽도 같은 이유로 바닥 위 점프 높이에 둔다 — 관측 발판(y=10)에 두면 전투 뒤
            // 바닥에서 8을 올라갈 방법이 없어 갇힌다.
            Link("residue_R06_S2", "R06", Side.D, new Vector2(21f, 7.2f), "S2", new Vector2(4f, 4.25f)),

            // R11↔S3는 손대지 않았다. 문은 닿게 만들 수 있지만 되감기 게이트를 그 앞에 세울
            // 자리가 없다 — 발판 두 개가 R12로 가는 유일한 길이라 거기에 막이를 두면 균열을
            // 깨기 전에는 지역을 통과하지 못한다. 지형 추가가 필요하다(보고서 참조).
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
