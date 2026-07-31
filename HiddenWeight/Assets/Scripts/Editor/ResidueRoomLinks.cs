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
