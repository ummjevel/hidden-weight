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

        // 역할별 지형 타일을 쓰는 지역에서는 배경을 이만큼 덜 누른다. 지형이 실제 그림이 되면
        // 배경과 헷갈릴 이유가 줄어들어, 원화를 어둡게 덮어 둘 필요가 없다.
        static readonly Color TiledZoneBackgroundTint = new Color(0.86f, 0.86f, 0.86f, 0.78f);

        // 방 크기(SingleRoomBackgroundBuilder가 room.WorldBounds.size로 채운다). 0이면 예전처럼
        // 카메라 뷰포트에 맞춰 매 프레임 다시 스케일한다 — 카메라가 방 안을 움직일 때마다
        // 그림이 계속 다시 확대·축소·재배치되어, 실제 오브젝트 크기와의 관계가 프레임마다
        // 달라지고("이상해 보인다") 정적으로 맞춰볼 기준점이 없었다. 방 크기로 한 번만
        // 맞추면 그림이 방 안에 고정되어 최소한 같은 방 안에서는 크기·위치가 일정하다.
        [SerializeField] Vector2 worldSize;
        static Sprite _visibilityPixel;

        public void ConfigureWorldSize(Vector2 size) => worldSize = size;

        bool UsesWorldSize => worldSize.x > 0f && worldSize.y > 0f;

        void Awake()
        {
            ApplyReadabilityTint();
            EnsureRoomVisualCuller();
            BuildTraversalEdges();
            if (UsesWorldSize) ApplyWorldSize();
        }

        void OnValidate() => ApplyReadabilityTint();

        void LateUpdate()
        {
            if (UsesWorldSize) return; // 방 크기에 이미 고정했으면 매 프레임 다시 맞출 필요가 없다.

            Camera camera = Camera.main;
            if (camera != null)
                Refresh(camera);
        }

        // 방 전체를 한 번만 덮도록 스케일한다(가로세로 비율은 유지하고 큰 쪽에 맞춰 방을
        // 완전히 덮는다 — 방 폭·높이 비율이 그림과 다르면 한 축은 방보다 더 그려지지만,
        // 비율을 억지로 늘려 그림이 찌그러지는 것보다는 낫다). 위치는 SingleRoomBackgroundBuilder가
        // 이미 room.WorldBounds.center로 잡아 두므로 여기서는 스케일만 계산한다.
        void ApplyWorldSize()
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null) return;

            renderer.color = backgroundTint;

            Vector2 spriteSize = renderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

            float scale = Mathf.Max(worldSize.x / spriteSize.x, worldSize.y / spriteSize.y);
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        public void Refresh(Camera camera)
        {
            if (UsesWorldSize || camera == null || !camera.orthographic)
                return;

            transform.position = new Vector3(
                camera.transform.position.x,
                camera.transform.position.y,
                transform.position.z);

            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null)
                return;

            renderer.color = EffectiveTint();

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
                renderer.color = EffectiveTint();
        }

        // LateUpdate에서 매 프레임 부르므로 한 번만 판정하고 기억한다.
        int _tiledZone = -1;
        Color _resolvedTint;

        Color EffectiveTint()
        {
            if (_tiledZone < 0)
            {
                var palette = Resources.Load<TraversalArtPalette>("TraversalArtPalette");
                _tiledZone = palette != null
                    && palette.TileSetFor(gameObject.scene.name) != null ? 1 : 0;
                _resolvedTint = _tiledZone == 1
                    ? ApplyFractureCurve(TiledZoneBackgroundTint)
                    : backgroundTint;
            }
            return _resolvedTint;
        }

        // 균열의 밝기 곡선(설계 1.1 감정 곡선).
        //
        // 모든 방을 똑같이 밝히면 "밝아서 안심함 → 안전해 보인 것이 무너짐"이라는 이 지역의
        // 첫 감정이 만들어지지 않는다. 방 번호에 따라 밝기와 색을 조금씩 옮겨,
        // 텍스트 없이 진행 단계를 느끼게 한다.
        //
        //   F01        거의 천국처럼 밝음
        //   F02~F04    밝기는 유지하되 색이 미세하게 어긋남
        //   F05        예지 획득 — 청백색으로 집중
        //   F06~F10    민트와 라벤더의 시간차가 벌어짐
        //   F11        과도하게 하얗고 비어 있음
        //   F12        세로 균열광만 남도록 주변을 눌러 둠
        Color ApplyFractureCurve(Color baseTint)
        {
            int room = FractureRoomNumber();
            if (room <= 0) return baseTint;

            switch (room)
            {
                case 1: return Scale(baseTint, 1.06f, 1.05f, 1.02f, 0.92f);
                case 2:
                case 3:
                case 4: return Scale(baseTint, 1.0f, 0.99f, 1.03f, 0.85f);
                case 5: return Scale(baseTint, 1.02f, 1.06f, 1.12f, 0.95f);
                case 6:
                case 7:
                case 8:
                case 9:
                case 10: return Scale(baseTint, 0.95f, 1.02f, 1.06f, 0.82f);
                case 11: return Scale(baseTint, 1.1f, 1.1f, 1.1f, 0.96f);
                case 12: return Scale(baseTint, 0.72f, 0.76f, 0.9f, 0.9f);
                default: return baseTint;
            }
        }

        static Color Scale(Color c, float r, float g, float b, float a)
            => new Color(Mathf.Clamp01(c.r * r), Mathf.Clamp01(c.g * g),
                         Mathf.Clamp01(c.b * b), Mathf.Clamp01(a));

        // 배경 오브젝트가 속한 방 이름에서 번호를 읽는다(FractureRoom07 → 7).
        // 비밀방은 곡선을 적용하지 않는다 — 주 동선의 리듬을 흐리기 때문이다.
        int FractureRoomNumber()
        {
            var room = GetComponentInParent<Room>();
            string name = room != null ? room.name : gameObject.scene.name;
            const string marker = "FractureRoom";
            int at = name.IndexOf(marker, System.StringComparison.Ordinal);
            if (at < 0) return 0;
            return int.TryParse(name.Substring(at + marker.Length), out int parsed) ? parsed : 0;
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
            ConfigureR04ChimneyEntry();
            ConfigureR08ChimneyExit();
            DisableR07FakeStairWall();
            DisableLegacyResidueFloorArt(palette);
            foreach (var tilemap in FindObjectsByType<Tilemap>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                FlattenResidueEntrance(tilemap);
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

        static void DisableR07FakeStairWall()
        {
            var stair = GameObject.Find("R07_StairVisual");
            var room = GameObject.Find("Room07")?.GetComponent<Room>();
            if (room == null || !IsResidue(room.gameObject.scene.name)) return;

            // 이 스프라이트는 계단과 거대한 아치 벽이 한 장이다. 실제 충돌은 중간의
            // SafePlatform에만 있어 아치 부분을 그대로 통과하므로 가짜 벽 오브젝트 자체를
            // 끈다. 렌더러만 끄면 방 컬러가 다시 적용되는 순서에 따라 잠깐 재활성화될 수 있다.
            if (stair != null) stair.SetActive(false);

            // 이미 생성된 Full 씬에는 이전 빌더의 돌진 충돌벽이 남아 있다. 이 벽은
            // SafePlatform과 겹쳐 출구 계단 위에서 캐릭터를 허공에 세우므로 판정과 그림을
            // 함께 끈다. 새로 빌드되는 씬에서는 아예 만들지 않는다.
            var crashWall = GameObject.Find("R07_CrashWall");
            if (crashWall != null && IsResidue(crashWall.scene.name))
                crashWall.SetActive(false);
        }

        static void ConfigureR04ChimneyEntry()
        {
            var left = GameObject.Find("R04_Chimney_L")?.GetComponent<BoxCollider2D>();
            var right = GameObject.Find("R04_Chimney_R")?.GetComponent<BoxCollider2D>();
            var room = GameObject.Find("Room04")?.GetComponent<Room>();
            if (left == null || right == null || room == null
                || !IsResidue(left.gameObject.scene.name))
                return;

            float floorY = room.WorldBounds.min.y + 2f;
            float topY = room.WorldBounds.min.y + 12f;
            SetWallVerticalSpan(left, floorY + 3f, topY); // 걸어서 들어가는 왼쪽 입구
            // 마지막 오른쪽 벽은 통과 경로로 확정됐다. 상부 콜라이더와 그림 일부만 남기면
            // 통과 가능한 가짜 벽처럼 보이므로 판정과 시각물을 함께 제거한다.
            right.enabled = false;
            foreach (var renderer in right.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.enabled = false;
        }

        static void ConfigureR08ChimneyExit()
        {
            var left = GameObject.Find("R08_Chimney_L")?.GetComponent<BoxCollider2D>();
            var right = GameObject.Find("R08_Chimney_R")?.GetComponent<BoxCollider2D>();
            var room = GameObject.Find("Room08")?.GetComponent<Room>();
            if (left == null || right == null || room == null
                || !IsResidue(left.gameObject.scene.name))
                return;

            // 왼쪽은 왕복 벽점프 높이를 확보하고, 오른쪽은 착지대 보행면(local y=13)과
            // 정확히 이어 붙인다. 오른쪽 벽이 더 낮으면 꼭대기에 붙은 캐릭터가 착지대의
            // 세로 옆면에 막혀 멈춘다.
            float bottomY = room.WorldBounds.min.y + 4f;
            float leftTopY = room.WorldBounds.min.y + 13f;
            float rightTopY = room.WorldBounds.min.y + 13f;
            SetWallVerticalSpan(left, bottomY, leftTopY);
            SetWallVerticalSpan(right, bottomY, rightTopY);
            InvalidateMismatchedWallVisual(left);
            InvalidateMismatchedWallVisual(right);
            ConfigureR08LiftRoute(room);
        }

        static void ConfigureR08LiftRoute(Room room)
        {
            var movers = new System.Collections.Generic.List<MovingPlatform>();
            foreach (var mover in FindObjectsByType<MovingPlatform>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (room.WorldBounds.Contains(mover.transform.position)) movers.Add(mover);
            movers.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));

            Vector3 origin = room.WorldBounds.min;
            if (movers.Count >= 1)
            {
                movers[0].name = "R08_MovingLower";
                movers[0].ConfigurePath(origin + new Vector3(13f, 13f, 0f),
                    new Vector2(4f, 0f), 4f);
            }
            if (movers.Count >= 2)
            {
                // 기존 y=17은 아래 발판보다 4유닛 높아 최대 점프로 닿지 않는다.
                movers[1].name = "R08_MovingUpper";
                movers[1].ConfigurePath(origin + new Vector3(13f, 15.5f, 0f),
                    new Vector2(4f, 0f), 4f);
            }

            if (GameObject.Find("R08_UpperStep") != null
                || GameObject.Find("R08_UpperStep_Runtime") != null)
                return;

            // 낮춘 두 번째 이동 발판과 y=21 고정 바닥 사이에도 안전 점프 높이를 유지한다.
            var step = new GameObject("R08_UpperStep_Runtime");
            step.transform.SetParent(room.transform, true);
            step.transform.position = origin + new Vector3(14.25f, 18f, 0f);
            step.layer = LayerMask.NameToLayer("Ground");
            var collider = step.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(3f, 0.5f);

            var palette = Resources.Load<TraversalArtPalette>("TraversalArtPalette");
            Sprite sprite = palette != null ? palette.ResiduePlatformFor(3f) : null;
            if (sprite == null || sprite.bounds.size.x <= 0f) return;
            var art = new GameObject("Art");
            art.transform.SetParent(step.transform, false);
            float scale = 3f / sprite.bounds.size.x;
            art.transform.localScale = Vector3.one * scale;
            art.transform.localPosition = new Vector3(
                -sprite.bounds.center.x * scale,
                collider.offset.y + collider.size.y * 0.5f - sprite.bounds.max.y * scale,
                0f);
            var renderer = art.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 4;
        }

        static void InvalidateMismatchedWallVisual(BoxCollider2D wall)
        {
            Transform root = wall.transform.Find("WallClimbSurfaces_Runtime");
            if (root == null) return;

            bool hasRenderer = false;
            Bounds artBounds = default;
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!renderer.enabled || renderer.sprite == null) continue;
                if (!hasRenderer) artBounds = renderer.bounds;
                else artBounds.Encapsulate(renderer.bounds);
                hasRenderer = true;
            }

            if (hasRenderer
                && Mathf.Abs(artBounds.min.y - wall.bounds.min.y) <= 0.05f
                && Mathf.Abs(artBounds.max.y - wall.bounds.max.y) <= 0.05f)
                return;

            // 기존 루트를 다른 이름으로 치워 두면 같은 프레임에 새 높이의 정식 모듈을
            // 만들 수 있다. Destroy의 프레임 지연 때문에 옛 루트를 그대로 두면 재생성이
            // 건너뛰어져 가짜 윗부분이 계속 남는다.
            root.name = "WallClimbSurfaces_Stale";
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.enabled = false;
            Destroy(root.gameObject);
        }

        static void SetWallVerticalSpan(BoxCollider2D wall, float bottomY, float topY)
        {
            float height = topY - bottomY;
            if (height <= 0f || wall.size.y <= 0f) return;
            float parentScaleY = wall.transform.parent != null
                ? Mathf.Max(0.0001f, Mathf.Abs(wall.transform.parent.lossyScale.y)) : 1f;
            Vector3 scale = wall.transform.localScale;
            scale.y = height / (wall.size.y * parentScaleY);
            wall.transform.localScale = scale;
            Vector3 position = wall.transform.position;
            position.y = (bottomY + topY) * 0.5f;
            wall.transform.position = position;
        }

        static void FlattenResidueEntrance(Tilemap tilemap)
        {
            if (tilemap == null || !IsResidue(tilemap.gameObject.scene.name)) return;
            var roomObject = GameObject.Find("Room01");
            var room = roomObject != null ? roomObject.GetComponent<Room>() : null;
            if (room == null) return;

            Bounds bounds = room.WorldBounds;
            int xMin = tilemap.WorldToCell(new Vector3(bounds.min.x + 0.05f, bounds.min.y, 0f)).x;
            int xMax = tilemap.WorldToCell(new Vector3(bounds.max.x - 0.05f, bounds.min.y, 0f)).x;
            // R01의 기본 바닥은 방 바닥 기준 local y=2다. 과거 둔덕·계단 셀이 남아
            // 평지인데도 두 번째 계단 그림이 생성됐으므로 기본 표면보다 높은 셀만 제거한다.
            int surfaceCellY = tilemap.WorldToCell(
                new Vector3(bounds.min.x, bounds.min.y + 2f - 0.05f, 0f)).y;
            bool changed = false;
            for (int x = xMin; x <= xMax; x++)
            for (int y = surfaceCellY + 1; y < tilemap.cellBounds.yMax; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (!tilemap.HasTile(cell)) continue;
                tilemap.SetTile(cell, null);
                changed = true;
            }
            if (changed) tilemap.RefreshAllTiles();
        }

        static void DisableLegacyResidueFloorArt(TraversalArtPalette palette)
        {
            if (palette == null || !palette.HasResidueModularV3) return;

            // 씬에 저장된 구형 FloorArt와 런타임 V3 표면을 동시에 그리면, 실제 발밑 표면
            // 뒤로 두 번째 바닥이 한 단 더 솟아 보인다. 잔재에서만 구형 오버레이를 끄고
            // 충돌 Tilemap과 일치하는 런타임 모듈 하나만 사용한다.
            foreach (var renderer in FindObjectsByType<SpriteRenderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.name != "FloorArt"
                    || !IsResidue(renderer.gameObject.scene.name))
                    continue;
                renderer.enabled = false;
            }
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

            foreach (var zone in FindObjectsByType<ZoneTrigger>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!string.IsNullOrEmpty(zone.RequiredEncounterId)
                    && zone.GetComponent<BlockedHint>() == null)
                    BlockedHint.AttachTo(zone.gameObject, zoneTrigger: zone);

            // 방 문은 막지 않지만 안내가 더 필요하다 — 문 너머는 아직 로드되지 않은 다른
            // 씬이라, 눈으로는 길인지 막다른 벽인지 구분할 방법이 없다.
            foreach (var door in FindObjectsByType<RoomDoor>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (door.GetComponent<BlockedHint>() == null)
                    BlockedHint.AttachTo(door.gameObject, door: door);

            // 방에 처음 들어설 때 출구 쪽으로 한 번 흐르는 빛. 문 자체는 이제 보이지만
            // 넓은 방에서는 그 문이 화면 밖이라 방향을 알 수 없다.
            foreach (var room in FindObjectsByType<Room>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (room.GetComponent<RoomEntryCue>() == null)
                    room.gameObject.AddComponent<RoomEntryCue>();

            foreach (var rewindable in FindObjectsByType<Rewindable>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                // 잔재는 RewindHighlight가 전용 아이콘과 HOLD K를 표시한다. 같은 대상에
                // BlockedHint까지 붙이면 사진처럼 흰/금색 안내가 겹쳐 보인다.
                if (!rewindable.gameObject.scene.name.Contains("Residue")
                    && rewindable.GetComponent<BlockedHint>() == null)
                    BlockedHint.AttachTo(rewindable.gameObject, rewindable: rewindable);
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

                // 방 경계벽도 Ground 레이어다. 하지만 밟으라고 둔 발판이 아니라 "여기서 더
                // 못 간다"는 보이지 않는 장벽이고, 높이가 26유닛이나 된다 — 발판으로 보고
                // 윗면에 바닥 아트를 얹으면 하늘 높이(y≈18)에 타일 덩어리가 떠오른다.
                // 화면 구석에 떠 있던 정체불명의 그림이 바로 이것이었다.
                if (platform.name.Contains("Boundary")) continue;

                string sceneName = platform.gameObject.scene.name;
                TerrainTileSet tiles = palette == null ? null : palette.TileSetFor(sceneName);
                if (tiles != null)
                {
                    BuildTiledPlatformSurface(platform, palette, tiles, sceneName);
                    continue;
                }

                // 빌더가 이미 충돌 윗면에 맞춘 정식 V3 발판을 가진 오브젝트에 런타임 표면을
                // 한 겹 더 만들면 R07 계단처럼 두 개의 발판이 포개져 공중 디딤돌로 보인다.
                // 잔재의 기존 V3 한 장만 사용하고 다른 지역 표면 생성에는 관여하지 않는다.
                var bakedResidueArt = platform.transform.Find("ResiduePlatformV3")
                    ?.GetComponent<SpriteRenderer>();
                if (IsResidue(sceneName) && bakedResidueArt != null && bakedResidueArt.sprite != null)
                {
                    bakedResidueArt.enabled = true;
                    continue;
                }

                Sprite sprite = palette == null ? null : palette.SurfaceFor(sceneName);
                if (sprite == null || sprite.bounds.size.x <= 0f || sprite.bounds.size.y <= 0f) continue;

                var root = new GameObject("PlatformSurface_Runtime");
                root.transform.SetParent(platform.transform, false);

                if (IsResidue(sceneName) && palette.HasResidueModularV3)
                {
                    Bounds world = platform.bounds;
                    AddResidueHorizontalRun(root.transform, palette,
                        new Vector3(world.min.x, world.max.y, platform.transform.position.z),
                        new Vector3(world.max.x, world.max.y, platform.transform.position.z),
                        true, 4);

                    // V3 모듈 자체가 충돌면과 밑면을 충분히 보여 준다. R02에만 덧그리던 네 면
                    // 윤곽은 두 몬스터 사이를 가르는 굵은 선으로 보였으므로 더 만들지 않는다.
                    continue;
                }

                // BuildSolidBlock이 크기를 localScale에 싣기 때문에 콜라이더 자체는 1x1이다.
                // 자식은 그 스케일을 그대로 물려받으므로 로컬 단위로만 계산한다.
                float topY = platform.offset.y + platform.size.y * 0.5f;

                // 윗면을 덮는 얇은 띠로만 그린다. 타일맵 쪽(AddTraversalSurface)이 1.65유닛
                // 띠를 쓰는 것과 같은 규칙이다. 예전에는 콜라이더 전체 높이로 그렸는데,
                // 지형 스프라이트 피벗이 하단이라 그 그림이 발판 위로 솟아 두꺼운 블록에서는
                // 공중에 뜬 상자처럼 보였다.
                float scaleY = Mathf.Max(0.0001f, Mathf.Abs(platform.transform.lossyScale.y));
                float bandHeight = Mathf.Min(platform.size.y, 1.65f / scaleY);

                var surface = new GameObject("PlatformSurface");
                surface.transform.SetParent(root.transform, false);
                if (sceneName.Contains("Prologue"))
                {
                    var surfaceScale = new Vector3(
                        platform.size.x / sprite.bounds.size.x,
                        bandHeight / sprite.bounds.size.y,
                        1f);
                    surface.transform.localScale = surfaceScale;
                    // 튜토리얼 생성 이미지는 중앙 피벗이므로 실제 bounds의 윗끝을 맞춘다.
                    surface.transform.localPosition = new Vector3(
                        platform.offset.x - sprite.bounds.center.x * surfaceScale.x,
                        topY - sprite.bounds.max.y * surfaceScale.y,
                        0f);
                }
                else
                {
                    // 응시·균열은 기존 배치 계산을 그대로 유지한다.
                    surface.transform.localPosition = new Vector3(
                        platform.offset.x, topY - bandHeight, 0f);
                    surface.transform.localScale = new Vector3(
                        platform.size.x / sprite.bounds.size.x,
                        bandHeight / sprite.bounds.size.y,
                        1f);
                }

                var fill = surface.AddComponent<SpriteRenderer>();
                fill.sprite = sprite;
                fill.color = palette.SurfaceTintFor(sceneName);
                fill.sortingOrder = 4;

                // 튜토리얼 전용 지형 이미지가 충돌면을 충분히 보여 준다. 예전 회색 블록을
                // 보완하던 네 면 테두리는 이제 바닥 위의 굵은 회색 선으로만 남으므로 생략한다.
                if (sceneName.Contains("Prologue"))
                    continue;

                // 네 면을 모두 두른다. 윗면만 그리면 서서 밟는 발판은 보이지만, 세로 벽이나
                // 천장은 플레이어가 부딪히는 면(옆·아래)에 아무것도 없어 "안 보이는데 막히는
                // 벽"이 그대로 남는다 — 응시의 G08_RightEdge·G05_LowCeiling이 그랬다.
                // 타일맵 쪽이 BuildTilemapWallEdges로 세로면을 따로 그리는 것과 같은 이유다.
                float scaleX = Mathf.Max(0.0001f, Mathf.Abs(platform.transform.lossyScale.x));
                float thickY = 0.14f / scaleY;
                float thickX = 0.14f / scaleX;
                Color edgeColor = TraversalEdgeColor(sceneName);
                float left = platform.offset.x - platform.size.x * 0.5f;
                float right = platform.offset.x + platform.size.x * 0.5f;
                float bottom = platform.offset.y - platform.size.y * 0.5f;

                AddPlatformEdge(root.transform, "PlatformEdgeTop", edgeColor,
                    new Vector2(platform.offset.x, topY - thickY * 0.5f),
                    new Vector2(platform.size.x, thickY));
                AddPlatformEdge(root.transform, "PlatformEdgeBottom", edgeColor,
                    new Vector2(platform.offset.x, bottom + thickY * 0.5f),
                    new Vector2(platform.size.x, thickY));
                AddPlatformEdge(root.transform, "PlatformEdgeLeft", edgeColor,
                    new Vector2(left + thickX * 0.5f, platform.offset.y),
                    new Vector2(thickX, platform.size.y));
                AddPlatformEdge(root.transform, "PlatformEdgeRight", edgeColor,
                    new Vector2(right - thickX * 0.5f, platform.offset.y),
                    new Vector2(thickX, platform.size.y));
            }
        }

        // 타일셋을 가진 지역의 독립 발판. 끝단·중간을 나눠 그리므로 테두리 밴드가 필요 없다.
        //
        // 아트는 반드시 스케일 1인 자식에 준다. 루트 localScale로 크기를 맞추면 BoxCollider2D가
        // 같은 배율로 줄어 3x0.5 판정이 슬리버가 된다(잔재·응시에서 실제로 겪은 함정).
        static void BuildTiledPlatformSurface(BoxCollider2D platform, TraversalArtPalette palette,
                                              TerrainTileSet tiles, string sceneName)
        {
            float scaleX = Mathf.Max(0.0001f, Mathf.Abs(platform.transform.lossyScale.x));
            float scaleY = Mathf.Max(0.0001f, Mathf.Abs(platform.transform.lossyScale.y));

            // 콜라이더는 로컬 단위이고 그림 비율은 월드 단위로 따져야 한다.
            float bandLocal = Mathf.Min(platform.size.y, SurfaceHeight / scaleY);
            float bandWorld = bandLocal * scaleY;
            float widthWorld = platform.size.x * scaleX;
            if (bandWorld <= 0f || widthWorld <= 0f) return;

            var root = new GameObject("PlatformSurface_Runtime");
            root.transform.SetParent(platform.transform, false);

            int moduleCount = ModuleCount(widthWorld, bandWorld, tiles.topMid[0]);
            float moduleLocal = platform.size.x / moduleCount;
            float left = platform.offset.x - platform.size.x * 0.5f;
            float topY = platform.offset.y + platform.size.y * 0.5f;
            int seed = Mathf.RoundToInt(platform.transform.position.x);

            for (int i = 0; i < moduleCount; i++)
            {
                Sprite sprite = moduleCount == 1 ? tiles.MidAt(seed, 0)
                    : i == 0 ? tiles.topLeft
                    : i == moduleCount - 1 ? tiles.topRight
                    : tiles.MidAt(seed + i, 0);
                if (sprite == null) continue;

                var surface = new GameObject("PlatformSurface");
                surface.transform.SetParent(root.transform, false);
                surface.transform.localPosition = new Vector3(
                    left + moduleLocal * (i + 0.5f), topY - bandLocal, 0f);
                surface.transform.localScale = new Vector3(
                    moduleLocal / sprite.bounds.size.x,
                    bandLocal / sprite.bounds.size.y, 1f);

                var renderer = surface.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = palette.SurfaceTintFor(sceneName);
                renderer.sortingOrder = 4;
            }
        }

        static void AddPlatformEdge(Transform parent, string name, Color color,
                                    Vector2 localCenter, Vector2 localSize)
        {
            Sprite pixel = VisibilityPixel();
            Vector2 spriteSize = pixel.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

            var edge = new GameObject(name);
            edge.transform.SetParent(parent, false);
            edge.transform.localPosition = new Vector3(localCenter.x, localCenter.y, 0f);
            edge.transform.localScale = new Vector3(
                localSize.x / spriteSize.x, localSize.y / spriteSize.y, 1f);

            var renderer = edge.AddComponent<SpriteRenderer>();
            renderer.sprite = pixel;
            renderer.color = color;
            renderer.sortingOrder = 6;
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
            TerrainTileSet tiles = palette == null ? null : palette.TileSetFor(sceneName);
            if (tiles != null)
            {
                // 깊은 슬래브의 노출 옆면을 끝까지 그리면 물 위로 이어지는 기둥이 된다
                // (방 바깥 끝에서 실제로 그렇게 보였다). 보이는 것은 밟는 면 바로 아래의
                // 단면뿐이면 충분하다 — 윗부분만 남기고 잘라 낸다.
                //
                // 이 값은 씬 빌더의 바닥 두께와 짝이다. 빌더가 이보다 깊게 채우면 남는
                // 깊이가 아무 그림도 못 받아 충돌 타일맵의 회색만 남는다.
                if (height > MaxWallFaceDrop)
                {
                    start = end - Vector3.up * MaxWallFaceDrop;
                    height = MaxWallFaceDrop;
                }

                // 벽면은 가로 켜를 세로로 쌓아 만든다. 한 장을 벽 높이만큼 늘이면 대리석
                // 결이 수십 유닛으로 번져 벽이 아니라 색면이 된다.
                const float faceWidth = 1.1f;
                int courses = Mathf.Max(1, Mathf.RoundToInt(
                    height / (faceWidth / Aspect(tiles.wallCourse[0]))));
                float courseHeight = height / courses;
                for (int i = 0; i < courses; i++)
                {
                    Sprite sprite = tiles.CourseAt(x * 31 + i, yMin);
                    if (sprite == null) continue;

                    var course = new GameObject("TraversalWallCourse");
                    course.transform.SetParent(parent, true);
                    // 켜 스프라이트도 피벗이 바닥이라 켜의 아래 끝에 놓는다.
                    course.transform.position = start
                        + Vector3.up * (i * courseHeight)
                        + Vector3.right * (-side * faceWidth * 0.5f);
                    course.transform.localScale = new Vector3(
                        faceWidth / sprite.bounds.size.x,
                        courseHeight / sprite.bounds.size.y, 1f);

                    var courseRenderer = course.AddComponent<SpriteRenderer>();
                    courseRenderer.sprite = sprite;
                    courseRenderer.color = WallTint(palette.SurfaceTintFor(sceneName));
                    ApplySorting(tilemap, courseRenderer, 2);
                }
                return;
            }

            Sprite surfaceSprite = palette == null ? null : palette.SurfaceFor(sceneName);
            if (IsResidue(sceneName) && palette != null && palette.HasResidueModularV3)
            {
                // 타일맵의 단순 높이차는 올라타는 독립 기둥이 아니다. 등반 기둥 이미지를
                // 세우면 평평한 통로마다 장애물이 솟은 것처럼 보이므로, 얇은 석조 측면만 댄다.
                AddResidueFlatWallFace(parent, palette,
                    start + Vector3.right * (-side * 0.5f),
                    end + Vector3.right * (-side * 0.5f), 2);
                return;
            }
            if (sceneName.Contains("Prologue") && palette != null && palette.prologueFill != null)
            {
                // 낮은 턱의 옆면은 등반용 기둥이 아니다. 무늬가 이어지는 채움재만
                // 실제 노출 높이만큼 붙여 가짜 장애물처럼 솟아 보이지 않게 한다.
                AddPrologueFlatWallFace(parent, palette.prologueFill,
                    start + Vector3.right * (-side * 0.36f),
                    end + Vector3.right * (-side * 0.36f), 2);
                return;
            }
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
                string sceneName = wall.gameObject.scene.name;
                if (wall.isTrigger || wall.gameObject.layer != LayerMask.NameToLayer("Wall"))
                    continue;
                // 비활성 벽 건너뛰기는 R04 통과 경로를 위한 잔재 전용 처리다.
                // 다른 지역은 기존 동작을 유지한다.
                if (!wall.enabled && IsResidue(sceneName)) continue;

                bool residueV3 = IsResidue(sceneName) && palette != null
                    && palette.HasResidueModularV3;

                // 스크립트 재컴파일 뒤에도 런타임 루트는 남을 수 있다. 이 검사를 루트 존재
                // 확인보다 먼저 해야, 구형 갈색 Art가 다시 켜져 콜라이더 위로 솟지 않는다.
                if (residueV3)
                {
                    Transform runtimeRoot = wall.transform.Find("WallClimbSurfaces_Runtime");
                    foreach (var oldRenderer in wall.GetComponentsInChildren<SpriteRenderer>(true))
                        if (runtimeRoot == null || !oldRenderer.transform.IsChildOf(runtimeRoot))
                            oldRenderer.enabled = false;
                }

                if (wall.transform.Find("WallClimbSurfaces_Runtime") != null) continue;

                // 전투 잠금벽도 Wall 레이어라 여기까지 온다. 하지만 그건 올라가라고 세운 벽이
                // 아니라 "지금은 못 지나간다"는 벽이다. 등반 표시를 붙이면 오를 수 있다고
                // 잘못 안내하게 되므로 건너뛰고, 대신 막힌 이유를 문구로 알려 준다.
                if (wall.GetComponentInParent<Encounter>() != null) continue;

                // 방 경계벽도 Wall 레이어다. 하지만 그건 "여기서 더 못 간다"는 보이지 않는
                // 장벽이지 건축물이 아니다 — 대리석을 입히면 바닥 아래 허공까지 이어지는
                // 기둥이 되어, 물 위에 타일이 쌓인 것처럼 보인다(실제로 그렇게 나왔다).
                //
                // 이름만 보던 예전 가드는 "Boundary"만 걸렀다. 균열의 방 경계는
                // Fracture_RoomEdge_W/E라 하나도 걸리지 않았고, 12개 방 전부에서 좌우
                // 끝에 방 높이 28유닛짜리 대리석 기둥이 섰다 — 화면 왼쪽에 세로로
                // 반복되는 띠가 바로 이것이었다.
                //
                // 이름 대신 의도를 본다: 빌더가 렌더러를 꺼 둔 벽은 "보이지 않게 하려고
                // 세운 것"이다. 이름 검사는 렌더러가 아예 없는 경우를 위해 남긴다.
                if (IsInvisibleBarrier(wall)) continue;

                TerrainTileSet tiles = palette == null ? null : palette.TileSetFor(sceneName);
                if (tiles != null)
                {
                    BuildTiledWallClimbSurface(wall, palette, tiles, sceneName);
                    continue;
                }

                Sprite sprite = palette == null ? null : palette.SurfaceFor(sceneName);
                if (sprite == null || sprite.bounds.size.x <= 0f || sprite.bounds.size.y <= 0f) continue;

                var root = new GameObject("WallClimbSurfaces_Runtime");
                root.transform.SetParent(wall.transform, false);

                if (residueV3)
                {
                    // 씬에 저장된 구형 세로벽 Art는 원래 벽 스케일을 다시 곱해 그린다. 런타임에
                    // 벽 높이를 조절하면 콜라이더 위로 길게 돌출되므로 V3 벽 모듈만 남긴다.
                    foreach (var oldRenderer in wall.GetComponentsInChildren<SpriteRenderer>(true))
                        if (!oldRenderer.transform.IsChildOf(root.transform))
                            oldRenderer.enabled = false;

                    Bounds world = wall.bounds;
                    AddResidueVerticalRun(root.transform, palette,
                        new Vector3(world.center.x, world.min.y, wall.transform.position.z),
                        new Vector3(world.center.x, world.max.y, wall.transform.position.z), 4);
                    continue;
                }

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

        // 지나가지 못하게만 세운 보이지 않는 벽인가. 이 벽에는 아트를 입히지 않는다.
        //
        // "렌더러가 꺼져 있으면 보이지 않기를 의도한 벽"이라는 판정을 한 번 넣었다가
        // 되돌렸다 — 정반대였다. 빌더는 굴뚝·전장 벽의 **플레이스홀더** 렌더러를 끄고
        // 여기서 진짜 지형 아트를 입히기를 기대한다. 그 판정을 넣자 잔재 R04 굴뚝의
        // 벽타기 면이 통째로 사라졌다(ResiduePlacementTests.벽타기_굴뚝의_충돌면이_눈에_보인다).
        //
        // 그래서 이름만 본다. 목록에 균열의 RoomEdge를 더한 것이 이번 수정의 전부다.
        static bool IsInvisibleBarrier(BoxCollider2D wall)
            => wall.name.Contains("Boundary") || wall.name.Contains("RoomEdge");

        // 굴뚝·전장 벽처럼 타일맵 밖에 선 Wall 블록. 켜를 세로로 쌓아 전체 면을 덮는다.
        static void BuildTiledWallClimbSurface(BoxCollider2D wall, TraversalArtPalette palette,
                                               TerrainTileSet tiles, string sceneName)
        {
            float scaleY = Mathf.Max(0.0001f, Mathf.Abs(wall.transform.lossyScale.y));
            float scaleX = Mathf.Max(0.0001f, Mathf.Abs(wall.transform.lossyScale.x));
            float heightWorld = wall.size.y * scaleY;
            float widthWorld = wall.size.x * scaleX;
            if (heightWorld <= 0f || widthWorld <= 0f) return;

            var root = new GameObject("WallClimbSurfaces_Runtime");
            root.transform.SetParent(wall.transform, false);

            int courses = Mathf.Max(1, Mathf.RoundToInt(
                heightWorld / (widthWorld / Aspect(tiles.wallCourse[0]))));
            float courseLocal = wall.size.y / courses;
            float bottom = wall.offset.y - wall.size.y * 0.5f;
            int seed = Mathf.RoundToInt(wall.transform.position.y);

            for (int i = 0; i < courses; i++)
            {
                Sprite sprite = tiles.CourseAt(seed, i);
                if (sprite == null) continue;

                var course = new GameObject("WallClimbCourse");
                course.transform.SetParent(root.transform, false);
                course.transform.localPosition = new Vector3(
                    wall.offset.x, bottom + courseLocal * i, 0f);
                course.transform.localScale = new Vector3(
                    wall.size.x / sprite.bounds.size.x,
                    courseLocal / sprite.bounds.size.y, 1f);

                var renderer = course.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = WallTint(palette.SurfaceTintFor(sceneName));
                renderer.sortingOrder = 4;
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

        // 바닥 윗면 한 구간. 구간의 시작과 끝은 실제 지형이 끊기는 자리이므로 끝단 타일을 쓴다.
        const float SurfaceHeight = 1.65f;

        // 밟는 면 아래로 옆면 그림을 그리는 높이. 씬 빌더의 바닥 슬래브 두께와 같은 값이다
        // (ZoneSceneBuilder의 FractureFloorDepth).
        public const float MaxWallFaceDrop = 3f;

        static void AddTraversalSurface(Tilemap tilemap, Transform parent,
                                        TraversalArtPalette palette,
                                        int xMin, int xMax, int y)
        {
            Vector3 start = tilemap.CellToWorld(new Vector3Int(xMin, y + 1, 0));
            Vector3 end = tilemap.CellToWorld(new Vector3Int(xMax, y + 1, 0));
            float width = Vector3.Distance(start, end);
            string sceneName = tilemap.gameObject.scene.name;
            TerrainTileSet tiles = palette == null ? null : palette.TileSetFor(sceneName);

            if (tiles != null && width > 0f)
            {
                // 모듈 폭은 그림의 원래 비율에서 얻는다. 폭을 먼저 정하고 그림을 맞추면
                // 장식이 옆으로 늘어나 대리석 무늬가 번진다.
                int moduleCount = ModuleCount(width, SurfaceHeight, tiles.topMid[0]);
                float moduleWidth = width / moduleCount;
                for (int i = 0; i < moduleCount; i++)
                {
                    Sprite sprite = moduleCount == 1 ? tiles.MidAt(xMin, y)
                        : i == 0 ? tiles.topLeft
                        : i == moduleCount - 1 ? tiles.topRight
                        : tiles.MidAt(xMin + i, y);
                    if (sprite == null) continue;

                    var surface = new GameObject("TraversalSurface");
                    surface.transform.SetParent(parent, true);
                    // 지형 시트 피벗이 Bottom Center라 그림 윗면이 충돌 표면에 닿도록 내린다.
                    surface.transform.position =
                        Vector3.Lerp(start, end, (i + 0.5f) / moduleCount)
                        + Vector3.down * SurfaceHeight;
                    surface.transform.localScale = new Vector3(
                        moduleWidth / sprite.bounds.size.x,
                        SurfaceHeight / sprite.bounds.size.y, 1f);

                    var tileRenderer = surface.AddComponent<SpriteRenderer>();
                    tileRenderer.sprite = sprite;
                    tileRenderer.color = palette.SurfaceTintFor(sceneName);
                    ApplySorting(tilemap, tileRenderer, 1);
                }

                // 테두리 선을 긋지 않는다. 타일 그림이 이미 윗면을 마감한다.
                return;
            }

            if (IsResidue(sceneName) && palette != null && palette.HasResidueModularV3)
            {
                AddResidueGroundMass(tilemap, parent, palette, xMin, xMax, y);
                AddResidueHorizontalRun(parent, palette, start, end, false, 1);
                return;
            }

            if (sceneName.Contains("Prologue") && palette != null && palette.prologueFill != null)
                AddPrologueGroundMass(tilemap, parent, palette.prologueFill, xMin, xMax, y);

            Sprite surfaceSprite = palette == null ? null : palette.SurfaceFor(sceneName);
            if (surfaceSprite != null && width > 0f)
            {
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
                        if (sceneName.Contains("Prologue"))
                        {
                            var worldScale = new Vector3(
                                moduleWidth / spriteSize.x, SurfaceHeight / spriteSize.y, 1f);
                            surface.transform.localScale = worldScale;
                            Vector3 segmentCenter = Vector3.Lerp(start, end, t);
                            surface.transform.position = new Vector3(
                                segmentCenter.x - surfaceSprite.bounds.center.x * worldScale.x,
                                start.y - surfaceSprite.bounds.max.y * worldScale.y,
                                segmentCenter.z);
                        }
                        else
                        {
                            // 응시·균열은 기존 피벗 전제와 위치 계산을 그대로 유지한다.
                            surface.transform.position = Vector3.Lerp(start, end, t)
                                + Vector3.down * SurfaceHeight;
                            surface.transform.localScale = new Vector3(
                                moduleWidth / spriteSize.x, SurfaceHeight / spriteSize.y, 1f);
                        }

                        var renderer = surface.AddComponent<SpriteRenderer>();
                        renderer.sprite = surfaceSprite;
                        renderer.color = palette.SurfaceTintFor(sceneName);
                        ApplySorting(tilemap, renderer, 1);
                    }
                }
            }

            if (!sceneName.Contains("Prologue"))
                AddTraversalEdge(tilemap, parent, xMin, xMax, y);
        }

        // 그림의 원래 가로세로 비율을 지키면서 구간을 채우는 데 필요한 모듈 수.
        static int ModuleCount(float span, float thickness, Sprite sprite)
        {
            if (sprite == null || sprite.bounds.size.y <= 0f) return 1;
            float natural = thickness * (sprite.bounds.size.x / sprite.bounds.size.y);
            if (natural <= 0f) return 1;
            return Mathf.Max(1, Mathf.RoundToInt(span / natural));
        }

        // 잔재 V3는 생성 원화의 종횡비를 보존한 채 조각을 반복한다. 폭이 달라도 돌과
        // 철골의 굵기가 일정해지고, 별도의 노란 충돌선 없이 푸른 석재 윗면이 길을 알린다.
        static void AddResidueHorizontalRun(Transform parent, TraversalArtPalette palette,
                                            Vector3 start, Vector3 end,
                                            bool standalone, int sortingOrder)
        {
            float width = Mathf.Abs(end.x - start.x);
            if (width <= 0.01f) return;

            Sprite sample = standalone
                ? palette.ResiduePlatformFor(width)
                : palette.residueGroundMiddle;
            if (sample == null || sample.bounds.size.x <= 0f || sample.bounds.size.y <= 0f)
                return;

            const float targetHeight = 1.65f;
            float naturalScale = targetHeight / sample.bounds.size.y;
            float naturalWidth = sample.bounds.size.x * naturalScale;
            int count = Mathf.Max(1, Mathf.CeilToInt(width / Mathf.Max(0.5f, naturalWidth * 0.94f)));
            float segmentWidth = width / count;
            float direction = Mathf.Sign(end.x - start.x);

            for (int i = 0; i < count; i++)
            {
                Sprite piece = sample;
                // 일반 보행면의 Left/Right 캡에는 세로 석주 장식이 들어 있어, 이어지는
                // 평지에서도 통과 불가능한 장애물처럼 보였다. 실제 독립 발판이 아닌 타일맵
                // 바닥은 중앙 평면 조각만 반복하고, 낭떠러지 여부는 지형 자체로 표현한다.

                // 캡 조각의 비율도 보존한다. 연결점이 겹치도록 각 구간의 중앙을 고정한다.
                float pieceScale = Mathf.Max(targetHeight / piece.bounds.size.y,
                    segmentWidth / piece.bounds.size.x);
                var go = new GameObject(standalone
                    ? "ResiduePlatformModule" : "ResidueGroundModule");
                go.transform.SetParent(parent, true);
                go.transform.position = new Vector3(
                    start.x + direction * segmentWidth * (i + 0.5f),
                    start.y - piece.bounds.size.y * pieceScale,
                    start.z);
                Vector3 parentScale = parent.lossyScale;
                go.transform.localScale = new Vector3(
                    pieceScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                    pieceScale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = piece;
                renderer.color = Color.white;
                renderer.sortingOrder = sortingOrder;
            }
        }

        static void AddResidueGroundMass(Tilemap tilemap, Transform parent,
                                         TraversalArtPalette palette,
                                         int xMin, int xMax, int surfaceY)
        {
            Sprite fill = palette.residueGroundFill;
            if (fill == null || fill.bounds.size.x <= 0f || fill.bounds.size.y <= 0f)
                return;

            // 이 수평면 아래에서 모든 열에 공통으로 실제 타일이 이어지는 깊이까지만 채운다.
            // 따라서 구덩이를 가짜 그림으로 막지 않으면서, 숨겨 둔 회색 타일 대신 석조 몸체가
            // 바닥 아래로 이어져 보인다.
            int commonBottom = tilemap.cellBounds.yMin;
            bool first = true;
            for (int x = xMin; x < xMax; x++)
            {
                int bottom = surfaceY;
                while (bottom - 1 >= tilemap.cellBounds.yMin
                       && tilemap.HasTile(new Vector3Int(x, bottom - 1, 0)))
                    bottom--;
                commonBottom = first ? bottom : Mathf.Max(commonBottom, bottom);
                first = false;
            }

            Vector3 worldBottom = tilemap.CellToWorld(new Vector3Int(xMin, commonBottom, 0));
            Vector3 worldTop = tilemap.CellToWorld(new Vector3Int(xMin, surfaceY + 1, 0));
            float collisionDepth = worldTop.y - worldBottom.y;
            float width = xMax - xMin;
            if (collisionDepth <= 0.05f || width <= 0.05f) return;

            // 충돌 타일은 낙하 방지를 위해 보통 6유닛 깊지만, 그림까지 그 깊이로 키우면
            // 석벽 한 장이 화면 절반을 차지한다. 보행면 아래 얕은 기단만 보여 주고 나머지는
            // 배경 어둠에 묻어 꿈 공간의 깊이로 처리한다.
            float visibleDepth = Mathf.Min(collisionDepth, 2.15f);
            float scale = visibleDepth / fill.bounds.size.y;
            float naturalWidth = fill.bounds.size.x * scale;
            int count = Mathf.Max(1, Mathf.CeilToInt(width / Mathf.Max(1f, naturalWidth * 0.9f)));
            float segmentWidth = width / count;
            for (int i = 0; i < count; i++)
            {
                var mass = new GameObject("ResidueGroundMass");
                mass.transform.SetParent(parent, true);
                mass.transform.position = new Vector3(
                    xMin + segmentWidth * (i + 0.5f), worldTop.y - visibleDepth, worldBottom.z);
                mass.transform.localScale = Vector3.one * scale;

                var renderer = mass.AddComponent<SpriteRenderer>();
                renderer.sprite = fill;
                renderer.color = new Color(0.72f, 0.76f, 0.86f, 0.58f);
                renderer.sortingOrder = 0;
            }
        }

        static void AddPrologueGroundMass(Tilemap tilemap, Transform parent, Sprite fill,
                                          int xMin, int xMax, int surfaceY)
        {
            if (fill == null || fill.bounds.size.x <= 0f || fill.bounds.size.y <= 0f)
                return;

            int commonBottom = tilemap.cellBounds.yMin;
            bool first = true;
            for (int x = xMin; x < xMax; x++)
            {
                int bottom = surfaceY;
                while (bottom - 1 >= tilemap.cellBounds.yMin
                       && tilemap.HasTile(new Vector3Int(x, bottom - 1, 0)))
                    bottom--;
                commonBottom = first ? bottom : Mathf.Max(commonBottom, bottom);
                first = false;
            }

            Vector3 worldBottom = tilemap.CellToWorld(new Vector3Int(xMin, commonBottom, 0));
            Vector3 worldTop = tilemap.CellToWorld(new Vector3Int(xMin, surfaceY + 1, 0));
            float visibleDepth = Mathf.Min(worldTop.y - worldBottom.y, 2f);
            float width = xMax - xMin;
            if (visibleDepth <= 0.05f || width <= 0.05f) return;

            // 일반 바닥 아래는 기둥 조각을 세우지 않고 전용 무경계 텍스처로 한 덩어리처럼
            // 채운다. Tiled 모드라 폭이나 깊이가 달라도 이미지가 늘어나지 않는다.
            var mass = new GameObject("PrologueGroundMass");
            mass.transform.SetParent(parent, true);
            mass.transform.position = new Vector3(
                xMin + width * 0.5f, worldTop.y - visibleDepth * 0.5f, worldTop.z);

            var renderer = mass.AddComponent<SpriteRenderer>();
            renderer.sprite = fill;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(width, visibleDepth);
            renderer.color = new Color(0.76f, 0.78f, 1f, 0.72f);
            renderer.sortingOrder = 0;
        }

        static void AddResidueVerticalRun(Transform parent, TraversalArtPalette palette,
                                          Vector3 start, Vector3 end, int sortingOrder,
                                          float targetWidth = 1f)
        {
            Sprite piece = palette.residueClimbPillar != null
                ? palette.residueClimbPillar : palette.residueWallMiddle;
            if (piece == null || piece.bounds.size.x <= 0f || piece.bounds.size.y <= 0f)
                return;

            float height = Mathf.Abs(end.y - start.y);
            if (height <= 0.01f) return;

            float naturalScale = targetWidth / piece.bounds.size.x;
            float naturalHeight = piece.bounds.size.y * naturalScale;
            int count = Mathf.Max(1, Mathf.CeilToInt(height / Mathf.Max(0.5f, naturalHeight * 0.96f)));
            float segmentHeight = height / count;
            float scaleX = naturalScale;
            float scaleY = segmentHeight / piece.bounds.size.y;
            float direction = Mathf.Sign(end.y - start.y);

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("ResidueWallModule");
                go.transform.SetParent(parent, true);
                go.transform.position = new Vector3(start.x,
                    start.y + direction * segmentHeight * (i + 0.5f), start.z);
                Vector3 parentScale = parent.lossyScale;
                go.transform.localScale = new Vector3(
                    scaleX / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                    scaleY / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = piece;
                renderer.color = Color.white;
                renderer.sortingOrder = sortingOrder;
            }
        }

        static void AddResidueFlatWallFace(Transform parent, TraversalArtPalette palette,
                                           Vector3 start, Vector3 end, int sortingOrder)
        {
            Sprite piece = palette.residueGroundFill;
            if (piece == null || piece.bounds.size.x <= 0f || piece.bounds.size.y <= 0f)
                return;

            float height = Mathf.Abs(end.y - start.y);
            if (height <= 0.01f) return;

            const float faceWidth = 0.58f;
            float scale = faceWidth / piece.bounds.size.x;
            float naturalHeight = Mathf.Max(0.35f, piece.bounds.size.y * scale);
            int count = Mathf.Max(1, Mathf.CeilToInt(height / naturalHeight));
            float segmentHeight = height / count;
            float direction = Mathf.Sign(end.y - start.y);
            float scaleY = segmentHeight / piece.bounds.size.y;

            for (int i = 0; i < count; i++)
            {
                var face = new GameObject("ResidueFlatWallFace");
                face.transform.SetParent(parent, true);
                face.transform.position = new Vector3(start.x,
                    start.y + direction * segmentHeight * (i + 0.5f), start.z);
                Vector3 parentScale = parent.lossyScale;
                face.transform.localScale = new Vector3(
                    scale / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                    scaleY / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);

                var renderer = face.AddComponent<SpriteRenderer>();
                renderer.sprite = piece;
                renderer.color = new Color(0.62f, 0.68f, 0.8f, 0.72f);
                renderer.sortingOrder = sortingOrder;
            }
        }

        static void AddPrologueFlatWallFace(Transform parent, Sprite fill,
                                            Vector3 start, Vector3 end, int sortingOrder)
        {
            if (fill == null || fill.bounds.size.x <= 0f || fill.bounds.size.y <= 0f)
                return;
            float height = Mathf.Abs(end.y - start.y);
            if (height <= 0.01f) return;

            const float faceWidth = 0.58f;
            var face = new GameObject("PrologueFlatWallFace");
            face.transform.SetParent(parent, true);
            face.transform.position = (start + end) * 0.5f;

            var renderer = face.AddComponent<SpriteRenderer>();
            renderer.sprite = fill;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(faceWidth, height);
            renderer.color = new Color(0.76f, 0.78f, 1f, 0.78f);
            renderer.sortingOrder = sortingOrder;
        }

        static bool IsResidue(string sceneName)
            => sceneName.Contains("Residue");

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

        // 벽면은 바닥보다 한 단계 눌러 그린다. 같은 밝기로 두면 벽이 화면에서 가장 밝은
        // 면이 되어 "밟을 수 있는 곳"보다 먼저 눈에 들어온다 — 서 있을 수 있는 자리가
        // 가장 잘 읽혀야 한다.
        static Color WallTint(Color surface)
            => new Color(surface.r * 0.72f, surface.g * 0.75f, surface.b * 0.82f, surface.a);

        static float Aspect(Sprite sprite)
        {
            if (sprite == null || sprite.bounds.size.y <= 0f) return 1f;
            return sprite.bounds.size.x / sprite.bounds.size.y;
        }

        static Color TraversalEdgeColor(string sceneName)
        {
            if (sceneName.Contains("Prologue"))
                return new Color(0.86f, 0.9f, 1f, 0.98f);
            if (sceneName.Contains("Residue"))
                return new Color(0.66f, 0.8f, 1f, 0.96f);
            if (sceneName.Contains("Gaze"))
                return new Color(0.82f, 0.74f, 1f, 0.98f);
            if (sceneName.Contains("Fracture"))
                return new Color(0.64f, 0.9f, 1f, 0.98f);
            return new Color(1f, 0.8f, 0.28f, 0.98f);
        }
    }
}
