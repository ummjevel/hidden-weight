using UnityEngine;

namespace HiddenWeight.World
{
    // 실제 충돌 타일맵 위에 얹을 지역별 바닥 모듈 참조.
    // Resources 에셋 하나로 들고 있어 방 씬을 다시 굽지 않아도 모든 룸에서 같은 규칙을 쓴다.
    public sealed class TraversalArtPalette : ScriptableObject
    {
        // 지형 한 장을 늘여 쓰는 옛 방식. 아직 역할별 타일셋이 없는 지역이 쓴다.
        public Sprite residueSurface;
        public Sprite gazeSurface;
        public Sprite fractureSurface;

        // 역할별 타일셋. 있으면 끝단·중간·벽 켜·천장을 구분해 그리고, 형광 테두리 밴드를
        // 그리지 않는다 — 그림 자체가 네 면을 마감하기 때문이다.
        public TerrainTileSet fractureTiles;

        public Sprite SurfaceFor(string sceneName)
        {
            if (sceneName.Contains("Gaze")) return gazeSurface;
            if (sceneName.Contains("Fracture")) return fractureSurface;
            return residueSurface;
        }

        // 타일셋을 가진 지역만 새 경로를 탄다. null이면 호출부가 옛 방식으로 되돌아가므로
        // 잔재·응시의 화면은 이 변경으로 달라지지 않는다.
        public TerrainTileSet TileSetFor(string sceneName)
        {
            if (sceneName.Contains("Fracture") && fractureTiles != null && fractureTiles.IsComplete)
                return fractureTiles;
            return null;
        }

        public Color SurfaceTintFor(string sceneName)
        {
            // 균열 원화는 백색 발광이 강해 배경보다 앞으로 튀므로 한 단계 눌러 쓴다.
            // 역할별 타일셋을 쓰는 경우는 예외다 — 그 그림이 곧 지형이라 원색으로 둔다.
            if (sceneName.Contains("Fracture"))
                return TileSetFor(sceneName) != null
                    ? Color.white
                    : new Color(0.66f, 0.72f, 0.78f, 0.92f);
            return Color.white;
        }

        public Color CollisionTintFor(string sceneName)
        {
            if (sceneName.Contains("Gaze"))
                return new Color(0.16f, 0.13f, 0.24f, 0.42f);
            if (sceneName.Contains("Fracture"))
                return new Color(0.16f, 0.22f, 0.25f, 0.34f);
            return new Color(0.18f, 0.13f, 0.08f, 0.46f);
        }
    }

    // 지형을 역할별로 나눠 든다. 한 장을 모든 면에 늘여 쓰면 끝단도 모서리도 없어 공간이
    // 통짜 사각형으로 읽힌다 — 그것이 균열이 박스로 보이던 이유다.
    [System.Serializable]
    public sealed class TerrainTileSet
    {
        [Header("바닥 윗면")]
        public Sprite topLeft;
        public Sprite[] topMid;
        public Sprite topRight;

        [Header("세로 벽면 (세로로 쌓는 켜)")]
        public Sprite[] wallCourse;

        [Header("천장·아랫면")]
        public Sprite ceilingLeft;
        public Sprite ceilingMid;
        public Sprite ceilingRight;

        public bool IsComplete =>
            topLeft != null && topRight != null
            && topMid != null && topMid.Length > 0
            && wallCourse != null && wallCourse.Length > 0
            && ceilingMid != null;

        // 중간 타일은 좌표로 고른다. 무작위로 고르면 방을 다시 로드할 때마다 무늬가 바뀌어
        // 플레이어가 같은 방을 다른 장소로 오인한다.
        public Sprite MidAt(int x, int y)
        {
            if (topMid == null || topMid.Length == 0) return null;
            int hash = x * 73856093 ^ y * 19349663;
            return topMid[(hash & 0x7fffffff) % topMid.Length];
        }

        public Sprite CourseAt(int x, int y)
        {
            if (wallCourse == null || wallCourse.Length == 0) return null;
            int hash = x * 83492791 ^ y * 29499439;
            return wallCourse[(hash & 0x7fffffff) % wallCourse.Length];
        }
    }
}
