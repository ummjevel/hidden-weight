using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.Core
{
    // 플레이어가 지나가면 이후 리스폰 지점을 이곳으로 갱신한다.
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        bool _used;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_used) return;
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;

            GameManager.Instance.Progress.LastCheckpoint = transform.position;
            AudioManager.Instance?.PlaySfx(SfxCue.Checkpoint, 0.7f);
            _used = true;

            // 체크포인트는 복귀 지점이자 휴식처다. 밟는 순간 체력을 채운다
            // (CONTENT_SYSTEM.md 6절 "체크포인트 휴식").
            var health = other.GetComponentInParent<HiddenWeight.Player.PlayerHealth>();
            if (health != null) health.RestoreFull();
        }
    }
}
