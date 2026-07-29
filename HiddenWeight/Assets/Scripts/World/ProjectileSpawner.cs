using System.Collections.Generic;
using UnityEngine;

namespace HiddenWeight.World
{
    // 지역에 하나 놓는 공격체 발사대. 적·보스는 "무엇을, 어디서, 어느 방향으로"만 알려 주고
    // 속도·피해·수명 같은 수치는 여기 등록된 정의를 따른다.
    //
    // ImpactVFX와 같은 구조다. 아트가 없는 지역에서는 이 컴포넌트를 두지 않으면 호출부가
    // 조용히 넘어가므로, 공격체 시트가 아직 없는 지역도 그대로 돌아간다.
    public class ProjectileSpawner : MonoBehaviour
    {
        [System.Serializable]
        public class Definition
        {
            public string name;
            public Sprite[] frames;
            public float fps = 14f;
            public float speed = 7f;
            public float lifetime = 2.5f;
            public float radius = 0.5f;
            public int damage = 1;
            public float displayHeight = 1f;

            [Tooltip("켜면 지형에 닿아도 사라지지 않는다. 바닥을 훑는 충격파처럼 지면을 따라가는 공격에 쓴다.")]
            public bool ignoreTerrain;
        }

        [SerializeField] Definition[] definitions;
        [SerializeField] int sortingOrder = 9; // 플레이어(10) 바로 아래 — 캐릭터를 가리지 않는다

        public static ProjectileSpawner Instance { get; private set; }

        readonly Dictionary<string, Definition> _byName = new Dictionary<string, Definition>();
        int _playerMask;
        int _obstacleMask;

        void Awake()
        {
            Instance = this;
            _playerMask = 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("PlayerHushed");
            _obstacleMask = 1 << LayerMask.NameToLayer("Ground") | 1 << LayerMask.NameToLayer("Wall");

            if (definitions == null) return;
            foreach (var definition in definitions)
                if (definition != null && !string.IsNullOrEmpty(definition.name)
                    && definition.frames != null && definition.frames.Length > 0)
                    _byName[definition.name] = definition;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Fire(string projectileName, Vector3 origin, Vector2 direction)
        {
            if (Instance != null) Instance.Launch(projectileName, origin, direction);
        }

        public bool Has(string projectileName) => _byName.ContainsKey(projectileName);

        void Launch(string projectileName, Vector3 origin, Vector2 direction)
        {
            if (!_byName.TryGetValue(projectileName, out var definition)) return;
            if (direction.sqrMagnitude <= 0.0001f) direction = Vector2.right;

            var go = new GameObject("Projectile_" + projectileName);
            go.transform.position = origin;

            var projectile = go.AddComponent<Projectile>();
            projectile.Launch(definition.frames, definition.fps, definition.speed, definition.lifetime,
                definition.radius, definition.damage, direction, definition.displayHeight,
                _playerMask, definition.ignoreTerrain ? 0 : _obstacleMask, sortingOrder);
        }
    }
}
