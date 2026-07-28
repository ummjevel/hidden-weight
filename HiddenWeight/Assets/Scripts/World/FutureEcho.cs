using UnityEngine;

namespace HiddenWeight.World
{
    // 지금은 없고 예지 안에서만 보이는 구조물. 균열 F03의 미래 문짝, F11의 완성된 폐허,
    // FS3의 "선택되지 않은 문"이 전부 이것이다(FRACTURE_LEVEL_DESIGN.md 4.3·4.11·5절).
    //
    // 규칙 두 가지를 코드로 못박는다.
    //   - 고스트는 충돌 지형이 아니다(4.11절 "고스트 위로 바로 점프하게 만들지 않는다").
    //     그래서 solid는 확정되기 전까지 꺼져 있다.
    //   - "세 번의 예지에서 같은 위치에 반복해서 나타나는 문"(4.11절, FS3)은 관찰 횟수로
    //     센다. sightingsToFix번 고스트로 보이고 나면 현재 공간에 고정되어 실제가 된다.
    //     ForesightSkill은 고스트를 띄울 때 PredictPosition을 정확히 한 번 부르므로,
    //     그 호출이 곧 "한 번 봤다"의 신호다.
    public class FutureEcho : MonoBehaviour, IForeseeable
    {
        [SerializeField] SpriteRenderer futureVisual;  // 평소 꺼져 있고 고스트 재료로만 쓰인다
        [SerializeField] Collider2D solid;             // 확정된 뒤에만 켜진다
        [SerializeField] int sightingsToFix = 0;       // 0이면 영원히 고스트로만 남는다
        [SerializeField] Shortcut linkedShortcut;      // 확정되면 열린다(숏컷 A)
        [SerializeField] Vector2 futureDrift;          // 2초 뒤 미세하게 어긋나는 랜드마크용

        int _sightings;

        public bool IsFixed { get; private set; }
        public int Sightings => _sightings;

        public Transform Transform => transform;
        public Sprite CurrentSprite => futureVisual != null ? futureVisual.sprite : null;

        void Awake()
        {
            Apply(false);
        }

        public Vector3 PredictPosition(float leadSeconds)
        {
            RegisterSighting();
            return transform.position + (Vector3)futureDrift;
        }

        public bool PredictActive(float leadSeconds) => true;

        void RegisterSighting()
        {
            if (IsFixed || sightingsToFix <= 0) return;

            _sightings++;
            if (_sightings >= sightingsToFix) Fix();
        }

        // 미래가 현재에 고정된다. 바깥에서도 부를 수 있게 public으로 둔다 — F05의 숏컷 A는
        // 성소 마지막에 문틀과 맞물리는 연출이라 관찰 횟수가 아니라 스크립트가 확정시킨다.
        public void Fix()
        {
            if (IsFixed) return;

            IsFixed = true;
            Apply(true);
            if (linkedShortcut != null) linkedShortcut.Open();
        }

        void Apply(bool fixedNow)
        {
            if (futureVisual != null)
            {
                var color = futureVisual.color;
                color.a = fixedNow ? 1f : 0f;
                futureVisual.color = color;
                futureVisual.enabled = true; // 고스트 재료로 sprite를 읽어야 하므로 켜 둔다
            }
            if (solid != null) solid.enabled = fixedNow;
        }
    }
}
