using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HiddenWeight.World
{
    // 한 번 터지고 사라지는 충돌 연출(ResidueImpactVFX_v1: 근접 타격 / 벽 충돌 / 일반 착지 /
    // 강한 충돌). 지역 씬에 하나만 놓고, 필요한 쪽이 Play로 자리와 종류만 알려 준다.
    //
    // 왜 프리팹이 아니라 지역 싱글턴인가: 이 프로젝트는 씬과 프리팹을 전부 코드로 짓기 때문에
    // 런타임에 프리팹을 참조하려면 어딘가에 그 참조를 들고 있어야 한다. 프레임 배열만 들고
    // 그때그때 오브젝트를 만드는 편이 빌더와 궁합이 맞고, 아트가 아직 없는 지역에서는 이
    // 컴포넌트를 아예 두지 않으면 호출부가 조용히 넘어간다.
    public class ImpactVFX : MonoBehaviour
    {
        [System.Serializable]
        public class Effect
        {
            public string name;
            public Sprite[] frames;
            public float fps = 16f;
            public float displayHeight = 1.2f;
        }

        [SerializeField] Effect[] effects;
        [SerializeField] int sortingOrder = 12; // 플레이어(10)·공격 섬광(11) 위

        public static ImpactVFX Instance { get; private set; }

        readonly Dictionary<string, Effect> _byName = new Dictionary<string, Effect>();

        void Awake()
        {
            Instance = this;

            if (effects == null) return;
            foreach (var effect in effects)
                if (effect != null && !string.IsNullOrEmpty(effect.name) && effect.frames != null
                    && effect.frames.Length > 0)
                    _byName[effect.name] = effect;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // 아트가 없는 지역에서도 호출부가 그대로 돌아가도록 정적 진입점을 둔다.
        // 인스턴스가 없거나 해당 효과가 없으면 아무 일도 일어나지 않는다.
        public static void Play(string effectName, Vector3 position, int facing = 1)
        {
            if (Instance != null) Instance.Spawn(effectName, position, facing);
        }

        public bool Has(string effectName) => _byName.ContainsKey(effectName);

        void Spawn(string effectName, Vector3 position, int facing)
        {
            if (!_byName.TryGetValue(effectName, out var effect)) return;

            var go = new GameObject("ImpactVFX_" + effectName);
            go.transform.position = position;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.flipX = facing < 0;

            StartCoroutine(PlayRoutine(go, renderer, effect));
        }

        IEnumerator PlayRoutine(GameObject go, SpriteRenderer renderer, Effect effect)
        {
            float interval = effect.fps <= 0f ? 0.05f : 1f / effect.fps;

            for (int i = 0; i < effect.frames.Length; i++)
            {
                if (go == null) yield break;

                renderer.sprite = effect.frames[i];

                // 프레임마다 원본 셀 크기가 달라도 화면 크기는 일정해야 한다(SpriteAnimator와 같은 규칙).
                if (effect.displayHeight > 0f && renderer.sprite != null)
                {
                    float height = renderer.sprite.bounds.size.y;
                    if (height > 0f)
                    {
                        float scale = effect.displayHeight / height;
                        go.transform.localScale = new Vector3(scale, scale, 1f);
                    }
                }

                yield return new WaitForSeconds(interval);
            }

            if (go != null) Destroy(go);
        }
    }
}
