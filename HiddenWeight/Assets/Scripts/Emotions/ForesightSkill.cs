using System.Collections.Generic;
using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.World;

namespace HiddenWeight.Emotions
{
    // 예지. 탭 입력. 반경 안 IForeseeable들의 previewLeadTime 뒤 상태를 보여준다.
    //
    // 고스트는 대상 그림을 그대로 복제하지 않는다. 예전 구현은 대상의 스프라이트와 루트
    // localScale을 그대로 베꼈는데, 큰 환경 오브젝트가 하나라도 걸리면 화면 상단을 덮는
    // 거대한 그림이 떠올랐고, 겉모습이 자식으로 옮겨진 적은 루트의 플레이스홀더 사각형이
    // 고스트가 됐다. 지금은 실제로 그려지는 렌더러를 찾아 그 크기로, 흰 외곽 실루엣만 만든다.
    //
    // 설계 근거(LEVEL_40_FRACTURE_DESIGN 7.2 공정성 규칙): 고스트는 정확해야 하고, 한 번에
    // 강조되는 필수 대상은 소수여야 하며, 배경과 겹쳐도 흰 외곽선이 사라지면 안 된다.
    public class ForesightSkill : EmotionSkill
    {
        public override EmotionId Id => EmotionId.Foresight;

        // 한 번에 강하게 보여줄 대상 수. 나머지는 낮은 밝기로 남겨 "더 있다"는 것만 알린다.
        const int StrongTargets = 3;

        // 이보다 큰 그림은 예측 대상에서 뺀다. 배경 장식·대형 구조물은 미래 위치를 알아도
        // 플레이어가 할 수 있는 행동이 없고, 화면만 덮는다.
        const float MaxGhostExtent = 7f;

        // 발동 순간 "무엇을 봤는지" 연결선을 잠깐 보여준다. 길면 화면이 어지럽다.
        const float LinkSeconds = 0.12f;

        readonly List<GameObject> _spawned = new List<GameObject>();
        readonly List<LineRenderer> _links = new List<LineRenderer>();
        float _timer;
        float _linkTimer;

        static Material _ghostMaterial;
        static Material _lineMaterial;
        static Sprite _pixel;

