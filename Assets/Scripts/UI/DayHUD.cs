using UnityEngine;
using UnityEngine.UI;
using HanGame.Common;
using HanGame.Day;
using HanGame.Weapons;

namespace HanGame.UI
{
    /// <summary>
    /// 낮 HUD. 기획서 14.1.
    /// HP·평판·층·남은시간·경험치·쿨타임·궁극기 게이지·상사의 시선 경고.
    /// </summary>
    public class DayHUD : MonoBehaviour
    {
        [Header("HP / 평판")]
        [SerializeField] private Slider hpBar;
        [SerializeField] private Image[] reputationBadges; // 사원증 3칸(기획서 14.3)

        [Header("진행")]
        [SerializeField] private Text floorText;
        [SerializeField] private Text timeText;
        [SerializeField] private Slider expBar;
        [SerializeField] private Text levelText;

        [Header("스킬")]
        [SerializeField] private Image delegateCooldownFill; // 업무 떠넘기기(0=준비)
        [SerializeField] private Image ultimateGaugeFill;    // 퇴사 통보 게이지

        [Header("경고")]
        [SerializeField] private GameObject bossGazeWarning;

        private PlayerHealth _health;
        private ExperienceSystem _exp;
        private WaveTimer _timer;
        private TaskDelegateSkill _delegate;
        private ResignationUltimate _ultimate;
        private BossGaze _bossGaze;

        private void Start()
        {
            var player = Player.Local;
            _health = player != null ? player.Health : FindObjectOfType<PlayerHealth>();
            _exp = FindObjectOfType<ExperienceSystem>();
            _timer = FindObjectOfType<WaveTimer>();
            _delegate = FindObjectOfType<TaskDelegateSkill>();
            _ultimate = FindObjectOfType<ResignationUltimate>();
            _bossGaze = FindObjectOfType<BossGaze>();

            if (_health != null)
            {
                _health.HpChanged += OnHp;
                _health.ReputationChanged += OnReputation;
                OnReputation(GameManager.Instance != null ? GameManager.Instance.Run.Reputation : 3);
            }
            if (_exp != null) _exp.ExpChanged += OnExp;
            if (_bossGaze != null) _bossGaze.WarningRaised += OnBossWarning;

            if (bossGazeWarning != null) bossGazeWarning.SetActive(false);
            if (floorText != null && GameManager.Instance != null)
                floorText.text = $"{GameManager.Instance.Run.Floor}층";
        }

        private void Update()
        {
            if (_timer != null && timeText != null)
                timeText.text = Mathf.CeilToInt(_timer.Remaining).ToString();

            if (_delegate != null && delegateCooldownFill != null)
                delegateCooldownFill.fillAmount = _delegate.Cooldown > 0f
                    ? Mathf.Clamp01(_delegate.CooldownRemaining / _delegate.Cooldown) : 0f;

            if (_ultimate != null && ultimateGaugeFill != null)
                ultimateGaugeFill.fillAmount = _ultimate.Gauge;

            if (_exp != null && levelText != null)
                levelText.text = $"Lv.{_exp.Level}";
        }

        private void OnHp(float cur, float max)
        {
            if (hpBar != null) hpBar.value = max > 0f ? cur / max : 0f;
        }

        private void OnReputation(int rep)
        {
            if (reputationBadges == null) return;
            for (int i = 0; i < reputationBadges.Length; i++)
                if (reputationBadges[i] != null)
                    reputationBadges[i].enabled = i < rep; // 남은 평판만큼 사원증 표시
        }

        private void OnExp(int cur, int toNext)
        {
            if (expBar != null) expBar.value = toNext > 0f ? (float)cur / toNext : 0f;
        }

        private void OnBossWarning() => StartCoroutine(FlashWarning());

        private System.Collections.IEnumerator FlashWarning()
        {
            if (bossGazeWarning == null) yield break;
            bossGazeWarning.SetActive(true);
            yield return new WaitForSeconds(2f);
            bossGazeWarning.SetActive(false);
        }
    }
}
