using System.Collections;
using UnityEngine;

namespace HiddenWeight.World
{
    // IRewindable의 기본 구현. 부서지거나 옮겨진 오브젝트를 원래 자리로 되돌린다 (기획서 4.2절).
    public class Rewindable : MonoBehaviour, IRewindable
    {
        Vector3 _initialPosition;
        Quaternion _initialRotation;
        bool _initialActive;
        Sprite _initialSprite;

        SpriteRenderer _sprite;
        Rigidbody2D _rb;
        Coroutine _bounceRoutine;

        public Transform Transform => transform;

        // 이미 초기 상태면 false. 위치 변화만 비교한다.
        public bool CanRewind => Vector3.SqrMagnitude(transform.position - _initialPosition) > 0.0001f;

        void Start()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            CaptureInitial();
        }

        public void CaptureInitial()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialActive = gameObject.activeSelf;
            if (_sprite != null) _initialSprite = _sprite.sprite;
        }

        public void Rewind()
        {
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            gameObject.SetActive(_initialActive);
            if (_sprite != null) _sprite.sprite = _initialSprite;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;

            if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
            _bounceRoutine = StartCoroutine(BounceRoutine());
        }

        // 0.3초에 걸쳐 스케일을 0.8 -> 1.0으로 튕기는 되감기 연출.
        IEnumerator BounceRoutine()
        {
            const float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float scale = Mathf.Lerp(0.8f, 1f, elapsed / duration);
                transform.localScale = new Vector3(scale, scale, 1f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = Vector3.one;
            _bounceRoutine = null;
        }
    }
}
