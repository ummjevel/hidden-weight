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
            //
            // 도착 앵커(toAnchor) y가 세 곳 다 2로, 각 비밀방 바닥 표면(y=2)과 똑같았다.
            // RoomDoor.DefaultArrivalOffset(Side.U)가 도착점에서 y를 1.5 더 내리므로(문
            // 위에서 떨어져 들어오는 느낌을 주려는 것) 실제 도착 y는 0.5 — 바닥 아래에
            // 박혀 캐릭터가 끼었다(GS1에서 실측 확인). y=4로 올려 도착 후에도 바닥(2)
            // 위 0.5만큼 여유를 둔다.
            //
            // GS1은 G04의 지그재그 하강 주 동선(x=9 이후)과 물리적으로 분리하기 위해
            // 맨 왼쪽(로컬 0~1)으로 옮겼다 — 일부러 왼쪽 끝까지 걸어가야만 찾는다.
            Link("gaze_G04_GS1", "G04", Side.D, new Vector2(0.5f, 0), "GS1", new Vector2(4, 4)),
            Link("gaze_G06_GS2", "G06", Side.D, new Vector2(20, 0), "GS2", new Vector2(20, 4)),
            Link("gaze_G11_GS3", "G11", Side.D, new Vector2(9, 1), "GS3", new Vector2(9, 4)),

            // 물리적 숏컷 C(G07↔G10)의 실제 통로. Shortcut 컴포넌트 자체는 "열림" 상태만
            // 기록할 뿐 방과 방을 잇지 않는다(원래 한 씬 안에서 막힌 길을 트는 방식이었던
            // BuildShortcut·ShortcutPassage가 방 단위 씬 분리 후에는 아무 곳에도 연결되지
            // 않는 죽은 코드로 남았다) — 그래서 숏컷 C를 승리로 열어도 G07↔G10 사이를
            // 오갈 방법이 전혀 없었다. requiredShortcutId로 gaze_shortcut_c가 열렸을 때만
            // 반응하는 문 한 쌍을 정식 RoomLink로 추가해 실제 통로를 만든다.
            Link("gaze_shortcut_c_passage", "G07", Side.S, new Vector2(33, 11.5f), "G10", new Vector2(2, 4.5f),
                requiredShortcutId: "gaze_shortcut_c"),
        };

        static RoomLink Link(string id, string from, Side fromSide, Vector2 fromAnchor,
            string to, Vector2 toAnchor, string requiredShortcutId = null) => new RoomLink
            {
                linkId = id,
                fromRoom = from,
                toRoom = to,
                fromSide = fromSide,
                toSide = RoomLink.Opposite(fromSide),
                fromAnchor = fromAnchor,
                toAnchor = toAnchor,
                requiredShortcutId = requiredShortcutId,
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
