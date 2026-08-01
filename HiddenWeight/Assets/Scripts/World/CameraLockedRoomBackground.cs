using UnityEngine;
using UnityEngine.Tilemaps;
using HiddenWeight.Enemies;

namespace HiddenWeight.World
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CameraLockedRoomBackground : MonoBehaviour
    {
        // 이 이미지는 실제 충돌 지형이 아니라 분위기를 만드는 콘셉트 배경이다.
        // 원색으로 그리면 그림 속 계단/발판이 실제 길보다 선명해져 플레이어가 가짜 길을
        // 따라가게 된다. 검은 카메라 배경 위에 반투명으로 눌러 실제 지형이 전경으로 읽히게 한다.
        [SerializeField] Color backgroundTint = new Color(0.68f, 0.68f, 0.68f, 0.58f);
        static Sprite _visibilityPixel;

        void Awake()
        {
            ApplyReadabilityTint();
            EnsureRoomVisualCuller();
            BuildTraversalEdges();
        }

        void OnValidate() => ApplyReadabilityTint();

        void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera != null)
                Refresh(camera);
        }

        public void Refresh(Camera camera)
        {
            if (camera == null || !camera.orthographic)
                return;

            transform.position = new Vector3(
                camera.transform.position.x,
                camera.transform.position.y,
                transform.position.z);

            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null)
                return;

            renderer.color = backgroundTint;

            Vector2 spriteSize = renderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f)
                return;

            float requiredHeight = camera.orthographicSize * 2f;
            float requiredWidth = requiredHeight * camera.aspect;
            float scale = Mathf.Max(
                requiredWidth / spriteSize.x,
                requiredHeight / spriteSize.y);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        void ApplyReadabilityTint()
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.color = backgroundTint;
        }

        void EnsureRoomVisualCuller()
        {
            Transform art = transform.parent;
            if (art != null && art.GetComponentInParent<Room>() != null
                && art.GetComponent<RoomVisualCuller>() == null)
                art.gameObject.AddComponent<RoomVisualCuller>();
        }

        // 배경 그림 속 가짜 계단과 실제 충돌 바닥을 구분한다. 타일맵에서 "위가 빈 타일"만
        // 찾아 연속 구간별로 얇은 선을 그리므로, 선이 끊기는 곳이 정확히 실제 구덩이다.
        // 런타임에 만들기 때문에 씬마다 수동 마킹할 필요가 없고 새 룸에도 자동 적용된다.
        static void BuildTraversalEdges()
        {
            var palette = Resources.Load<TraversalArtPalette>("TraversalArtPalette");
            foreach (var tilemap in FindObjectsByType<Tilemap>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // 일부 예전 생성 씬에는 이 값이 꺼진 채 저장돼 있다. 충돌은 남아 있어
                // "안 보이는데 막히는 바닥"이 되므로 런타임에서 반드시 복구한다.
                var tileRenderer = tilemap.GetComponent<TilemapRenderer>();
                if (tileRenderer != null)
                    tileRenderer.enabled = true;
                if (palette != null)
                    tilemap.color = palette.CollisionTintFor(tilemap.gameObject.scene.name);

                if (tilemap.transform.Find("TraversalEdges_Runtime") != null)
                    continue;

                var root = new GameObject("TraversalEdges_Runtime");
                root.transform.SetParent(tilemap.transform, false);

                BoundsInt bounds = tilemap.cellBounds;
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    int runStart = -1;
                    for (int x = bounds.xMin; x <= bounds.xMax; x++)
                    {
                        bool surface = x < bounds.xMax
                            && tilemap.HasTile(new Vector3Int(x, y, 0))
                            && !tilemap.HasTile(new Vector3Int(x, y + 1, 0));

                        if (surface && runStart < 0)
                            runStart = x;
                        else if (!surface && runStart >= 0)
                        {
                            AddTraversalSurface(tilemap, root.transform, palette, runStart, x, y);
                            runStart = -1;
                        }
                    }
                }

                BuildTilemapWallEdges(tilemap, root.transform, palette);
            }

            BuildWallClimbSurfaces(palette);
            BuildPlatformSurfaces(palette);
            BuildBlockedHints();
        }

        // 길을 막는 것들에 "왜 막혔는지" 문구를 붙인다. 전투 잠금벽·능력 게이트·아직 열지 않은
        // 숏컷은 생김새가 서로 비슷해서, 문구가 없으면 넘어갈 수 있는 벽인 줄 알고 시도하게 된다.
        // 위의 표시들과 같은 이유로 런타임에 붙이므로 씬을 다시 굽지 않아도 된다.
        static void BuildBlockedHints()
        {
            foreach (var encounter in FindObjectsByType<Encounter>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                foreach (var wall in encounter.GetComponentsInChildren<BoxCollider2D>(true))
                {
                    if (wall.isTrigger || wall.GetComponent<BlockedHint>() != null) continue;
                    BlockedHint.AttachTo(wall.gameObject, encounter: encounter);
                }

            foreach (var gate in FindObjectsByType<Gate>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (gate.GetComponent<BlockedHint>() == null)
                    BlockedHint.AttachTo(gate.gameObject, gate: gate);

            foreach (var shortcut in FindObjectsByType<Shortcut>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (shortcut.GetComponent<BlockedHint>() == null)
                    BlockedHint.AttachTo(shortcut.gameObject, shortcut: shortcut);
        }

        // 타일맵 밖에 따로 서 있는 발판(안전 발판·이동 발판·붕괴 발판 등)에도 같은 표시를 그린다.
        //
        // 위의 타일맵 훑기는 셀 격자만 보고, BuildWallClimbSurfaces는 Wall 레이어만 본다. 그
        // 사이에 Ground 레이어의 독립 BoxCollider가 통째로 빠져 있었다 — 밟히는데 아무 표시도
        // 없어서, 어두운 배경 앞에서는 허공에 서 있는 것처럼 보였다. 잔재 기준 방마다 3~13개다.
        static void BuildPlatformSurfaces(TraversalArtPalette palette)
        {
            int ground = LayerMask.NameToLayer("Ground");

            foreach (var platform in FindObjectsByType<BoxCollider2D>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (platform.isTrigger || platform.gameObject.layer != ground) continue;
                if (platform.GetComponentInParent<Tilemap>() != null) continue;  // 타일맵은 위에서 처리했다
                if (platform.transform.Find("PlatformSurface_Runtime") != null) continue;

                string sceneName = platform.gameObject.scene.name;
                Sprite sprite = palette == null ? null : palette.SurfaceFor(sceneName);
                if (sprite == null || sprite.bounds.size.x <= 0f || sprite.bounds.size.y <= 0f) continue;

                var root = new GameObject("PlatformSurface_Runtime");
                root.transform.SetParent(platform.transform, false);

                // BuildSolidBlock이 크기를 localScale에 싣기 때문에 콜라이더 자체는 1x1이다.
                // 자식은 그 스케일을 그대로 물려받으므로 로컬 단위로만 계산한다.
                float topY = platform.offset.y + platform.size.y * 0.5f;

                var surface = new GameObject("PlatformSurface");
                surface.transform.SetParent(root.transform, false);
                surface.transform.localPosition = new Vector3(platform.offset.x, topY, 0f);
                surface.transform.localScale = new Vector3(
                    platform.size.x / sprite.bounds.size.x,
                    platform.size.y / sprite.bounds.size.y,
                    1f);

                var fill = surface.AddComponent<SpriteRenderer>();
                fill.sprite = sprite;
                fill.color = palette.SurfaceTintFor(sceneName);
                fill.sortingOrder = 4;

                // 윗면 선은 타일맵 바닥과 같은 색을 써서 "밟을 수 있는 면"이 한 가지로 읽히게 한다.
                float scaleY = Mathf.Max(0.0001f, Mathf.Abs(platform.transform.lossyScale.y));
                float edgeHeight = 0.14f / scaleY;

                var edge = new GameObject("PlatformEdge");
                edge.transform.SetParent(root.transform, false);
                edge.transform.localPosition = new Vector3(platform.offset.x, topY - edgeHeight * 0.5f, 0f);
                edge.transform.localScale = new Vector3(
                    platform.size.x / sprite.bounds.size.x,
                    edgeHeight / sprite.bounds.size.y,
                    1f);

                var edgeRenderer = edge.AddComponent<SpriteRenderer>();
                edgeRenderer.sprite = VisibilityPixel();
                edgeRenderer.color = TraversalEdgeColor(sceneName);
                edgeRenderer.sortingOrder = 6;
            }
        }

        // 높이가 달라지는 바닥 턱과 구덩이 옆면도 실제로는 벽잡기가 가능한 TilemapCollider다.
        // 수평 윗면만 표시하면 플레이어는 보이지 않는 세로 면을 타게 되므로, 좌우가 빈 타일을
        // 세로로 묶어 밝은 벽면과 경계선을 만든다.
        static void BuildTilemapWallEdges(Tilemap tilemap, Transform parent,
                                          TraversalArtPalette palette)
        {
            BoundsInt bounds = tilemap.cellBounds;
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                BuildTilemapWallSide(tilemap, parent, palette, bounds, x, -1);
                BuildTilemapWallSide(tilemap, parent, palette, bounds, x, 1);
            }
        }

        static void BuildTilemapWallSide(Tilemap tilemap, Transform parent,
                                         TraversalArtPalette palette, BoundsInt bounds,
                                         int x, int side)
        {
            int runStart = int.MinValue;
            for (int y = bounds.yMin; y <= bounds.yMax; y++)
            {
                bool exposed = y < bounds.yMax
                    && tilemap.HasTile(new Vector3Int(x, y, 0))
                    && !tilemap.HasTile(new Vector3Int(x + side, y, 0));

                if (exposed && runStart == int.MinValue)
                {
                    runStart = y;
                }
                else if (!exposed && runStart != int.MinValue)
                {
                    AddTilemapWallFace(tilemap, parent, palette, x, runStart, y, side);
                    runStart = int.MinValue;
                }
            }
        }

        static void AddTilemapWallFace(Tilemap tilemap, Transform parent,
                                       TraversalArtPalette palette,
                                       int x, int yMin, int yMax, int side)
        {
            int boundaryX = side < 0 ? x : x + 1;
            Vector3 start = tilemap.CellToWorld(new Vector3Int(boundaryX, yMin, 0));
            Vector3 end = tilemap.CellToWorld(new Vector3Int(boundaryX, yMax, 0));
            float height = Vector3.Distance(start, end);
            if (height <= 0f) return;

            string sceneName = tilemap.gameObject.scene.name;
            Sprite surfaceSprite = palette == null ? null : palette.SurfaceFor(sceneName);
            if (surfaceSprite != null && surfaceSprite.bounds.size.x > 0f
                && surfaceSprite.bounds.size.y > 0f)
            {
                const float faceWidth = 0.72f;
                var face = new GameObject("TraversalWallFace");
                face.transform.SetParent(parent, true);
                face.transform.position = (start + end) * 0.5f
                    + Vector3.right * (-side * faceWidth * 0.5f);
                face.transform.localScale = new Vector3(
                    faceWidth / surfaceSprite.bounds.size.x,
                    height / surfaceSprite.bounds.size.y,
                    1f);

                var faceRenderer = face.AddComponent<SpriteRenderer>();
                faceRenderer.sprite = surfaceSprite;
                Color tint = palette.SurfaceTintFor(sceneName);
                tint.a = Mathf.Max(tint.a, 0.72f);
                faceRenderer.color = tint;
                ApplySorting(tilemap, faceRenderer, 2);
            }

            // 지형 아트는 검은 픽셀이 많아 tint를 밝게 해도 배경에서 사라진다. 순백 픽셀을
            // 사용해 벽잡기 경계만큼은 원본 그림의 명암과 무관하게 보이도록 한다.
            Sprite edgeSprite = VisibilityPixel();

            var edge = new GameObject("TraversalWallEdge");
            edge.transform.SetParent(parent, true);
            edge.transform.position = (start + end) * 0.5f
                + Vector3.right * (-side * 0.06f);
            edge.transform.localScale = new Vector3(
                0.14f / edgeSprite.bounds.size.x,
                height / edgeSprite.bounds.size.y,
                1f);

            var edgeRenderer = edge.AddComponent<SpriteRenderer>();
            edgeRenderer.sprite = edgeSprite;
            edgeRenderer.color = TraversalEdgeColor(sceneName);
            ApplySorting(tilemap, edgeRenderer, 5);
        }

        // 굴뚝과 보스 전장 벽은 Tilemap이 아니라 BoxCollider2D 블록이라 위의 바닥 표면
        // 탐색에 잡히지 않는다. 기존 장식이 어두운 배경과 섞여 충돌만 남는 경우에도 실제
        // 벽잡기 면을 읽을 수 있도록 모든 Wall 레이어 블록에 밝은 세로 면과 양쪽 테두리를 둔다.
        static void BuildWallClimbSurfaces(TraversalArtPalette palette)
        {
            foreach (var wall in FindObjectsByType<BoxCollider2D>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (wall.isTrigger || wall.gameObject.layer != LayerMask.NameToLayer("Wall")) continue;
                if (wall.transform.Find("WallClimbSurfaces_Runtime") != null) continue;

                // 전투 잠금벽도 Wall 레이어라 여기까지 온다. 하지만 그건 올라가라고 세운 벽이
                // 아니라 "지금은 못 지나간다"는 벽이다. 등반 표시를 붙이면 오를 수 있다고
                // 잘못 안내하게 되므로 건너뛰고, 대신 막힌 이유를 문구로 알려 준다.
                if (wall.GetComponentInParent<Encounter>() != null) continue;

                string sceneName = wall.gameObject.scene.name;
                Sprite sprite = palette == null ? null : palette.SurfaceFor(sceneName);
                if (sprite == null || sprite.bounds.size.x <= 0f || sprite.bounds.size.y <= 0f) continue;

                var root = new GameObject("WallClimbSurfaces_Runtime");
                root.transform.SetParent(wall.transform, false);

                Color fill = palette.SurfaceTintFor(sceneName);
                fill.a = Mathf.Max(fill.a, 0.48f);
                AddWallStrip(wall, root.transform, sprite, "WallClimbSurface",
                    wall.offset.x, wall.size.x, fill, 4);

                float scaleX = Mathf.Max(0.0001f, Mathf.Abs(wall.transform.lossyScale.x));
                float edgeWidth = 0.22f / scaleX;
                Color edge = TraversalEdgeColor(sceneName);
                Sprite edgeSprite = VisibilityPixel();
                AddWallStrip(wall, root.transform, edgeSprite, "WallClimbEdgeLeft",
                    wall.offset.x - wall.size.x * 0.5f + edgeWidth * 0.5f,
                    edgeWidth, edge, 6);
                AddWallStrip(wall, root.transform, edgeSprite, "WallClimbEdgeRight",
                    wall.offset.x + wall.size.x * 0.5f - edgeWidth * 0.5f,
                    edgeWidth, edge, 6);
            }
        }

        static void AddWallStrip(BoxCollider2D wall, Transform parent, Sprite sprite,
                                 string name, float localX, float localWidth,
                                 Color color, int sortingOrder)
        {
            var strip = new GameObject(name);
            strip.transform.SetParent(parent, false);
            strip.transform.localPosition = new Vector3(localX, wall.offset.y, 0f);

            Vector2 spriteSize = sprite.bounds.size;
            strip.transform.localScale = new Vector3(
                localWidth / spriteSize.x,
                wall.size.y / spriteSize.y,
                1f);

            var renderer = strip.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        static void AddTraversalSurface(Tilemap tilemap, Transform parent,
                                        TraversalArtPalette palette,
                                        int xMin, int xMax, int y)
        {
            Vector3 start = tilemap.CellToWorld(new Vector3Int(xMin, y + 1, 0));
            Vector3 end = tilemap.CellToWorld(new Vector3Int(xMax, y + 1, 0));
            float width = Vector3.Distance(start, end);
            string sceneName = tilemap.gameObject.scene.name;
            Sprite surfaceSprite = palette == null ? null : palette.SurfaceFor(sceneName);

            if (surfaceSprite != null && width > 0f)
            {
                const float surfaceHeight = 1.65f;
                const float maxModuleWidth = 8f;
                Vector2 spriteSize = surfaceSprite.bounds.size;
                if (spriteSize.x > 0f && spriteSize.y > 0f)
                {
                    // 긴 바닥 하나에 그림 한 장을 늘리면 장식과 돌무늬가 옆으로 퍼진다.
                    // 8유닛 이하 모듈로 반복해 원래 지형 밀도를 유지한다.
                    int moduleCount = Mathf.Max(1, Mathf.CeilToInt(width / maxModuleWidth));
                    float moduleWidth = width / moduleCount;
                    for (int i = 0; i < moduleCount; i++)
                    {
                        var surface = new GameObject("TraversalSurface");
                        surface.transform.SetParent(parent, true);
                        float t = (i + 0.5f) / moduleCount;
                        // 지형 시트 피벗이 Bottom Center라 그림 윗면이 충돌 표면에 닿도록 내린다.
                        surface.transform.position = Vector3.Lerp(start, end, t)
                            + Vector3.down * surfaceHeight;
                        surface.transform.localScale = new Vector3(
                            moduleWidth / spriteSize.x, surfaceHeight / spriteSize.y, 1f);

                        var renderer = surface.AddComponent<SpriteRenderer>();
                        renderer.sprite = surfaceSprite;
                        renderer.color = palette.SurfaceTintFor(sceneName);
                        ApplySorting(tilemap, renderer, 1);
                    }
                }
            }

            AddTraversalEdge(tilemap, parent, xMin, xMax, y);
        }

        static void AddTraversalEdge(Tilemap tilemap, Transform parent,
                                     int xMin, int xMax, int y)
        {
            var cell = new Vector3Int(xMin, y, 0);
            Sprite sprite = tilemap.GetSprite(cell);
            if (sprite == null) return;

            Vector3 start = tilemap.CellToWorld(new Vector3Int(xMin, y + 1, 0));
            Vector3 end = tilemap.CellToWorld(new Vector3Int(xMax, y + 1, 0));
            float width = Vector3.Distance(start, end);
            Vector2 spriteSize = sprite.bounds.size;
            if (width <= 0f || spriteSize.x <= 0f || spriteSize.y <= 0f) return;

            var edge = new GameObject("TraversalEdge");
            edge.transform.SetParent(parent, true);
            edge.transform.position = (start + end) * 0.5f + Vector3.down * 0.06f;
            edge.transform.localScale = new Vector3(width / spriteSize.x, 0.12f / spriteSize.y, 1f);

            var renderer = edge.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = TraversalEdgeColor(tilemap.gameObject.scene.name);

            ApplySorting(tilemap, renderer, 3);
        }

        static void ApplySorting(Tilemap tilemap, SpriteRenderer renderer, int offset)
        {
            var tileRenderer = tilemap.GetComponent<TilemapRenderer>();
            if (tileRenderer == null)
            {
                renderer.sortingOrder = offset;
                return;
            }

            renderer.sortingLayerID = tileRenderer.sortingLayerID;
            renderer.sortingOrder = Mathf.Max(offset, tileRenderer.sortingOrder + offset);
        }

        static Sprite VisibilityPixel()
        {
            if (_visibilityPixel != null) return _visibilityPixel;

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.name = "TraversalVisibilityPixel";
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _visibilityPixel = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f), 1f);
            _visibilityPixel.name = "TraversalVisibilityLine";
            return _visibilityPixel;
        }

        static Color TraversalEdgeColor(string sceneName)
        {
            if (sceneName.Contains("Gaze"))
                return new Color(0.82f, 0.74f, 1f, 0.98f);
            if (sceneName.Contains("Fracture"))
                return new Color(0.64f, 0.9f, 1f, 0.98f);
            return new Color(1f, 0.8f, 0.28f, 0.98f);
        }
    }
}
