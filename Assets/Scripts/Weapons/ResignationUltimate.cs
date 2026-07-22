using System;
using System.Collections.Generic;
using UnityEngine;
using HanGame.Common;
using HanGame.Data;
using HanGame.Day;

namespace HanGame.Weapons
{
    /// <summary>
    /// 퇴사 통보(궁극기). R로 사용. 기획서 8.5.
    /// 일반 적 공포(도주), 정예 감속, CEO 웨이브 3초 정지.
    /// 게이지는 적 처리 시 충전(gaugePerKill), 가득 차야 사용 가능.
    /// 3층 밤 보상 획득 후 사용 가능.
    /// </summary>
    public class ResignationUltimate : MonoBehaviour
    {
        [SerializeField] private WeaponData data; // resignation
        [SerializeField] private KeyCode key = KeyCode.R;
        [SerializeField] private EnemySpawner spawner; // 처리 이벤트 구독용(선택)

        [Range(0f, 1f)] public float Gauge { get; private set; }
        public bool Ready => Gauge >= 1f;
        public event Action<float> GaugeChanged;
        public event Action Used;

        private readonly List<Enemy> _buffer = new();

        private void OnEnable()
        {
            if (spawner != null) spawner.EnemyKilled += OnKill;
        }

        private void OnDisable()
        {
            if (spawner != null) spawner.EnemyKilled -= OnKill;
        }

        private void OnKill(Enemy e)
        {
            if (data == null) return;
            Gauge = Mathf.Min(1f, Gauge + data.gaugePerKill);
            GaugeChanged?.Invoke(Gauge);
        }

        private void Update()
        {
            var run = GameManager.Instance != null ? GameManager.Instance.Run : null;
            if (run == null || !run.HasWeapon(WeaponIds.ResignationNotice)) return;
            if (Time.timeScale == 0f) return;

            if (Input.GetKeyDown(key) && Ready)
                Activate();
        }

        private void Activate()
        {
            if (data == null) return;

            // 모든 살아있는 적에 효과 적용.
            var all = new List<Enemy>(EnemyRegistry.Alive);
            foreach (var e in all)
            {
                if (e == null || e.IsDead) continue;
                if (e.Type == EnemyType.CeoDirective) e.ApplyStun(data.ceoStunDuration);
                else e.ApplyFear(data.fearDuration, data.eliteSlow);
            }

            Gauge = 0f;
            GaugeChanged?.Invoke(Gauge);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.Resignation);
            Used?.Invoke();
        }
    }
}
