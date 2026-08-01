using UnityEditor;
using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.EditorTools
{
    // 균열 방 연결의 원본. 좌표는 FractureZoneBuilder.BuildFractureConnections()의 links
    // 배열과 샤프트 3개에서 그대로 옮겨 왔다. 응시(GazeRoomLinks)와 주 동선 좌표가 같은데,
    // 세 지역이 같은 방 크기 규격을 쓰기 때문이다(LEVEL_01_STANDARD).
    public static class FractureRoomLinks
    {
        const string AssetPath = "Assets/ScriptableObjects/RoomLinks_Fracture.asset";

        public static readonly string[] RoomNames =
        {
            "F01", "F02", "F03", "F04", "F05", "F06", "F07", "F08",
            "F09", "F10", "F11", "F12", "FS1", "FS2", "FS3"
        };

        public static readonly RoomLink[] Links =
        {
            Link("fracture_F01_F02", "F01", Side.E, new Vector2(26, 2), "F02", new Vector2(0, 2)),
            Link("fracture_F02_F03", "F02", Side.E, new Vector2(28, 3), "F03", new Vector2(0, 3)),
            Link("fracture_F03_F04", "F03", Side.E, new Vector2(28, 2), "F04", new Vector2(2, 18)),
            Link("fracture_F04_F05", "F04", Side.E, new Vector2(22, 2), "F05", new Vector2(0, 2)),
            Link("fracture_F05_F06", "F05", Side.E, new Vector2(26, 2), "F06", new Vector2(0, 2)),
            Link("fracture_F06_F07", "F06", Side.E, new Vector2(32, 4), "F07", new Vector2(0, 4)),
            Link("fracture_F07_F08", "F07", Side.E, new Vector2(34, 4), "F08", new Vector2(2, 2)),
            Link("fracture_F08_F09", "F08", Side.E, new Vector2(22, 26), "F09", new Vector2(0, 3)),
            Link("fracture_F09_F10", "F09", Side.E, new Vector2(32, 4), "F10", new Vector2(0, 3)),
            Link("fracture_F10_F11", "F10", Side.E, new Vector2(24, 7), "F11", new Vector2(0, 3)),
            Link("fracture_F11_F12", "F11", Side.E, new Vector2(28, 4), "F12", new Vector2(0, 4)),

            // 비밀방 3곳. 응시와 달리 FS2의 샤프트가 방 왼쪽(x+4)에 붙어 있다.
            Link("fracture_F04_FS1", "F04", Side.D, new Vector2(8, 0), "FS1", new Vector2(8, 2)),
            Link("fracture_F06_FS2", "F06", Side.D, new Vector2(4, 0), "FS2", new Vector2(4, 2)),
            Link("fracture_F11_FS3", "F11", Side.D, new Vector2(8, 1), "FS3", new Vector2(8, 2)),
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

        [MenuItem("Hidden Weight/Build Fracture Room Links")]
        public static void BuildAsset()
        {
            var table = AssetDatabase.LoadAssetAtPath<RoomLinkTable>(AssetPath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<RoomLinkTable>();
                AssetDatabase.CreateAsset(table, AssetPath);
            }

            table.zone = ZoneId.Fracture;
            table.links = Links;
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FractureRoomLinks] 링크 {Links.Length}개를 {AssetPath} 에 저장했다.");
        }
    }
}
