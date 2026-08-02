using UnityEngine;

namespace HiddenWeight.World
{
    // 되감기 가능한 오브젝트의 골드빛 아웃라인 (기획서 EMOTION_SYSTEM 1.3절).
    // 같은 GameObject의 IRewindable을 관찰만 하고, 되감을 것이 있을 때(CanRewind)만 표시한다.
    // 자각 없이도 식별 가능해야 진행이 막히지 않는다는 규칙이므로 다른 시스템과 연동하지 않는다.
    public class RewindHighlight : MonoBehaviour
    {
        [SerializeField] Color outlineColor = new Color(1f, 0.82f, 0.35f); // 옅은 골드~앰버
        [SerializeField] float outlineScale = 1.2f;
        [SerializeField] float pulseSpeed = 2.5f;

        IRewindable _target;
        SpriteRenderer _source;
        SpriteRenderer _outline;
        TextMesh _marker;
        MeshRenderer _markerRenderer;
        Transform _player;
        bool _residueScene;

        void Start()
        {
            _target = GetComponent<IRewindable>();
            _source = GetComponent<SpriteRenderer>();
            // 지역 아트를 씌운 오브젝트는 루트의 플레이스홀더 렌더러를 끄고 Art 자식만
            // 사용한다. 꺼진 루트를 복제하면 실제 기물과 다른 작은 네모만 빛나므로, 현재
            // 화면에 쓰이는 자식 렌더러를 강조 기준으로 삼는다.
            if (_source == null || !_source.enabled || _source.sprite == null)
            {
                foreach (var candidate in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (!candidate.enabled || candidate.sprite == null) continue;
                    _source = candidate;
                    break;
                }
            }
            if (_target == null || _source == null) { enabled = false; return; }

            _residueScene = gameObject.scene.name.Contains("Residue");
            if (_residueScene)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null) _player = playerObject.transform;
            }

            var go = new GameObject("RewindOutline");
            // 실제 아트의 스케일·회전을 그대로 물려받아 외곽선이 본체와 정확히 겹치게 한다.
            go.transform.SetParent(_source.transform, false);
            go.transform.localScale = Vector3.one * outlineScale;
            _outline = go.AddComponent<SpriteRenderer>();
            _outline.sortingLayerID = _source.sortingLayerID;
            _outline.sortingOrder = _source.sortingOrder - 1; // 본체 바로 뒤
            _outline.enabled = false;

            var markerObject = new GameObject("RewindTargetMarker");
            markerObject.transform.SetParent(transform, false);
            markerObject.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            _marker = markerObject.AddComponent<TextMesh>();
            // LegacyRuntime 폰트에는 ◇ 글리프가 없어 흰 네모로 보인다. ASCII 키 문자는
            // 모든 기본 폰트에서 보장되므로 대상 표식과 실제 입력을 동시에 보여 준다.
            _marker.text = _residueScene ? "K 길게" : "K";
            _marker.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _marker.fontSize = 48;
            _marker.characterSize = _residueScene ? 0.045f : 0.08f;
            _marker.anchor = TextAnchor.MiddleCenter;
            _marker.alignment = TextAlignment.Center;
            _markerRenderer = markerObject.GetComponent<MeshRenderer>();
            _markerRenderer.material = _marker.font.material;
            _markerRenderer.sortingOrder = 39;
            _markerRenderer.enabled = false;
        }

        void Update()
        {
            if (_outline == null) return;

            if (_residueScene && _player == null)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null) _player = playerObject.transform;
            }

            bool show = _target.CanRewind;
            _outline.enabled = show;
            // 잔재의 K 표식은 예전에는 방 반대편에서도 보여서 발판이 사라진 빈 공간에
            // 글자만 둥둥 떠 있었다. 대상은 아웃라인/잔상으로 계속 찾을 수 있게 두되,
            // 조작 표식은 실제 되감기 사거리 부근에 들어왔을 때만 보여 준다.
            bool markerInRange = !_residueScene || (_player != null
                && Vector2.Distance(_player.position, transform.position) <= 6f);
            if (_markerRenderer != null) _markerRenderer.enabled = show && markerInRange;
            if (!show) return;

            // 본체 스프라이트가 꺼진 상태(무너진 발판)에서도 아웃라인은 자리를 표시해야 하므로
            // 스프라이트만 복사하고 enabled는 따라가지 않는다.
            _outline.sprite = _source.sprite;

            float alpha = Mathf.Lerp(0.35f, 0.8f, Mathf.PingPong(Time.time * pulseSpeed, 1f));
            _outline.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, alpha);
            if (_marker != null)
            {
                _marker.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, alpha);
                if (_residueScene)
                {
                    _marker.transform.position = transform.position + Vector3.up
                        * (1.15f + Mathf.Sin(Time.time * pulseSpeed) * 0.08f);
                    _marker.transform.rotation = Quaternion.identity;
                }
            }
        }
    }
}
