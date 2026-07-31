using UnityEngine;

namespace HiddenWeight.World
{
    // 실제 충돌 타일맵 위에 얹을 지역별 바닥 모듈 참조.
    // Resources 에셋 하나로 들고 있어 방 씬을 다시 굽지 않아도 모든 룸에서 같은 규칙을 쓴다.
    public sealed class TraversalArtPalette : ScriptableObject
    {
        public Sprite residueSurface;
        public Sprite gazeSurface;
        public Sprite fractureSurface;

        public Sprite SurfaceFor(string sceneName)
        {
            if (sceneName.Contains("Gaze")) return gazeSurface;
            if (sceneName.Contains("Fracture")) return fractureSurface;
            return residueSurface;
        }

        public Color SurfaceTintFor(string sceneName)
        {
            // 균열 원화는 백색 발광이 강해 배경보다 앞으로 튀므로 한 단계 눌러 쓴다.
            if (sceneName.Contains("Fracture"))
                return new Color(0.66f, 0.72f, 0.78f, 0.92f);
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
}
