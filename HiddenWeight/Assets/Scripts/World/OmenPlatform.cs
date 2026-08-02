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
            _sprite = GetComponent<SpriteRenderer>();
            _animator = GetComponent<SpriteAnimator>();
            _collider = GetComponent<Collider2D>();
            _base = _sprite.color;
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
                    _sprite.color = new Color(_base.r, _base.g, _base.b, 0f);
                    if (_collider != null) _collider.enabled = false;
                    break;

                case State.Gone:
                    _timer -= Time.deltaTime;
                    if (_timer > 0f) return;

                    _state = State.Waiting;
                    _sprite.color = _base;
                    if (_collider != null) _collider.enabled = true;
                    if (_animator != null) _animator.Play("FracturePlatformSafe", true);
                    break;
            }
        }
    }
}
