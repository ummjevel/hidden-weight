using System;
using System.Collections.Generic;
using UnityEngine;
using HanGame.Common;
using HanGame.Data;
using HanGame.Day;

namespace HanGame.Weapons
{
    /// <summary>
    /// 업무 떠넘기기(액티브). Space로 사용. 기획서 8.4.
    /// 주변 적을 바깥으로 밀어내 포위 탈출. 쿨타임 12초(권장), '짬' 강화로 감소.
    /// 2층 밤 보상 획득 후 사용 가능.
    /// </summary>
    public class TaskDelegateSkill : MonoBehaviour
    {
        [SerializeField] private WeaponData data; // task_delegate
        [SerializeField] private KeyCode key = KeyCode.Space;

        public float Cooldown { get; private set; }
        public float CooldownRemaining { get; private set; }
        public event Action Used;

        private readonly List<Enemy> _buffer = new();
        private PlayerStats _stats;

        private void Start() => _stats = Player.Local != null ? Player.Local.Stats : null;

        private void Update()
        {
            if (CooldownRemaining > 0f) CooldownRemaining -= Time.deltaTime;

            var run = GameManager.Instance != null ? GameManager.Instance.Run : null;
            if (run == null || !run.HasWeapon(WeaponIds.TaskDelegate)) return;
            if (Time.timeScale == 0f) return;

            if (Input.GetKeyDown(key) && CooldownRemaining <= 0f)
                Activate();
        }

        private void Activate()
        {
            if (data == null || Player.Local == null) return;
            Vector2 origin = Player.Local.Position;

            foreach (var e in EnemyRegistry.InRadius(origin, data.pushRadius, _buffer))
                e.Push(origin, data.pushForce);

            float mul = _stats != null ? _stats.ActiveCooldownMul : 1f;
            Cooldown = data.cooldown * mul;
            CooldownRemaining = Cooldown;
            Used?.Invoke();
        }
    }
}