        protected override void OnBegin()
        {
            _timer = Data.effectDuration;
            _linkTimer = LinkSeconds;

            // 가까운 것부터 고른다. 무엇이 "필수 대상"인지 판단할 근거가 거리뿐이다.
            var found = new List<(IForeseeable target, SpriteRenderer visual, float distance)>();
            var seen = new HashSet<Transform>();

            foreach (var hit in Physics2D.OverlapCircleAll(Player.transform.position, Data.range))
            {
                var target = hit.GetComponentInParent<IForeseeable>();
                if (target == null || !seen.Add(target.Transform)) continue;

                var visual = VisualOf(target);
                if (visual == null) continue;

                Vector2 size = visual.bounds.size;
                if (size.x > MaxGhostExtent || size.y > MaxGhostExtent) continue;

                float distance = Vector2.Distance(Player.transform.position, target.Transform.position);
                found.Add((target, visual, distance));
            }

            found.Sort((a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < found.Count; i++)
            {
                var (target, visual, _) = found[i];
                bool strong = i < StrongTargets;

                if (target.PredictActive(Data.previewLeadTime))
                {
                    SpawnGhost(target, visual, strong);
                    if (strong) SpawnLink(visual.bounds.center);
                    continue;
                }

                // 사라질 발판. "고스트가 없다"만으로는 못 읽는다 — 봤는데 아무 일도 없는 것과
                // 구분되지 않기 때문이다. 지금 발판의 외곽을 두 번 끊어 그려, 이 자리가
                // 곧 끊어진다는 것을 현재 공간 위에서 알린다.
                SpawnBreakingOutline(visual.bounds, strong);
            }
        }

        // 실제로 화면에 그려지는 렌더러를 찾는다. 겉모습이 자식으로 옮겨진 대상이 많아
        // 루트 렌더러를 믿으면 플레이스홀더가 잡힌다.
        static SpriteRenderer VisualOf(IForeseeable target)
        {
            foreach (var renderer in target.Transform.GetComponentsInChildren<SpriteRenderer>(false))
                if (renderer.enabled && renderer.sprite != null) return renderer;
            return null;
        }

        void SpawnGhost(IForeseeable target, SpriteRenderer visual, bool strong)
        {
            var go = new GameObject("ForesightGhost");
            // 예측 위치는 대상 원점 기준이다. 그림이 자식에 있으면 그만큼 어긋나므로
            // 원점에서 그림 중심까지의 차이를 그대로 더한다.
            Vector3 offset = visual.bounds.center - target.Transform.position;
            go.transform.position = target.PredictPosition(Data.previewLeadTime) + offset;
            // 루트가 아니라 실제로 그려지는 것의 월드 배율을 쓴다.
            go.transform.localScale = visual.transform.lossyScale;
            go.transform.rotation = visual.transform.rotation;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = visual.sprite;
            sr.material = GhostMaterial();
            sr.color = new Color(1f, 1f, 1f, strong ? 1f : 0.4f);
            sr.sortingOrder = 50;
            _spawned.Add(go);
        }

        // 플레이어에서 대상까지 짧게 잇는 선. 무엇을 읽었는지 알려 주고 곧 사라진다.
        void SpawnLink(Vector3 to)
        {
            var go = new GameObject("ForesightLink");
            var line = go.AddComponent<LineRenderer>();
            line.material = LineMaterial();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.SetPosition(0, Player.transform.position);
            line.SetPosition(1, to);
            line.startWidth = 0.06f;
            line.endWidth = 0.02f;
            line.startColor = new Color(1f, 1f, 1f, 0.75f);
            line.endColor = new Color(1f, 1f, 1f, 0.2f);
            line.sortingOrder = 49;
            _spawned.Add(go);
            _links.Add(line);
        }

        // 두 번 끊어진 사각 외곽. 각 변을 두 토막으로 그려 가운데를 비운다.
        void SpawnBreakingOutline(Bounds bounds, bool strong)
        {
            var root = new GameObject("ForesightBreaking");
            root.transform.position = bounds.center;
            _spawned.Add(root);

            const float thickness = 0.08f;
            const float gap = 0.34f;          // 변 길이 중 비우는 비율
            float halfX = bounds.extents.x;
            float halfY = bounds.extents.y;
            float runX = bounds.size.x * (1f - gap) * 0.5f;
            float runY = bounds.size.y * (1f - gap) * 0.5f;
            float offX = bounds.size.x * 0.5f - runX * 0.5f;
            float offY = bounds.size.y * 0.5f - runY * 0.5f;
            var color = new Color(1f, 1f, 1f, strong ? 0.95f : 0.4f);

            // 위·아래는 좌우 두 토막, 좌·우는 상하 두 토막.
            AddDash(root.transform, new Vector2(-offX, halfY), new Vector2(runX, thickness), color);
            AddDash(root.transform, new Vector2(offX, halfY), new Vector2(runX, thickness), color);
            AddDash(root.transform, new Vector2(-offX, -halfY), new Vector2(runX, thickness), color);
            AddDash(root.transform, new Vector2(offX, -halfY), new Vector2(runX, thickness), color);
            AddDash(root.transform, new Vector2(-halfX, -offY), new Vector2(thickness, runY), color);
            AddDash(root.transform, new Vector2(-halfX, offY), new Vector2(thickness, runY), color);
            AddDash(root.transform, new Vector2(halfX, -offY), new Vector2(thickness, runY), color);
            AddDash(root.transform, new Vector2(halfX, offY), new Vector2(thickness, runY), color);
        }

        static void AddDash(Transform parent, Vector2 offset, Vector2 size, Color color)
        {
            var go = new GameObject("Dash");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            Sprite pixel = Pixel();
            go.transform.localScale = new Vector3(
                size.x / pixel.bounds.size.x, size.y / pixel.bounds.size.y, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pixel;
            sr.color = color;
            sr.sortingOrder = 51;
        }

        protected override void OnTick(float dt)
        {
            if (_linkTimer > 0f)
            {
                _linkTimer -= dt;
                if (_linkTimer <= 0f)
                    foreach (var link in _links) if (link != null) link.enabled = false;
            }

            _timer -= dt;
            if (_timer <= 0f) End();
        }

        protected override void OnEnd()
        {
            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear();
            _links.Clear();
        }

        static Material GhostMaterial()
        {
            if (_ghostMaterial != null) return _ghostMaterial;

            var shader = Shader.Find("Hidden Weight/SpriteOutline");
            if (shader == null) return null;

            _ghostMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
            _ghostMaterial.SetColor("_OutlineColor", Color.white);
            _ghostMaterial.SetFloat("_OutlineWidth", 2.6f);
            // 안쪽을 거의 비워 "형태만" 남긴다. 미래는 아직 실체가 아니다.
            _ghostMaterial.SetFloat("_FillAlpha", 0.12f);
            return _ghostMaterial;
        }

        static Material LineMaterial()
        {
            if (_lineMaterial != null) return _lineMaterial;
            var shader = Shader.Find("Sprites/Default");
            _lineMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
            return _lineMaterial;
        }

        static Sprite Pixel()
        {
            if (_pixel != null) return _pixel;
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _pixel = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            _pixel.name = "ForesightPixel";
            return _pixel;
        }
    }
}
