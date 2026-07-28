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
        SpriteRenderer _sprite;
        Coroutine _crumbleRoutine;
        float _crumbleTimer;

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
            _sprite = GetComponent<SpriteRenderer>();
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

            while (_crumbleTimer > 0f)
            {
                // 무너지기 전 흔들림 연출
                float shakeX = Random.Range(-0.05f, 0.05f);
                transform.localPosition = originalLocalPos + new Vector3(shakeX, 0f, 0f);
                _crumbleTimer -= Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalLocalPos;
            _collider.enabled = false;
            _sprite.enabled = false;
            HasCrumbled = true;
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
            _collider.enabled = true;
            _sprite.enabled = true;
            HasCrumbled = false;
        }

        public Vector3 PredictPosition(float leadSeconds) => transform.position; // 움직이지 않는다

        public bool PredictActive(float leadSeconds)
        {
            if (_crumbleTimer > 0f && _crumbleTimer <= leadSeconds) return false;
            return !HasCrumbled;
        }
    }
}
