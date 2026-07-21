using UnityEngine;

namespace RookieToCEO.Gameplay.Night
{
    // GDD 10번 "비상계단"(출구). 플레이어가 닿기만 하면 탈출 판정 - 조사를 했는지 여부에 따라
    // NightManager/NightMissionState가 Success/SuccessWithoutWeapon을 가른다.
    [RequireComponent(typeof(Collider2D))]
    public class ExitPoint : MonoBehaviour
    {
        private NightManager _nightManager;

        private void Start()
        {
            _nightManager = FindObjectOfType<NightManager>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_nightManager == null || _nightManager.IsFinished) return;
            if (other.GetComponent<PlayerController>() == null) return;

            _nightManager.ReportReachedExit();
        }
    }
}
