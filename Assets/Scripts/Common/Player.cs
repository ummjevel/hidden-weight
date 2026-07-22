using UnityEngine;

namespace HanGame.Common
{
    /// <summary>
    /// 플레이어 루트 파사드. 씬에 하나 존재하며 자신을 static으로 등록해
    /// 적·무기·시스템이 참조를 싸게 얻게 한다.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class Player : MonoBehaviour
    {
        public static Player Local { get; private set; }

        public PlayerController Controller { get; private set; }
        public PlayerHealth Health { get; private set; }
        public PlayerStats Stats { get; private set; }

        public Vector2 Position => transform.position;

        private void Awake()
        {
            Local = this;
            Controller = GetComponent<PlayerController>();
            Health = GetComponent<PlayerHealth>();
            Stats = GetComponent<PlayerStats>();
        }

        private void OnDestroy()
        {
            if (Local == this) Local = null;
        }
    }
}
