using UnityEngine;

namespace HiddenWeight.World
{
    // 자각(L 홀드) 중에만 드러나는 구조물. HiddenFragment가 "자각으로만 먹을 수 있는 파편"이라면
    // 이쪽은 "자각으로만 보이는 표식·거울문·발판"이다(GAZE_LEVEL_DESIGN.md 4.11절 G11 표식
    // 퍼즐, 5절 GS3 거울문).
    //
    // solid를 지정하면 자각 중에만 밟히거나 지나갈 수 있다. 지정하지 않으면 보이기만 한다.
    // 균열 지역에서는 AwarenessSystem이 항상 false만 방송하므로(awarenessStable=false) 이
    // 컴포넌트를 놓아도 아무것도 드러나지 않는다 — FRACTURE_LEVEL_DESIGN.md 1.2절의
    // "자각은 숨겨진 필수 경로의 열쇠로 사용하지 않는다"가 코드 차원에서 지켜진다.
    public class AwarenessRevealed : MonoBehaviour, IAwarenessReactive
    {
        [SerializeField] SpriteRenderer visual;
        [SerializeField] Collider2D solid;      // 비어 있어도 된다
        [SerializeField] float hiddenAlpha = 0.08f; // 0이면 완전히 사라진다

        // 반대로 동작한다: 평소에는 막혀 있고 자각 중에만 길이 열린다. GS3의 거울문이 이것이다 —
        // "자각으로만 나타나는 문"은 결국 "자각 중에만 사라지는 벽"과 같은 장치다.
        [SerializeField] bool invert = false;

        public bool IsRevealed { get; private set; }

        void Awake()
        {
            if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
            Apply(false);
        }

        void OnEnable() => AwarenessRegistry.Register(this);
        void OnDisable() => AwarenessRegistry.Unregister(this);

        public void OnAwarenessChanged(bool active) => Apply(active);

        // 자각을 켠 순간 화면 곳곳이 동시에 바뀌면 어느 쪽이 새로 드러났는지 놓친다.
        // 나타나는 쪽만 소리를 내서 시선을 그쪽으로 넘긴다.
        bool _wasPresent;

        void Apply(bool active)
        {
            IsRevealed = active;
            bool present = invert ? !active : active;

            if (present && !_wasPresent && Application.isPlaying)
                Core.AudioManager.Instance?.PlaySfx(Core.SfxCue.SecretReveal, 0.5f);
            _wasPresent = present;

            if (visual != null)
            {
                var color = visual.color;
                color.a = present ? 1f : hiddenAlpha;
                visual.color = color;
            }

            if (solid != null) solid.enabled = present;
        }
    }
}
