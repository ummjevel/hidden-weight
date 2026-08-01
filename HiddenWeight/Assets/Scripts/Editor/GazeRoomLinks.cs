using UnityEditor;
using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.EditorTools
{
    // 응시 방 연결의 원본. 좌표는 GazeZoneBuilder.BuildGazeConnections()의 links 배열과
    // 샤프트 3개에서 그대로 옮겨 왔다 — 복도를 문으로 바꾸는 것이지 동선을 다시 설계하는
    // 것이 아니다. 잔재(ResidueRoomLinks)와 같은 구조다.
    public static class GazeRoomLinks
    {
        const string AssetPath = "Assets/ScriptableObjects/RoomLinks_Gaze.asset";

        public static readonly string[] RoomNames =
        {
            "G01", "G02", "G03", "G04", "G05", "G06", "G07", "G08",
            "G09", "G10", "G11", "G12", "GS1", "GS2", "GS3"
        };

        public static readonly RoomLink[] Links =
        {
            Link("gaze_G01_G02", "G01", Side.E, new Vector2(26, 2), "G02", new Vector2(0, 2)),
            Link("gaze_G02_G03", "G02", Side.E, new Vector2(28, 3), "G03", new Vector2(0, 3)),
            Link("gaze_G03_G04", "G03", Side.E, new Vector2(28, 2), "G04", new Vector2(2, 18)),
            Link("gaze_G04_G05", "G04", Side.E, new Vector2(22, 2), "G05", new Vector2(0, 2)),
            Link("gaze_G05_G06", "G05", Side.E, new Vector2(26, 2), "G06", new Vector2(0, 2)),
            Link("gaze_G06_G07", "G06", Side.E, new Vector2(32, 4), "G07", new Vector2(0, 4)),
            Link("gaze_G07_G08", "G07", Side.E, new Vector2(34, 4), "G08", new Vector2(2, 2)),
            Link("gaze_G08_G09", "G08", Side.E, new Vector2(22, 26), "G09", new Vector2(0, 3)),
            Link("gaze_G09_G10", "G09", Side.E, new Vector2(32, 4), "G10", new Vector2(0, 3)),
            Link("gaze_G10_G11", "G10", Side.E, new Vector2(24, 7), "G11", new Vector2(0, 3)),
            Link("gaze_G11_G12", "G11", Side.E, new Vector2(28, 4), "G12", new Vector2(0, 4)),

            // 비밀방 3곳. 예전에는 수직 샤프트였고 셋 다 위쪽 방에서 아래로 내려간다.
            // 잔재에서 겪었듯 샤프트 좌표는 벽만 세우고 타일을 뚫지 않아 그대로 문으로 쓰면
            // 닿지 않을 수 있다 — 실제 도달 가능 여부는 씬을 구운 뒤 확인해야 한다.
            Link("gaze_G04_GS1", "G04", Side.D, new Vector2(8, 0), "GS1", new Vector2(8, 2)),
            Link("gaze_G06_GS2", "G06", Side.D, new Vector2(20, 0), "GS2", new Vector2(20, 2)),
            Link("gaze_G11_GS3", "G11", Side.D, new Vector2(9, 1), "GS3", new Vector2(9, 2)),
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

        [MenuItem("Hidden Weight/Build Gaze Room Links")]
        public static void BuildAsset()
        {
            var table = AssetDatabase.LoadAssetAtPath<RoomLinkTable>(AssetPath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<RoomLinkTable>();
                AssetDatabase.CreateAsset(table, AssetPath);
            }

            table.zone = ZoneId.Gaze;
            table.links = Links;
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            Debug.Log($"[GazeRoomLinks] 링크 {Links.Length}개를 {AssetPath} 에 저장했다.");
        }
    }
}
