using System.Collections;
using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    // 밟으면 흔들리다 무너지는 발판. 되감기로 복구되고, 예지로 무너진 뒤(사라짐)를 미리 볼 수 있다.
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CrumblingPlatform : MonoBehaviour, IRewindable, IForeseeable
    {
        [SerializeField] float crumbleDelay = 0.6f; // 밟은 뒤 무너지기까지
        [SerializeField] float respawnDelay = 0f;   // 0이면 되감기로만 복구된다

        Collider2D _collider;
        Collider2D _rewindSensor;
        SpriteRenderer _sprite;
        Color _intactColor = Color.white;
        Coroutine _crumbleRoutine;
        float _crumbleTimer;

        // 상태 애니메이션(ResiduePlatformStates_v1: 균열 → 붕괴 → 파손 정착 → 되감기 복구).
        // 시트가 붙어 있지 않으면 지금까지처럼 스프라이트를 끄고 켜는 것으로 대신한다 —
        // 아트가 없는 지역(응시·균열)에서도 발판은 그대로 동작해야 한다.
        SpriteAnimator _animator;

        bool PlayState(string clip)
        {
            if (_animator == null || !_animator.Has(clip)) return false;
            _animator.Play(clip, true);
            return true;
        }

        public bool HasCrumbled { get; private set; }

        // 스스로 되살아나기까지의 시간. 0이면 되감기로만 복구된다 — 되감기가 없는 지역
        // (균열)에서 0이면 한 번 무너진 발판이 영영 돌아오지 않아 진행 불가가 된다.
        // 검증: Assets/Tests/PlayMode/GazeFractureZoneTests.cs
        public float RespawnDelay => respawnDelay;

        public Transform Transform => transform;
        public bool CanRewind => HasCrumbled;
        public Sprite CurrentSprite => _sprite.sprite;

        void Awake()
        {
            _collider = GetComponent<Collider2D>();

            // 무너지면 본 충돌체를 꺼야 플레이어가 아래로 떨어진다. 하지만 RewindSkill은
            // Physics2D 겹침 검사로 대상을 찾으므로 충돌체가 하나도 남지 않으면 K를 눌러도
            // 이 발판을 찾을 수 없다. 잔재에서만 플레이를 막지 않는 트리거 센서를 별도로 둔다.
            if (gameObject.scene.name.Contains("Residue") && _collider is BoxCollider2D box)
            {
                var sensorObject = new GameObject("RewindTargetSensor");
                sensorObject.layer = gameObject.layer;
                sensorObject.transform.SetParent(transform, false);
                var sensor = sensorObject.AddComponent<BoxCollider2D>();
                sensor.size = box.size;
                sensor.offset = box.offset;
                sensor.isTrigger = true;
                sensor.enabled = false;
                _rewindSensor = sensor;
            }

            // 지역 아트는 루트 렌더러를 끄고 "Art" 자식에 그린다(ReplaceArt). 루트만 잡으면
            // 무너질 때 이미 꺼진 렌더러를 다시 끄는 셈이라, 아트가 그려진 발판은 사라지지
            // 않은 채 충돌만 없어진다 — 보이는 렌더러를 잡아야 한다.
            _sprite = GetComponent<SpriteRenderer>();
            if (_sprite == null || !_sprite.enabled)
                foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
                    if (renderer.enabled) { _sprite = renderer; break; }

            if (_sprite != null) _intactColor = _sprite.color;

            _animator = GetComponentInChildren<SpriteAnimator>();
        }

        // 위치를 바꾸지 않는 발판이라 되돌릴 상태는 HasCrumbled 하나뿐이다.
        public void CaptureInitial() { }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (HasCrumbled || _crumbleRoutine != null) return;
            if (!PlayerLayers.SteppedOnFromAbove(collision, transform)) return;

            _crumbleRoutine = StartCoroutine(CrumbleRoutine());
        }

        IEnumerator CrumbleRoutine()
        {
            _crumbleTimer = crumbleDelay;
            var originalLocalPos = transform.localPosition;

            // 1행: 밟은 직후의 금과 미세한 흔들림. 아직 밟고 서 있을 수 있는 단계다.
            // 금 가는 소리가 곧 남은 시간이다 — 발밑을 볼 수 없는 점프 중에도 들려야 한다.
            Core.AudioManager.Instance?.PlaySfx(Core.SfxCue.PlatformCrack, 0.5f);
            bool animated = PlayState("PlatformCrack");

            while (_crumbleTimer > 0f)
            {
                // 무너지기 전 흔들림 연출. 전용 아트가 있으면 흔들림은 프레임이 맡으므로
                // 위치를 흔들지 않는다 — 둘을 겹치면 그림이 두 번 떨린다.
                if (!animated)
                {
                    float shakeX = Random.Range(-0.05f, 0.05f);
                    transform.localPosition = originalLocalPos + new Vector3(shakeX, 0f, 0f);
                }
                _crumbleTimer -= Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalLocalPos;
            _collider.enabled = false;
            HasCrumbled = true;
            if (_rewindSensor != null) _rewindSensor.enabled = true;
            Core.AudioManager.Instance?.PlaySfx(Core.SfxCue.PlatformCollapse, 0.55f);

            // 2행 붕괴 → 3행 파손 정착. 파손 상태를 그림으로 보여줄 수 있으면 스프라이트를
            // 끄지 않는다. 발판이 있던 자리가 눈에 남아야 되감기로 되돌릴 대상이 읽힌다.
            if (PlayState("PlatformCollapse"))
            {
                // 붕괴가 끝나야 파손 정착으로 넘어간다. 이 동안에도 _crumbleRoutine을 들고
                // 있으므로 다시 밟아도 붕괴가 두 번 시작되지 않는다.
                while (_animator != null && !_animator.IsFinished) yield return null;
                PlayState("PlatformBroken");
                ShowResidueBrokenSilhouette(0.48f);
            }
            else if (!PlayState("PlatformBroken"))
            {
                if (!ShowResidueBrokenSilhouette(0.28f)) _sprite.enabled = false;
            }
            else ShowResidueBrokenSilhouette(0.48f);

            _crumbleTimer = 0f;
            _crumbleRoutine = null;

            if (respawnDelay > 0f) StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            Rewind();
        }

        public void Rewind()
        {
            if (_crumbleRoutine != null)
            {
                StopCoroutine(_crumbleRoutine);
                _crumbleRoutine = null;
            }

            _crumbleTimer = 0f;
            if (_rewindSensor != null) _rewindSensor.enabled = false;
            _collider.enabled = true;
            _sprite.enabled = true;
            _sprite.color = _intactColor;
            HasCrumbled = false;

            // 4행: 되감기 복구. 붕괴의 역순으로 다시 쌓인다(명세의 "reverse visual order").
            // 없으면 스프라이트를 그냥 켜는 것으로 끝난다.
            if (!PlayState("PlatformRestore")) PlayState("PlatformCrack");
        }

        // 잔재에서는 무너진 발판이 완전히 사라지면 공중에 K 글자만 남아 복원 대상을
        // 알아볼 수 없다. 충돌은 꺼 둔 채 파손 그림만 반투명하게 남겨 "밟을 수 없는
        // 되감기 대상"임을 보여 준다. 다른 지역의 기존 붕괴 표현에는 영향을 주지 않는다.
        bool ShowResidueBrokenSilhouette(float alpha)
        {
            if (_sprite == null || !gameObject.scene.name.Contains("Residue")) return false;

            _sprite.enabled = true;
            Color broken = _intactColor;
            broken.a = Mathf.Min(_intactColor.a, alpha);
            _sprite.color = broken;
            return true;
        }

        public Vector3 PredictPosition(float leadSeconds) => transform.position; // 움직이지 않는다

        public bool PredictActive(float leadSeconds)
        {
            if (_crumbleTimer > 0f && _crumbleTimer <= leadSeconds) return false;
            return !HasCrumbled;
        }
    }
}
