using System.Collections.Generic;
using UnityEngine;

namespace HanGame.Day
{
    /// <summary>
    /// 살아있는 적을 추적하는 정적 레지스트리.
    /// 자동 조준(가장 가까운 적), 상사의 시선, 업무 떠넘기기, 퇴사 통보가
    /// FindObjectsOfType 없이 대상을 얻게 한다.
    /// </summary>
    public static class EnemyRegistry
    {
        public static readonly List<Enemy> Alive = new();

        public static int Count => Alive.Count;

        public static void Register(Enemy e) { if (!Alive.Contains(e)) Alive.Add(e); }
        public static void Unregister(Enemy e) => Alive.Remove(e);
        public static void Clear() => Alive.Clear();

        /// <summary>from 위치에서 maxRange 안의 가장 가까운 적. 없으면 null.</summary>
        public static Enemy Nearest(Vector2 from, float maxRange)
        {
            Enemy best = null;
            float bestSqr = maxRange * maxRange;
            for (int i = 0; i < Alive.Count; i++)
            {
                var e = Alive[i];
                if (e == null || e.IsDead) continue;
                float d = ((Vector2)e.transform.position - from).sqrMagnitude;
                if (d <= bestSqr) { bestSqr = d; best = e; }
            }
            return best;
        }

        /// <summary>center 반경 안의 적을 모아 반환(비할당 재사용 버퍼).</summary>
        public static List<Enemy> InRadius(Vector2 center, float radius, List<Enemy> buffer)
        {
            buffer.Clear();
            float sqr = radius * radius;
            for (int i = 0; i < Alive.Count; i++)
            {
                var e = Alive[i];
                if (e == null || e.IsDead) continue;
                if (((Vector2)e.transform.position - center).sqrMagnitude <= sqr) buffer.Add(e);
            }
            return buffer;
        }
    }
}
