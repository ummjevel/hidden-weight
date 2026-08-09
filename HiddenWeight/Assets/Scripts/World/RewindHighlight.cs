using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.UI;

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
        SpriteRenderer _placeholder;
        SpriteRenderer _outline;
        Text _marker;
        Canvas _markerCanvas;
        Transform _markerRoot;
        Transform _player;
        bool _residueScene;

        void Start()
        {
            _target = GetComponent<IRewindable>();
            _residueScene = gameObject.scene.name.Contains("Residue");
            _placeholder = GetComponent<SpriteRenderer>();
            _source = null;
            // 지역 아트를 씌운 오브젝트는 루트의 플레이스홀더 렌더러를 끄고 Art 자식만
            // 사용한다. 꺼진 루트를 복제하면 실제 기물과 다른 작은 네모만 빛나므로, 현재
            // 화면에 쓰이는 자식 렌더러를 강조 기준으로 삼는다.
            if (_residueScene)
            {
                // 잔재에서는 루트 Tile의 enabled 상태와 무관하게 반드시 실제 자식 아트를
                // 고른다. 여러 Start의 실행 순서에 따라 루트가 잠깐 켜져 있어도 선택하지 않는다.
                foreach (var candidate in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (candidate == _placeholder || candidate.sprite == null) continue;
                    if (candidate.name == "RewindOutline") continue;
                    _source = candidate;
                    break;
                }
            }
            else
            {
                _source = _placeholder;
            }

            // 실제 아트가 없는 잘못된 잔재 오브젝트라면 루트 흰 Tile로 대체하지 않는다.
            // 표시를 생략하는 편이 흰 사각형을 기능 아이콘처럼 노출하는 것보다 안전하다.
            DisableResiduePlaceholder();
            if (_target == null || _source == null) { enabled = false; return; }

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

            var markerObject = new GameObject("RewindTargetMarker", typeof(RectTransform));
            markerObject.transform.SetParent(transform, false);
            markerObject.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            // 일부 Unity 임포트 환경에서 런타임 Sprite 아이콘이 텍스처 없는 흰 사각형으로
            // 렌더링됐다. 표식은 모든 기본 폰트가 보장하는 ASCII만 사용해 확실히 표시한다.
            BuildTextMarker(markerObject, _residueScene ? "HOLD K" : "K",
                _residueScene ? 0.045f : 0.08f);
        }

        void Update()
        {
            if (_outline == null) return;

            // Rewindable 프리팹 루트에는 에디터 식별용 Tile(흰 사각형)이 남아 있다.
            // 잔재에서는 실제 Art/복원 발판 자식만 화면에 쓰므로, 방 컬러링이나 복원 과정이
            // 루트 렌더러를 다시 켜더라도 매 프레임 숨겨 흰 네모가 재등장하지 않게 한다.
            DisableResiduePlaceholder();

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
            if (_markerCanvas != null) _markerCanvas.enabled = show && markerInRange;
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
                    _markerRoot.position = transform.position + Vector3.up
                        * (1.15f + Mathf.Sin(Time.time * pulseSpeed) * 0.08f);
                    _markerRoot.rotation = Quaternion.identity;
                }
            }
        }

        void DisableResiduePlaceholder()
        {
            if (!_residueScene || _placeholder == null) return;
            _placeholder.enabled = false;
        }

        void BuildTextMarker(GameObject markerObject, string text, float characterSize)
        {
            _markerRoot = markerObject.transform;
            var rootRect = (RectTransform)_markerRoot;
            rootRect.sizeDelta = new Vector2(420f, 90f);
            rootRect.localScale = Vector3.one * (characterSize / 7.2f);

            _markerCanvas = markerObject.AddComponent<Canvas>();
            _markerCanvas.renderMode = RenderMode.WorldSpace;
            _markerCanvas.overrideSorting = true;
            _markerCanvas.sortingOrder = 39;

            var textObject = new GameObject("MarkerText", typeof(RectTransform));
            textObject.transform.SetParent(markerObject.transform, false);
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;

            _marker = textObject.AddComponent<Text>();
            _marker.font = UIBuilder.WorldHintFont;
            _marker.fontSize = 48;
            _marker.alignment = TextAnchor.MiddleCenter;
            _marker.horizontalOverflow = HorizontalWrapMode.Overflow;
            _marker.verticalOverflow = VerticalWrapMode.Overflow;
            _marker.raycastTarget = false;
            _marker.text = text;
            _markerCanvas.enabled = false;
        }
    }
}
