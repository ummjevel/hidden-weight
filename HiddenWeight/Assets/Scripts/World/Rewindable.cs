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
        Coroutine _restoredColliderRoutine;
        bool _started;
        Sprite _restoredPlatformSprite;
        Vector2 _restoredColliderSize;
        Vector2 _restoredColliderOffset;
        float _restoredSurfaceInsetNormalized;

        public Transform Transform => transform;
        public float RestoredSurfaceInsetNormalized => _restoredSurfaceInsetNormalized;

        // 이 대상을 되감으면 함께 열리는 숏컷. R05 사슬장치 → 숏컷 A, R08 도르래 → 숏컷 B처럼
        // "복원이 세계를 바꾼다"는 연결을 만든다(RESIDUE_LEVEL_DESIGN.md 숏컷 A/B).
        // 여러 개를 요구하는 숏컷(R08 도르래 2개)은 requiredSiblings로 함께 묶는다.
        [SerializeField] Shortcut linkedShortcut;
        // 방이 씬으로 갈라진 뒤로 되감기 대상과 숏컷은 서로 다른 씬에 산다(R05→R03, R08→R03).
        // 유니티는 씬을 넘는 오브젝트 참조를 저장하지 못해 위 필드가 null로 구워지므로,
        // 오브젝트가 없어도 여는 길을 id로 하나 더 둔다. 저장 계층은 이미 id 기반이다.
        [SerializeField] string linkedShortcutId;
        [SerializeField] Rewindable[] requiredSiblings;

        // 이미 초기 상태면 false. 위치 변화만 비교한다.
        public bool CanRewind => Vector3.SqrMagnitude(transform.position - _initialPosition) > 0.0001f;

        public void ConfigureLinkedShortcut(Shortcut shortcut, params Rewindable[] siblings)
            => ApplyLink(shortcut, null, siblings);

        // 같은 씬에 숏컷 오브젝트가 없을 때 쓰는 연결. 여는 대상을 id로만 지정한다.
        public void ConfigureLinkedShortcut(string shortcutId, params Rewindable[] siblings)
            => ApplyLink(null, shortcutId, siblings);

        // R05처럼 복원 대상 자체가 다음 턱까지 이어지는 디딤판인 경우에만 사용한다.
        // 파손 상태의 작은 잔해 그림과 1x1 판정을 그대로 두면 복원 뒤 허공에 서 보이고,
        // 턱 직전의 좁은 틈으로 다시 떨어질 수 있다.
        public void ConfigureRestoredPlatform(Sprite sprite, Vector2 colliderSize,
                                              Vector2 colliderOffset = default,
                                              float surfaceInsetNormalized = 0f)
        {
            _restoredPlatformSprite = sprite;
            _restoredColliderSize = colliderSize;
            _restoredColliderOffset = colliderOffset;
            _restoredSurfaceInsetNormalized = Mathf.Clamp01(surfaceInsetNormalized);
            if (_started && _rb != null && _rb.bodyType == RigidbodyType2D.Static && !CanRewind)
                ApplyRestoredPlatformPresentation();
        }

        void ApplyLink(Shortcut shortcut, string shortcutId, Rewindable[] siblings)
        {
            linkedShortcut = shortcut;
            linkedShortcutId = shortcutId;
            requiredSiblings = siblings;

            // 런타임 연결은 Start 이후에 설정될 수 있다. 이전 방문에서 이미 복원된 대상이면
            // Start 때 놓친 숏컷 동기화를 여기서 즉시 보충한다.
            if (_started && GameManager.Instance != null && GameManager.Instance.Progress.IsRewound(persistentId))
                TryOpenLinkedShortcut();
        }

        // 보스 페이즈가 복원된 전장 구조를 다시 무너뜨릴 때 사용한다. 진행 저장에는
        // "한 번 복원했다"는 사실을 남기되, 현재 전투 안에서는 다시 되감을 수 있게 한다.
        public void BreakForEncounter(Vector2 impulse)
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_rb == null) return;
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.linearVelocity = impulse;
            transform.position += (Vector3)(impulse.normalized * 0.35f);
        }

        void Start()
        {
            _sprite = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            CaptureInitial();

            if (string.IsNullOrEmpty(persistentId))
            {
                persistentId = $"{gameObject.scene.name}:{name}:{_initialPosition.x:F1},{_initialPosition.y:F1}";
            }

            _started = true;

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
            ApplyRestoredPlatformPresentation();

            if (GameManager.Instance != null) GameManager.Instance.Progress.MarkRewound(persistentId);

            TryOpenLinkedShortcut();

            if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
            _bounceRoutine = StartCoroutine(BounceRoutine());
        }

        // 묶인 대상이 전부 복원됐을 때만 숏컷을 연다.
        void TryOpenLinkedShortcut()
        {
            // 게이트 판정을 먼저 한다. 오브젝트 참조가 비어 있다고 여기서 빠져나가면
            // id로 여는 길까지 함께 막힌다.
            if (requiredSiblings != null)
                foreach (var sibling in requiredSiblings)
                    if (sibling != null && sibling.CanRewind) return; // 아직 안 돌아온 것이 있다

            if (linkedShortcut != null)
            {
                linkedShortcut.Open();
                return;
            }

            // 숏컷이 다른 방 씬에 있어 지금 메모리에 없는 경우다. 진행 상태에만 열림을 남긴다.
            // 개방 효과음과 봉인 애니메이션은 일부러 생략한다 — 플레이어가 서 있지도 않은 방의
            // 소리는 어디서 났는지 알 수 없고, 로드되지 않은 씬의 연출은 재생할 수도 없다.
            // 그 방에 다음에 들어설 때 Shortcut.Start가 저장값을 읽어 이미 열린 채로 맞이한다.
            if (!string.IsNullOrEmpty(linkedShortcutId) && GameManager.Instance != null)
                GameManager.Instance.Progress.MarkShortcutOpen(linkedShortcutId);
        }

        void Freeze()
        {
            if (_rb == null) return;
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Static;
        }

        void ApplyRestoredPlatformPresentation()
        {
            if (_restoredPlatformSprite == null || _restoredColliderSize.x <= 0f
                || _restoredColliderSize.y <= 0f)
                return;

            var collider = GetComponent<BoxCollider2D>();
            if (collider == null) return;

            // 채널링 중인 플레이어가 복원될 3x1 영역 안에 있으면 그 자리에서 판정을
            // 넓히지 않는다. 강제로 위로 옮기면 공중에 뜨고, 즉시 넓히면 아래 바닥과
            // 사이에 끼므로 플레이어가 영역을 벗어난 첫 물리 프레임에 안전하게 확장한다.
            if (PlayerOverlapsRestoredBounds(collider))
            {
                if (_restoredColliderRoutine == null)
                    _restoredColliderRoutine = StartCoroutine(ExpandRestoredColliderWhenClear(collider));
            }
            else
            {
                collider.offset = _restoredColliderOffset;
                collider.size = _restoredColliderSize;
                Physics2D.SyncTransforms();
            }

            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                renderer.enabled = false;

            Transform visual = transform.Find("RestoredPlatformVisual");
            if (visual == null)
            {
                var go = new GameObject("RestoredPlatformVisual");
                visual = go.transform;
                visual.SetParent(transform, false);
                go.AddComponent<SpriteRenderer>();
            }

            var platform = visual.GetComponent<SpriteRenderer>();
            platform.enabled = true;
            platform.sprite = _restoredPlatformSprite;
            platform.color = Color.white;
            platform.sortingOrder = 5;

            Vector2 spriteSize = _restoredPlatformSprite.bounds.size;
            float scale = _restoredColliderSize.x / Mathf.Max(0.001f, spriteSize.x);
            visual.localScale = Vector3.one * scale;
            visual.localPosition = Vector3.zero;

            AlignRestoredVisualSurface(visual, platform, collider);
        }

        void AlignRestoredVisualSurface(Transform visual, SpriteRenderer platform,
                                        BoxCollider2D collider)
        {
            Bounds collisionBounds = RestoredWorldBounds(collider);
            Bounds artBounds = platform.bounds;
            // 잔재 발판은 난간 장식이 돌 보행면보다 위로 솟아 있다. 사각 bounds 최고점을
            // 충돌면에 맞추면 실제 돌 윗면은 아래에 남아 캐릭터가 허공을 걷게 된다.
            float visibleSurfaceY = artBounds.max.y
                - artBounds.size.y * _restoredSurfaceInsetNormalized;
            visual.position += new Vector3(
                collisionBounds.center.x - artBounds.center.x,
                collisionBounds.max.y - visibleSurfaceY,
                0f);
        }

        IEnumerator ExpandRestoredColliderWhenClear(BoxCollider2D collider)
        {
            while (collider != null && PlayerOverlapsRestoredBounds(collider))
                yield return new WaitForFixedUpdate();

            if (collider != null)
            {
                collider.offset = _restoredColliderOffset;
                collider.size = _restoredColliderSize;
                Physics2D.SyncTransforms();
            }
            _restoredColliderRoutine = null;
        }

        bool PlayerOverlapsRestoredBounds(BoxCollider2D collider)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int hushedLayer = LayerMask.NameToLayer("PlayerHushed");
            int mask = 0;
            if (playerLayer >= 0) mask |= 1 << playerLayer;
            if (hushedLayer >= 0) mask |= 1 << hushedLayer;
            if (mask == 0) return false;

            Bounds bounds = RestoredWorldBounds(collider);
            // 위에 정상적으로 서 있는 접촉은 겹침으로 보지 않도록 세로 영역을 조금 줄인다.
            Vector2 querySize = new Vector2(bounds.size.x * 0.98f, bounds.size.y * 0.9f);
            return Physics2D.OverlapBox(bounds.center, querySize, 0f, mask) != null;
        }

        Bounds RestoredWorldBounds(BoxCollider2D collider)
        {
            Vector3 scale = transform.lossyScale;
            Vector3 center = transform.TransformPoint(_restoredColliderOffset);
            return new Bounds(center, new Vector3(
                _restoredColliderSize.x * Mathf.Abs(scale.x),
                _restoredColliderSize.y * Mathf.Abs(scale.y), 0.1f));
        }

        // 0.3초에 걸쳐 스케일을 0.8 -> 1.0으로 튕기는 되감기 연출.
        IEnumerator BounceRoutine()
        {
            const float duration = 0.3f;
            float elapsed = 0f;

            // R05 복원 발판은 그림만 튕겨야 한다. 루트를 튕기면 3x1 콜라이더까지 매 프레임
            // 커졌다 줄어들어, 방금 위로 올린 플레이어를 다시 바닥과 발판 사이에 끼운다.
            Transform bounceTarget = transform.Find("RestoredPlatformVisual");
            bool visualOnly = bounceTarget != null;
            if (!visualOnly) bounceTarget = transform;
            Vector3 finalScale = bounceTarget.localScale;
            var platform = visualOnly ? bounceTarget.GetComponent<SpriteRenderer>() : null;
            var collider = visualOnly ? GetComponent<BoxCollider2D>() : null;

            while (elapsed < duration)
            {
                float scale = Mathf.Lerp(0.8f, 1f, elapsed / duration);
                bounceTarget.localScale = new Vector3(
                    finalScale.x * scale, finalScale.y * scale, finalScale.z);
                if (platform != null && collider != null)
                    AlignRestoredVisualSurface(bounceTarget, platform, collider);
                elapsed += Time.deltaTime;
                yield return null;
            }

            bounceTarget.localScale = finalScale;
            if (platform != null && collider != null)
                AlignRestoredVisualSurface(bounceTarget, platform, collider);
            _bounceRoutine = null;
        }
    }
}
