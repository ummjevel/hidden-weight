using System;
using UnityEngine;
using HanGame.Common;

namespace HanGame.Night
{
    /// <summary>
    /// 출구. 무기 조사 후 도착하면 자동 탈출. 기획서 11.3/11.4.
    /// 조사 전에는 탈출 불가.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ExitZone : MonoBehaviour
    {
        public event Action PlayerReached;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<Player>() != null) PlayerReached?.Invoke();
        }
    }
}
