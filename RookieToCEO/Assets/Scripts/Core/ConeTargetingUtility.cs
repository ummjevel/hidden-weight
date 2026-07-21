using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RookieToCEO.Core
{
    // 키보드 샷건(넓은 부채꼴, 다중 타겟)과 스테이플러 연사(좁은 직선, 관통)가 공통으로 쓰는
    // "정면 부채꼴 범위 안의 적 찾기" 계산. TargetingUtility(최근접 1명)와 달리 범위/각도/개수
    // 제한이 있는 다중 타겟용이라 별도 유틸리티로 분리했다.
    public static class ConeTargetingUtility
    {
        // origin에서 facingDirection 방향으로, halfAngleDegrees(중심선 기준 좌우 각도)와 range 안에 있는
        // 후보들을 가까운 순으로 최대 maxTargets개까지 인덱스로 반환한다.
        public static List<int> FindTargetsInCone(
            Vector2 origin,
            Vector2 facingDirection,
            float halfAngleDegrees,
            float range,
            IReadOnlyList<Vector2> candidates,
            int maxTargets)
        {
            var result = new List<(int index, float sqrDistance)>();
            var normalizedFacing = facingDirection.sqrMagnitude > 0f ? facingDirection.normalized : Vector2.up;
            var rangeSqr = range * range;

            for (var i = 0; i < candidates.Count; i++)
            {
                var toCandidate = candidates[i] - origin;
                var sqrDistance = toCandidate.sqrMagnitude;
                if (sqrDistance > rangeSqr) continue;
                if (sqrDistance <= 0f)
                {
                    // 원점과 완전히 같은 위치면 각도를 정의할 수 없으니 그냥 포함시킨다.
                    result.Add((i, 0f));
                    continue;
                }

                var angle = Vector2.Angle(normalizedFacing, toCandidate);
                if (angle <= halfAngleDegrees)
                {
                    result.Add((i, sqrDistance));
                }
            }

            return result
                .OrderBy(entry => entry.sqrDistance)
                .Take(maxTargets)
                .Select(entry => entry.index)
                .ToList();
        }
    }
}
