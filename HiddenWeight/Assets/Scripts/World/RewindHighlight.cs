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

        void Start()
        {
            _target = GetComponent<IRewindable>();
            _source = GetComponent<SpriteRenderer>();
            if (_target == null || _source == null) { enabled = false; return; }

            var go = new GameObject("RewindOutline");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * outlineScale;
            _outline = go.AddComponent<SpriteRenderer>();
            _outline.sortingLayerID = _source.sortingLayerID;
            _outline.sortingOrder = _source.sortingOrder - 1; // 본체 바로 뒤
            _outline.enabled = false;
        }

        void Update()
        {
            if (_outline == null) return;

            bool show = _target.CanRewind;
            _outline.enabled = show;
            if (!show) return;

            // 본체 스프라이트가 꺼진 상태(무너진 발판)에서도 아웃라인은 자리를 표시해야 하므로
            // 스프라이트만 복사하고 enabled는 따라가지 않는다.
            _outline.sprite = _source.sprite;

            float alpha = Mathf.Lerp(0.35f, 0.8f, Mathf.PingPong(Time.time * pulseSpeed, 1f));
            _outline.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, alpha);
        }
    }
}
