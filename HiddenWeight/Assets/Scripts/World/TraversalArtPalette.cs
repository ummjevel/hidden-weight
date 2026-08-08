using UnityEngine;

namespace HiddenWeight.World
{
    // 실제 충돌 타일맵 위에 얹을 지역별 바닥 모듈 참조.
    // Resources 에셋 하나로 들고 있어 방 씬을 다시 굽지 않아도 모든 룸에서 같은 규칙을 쓴다.
    public sealed class TraversalArtPalette : ScriptableObject
    {
        // 지형 한 장을 늘여 쓰는 옛 방식. 아직 역할별 타일셋이 없는 지역이 쓴다.
        public Sprite prologueSurface;
        public Sprite prologueWall;
        public Sprite prologueFill;
        public Sprite residueSurface;
        public Sprite residueGroundLeft;
        public Sprite residueGroundMiddle;
        public Sprite residueGroundRight;
        public Sprite residueGroundFill;
        public Sprite residuePlatformShort;
        public Sprite residuePlatformMedium;
        public Sprite residuePlatformLong;
        public Sprite residueWallMiddle;
        public Sprite residueClimbPillar;
        public Sprite gazeSurface;
        public Sprite fractureSurface;

        // 역할별 타일셋. 있으면 끝단·중간·벽 켜·천장을 구분해 그리고, 형광 테두리 밴드를
        // 그리지 않는다 — 그림 자체가 네 면을 마감하기 때문이다.
        public TerrainTileSet fractureTiles;
        public ContinuousTerrainSet fractureContinuous;
        public TerrainTileSet gazeTiles;

        public Sprite SurfaceFor(string sceneName)
        {
            if (sceneName.Contains("Prologue")) return prologueSurface;
            if (sceneName.Contains("Gaze")) return gazeSurface;
            if (sceneName.Contains("Fracture")) return fractureSurface;
            return residueSurface;
        }

        // 타일셋을 가진 지역만 새 경로를 탄다. null이면 호출부가 옛 방식으로 되돌아가므로
        // 잔재의 화면은 이 변경으로 달라지지 않는다.
        public TerrainTileSet TileSetFor(string sceneName)
        {
            if (sceneName.Contains("Fracture") && fractureTiles != null && fractureTiles.IsComplete)
                return fractureTiles;
            if (sceneName.Contains("Gaze") && gazeTiles != null && gazeTiles.IsComplete)
                return gazeTiles;
            return null;
        }

        // 긴 표면·벽 모듈은 균열만 쓴다. 세트가 하나라도 비면 null을 반환해 호출부가
        // 역할별 v2 타일셋으로 안전하게 되돌아가게 한다.
        public ContinuousTerrainSet ContinuousSetFor(string sceneName)
        {
            if (sceneName.Contains("Fracture")
                && fractureContinuous != null
                && fractureContinuous.IsComplete)
                return fractureContinuous;
            return null;
        }

        // 잔재는 타일셋이 아니라 모듈 조각(끝단·중간·발판 길이별)으로 간다. 균열의
        // TileSetFor와 목적은 같고 조각을 나누는 축만 다르다 — 두 지역이 서로 다른 원화
        // 구조를 받았기 때문에 하나로 합치지 않는다.
        public bool HasResidueModularV3 => residueGroundMiddle != null
            && residueGroundFill != null
            && residuePlatformShort != null && residuePlatformMedium != null
            && residuePlatformLong != null && residueWallMiddle != null;

        public Sprite ResiduePlatformFor(float width)
        {
            if (width <= 3f) return residuePlatformShort;
            if (width <= 6f) return residuePlatformMedium;
            return residuePlatformLong;
        }

        public Color SurfaceTintFor(string sceneName)
        {
            if (sceneName.Contains("Prologue"))
                return new Color(0.82f, 0.84f, 1f, 0.9f);
            // 균열 원화는 백색 발광이 강해 배경보다 앞으로 튀므로 한 단계 눌러 쓴다.
            // 역할별 타일셋을 쓰는 경우는 예외다 — 그 그림이 곧 지형이라 원색으로 둔다.
            if (sceneName.Contains("Fracture"))
                return TileSetFor(sceneName) != null
                    ? Color.white
                    : new Color(0.66f, 0.72f, 0.78f, 0.92f);
            return Color.white;
        }

        // 충돌 타일맵 자체의 색. 이건 "밟히는 곳이 아예 안 보이는 것"을 막는 최후의 보루라
        // 아트가 없는 지역에서는 진하게 남긴다.
        //
        // 균열은 예외다. 역할별 타일셋이 들어온 뒤로 이 지역의 지형은 윗면(TraversalSurface)과
        // 노출된 옆면(TraversalWallCourse)이 전부 실제 그림으로 덮인다. 그런데도 진한 틴트를
        // 그대로 두니, 대리석 난간 옆과 아래로 **반투명한 잿빛 직사각형**이 삐져나와 물 위에
        // 유리 상자가 놓인 것처럼 보였다. 그림이 이미 형태를 말하고 있으므로 여기서는
        // 두께를 느끼게 하는 옅은 그림자만 남긴다.
        public Color CollisionTintFor(string sceneName)
        {
            if (sceneName.Contains("Prologue"))
                return new Color(0.1f, 0.09f, 0.2f, 0.05f);
            if (sceneName.Contains("Gaze"))
                return new Color(0.16f, 0.13f, 0.24f, 0.42f);
            if (sceneName.Contains("Fracture"))
                return new Color(0.20f, 0.26f, 0.34f, 0.14f);
            if (sceneName.Contains("Residue"))
            {
                // 잔재 V3는 충돌면을 새 모듈 아트가 직접 덮는다. 기존 황갈색 타일은
                // 보조선처럼 보였으므로 충돌 확인용으로만 아주 희미하게 남긴다.
                return new Color(0.12f, 0.16f, 0.22f, 0.08f);
            }
            return new Color(0.18f, 0.13f, 0.08f, 0.46f);
        }
    }

    // 짧은 칸을 반복하지 않고, 실제로 끊기는 곳에만 캡을 두는 연속형 지형 세트.
    [System.Serializable]
    public sealed class ContinuousTerrainSet
    {
        [Header("수평 바닥")]
        public Sprite surfaceLeft;
        public Sprite surfaceMiddle;
        public Sprite surfaceRight;

        [Header("세로 벽")]
        public Sprite wallTop;
        public Sprite wallMiddle;
        public Sprite wallBottom;

        [Header("내부 채움")]
        public Sprite fill;

        public bool IsComplete => surfaceLeft != null && surfaceMiddle != null
            && surfaceRight != null && wallTop != null && wallMiddle != null
            && wallBottom != null && fill != null;
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
