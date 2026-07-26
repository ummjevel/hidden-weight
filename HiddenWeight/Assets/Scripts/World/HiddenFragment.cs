using UnityEngine;

namespace HiddenWeight.World
{
    // 자각(L 홀드) 중에만 보이고 만질 수 있는 파편.
    public class HiddenFragment : StoryFragment, IAwarenessReactive
    {
        [SerializeField] SpriteRenderer visual;
        bool _revealed;

        protected override bool IsCollectable => _revealed;

        void OnEnable() => AwarenessRegistry.Register(this);
        void OnDisable() => AwarenessRegistry.Unregister(this);

        public void OnAwarenessChanged(bool active)
        {
            _revealed = active;
            if (visual != null) visual.enabled = active;
        }
    }
}
