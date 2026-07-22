using UnityEngine;
using UnityEngine.UI;
using HanGame.Common;
using HanGame.Night;

namespace HanGame.UI
{
    /// <summary>
    /// 밤 HUD. 기획서 14.2/13.3.
    /// 남은시간·목표·목표위치·출구위치·무기획득여부·평판·시야는 항상 표시.
    /// </summary>
    public class NightHUD : MonoBehaviour
    {
        [Header("정보")]
        [SerializeField] private Text timeText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private GameObject weaponAcquiredMark;

        [Header("월드 마커(화면 가장자리 방향 표시)")]
        [SerializeField] private RectTransform objectiveMarker;
        [SerializeField] private RectTransform exitMarker;
        [SerializeField] private Transform objectiveWorld;
        [SerializeField] private Transform exitWorld;
        [SerializeField] private Camera worldCamera;

        [Header("평판")]
        [SerializeField] private Image[] reputationBadges;

        private NightStealthManager _manager;

        private void Start()
        {
            _manager = FindObjectOfType<NightStealthManager>();
            if (_manager != null)
            {
                _manager.TimeTicked += OnTime;
                _manager.WeaponInvestigated += OnWeaponAcquired;
            }
            if (weaponAcquiredMark != null) weaponAcquiredMark.SetActive(false);
            if (worldCamera == null) worldCamera = Camera.main;

            if (reputationBadges != null && GameManager.Instance != null)
            {
                int rep = GameManager.Instance.Run.Reputation;
                for (int i = 0; i < reputationBadges.Length; i++)
                    if (reputationBadges[i] != null) reputationBadges[i].enabled = i < rep;
            }
        }

        private void Update()
        {
            UpdateMarker(objectiveMarker, objectiveWorld);
            UpdateMarker(exitMarker, exitWorld);
        }

        private void OnTime(float remaining)
        {
            if (timeText != null) timeText.text = Mathf.CeilToInt(Mathf.Max(0f, remaining)).ToString();
        }

        private void OnWeaponAcquired()
        {
            if (weaponAcquiredMark != null) weaponAcquiredMark.SetActive(true);
        }

        // 목표·출구를 화면상 위치로 투영해 마커 이동(간단 표시).
        private void UpdateMarker(RectTransform marker, Transform world)
        {
            if (marker == null || world == null || worldCamera == null) return;
            Vector3 sp = worldCamera.WorldToScreenPoint(world.position);
            marker.position = sp;
        }
    }
}
