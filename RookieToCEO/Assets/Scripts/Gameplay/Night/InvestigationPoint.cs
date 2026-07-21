using UnityEngine;
using UnityEngine.InputSystem;

namespace RookieToCEO.Gameplay.Night
{
    // GDD 10/12번: "서류 또는 장비 조사". 플레이어가 조사 반경 안에서 E를 누르면 무기를 배웠다는
    // 판정만 남긴다(실제 보유는 GDD 12번대로 탈출까지 마쳐야 확정 - NightManager가 처리).
    public class InvestigationPoint : MonoBehaviour
    {
        [SerializeField] private float interactRadius = 1.2f;

        private NightManager _nightManager;
        private Transform _playerTransform;

        private void Start()
        {
            _nightManager = FindObjectOfType<NightManager>();
            var player = FindObjectOfType<PlayerController>();
            if (player != null) _playerTransform = player.transform;
        }

        private void Update()
        {
            if (_playerTransform == null || _nightManager == null || _nightManager.IsFinished) return;
            if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;

            var distanceSqr = ((Vector2)_playerTransform.position - (Vector2)transform.position).sqrMagnitude;
            if (distanceSqr > interactRadius * interactRadius) return;

            _nightManager.ReportInvestigated();
        }
    }
}
