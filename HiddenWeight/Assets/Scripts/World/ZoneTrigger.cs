using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    // 지역 클리어 지점. 플레이어가 닿으면 다음 씬으로 넘어간다.
    [RequireComponent(typeof(Collider2D))]
    public class ZoneTrigger : MonoBehaviour
    {
        [SerializeField] bool marksFractureCleared; // 균열 지역 출구만 true
        [SerializeField] string requiredEncounterId;

        public string RequiredEncounterId => requiredEncounterId;

        // 조건이 없으면 항상 열려 있다. 안내 문구(BlockedHint)가 "왜 못 나가는지"를
        // 판단할 때 쓰므로, 막는 쪽 로직과 같은 기준을 한곳에서 읽게 둔다.
        public bool IsOpen =>
            string.IsNullOrEmpty(requiredEncounterId)
            || (GameManager.Instance != null
                && GameManager.Instance.Progress.IsEncounterCleared(requiredEncounterId));

        SpriteRenderer[] _exitBars;
        TextMesh _exitLabel;
        static Sprite _barSprite;

        void Start()
        {
            if (!string.IsNullOrEmpty(requiredEncounterId)) BuildExitVisual();
        }

        void Update()
        {
            if (_exitBars == null) return;

            bool open = GameManager.Instance != null
                && GameManager.Instance.Progress.IsEncounterCleared(requiredEncounterId);
            Color color = open
                ? new Color(0.55f, 0.9f, 0.95f, 0.9f)
                : new Color(0.95f, 0.68f, 0.25f, 0.82f);
            float pulse = 0.82f + Mathf.PingPong(Time.time * 1.4f, 0.18f);
            color.a *= pulse;

            foreach (var bar in _exitBars)
                if (bar != null) bar.color = color;
            if (_exitLabel != null)
            {
                bool residue = gameObject.scene.name == "Zone_Residue_Full";
                _exitLabel.text = residue
                    ? (open ? "다음 지역 · 응시" : "기억의 교수자를 처치하면 열립니다.")
                    : (open ? "응시로 가는 통로" : "기억의 교수자를 쓰러뜨려야 열린다");
                _exitLabel.color = color;
            }
        }

        public void RequireEncounter(string encounterId)
        {
            requiredEncounterId = encounterId;
            if (Application.isPlaying && !string.IsNullOrEmpty(requiredEncounterId)) BuildExitVisual();
        }

        void BuildExitVisual()
        {
            if (transform.Find("RegionExitVisual") != null) return;

            var root = new GameObject("RegionExitVisual");
            root.transform.SetParent(transform, false);

            _exitBars = new[]
            {
                AddBar(root.transform, "LeftPillar", new Vector2(-0.9f, 0f), new Vector2(0.18f, 3.2f)),
                AddBar(root.transform, "RightPillar", new Vector2(0.9f, 0f), new Vector2(0.18f, 3.2f)),
                AddBar(root.transform, "Lintel", new Vector2(0f, 1.5f), new Vector2(2f, 0.18f)),
            };

            var labelObject = new GameObject("ExitLabel");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            _exitLabel = labelObject.AddComponent<TextMesh>();
            _exitLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _exitLabel.fontSize = 48;
            _exitLabel.characterSize = 0.045f;
            _exitLabel.anchor = TextAnchor.MiddleCenter;
            _exitLabel.alignment = TextAlignment.Center;
            var renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.material = _exitLabel.font.material;
            renderer.sortingOrder = 42;
        }

        static SpriteRenderer AddBar(Transform parent, string name, Vector2 position, Vector2 size)
        {
            if (_barSprite == null)
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.name = "RegionExitPixel";
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _barSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f), 1f);
                _barSprite.name = "RegionExitBar";
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(position.x, position.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _barSprite;
            renderer.sortingOrder = 41;
            return renderer;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;

            var gm = GameManager.Instance;
            if (!string.IsNullOrEmpty(requiredEncounterId)
                && !gm.Progress.IsEncounterCleared(requiredEncounterId))
                return;
            if (marksFractureCleared) gm.Progress.MarkFractureCleared();

            var next = gm.CurrentZoneData != null ? gm.CurrentZoneData.nextSceneName : SceneFlow.Title;

            // 백트래킹 규칙: 균열을 클리어한 뒤 잔재로 되돌아오면 엔딩으로 보낸다.
            if (gm.Progress.CurrentZone == ZoneId.Residue && gm.Progress.HasClearedFracture)
                next = SceneFlow.Ending;

            SceneFlow.LoadWithFade(next);
        }
    }
}
