using UnityEngine;

namespace HiddenWeight.World
{
    // 여러 갈래 중 플레이어가 향한 하나만 실제가 되는 갈림길. 균열 지역 보스 마지막 단계
    // "내일로 걷기"가 쓴다(FRACTURE_LEVEL_DESIGN.md 4.12·7.2절: 하늘 균열이 세 갈래로
    // 보이다가 플레이어가 이동을 선택한 방향 하나만 발판으로 고정된다).
    //
    // "선택"은 버튼이 아니라 이동이다. 각 갈래 입구에 감지 반경을 두고, 플레이어가 처음
    // 들어선 갈래를 확정한다. 확정 전에는 어느 갈래도 밟히지 않으므로 고스트를 밟고
    // 건너가는 편법이 생기지 않는다.
    public class PathChoice : MonoBehaviour
    {
        [System.Serializable]
        public class Branch
        {
            public Transform entry;        // 이 지점에 다가서면 선택된 것으로 본다
            public GameObject solid;       // 확정되면 켜지는 실제 발판 묶음
            public SpriteRenderer preview; // 확정 전 반투명 미리보기
        }

        [SerializeField] Branch[] branches;
        [SerializeField] float entryRadius = 2.5f;
        [SerializeField] float previewAlpha = 0.3f;

        int _chosen = -1;

        public int ChosenIndex => _chosen;
        public bool HasChosen => _chosen >= 0;

        void Start() => Apply();

        void Update()
        {
            if (_chosen >= 0 || branches == null) return;

            var player = HiddenWeight.Player.PlayerController.Instance;
            if (player == null) return;

            for (int i = 0; i < branches.Length; i++)
            {
                var entry = branches[i].entry;
                if (entry == null) continue;
                if (Vector2.Distance(player.transform.position, entry.position) > entryRadius) continue;

                _chosen = i;
                Apply();
                return;
            }
        }

        void Apply()
        {
            if (branches == null) return;

            for (int i = 0; i < branches.Length; i++)
            {
                bool chosen = i == _chosen;
                var branch = branches[i];

                if (branch.solid != null) branch.solid.SetActive(chosen);

                if (branch.preview != null)
                {
                    // 선택되지 않은 갈래는 사라지지 않고 흔적으로 남는다 — 9절 환경 서사의
                    // "선택되지 않은 미래도 흔적으로 남아 있음"을 그대로 둔다.
                    var color = branch.preview.color;
                    color.a = _chosen < 0 ? previewAlpha : (chosen ? 0f : previewAlpha * 0.5f);
                    branch.preview.color = color;
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            if (branches == null) return;
            Gizmos.color = Color.cyan;
            foreach (var branch in branches)
                if (branch.entry != null) Gizmos.DrawWireSphere(branch.entry.position, entryRadius);
        }
    }
}
