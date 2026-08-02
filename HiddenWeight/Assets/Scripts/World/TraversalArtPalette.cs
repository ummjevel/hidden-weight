using UnityEngine;

namespace HiddenWeight.World
{
    // 실제 충돌 타일맵 위에 얹을 지역별 바닥 모듈 참조.
    // Resources 에셋 하나로 들고 있어 방 씬을 다시 굽지 않아도 모든 룸에서 같은 규칙을 쓴다.
    public sealed class TraversalArtPalette : ScriptableObject
    {
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

        public Sprite SurfaceFor(string sceneName)
        {
            if (sceneName.Contains("Prologue")) return prologueSurface;
            if (sceneName.Contains("Gaze")) return gazeSurface;
            if (sceneName.Contains("Fracture")) return fractureSurface;
            return residueSurface;
        }

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
            if (sceneName.Contains("Fracture"))
                return new Color(0.66f, 0.72f, 0.78f, 0.92f);
            return Color.white;
        }

        public Color CollisionTintFor(string sceneName)
        {
            if (sceneName.Contains("Prologue"))
                return new Color(0.1f, 0.09f, 0.2f, 0.05f);
            if (sceneName.Contains("Gaze"))
                return new Color(0.16f, 0.13f, 0.24f, 0.42f);
            if (sceneName.Contains("Fracture"))
                return new Color(0.16f, 0.22f, 0.25f, 0.34f);
            if (sceneName.Contains("Residue"))
            {
                // 잔재 V3는 충돌면을 새 모듈 아트가 직접 덮는다. 기존 황갈색 타일은
                // 보조선처럼 보였으므로 충돌 확인용으로만 아주 희미하게 남긴다.
                return new Color(0.12f, 0.16f, 0.22f, 0.08f);
            }
            return new Color(0.18f, 0.13f, 0.08f, 0.46f);
        }
    }
}
