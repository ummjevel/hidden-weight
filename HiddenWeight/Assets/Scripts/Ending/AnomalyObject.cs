using UnityEngine;
using HiddenWeight.World;

namespace HiddenWeight.Ending
{
    // Ending 씬에 배치되는 "이상" 오브젝트. 자각(L 홀드) 중에만 이상 상태를 드러낸다.
    // 1단계에서는 EndingSequence가 자각 누적에 맞춰 OnAwarenessChanged를 직접 호출하고,
    // 2단계에서는 Enabled를 false로 내려 더 이상 아무것도 드러나지 않게 만든다.
    public class AnomalyObject : MonoBehaviour, IAwarenessReactive
    {
        public enum Kind { InvertedCandle, MismatchedShadow, TremblingWall }

        [SerializeField] Kind type;
        [SerializeField] SpriteRenderer visual; // 이상 상태를 보여주는 스프라이트

        public Kind Type => type;
        public bool IsRevealed { get; private set; }
        public bool Enabled { get; set; } = true; // 2단계에서 false

        Vector3 _originalLocalPosition;
        Quaternion _originalLocalRotation;
        bool _originalFlipY;

        void Awake()
        {
            if (visual == null) return;

            _originalLocalPosition = visual.transform.localPosition;
            _originalLocalRotation = visual.transform.localRotation;
            _originalFlipY = visual.flipY;
        }

        void OnEnable() => AwarenessRegistry.Register(this);
        void OnDisable() => AwarenessRegistry.Unregister(this);

        // TremblingWall은 켜져 있는 동안 매 프레임 흔들려야 하므로 Update에서 계속 갱신한다.
        void Update()
        {
            if (!IsRevealed || type != Kind.TremblingWall || visual == null) return;

            var offset = _originalLocalPosition;
            offset.x += Mathf.Sin(Time.time * 40f) * 0.02f;
            visual.transform.localPosition = offset;
        }

        public void OnAwarenessChanged(bool active)
        {
            if (!Enabled) return; // 2단계: 자각이 와도 무시한다

            IsRevealed = active;
            if (visual == null) return;

            switch (type)
            {
                case Kind.InvertedCandle:
                    visual.flipY = active ? true : _originalFlipY;
                    break;

                case Kind.MismatchedShadow:
                    visual.transform.localRotation = active
                        ? Quaternion.Euler(0f, 0f, 90f)
                        : _originalLocalRotation;
                    break;

                case Kind.TremblingWall:
                    if (!active) visual.transform.localPosition = _originalLocalPosition;
                    // 켜져 있는 동안의 떨림은 Update에서 처리한다.
                    break;
            }
        }
    }
}
