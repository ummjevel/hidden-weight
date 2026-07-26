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

        protected PlayerController Player { get; private set; }

        // OnEnd에서 true로 설정하면, 이번 End()에서는 쿨타임을 걸지 않는다.
        // (대상 없이 취소된 되감기 등 — 실패에 쿨타임을 물리지 않기 위함)
        protected bool SkipCooldown;

        void Awake()
        {
            Player = PlayerController.Instance;
        }

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
