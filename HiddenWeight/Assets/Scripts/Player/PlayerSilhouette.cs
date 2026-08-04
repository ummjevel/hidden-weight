using UnityEngine;

namespace HiddenWeight.Player
{
    // 플레이어를 밝은 배경에서 분리해 준다.
    //
    // 치비 플레이어는 거의 흰색이다. 어두운 빌드 화면에서는 잘 보였지만, 균열의 최종 배경은
    // 흰 대리석과 파스텔이라 캐릭터가 지형에 녹는다. 원본 PNG를 다시 뽑지 않고 두 가지만 얹는다.
    //
    //  - 발밑 접촉 그림자: "지금 바닥에 닿아 있다"를 알려 주는 가장 싼 신호다. 공중에서는
    //    작아지고 옅어져, 점프 중인지 서 있는지가 실루엣만으로 읽힌다.
    //  - 청회색 외곽 1단: 흰 배경 위에서도 머리와 외투의 경계가 남는다.
    //
    // 프리팹을 다시 굽지 않고 런타임에 붙인다. 플레이어 프리팹 전체 재생성은 씬에 남은
    // 인스턴스 오버라이드를 끊는다(PrefabBuilder.ApplyPlayerPhysicsMaterial 주석과 같은 이유).
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerSilhouette : MonoBehaviour
    {
        const float ShadowWidth = 0.62f;
        const float ShadowHeight = 0.16f;
        const float MaxDrop = 4f;          // 이보다 멀면 바닥이 없는 것으로 본다

        static readonly Color OutlineColor = new Color(0.42f, 0.48f, 0.62f, 1f);

        SpriteRenderer _shadow;
        PlayerController _controller;
        int _groundMask;
        static Sprite _blob;

        void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _groundMask = LayerMask.GetMask("Ground", "Wall");
            BuildShadow();
            ApplyOutline();
        }

        // 그림자는 플레이어의 자식이 아니라 씬에 따로 둔다.
        //
        // 자식으로 두면 두 가지가 어긋난다 — 플레이어가 방향을 바꿀 때 루트 스케일이
        // 뒤집혀 그림자도 같이 뒤집히고, "플레이어에 보이는 스프라이트는 하나"라는 규칙이
        // 깨져 피격 점멸이 엉뚱한 렌더러를 켤 수 있다(회귀 검사가 이것을 잡는다).
        void BuildShadow()
        {
            var go = new GameObject("PlayerContactShadow");
            _shadow = go.AddComponent<SpriteRenderer>();
            _shadow.sprite = Blob();
            // 플레이어(10)보다 뒤, 지형 표면 아트(4~6)보다 앞.
            _shadow.sortingOrder = 8;
            _shadow.color = new Color(0.16f, 0.18f, 0.28f, 0.34f);
        }

        // 스프라이트를 새로 그리지 않고 외곽선만 얹는다. 전용 셰이더가 없으면 조용히 넘어간다.
        void ApplyOutline()
        {
            var shader = Shader.Find("Hidden Weight/SpriteOutline");
            if (shader == null) return;

            var material = new Material(shader) { hideFlags = HideFlags.DontSave };
            material.SetColor("_OutlineColor", OutlineColor);
            material.SetFloat("_OutlineWidth", 1.6f);
            material.SetFloat("_FillAlpha", 1f);   // 캐릭터 자체는 그대로 보여야 한다

            // 그림자는 이미 씬에 따로 있으므로 여기 목록에 들어오지 않는다.
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                // 공격 궤적·이펙트까지 두르면 번져 보인다. 실제 몸통만 대상으로 한다.
                if (renderer.sortingOrder < 9) continue;
                renderer.sharedMaterial = material;
            }
        }

        void OnDestroy()
        {
            if (_shadow != null) Destroy(_shadow.gameObject);
        }

        void LateUpdate()
        {
            if (_shadow == null) return;

            var origin = (Vector2)transform.position;
            var hit = Physics2D.Raycast(origin, Vector2.down, MaxDrop, _groundMask);
            if (hit.collider == null)
            {
                _shadow.enabled = false;
                return;
            }

            _shadow.enabled = true;
            // 그림자는 월드에 놓인다. 부모를 따라 기울거나 뒤집히면 안 된다.
            _shadow.transform.position = new Vector3(origin.x, hit.point.y + 0.03f, 0f);
            _shadow.transform.rotation = Quaternion.identity;

            // 멀어질수록 작고 옅게. 점프 높이를 눈으로 읽게 해 주는 것이 이 그림자의 본래 일이다.
            float drop = Mathf.Clamp01(hit.distance / 3f);
            float scale = Mathf.Lerp(1f, 0.5f, drop);
            Vector2 size = Blob().bounds.size;
            _shadow.transform.localScale = new Vector3(
                ShadowWidth * scale / size.x, ShadowHeight * scale / size.y, 1f);

            var color = _shadow.color;
            color.a = Mathf.Lerp(0.34f, 0.1f, drop);
            _shadow.color = color;
        }

        // 가장자리가 부드러운 타원 한 장. 사각형 그림자는 발밑에 상자를 놓은 것처럼 보인다.
        static Sprite Blob()
        {
            if (_blob != null) return _blob;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            texture.Apply();
            _blob = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _blob.name = "ContactShadowBlob";
            return _blob;
        }
    }
}
