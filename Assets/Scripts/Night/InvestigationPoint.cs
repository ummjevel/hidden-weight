using System;
using UnityEngine;
using HanGame.Common;

namespace HanGame.Night
{
    /// <summary>
    /// 조사 대상(설계서·규칙·사직서). E로 1.5초 조사. 기획서 11.4/8.6.
    /// 조사만으로는 성공 아님 — 이후 출구 탈출 필요(11.3).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class InvestigationPoint : MonoBehaviour
    {
        [SerializeField] private float investigateSeconds = 1.5f;
        [SerializeField] private KeyCode key = KeyCode.E;

        public bool Investigated { get; private set; }
        public event Action<float> Progress; // 0~1
        public event Action Completed;

        private bool _playerInRange;
        private float _timer;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<Player>() != null) _playerInRange = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<Player>() != null)
            {
                _playerInRange = false;
                if (!Investigated) { _timer = 0f; Progress?.Invoke(0f); }
            }
        }

        private void Update()
        {
            if (Investigated || !_playerInRange) return;

            if (Input.GetKey(key))
            {
                _timer += Time.deltaTime;
                Progress?.Invoke(Mathf.Clamp01(_timer / investigateSeconds));
                if (_timer >= investigateSeconds)
                {
                    Investigated = true;
                    Completed?.Invoke();
                }
            }
            else if (_timer > 0f)
            {
                _timer = 0f;
                Progress?.Invoke(0f);
            }
        }
    }
}
