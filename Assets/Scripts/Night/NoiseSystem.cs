using UnityEngine;
using HanGame.Common;

namespace HanGame.Night
{
    /// <summary>
    /// 달리기 소음. 기획서 11.8. 달리면 소음 범위 안의 경비가 위치를 확인하러 옴.
    /// 개발 기간 부족 시 enabled=false로 소음 제거하고 달리기만 유지(보류 가능).
    /// </summary>
    public class NoiseSystem : MonoBehaviour
    {
        [SerializeField] private bool systemEnabled = true;
        [SerializeField] private float noiseRadius = 3f;
        [SerializeField] private float alertInterval = 0.5f;

        private GuardPatrol[] _guards;
        private float _timer;

        private void Start() => _guards = FindObjectsOfType<GuardPatrol>();

        public void SetEnabled(bool value) => systemEnabled = value;

        private void Update()
        {
            if (!systemEnabled) return;
            var player = Player.Local;
            if (player == null || player.Controller == null || !player.Controller.IsRunning) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = alertInterval;

            Vector2 pos = player.Position;
            foreach (var g in _guards)
            {
                if (g == null) continue;
                if (Vector2.Distance(g.transform.position, pos) <= noiseRadius)
                    g.HearNoise(pos);
            }
        }

        // 소음 범위는 UI(밤 HUD)에서 player.Controller.IsRunning일 때 원으로 표시.
        public float NoiseRadius => noiseRadius;
        public bool Enabled => systemEnabled;
    }
}
