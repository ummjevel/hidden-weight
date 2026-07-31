using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;

namespace HiddenWeight.World
{
    // 소형 획득물(일반 재화). CONTENT_SYSTEM.md 2절의 "소형 획득물 지점" 슬롯을 채운다.
    // 점프 경로를 따라 늘어놓아 플레이어가 갈 곳을 눈으로 따라가게 만드는 유도선 역할이 크다.
    //
    // 기억 파편(StoryFragment)과 달리 서사가 붙지 않고 ProgressState의 숫자만 올린다.
    // 재화 자체는 사망해도 유지되지만(ProgressState.Currency), 놓여 있는 획득물은 씬을 다시
    // 로드하면 되살아난다 — 일반 등급 배치물의 기본 규칙(CONTENT_SYSTEM.md 3.2절)과 같다.
    [RequireComponent(typeof(Collider2D))]
    public class CurrencyPickup : MonoBehaviour
    {
        [SerializeField] int amount = 1;

        public int Amount => amount;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;

            GameManager.Instance.Progress.AddCurrency(amount);
            AudioManager.Instance?.PlaySfx(SfxCue.ItemPickup, 0.42f);
            gameObject.SetActive(false);
        }
    }
}
