using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.Player;

namespace HiddenWeight.Emotions
{
    // 감정 스킬(되감기/숨죽이기/예지) 공통 베이스. 쿨타임·이동속도 배율 적용을
    // 여기서 한 번만 처리하고, 각 감정은 OnBegin/OnTick/OnEnd만 구현한다.
    public abstract class EmotionSkill : MonoBehaviour
    {
        public abstract EmotionId Id { get; }
        public EmotionData Data { get; set; }
        public bool IsActive { get; protected set; }
        public float CooldownRemaining { get; protected set; }
        public virtual bool CanUse => CooldownRemaining <= 0f && !IsActive;

        // 스킬은 항상 플레이어와 같은 GameObject에 붙는다. 그러니 static Instance를 거치지 말고
        // 자기 자신에서 찾는다 — Awake에서 PlayerController.Instance를 캡처하면 같은
        // GameObject 안의 컴포넌트 Awake 순서(보장되지 않는다)에 따라 null이 잡히고, 그 뒤로
        // 영원히 null로 남아 Begin()이 ApplySpeedMultiplier에서 NullReference로 죽는다.
        // 즉 감정 스킬 3개(되감기·숨죽이기·예지) 전부가 조용히 먹통이 됐던 원인.
        // 검증: Assets/Tests/PlayMode/EmotionSkillTests.cs
        PlayerController _player;
        protected PlayerController Player
            => _player != null ? _player : (_player = GetComponent<PlayerController>());

        // OnEnd에서 true로 설정하면, 이번 End()에서는 쿨타임을 걸지 않는다.
        // (대상 없이 취소된 되감기 등 — 실패에 쿨타임을 물리지 않기 위함)
        protected bool SkipCooldown;

        void Update()
        {
            if (CooldownRemaining > 0f) CooldownRemaining -= Time.deltaTime;
        }

        public void Begin()
        {
            if (!CanUse) return;

            IsActive = true;
            // OnBegin보다 먼저 적용한다. RewindSkill처럼 OnBegin 내부에서 즉시 End()를
            // 호출하는 경우(대상 없음)에도 End()의 복구 로직이 그대로 되감아 주므로 안전하다.
            ApplySpeedMultiplier();
            HiddenWeight.Core.AudioManager.Instance?.PlaySfx(HiddenWeight.Core.SfxCue.Ability, 0.65f);
            OnBegin();
        }

        public void Tick(float dt)
        {
            OnTick(dt);
        }

        public void End()
        {
            if (!IsActive) return;

            SkipCooldown = false;
            OnEnd();
            RestoreSpeedMultiplier();

            IsActive = false;
            if (!SkipCooldown) CooldownRemaining = Data.cooldown;
            SkipCooldown = false;
        }

        void ApplySpeedMultiplier()
        {
            Player.ExternalSpeedMultiplier = Data.moveSpeedMultiplier;
            if (Data.moveSpeedMultiplier == 0f) Player.MovementLocked = true;
        }

        void RestoreSpeedMultiplier()
        {
            Player.ExternalSpeedMultiplier = 1f;
            if (Data.moveSpeedMultiplier == 0f) Player.MovementLocked = false;
        }

        protected abstract void OnBegin();
        protected abstract void OnTick(float dt);
        protected abstract void OnEnd();
    }
}
