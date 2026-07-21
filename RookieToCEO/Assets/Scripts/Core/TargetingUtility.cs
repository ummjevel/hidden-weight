using System.Collections.Generic;
using UnityEngine;

namespace RookieToCEO.Core
{
    // GDD 2번(조작 방식) "가장 가까운 적을 기본 무기로 자동 공격"의 계산만 담당하는 순수 함수.
    // Transform을 직접 순회하지 않고 위치 배열을 받는 형태로 만들어서,
    // MonoBehaviour/씬 없이도 EditMode에서 타겟팅 알고리즘 자체를 테스트할 수 있게 한다.
    public static class TargetingUtility
    {
        // 후보가 없으면 -1을 반환한다.
        public static int FindNearestIndex(Vector2 origin, IReadOnlyList<Vector2> candidates)
        {
            var nearestIndex = -1;
            var nearestSqrDistance = float.MaxValue;

            for (var i = 0; i < candidates.Count; i++)
            {
                var sqrDistance = (candidates[i] - origin).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }
    }
}
