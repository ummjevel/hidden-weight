using System.Collections;
using UnityEngine;
using HiddenWeight.Core;

namespace HiddenWeight.World
{
    // IRewindable의 기본 구현. 부서지거나 옮겨진 오브젝트를 원래 자리로 되돌린다 (기획서 4.2절).
    //
    // 되감기는 영구다 (EMOTION_SYSTEM 1.2절): 되돌린 사실을 ProgressState에 persistentId로
    // 기록하고, 씬이 다시 로드되면 Start에서 곧바로 복원 상태로 시작한다. 복원된 오브젝트는
    // Static으로 고정한다 — 중력으로 다시 무너지면 "영구"가 아니게 되기 때문이다.
    public class Rewindable : MonoBehaviour, IRewindable
    {
        [SerializeField] string persistentId; // 비워두면 씬 이름 + 초기 좌표로 자동 생성

        Vector3 _initialPosition;
        Quaternion _initialRotation;
        bool _initialActive;
        Sprite _initialSprite;

        SpriteRenderer _sprite;
        Rigidbody2D _rb;
        Coroutine _bounceRoutine;

        public Transform Transform => transform;

        // 이 대상을 되감으면 함께 열리는 숏컷. R05 사슬장치 → 숏컷 A, R08 도르래 → 숏컷 B처럼
        // "복원이 세계를 바꾼다"는 연결을 만든다(RESIDUE_LEVEL_DESIGN.md 숏컷 A/B).
        // 여러 개를 요구하는 숏컷(R08 도르래 2개)은 requiredSiblings로 함께 묶는다.
        [SerializeField] Shortcut linkedShortcut;
        [SerializeField] Rewindable[] requiredSiblings;

        // 이미 초기 상태면 false. 위치 변화만 비교한다.
        public bool CanRewind => Vector3.SqrMagnitude(transform.position - _initialPosition) > 0.0001f;

        void Start()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            CaptureInitial();

            if (string.IsNullOrEmpty(persistentId))
            {
                persistentId = $"{gameObject.scene.name}:{name}:{_initialPosition.x:F1},{_initialPosition.y:F1}";
            }

            // 이전 방문에서 이미 되감은 오브젝트는 복원 상태 그대로 시작한다.
            if (GameManager.Instance != null && GameManager.Instance.Progress.IsRewound(persistentId))
            {
                Freeze();
                TryOpenLinkedShortcut(); // 이전 방문에서 이미 복원했다면 숏컷도 열린 채로 시작한다
            }
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
            Freeze();

            if (GameManager.Instance != null) GameManager.Instance.Progress.MarkRewound(persistentId);

            TryOpenLinkedShortcut();

            if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
            _bounceRoutine = StartCoroutine(BounceRoutine());
        }

        // 묶인 대상이 전부 복원됐을 때만 숏컷을 연다.
        void TryOpenLinkedShortcut()
        {
            if (linkedShortcut == null) return;

            if (requiredSiblings != null)
                foreach (var sibling in requiredSiblings)
                    if (sibling != null && sibling.CanRewind) return; // 아직 안 돌아온 것이 있다

            linkedShortcut.Open();
        }

        void Freeze()
        {
            if (_rb == null) return;
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Static;
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
