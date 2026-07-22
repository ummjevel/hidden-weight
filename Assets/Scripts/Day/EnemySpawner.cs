using System.Collections.Generic;
using UnityEngine;
using HanGame.Common;
using HanGame.Data;

namespace HanGame.Day
{
    /// <summary>
    /// 층별 60초 생성표(WaveTable)를 읽어 맵 사방에서 적을 스폰. 기획서 5.1/5.3.
    /// 동시 최대 개체 수(maxAlive)를 초과하면 스폰 보류.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private float spawnRadius = 10f; // 플레이어 주변 이 반경 밖에서 등장
        [SerializeField] private float mapHalfExtent = 12f;

        private FloorConfig _floor;
        private WaveTable _table;
        private float _time;
        private bool _running;
        private readonly Dictionary<int, float> _nextSpawnAt = new(); // entry index → 다음 스폰 시각

        // 처리(사망) 콜백 외부 구독용.
        public System.Action<Enemy> EnemyKilled;

        public void Begin(FloorConfig floor)
        {
            _floor = floor;
            _table = floor.waveTable;
            _time = 0f;
            _running = true;
            _nextSpawnAt.Clear();
            EnemyRegistry.Clear();

            if (_table != null)
                for (int i = 0; i < _table.entries.Count; i++)
                    _nextSpawnAt[i] = _table.entries[i].startTime;
        }

        public void Stop() => _running = false;

        private void Update()
        {
            if (!_running || _table == null) return;
            _time += Time.deltaTime; // 레벨업 정지 반영

            int maxAlive = Mathf.RoundToInt(_table.maxAlive * SpawnMul);
            for (int i = 0; i < _table.entries.Count; i++)
            {
                var e = _table.entries[i];
                if (_time < e.startTime || _time > e.endTime) continue;
                if (_time < _nextSpawnAt[i]) continue;
                if (EnemyRegistry.Count >= maxAlive) continue;

                int count = Mathf.Max(1, Mathf.RoundToInt(e.countPerSpawn * SpawnMul));
                for (int c = 0; c < count && EnemyRegistry.Count < maxAlive; c++)
                    Spawn(e.enemy);

                _nextSpawnAt[i] = _time + Mathf.Max(0.05f, e.interval);
            }
        }

        private float SpawnMul => _floor != null ? _floor.spawnMultiplier : 1f;

        private void Spawn(EnemyData data)
        {
            if (data == null || data.prefab == null) return;
            Vector2 pos = RandomEdgePosition();
            var go = Instantiate(data.prefab, pos, Quaternion.identity);
            var enemy = go.GetComponent<Enemy>();
            if (enemy == null) enemy = go.AddComponent<Enemy>();
            enemy.Init(data, _floor);
            enemy.Killed += OnEnemyKilled;
        }

        private void OnEnemyKilled(Enemy e) => EnemyKilled?.Invoke(e);

        // 플레이어에서 일정 거리 밖, 맵 경계 안의 무작위 위치.
        private Vector2 RandomEdgePosition()
        {
            Vector2 center = Player.Local != null ? Player.Local.Position : Vector2.zero;
            for (int tries = 0; tries < 8; tries++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                Vector2 p = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spawnRadius;
                p.x = Mathf.Clamp(p.x, -mapHalfExtent, mapHalfExtent);
                p.y = Mathf.Clamp(p.y, -mapHalfExtent, mapHalfExtent);
                return p;
            }
            return center + Vector2.right * spawnRadius;
        }

        /// <summary>60초 종료 시 남은 일반 적 처리 연출 후 제거(기획서 5.1).</summary>
        public void ClearRemaining()
        {
            _running = false;
            var snapshot = new List<Enemy>(EnemyRegistry.Alive);
            foreach (var e in snapshot)
                if (e != null && e.Type != EnemyType.CeoDirective) e.Kill();
        }
    }
}
