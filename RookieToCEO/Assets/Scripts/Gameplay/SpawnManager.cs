using System;
using System.Collections.Generic;
using RookieToCEO.Core;
using UnityEngine;

namespace RookieToCEO.Gameplay
{
    [Serializable]
    public struct EnemyPrefabEntry
    {
        public EnemyType Type;
        public GameObject Prefab;
    }

    // GDD 5번(층별 낮 디펜스 구조)의 스폰을 실제로 실행하는 MonoBehaviour.
    // "언제 어떤 타입이 나올 수 있는지"는 WaveSpawnTable(순수 로직, EditMode 테스트 대상)에 맡기고,
    // 여기서는 그 결과로 실제 GameObject를 Instantiate하는 것만 담당한다.
    // 프리팹(도트 스프라이트)은 M9 전까지 비어 있을 수 있으므로, 비어 있으면 조용히 스킵한다.
    public class SpawnManager : MonoBehaviour
    {
        [SerializeField] private int floor = 1;
        [SerializeField] private float spawnRadius = 8f; // 플레이어 기준 스폰 반경
        [SerializeField] private List<EnemyPrefabEntry> enemyPrefabs = new List<EnemyPrefabEntry>();

        private Transform _playerTransform;
        private float _spawnTimer;
        private float _baseSpawnInterval;
        private readonly Dictionary<EnemyType, GameObject> _prefabLookup = new Dictionary<EnemyType, GameObject>();

        private void Awake()
        {
            foreach (var entry in enemyPrefabs)
            {
                _prefabLookup[entry.Type] = entry.Prefab;
            }
        }

        private void Start()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player != null) _playerTransform = player.transform;

            RefreshFloorInterval();
        }

        // GameFlowManager가 Day/Night/Boss 씬을 오가며 같은 SpawnManager를 여러 층에 재사용할 때
        // (Day 씬은 1~3층에서 재사용된다) 층 번호를 바꿔주기 위한 런타임 설정 메서드.
        public void SetFloor(int newFloor)
        {
            floor = newFloor;
            RefreshFloorInterval();
        }

        private void RefreshFloorInterval()
        {
            // 층별 기준 스폰 간격(GDD 5번: 1층은 적게, 3층은 크게 증가) - WaveSpawnTable에서 가져온다.
            _baseSpawnInterval = WaveSpawnTable.GetBaseSpawnIntervalSeconds(floor);
            _spawnTimer = _baseSpawnInterval;
        }

        // DayWaveManager(M6)가 매 프레임 웨이브 경과 시간을 넘겨 호출한다.
        public void Tick(float elapsedSeconds, float deltaTime)
        {
            _spawnTimer -= deltaTime * WaveSpawnTable.GetSpawnRateMultiplier(elapsedSeconds);
            if (_spawnTimer > 0f) return;

            _spawnTimer = _baseSpawnInterval;
            SpawnOne(elapsedSeconds);
        }

        private void SpawnOne(float elapsedSeconds)
        {
            var activeTypes = WaveSpawnTable.GetActiveEnemyTypes(floor, elapsedSeconds);
            if (activeTypes.Count == 0) return;

            var type = PickRandom(activeTypes);
            if (!_prefabLookup.TryGetValue(type, out var prefab) || prefab == null) return;

            var spawnPosition = RandomPointOnRing();
            Instantiate(prefab, spawnPosition, Quaternion.identity);
        }

        private EnemyType PickRandom(HashSet<EnemyType> types)
        {
            var pickIndex = UnityEngine.Random.Range(0, types.Count);
            var i = 0;
            foreach (var type in types)
            {
                if (i == pickIndex) return type;
                i++;
            }

            return EnemyType.EmailEnvelope; // 도달할 일 없지만 컴파일러를 위한 기본값
        }

        private Vector2 RandomPointOnRing()
        {
            var center = _playerTransform != null ? (Vector2)_playerTransform.position : Vector2.zero;
            var angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
            return center + offset;
        }
    }
}
