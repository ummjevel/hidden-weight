using System;
using UnityEngine;
using HanGame.Common;

namespace HanGame.Night
{
    /// <summary>
    /// 부채꼴 시야 판정. 경비·야근자·CCTV가 공용으로 사용. 기획서 11.5/11.7.
    /// 시야각·거리 안이고 벽에 가리지 않으면 발각.
    /// 판정은 명확하고 공정해야 함(기획서 11.9) → 벽 차단은 Raycast로 검사.
    /// </summary>
    public class VisionCone : MonoBehaviour
    {
        [SerializeField] private float viewDistance = 4f;
        [SerializeField] private float viewAngle = 60f;
        [SerializeField] private LayerMask obstacleMask; // 벽·책상 레이어
        [SerializeField] private Transform origin;        // 시야 시작점(없으면 자기 자신)

        /// <summary>바라보는 방향(경비/CCTV가 매 프레임 갱신).</summary>
        public Vector2 FacingDir { get; set; } = Vector2.right;

        public bool Active { get; set; } = true;
        public event Action PlayerSpotted;

        private bool _fired;

        public void Configure(float distance, float angle, LayerMask mask)
        {
            viewDistance = distance;
            viewAngle = angle;
            obstacleMask = mask;
        }

        private Vector2 Origin => origin != null ? (Vector2)origin.position : (Vector2)transform.position;

        private void Update()
        {
            if (!Active || _fired) return;
            if (CanSeePlayer()) { _fired = true; PlayerSpotted?.Invoke(); }
        }

        public bool CanSeePlayer()
        {
            var player = Player.Local;
            if (player == null) return false;

            Vector2 o = Origin;
            Vector2 to = player.Position - o;
            float dist = to.magnitude;
            if (dist > viewDistance) return false;

            Vector2 dir = dist > 0.001f ? to / dist : FacingDir;
            float angle = Vector2.Angle(FacingDir, dir);
            if (angle > viewAngle * 0.5f) return false;

            // 벽 차단 검사.
            var hit = Physics2D.Raycast(o, dir, dist, obstacleMask);
            return hit.collider == null;
        }

        public void ResetTrigger() => _fired = false;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
            Vector2 o = Origin;
            Vector2 left = Rotate(FacingDir, -viewAngle * 0.5f);
            Vector2 right = Rotate(FacingDir, viewAngle * 0.5f);
            Gizmos.DrawLine(o, o + left * viewDistance);
            Gizmos.DrawLine(o, o + right * viewDistance);
        }

        private static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
