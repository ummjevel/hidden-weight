using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.World
{
    // 플레이어가 닿기 전에 스스로 무너지는 발판.
    //
    // 균열의 첫 규칙은 "밝음이 안전을 보장하지 않는다"이고, 설계 4.1은 그것을 F01 방 끝에서
    // **먼저 보여주라**고 정한다 — 설명문이 아니라 사건으로. 밟아야 무너지는
    // CrumblingPlatform으로는 이 장면을 만들 수 없다. 그때는 이미 배우는 대신 당하는 것이다.
    //
    // 그래서 이것은 위험이 아니라 교재다. 플레이어가 다가오면 시야 안에서 혼자 갈라지고
    // 무너진 뒤, 잠시 뒤 되살아나 다음 플레이에서도 같은 것을 보여 준다.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class OmenPlatform : MonoBehaviour
    {
        [SerializeField] float noticeDistance = 9f;   // 이 거리에 들어오면 시작한다
        [SerializeField] float crackSeconds = 0.7f;   // 갈라지는 시간
        [SerializeField] float goneSeconds = 3.5f;    // 사라져 있는 시간

        SpriteRenderer _sprite;
        SpriteAnimator _animator;
        Collider2D _collider;
        Color _base;
        float _timer;
        State _state = State.Waiting;

        enum State { Waiting, Cracking, Gone }

        void Awake()
        {
            // 겉모습은 자식 "Art"에 있다. 빌더(ReplaceArt)가 루트 렌더러를 끄고 자식에
            // 지역 아트를 붙이기 때문이다.
            //
            // 여기서 GetComponent로 루트를 잡고 있었다. 그래서 알파를 0으로 내려도 꺼진
            // 렌더러만 투명해지고 화면의 발판은 그대로였고, 애니메이터도 자식에 있어
            // null이라 갈라지는 그림도 무너지는 그림도 재생되지 않았다. 남는 것은
            // collider.enabled = false 하나뿐 — **멀쩡해 보이는 발판을 그대로 통과해
            // 떨어지는** 상태가 3.5초마다 반복됐다. F01은 균열의 첫 방이다.
            _animator = GetComponentInChildren<SpriteAnimator>();
            _sprite = _animator != null && _animator.Renderer != null
                ? _animator.Renderer
                : VisibleRenderer();
            _collider = GetComponent<Collider2D>();
            if (_sprite != null) _base = _sprite.color;
        }

        SpriteRenderer VisibleRenderer()
        {
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.enabled) return renderer;
            return GetComponent<SpriteRenderer>();
        }

        // 발판 위에 실제로 그려지는 것은 Art 한 장이 아니다. 런타임 지형 입히기
        // (CameraLockedRoomBackground)가 밟는 면을 따라 PlatformSurface 타일을 **더 앞에**
        // (정렬 4 > Art의 2) 얹는다. Art만 감추면 그 타일들이 남아 발판이 그대로 보인다 —
        // 알파를 0으로 내렸는데도 화면에서 사라지지 않던 이유가 이것이다.
        //
        // 타일은 Awake 뒤에 붙으므로 미리 모아 둘 수 없다. 감출 때마다 다시 찾는다.
        readonly System.Collections.Generic.List<SpriteRenderer> _hidden =
            new System.Collections.Generic.List<SpriteRenderer>();

        void Hide(bool surfaceOnly)
        {
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(false))
            {
                if (!renderer.enabled) continue;
                // 갈라지는 동안에는 Art를 남겨 둔다. 그 위의 타일만 걷어야 금이 보인다.
                if (surfaceOnly && renderer == _sprite) continue;
                renderer.enabled = false;
                _hidden.Add(renderer);
            }
        }

        void Restore()
        {
            foreach (var renderer in _hidden)
                if (renderer != null) renderer.enabled = true;
            _hidden.Clear();
        }

        void Update()
        {
            switch (_state)
            {
                case State.Waiting:
                    var player = PlayerController.Instance;
                    if (player == null) return;
                    // 가까워지는 것만으로 시작한다. 밟을 필요가 없다 — 밟게 하면 교재가 아니라 함정이다.
                    if (Vector2.Distance(player.transform.position, transform.position) > noticeDistance)
                        return;
                    _state = State.Cracking;
                    _timer = crackSeconds;
                    // 위에 얹힌 표면 타일을 먼저 걷는다. 안 그러면 금 간 그림이 타일에 가려
                    // 경고가 화면에 나타나지 않는다.
                    Hide(surfaceOnly: true);
                    if (_animator != null) _animator.Play("PlatformCrack", true);
                    break;

                case State.Cracking:
                    _timer -= Time.deltaTime;
                    // 갈라지는 동안 미세하게 떤다. 소리가 없어도 "곧 무너진다"가 읽힌다.
                    transform.localPosition += new Vector3(
                        Mathf.Sin(Time.time * 46f) * 0.012f, 0f, 0f);
                    if (_timer > 0f) return;

                    _state = State.Gone;
                    _timer = goneSeconds;
                    if (_animator != null) _animator.Play("PlatformCollapse", true);
                    Hide(surfaceOnly: false);
                    if (_collider != null) _collider.enabled = false;
                    break;

                case State.Gone:
                    _timer -= Time.deltaTime;
                    if (_timer > 0f) return;

                    _state = State.Waiting;
                    Restore();
                    if (_collider != null) _collider.enabled = true;
                    if (_animator != null) _animator.Play("FracturePlatformSafe", true);
                    break;
            }
        }
    }
}
