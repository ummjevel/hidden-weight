using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    // 지역 클리어 지점. 플레이어가 닿으면 다음 씬으로 넘어간다.
    [RequireComponent(typeof(Collider2D))]
    public class ZoneTrigger : MonoBehaviour
    {
        [SerializeField] bool marksFractureCleared; // 균열 지역 출구만 true
        [SerializeField] string requiredEncounterId;

        public string RequiredEncounterId => requiredEncounterId;

        public void RequireEncounter(string encounterId) => requiredEncounterId = encounterId;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;

            var gm = GameManager.Instance;
            if (!string.IsNullOrEmpty(requiredEncounterId)
                && !gm.Progress.IsEncounterCleared(requiredEncounterId))
                return;
            if (marksFractureCleared) gm.Progress.MarkFractureCleared();

            var next = gm.CurrentZoneData != null ? gm.CurrentZoneData.nextSceneName : SceneFlow.Title;

            // 백트래킹 규칙: 균열을 클리어한 뒤 잔재로 되돌아오면 엔딩으로 보낸다.
            if (gm.Progress.CurrentZone == ZoneId.Residue && gm.Progress.HasClearedFracture)
                next = SceneFlow.Ending;

            SceneFlow.LoadWithFade(next);
        }
    }
}
