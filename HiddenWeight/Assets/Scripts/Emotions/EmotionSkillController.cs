using System.Collections.Generic;
using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Player;

namespace HiddenWeight.Emotions
{
    // 플레이어에 붙는 싱글턴. 세 스킬 컴포넌트를 전부 들고 있다가, 현재 지역의 grantedSkill 중
    // 보유한 것 하나를 활성 스킬로 지정한다. 입력 라우팅(Hold/Tap)도 여기서 담당한다.
    public class EmotionSkillController : MonoBehaviour
    {
        public static EmotionSkillController Instance { get; private set; }

        List<EmotionSkill> _skills;
        EmotionSkill _active;

        public EmotionSkill Active => _active;
        public EmotionId CurrentEmotion { get; private set; } = EmotionId.None;

        public event System.Action<EmotionId> EmotionChanged;

        void Awake()
        {
            Instance = this;
            _skills = new List<EmotionSkill>(GetComponents<EmotionSkill>());

            var balance = GameManager.Instance.Balance;
            foreach (var s in _skills) s.Data = balance.GetEmotion(s.Id);
        }

        void Update()
        {
            if (Active == null) { RefreshActive(); return; }

            if (Active.Data.inputMode == SkillInput.Hold)
            {
                if (PlayerInput.SkillHeld && !Active.IsActive) Active.Begin();
                else if (!PlayerInput.SkillHeld && Active.IsActive) Active.End();
            }
            else // Tap
            {
                if (PlayerInput.SkillPressed && !Active.IsActive) Active.Begin();
            }

            if (Active.IsActive) Active.Tick(Time.deltaTime);
        }

        public void RefreshActive()
        {
            var gm = GameManager.Instance;
            var zone = gm.CurrentZoneData;
            var wanted = zone != null ? zone.grantedSkill : EmotionId.None;

            // 지역이 주는 스킬을 아직 못 얻었으면 활성 스킬이 없다.
            if (wanted == EmotionId.None || !gm.Progress.HasSkill(wanted))
            {
                SetActive(null);
                return;
            }
            SetActive(_skills.Find(s => s.Id == wanted));
        }

        void SetActive(EmotionSkill skill)
        {
            if (_active == skill) return;

            // 스킬이 바뀔 때 이전 활성 스킬이 채널링/유지 중이면 정리하고 넘어간다.
            if (_active != null && _active.IsActive) _active.End();

            _active = skill;
            CurrentEmotion = skill != null ? skill.Id : EmotionId.None;
            EmotionChanged?.Invoke(CurrentEmotion);
        }
    }
}
