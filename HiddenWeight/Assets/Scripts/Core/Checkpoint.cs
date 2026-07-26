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
            _used = true;
        }
    }
}
