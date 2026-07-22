using System.Collections.Generic;
using UnityEngine;
using HanGame.Common;
using HanGame.Data;
using HanGame.Day;

namespace HanGame.Weapons
{
    /// <summary>
    /// 자동 무기 구동. 기획서 4.3/8장.
    /// RunState.Weapons에 있는 자동 무기(키보드 샷건·스테이플러)를 각자 쿨타임으로 발사.
    /// 공격 시마다 가장 가까운 적을 재탐색(기획서 8.2 권장 자동 조준).
    /// 상사의 시선 '일하는 척' 중에는 발사 정지(기획서 10.2).
    /// </summary>
    public class AutoAttackSystem : MonoBehaviour
    {
        [Header("무기 데이터(자동 무기)")]
        [SerializeField] private WeaponData keyboardShotgun;
        [SerializeField] private WeaponData staplerRapid;

        [SerializeField] private BossGaze bossGaze; // 없으면 항상 발사

        private PlayerStats _stats;
        private readonly Dictionary<string, float> _cooldownTimers = new();

        private void Start()
        {
            _stats = Player.Local != null ? Player.Local.Stats : GetComponent<PlayerStats>();
        }

        private void Update()
        {
            if (Time.timeScale == 0f) return;                 // 레벨업 정지
            if (bossGaze != null && bossGaze.PlayerCaught) return; // 일하는 척

            TickWeapon(keyboardShotgun);
            TickWeapon(staplerRapid);
        }

        private void TickWeapon(WeaponData w)
        {
            if (w == null) return;
            var run = GameManager.Instance != null ? GameManager.Instance.Run : null;
            if (run != null && !run.HasWeapon(w.id)) return; // 미보유

            float atkSpeedMul = _stats != null ? _stats.AttackSpeedMul : 1f;
            float rangeMul = _stats != null ? _stats.AttackRangeMul : 1f;
            float dmgMul = _stats != null ? _stats.AttackPowerMul : 1f;

            float t = _cooldownTimers.TryGetValue(w.id, out var v) ? v : 0f;
            t -= Time.deltaTime;
            if (t <= 0f)
            {
                if (Fire(w, w.range * rangeMul, w.damage * dmgMul))
                    t = w.attackInterval / Mathf.Max(0.1f, atkSpeedMul);
                else
                    t = 0.1f; // 적 없으면 짧게 재시도
            }
            _cooldownTimers[w.id] = t;
        }

        private bool Fire(WeaponData w, float range, float damage)
        {
            var player = Player.Local;
            if (player == null || w.projectilePrefab == null) return false;

            var target = EnemyRegistry.Nearest(player.Position, range);
            if (target == null) return false;

            Vector2 origin = player.Position;
            Vector2 baseDir = ((Vector2)target.transform.position - origin).normalized;

            if (w.id == WeaponIds.KeyboardShotgun)
            {
                // 부채꼴 다발(기획서 8.2).
                int n = Mathf.Max(1, w.pellets);
                float step = n > 1 ? w.spreadAngle / (n - 1) : 0f;
                float start = -w.spreadAngle * 0.5f;
                for (int i = 0; i < n; i++)
                {
                    float ang = start + step * i;
                    Vector2 dir = Rotate(baseDir, ang);
                    SpawnProjectile(w, origin, dir, damage);
                }
                PlaySfx(Sfx.KeyboardHit);
            }
            else
            {
                // 단일 직선(스테이플러, 기획서 8.3).
                SpawnProjectile(w, origin, baseDir, damage);
                PlaySfx(Sfx.StaplerFire);
            }
            return true;
        }

        private void SpawnProjectile(WeaponData w, Vector2 origin, Vector2 dir, float damage)
        {
            var go = Instantiate(w.projectilePrefab, origin, Quaternion.identity);
            var proj = go.GetComponent<Projectile>();
            if (proj == null) proj = go.AddComponent<Projectile>();
            proj.Launch(dir, w.projectileSpeed, damage, w.pierces);
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(r), sin = Mathf.Sin(r);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        private void PlaySfx(string id)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(id);
        }
    }
}
