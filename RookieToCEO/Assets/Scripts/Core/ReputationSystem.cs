using System;
using UnityEngine;

namespace RookieToCEO.Core
{
    // GDD 7번(HP와 평판)의 상태머신.
    // MonoBehaviour의 Update에 의존하지 않고 Tick(deltaTime)을 직접 호출하는 방식으로 만들어서,
    // 씬이나 Play Mode 없이도 EditMode 테스트에서 "HP 0 -> 평판 -1 -> 부활 -> 무적" 흐름 전체를
    // 시간을 임의로 흘려보내며 검증할 수 있다.
    public class ReputationSystem
    {
        public const int StartingReputation = 3;      // GDD: 기본 평판 3
        public const float ReviveDelaySeconds = 2f;    // GDD: 2초 후 HP 전체 회복
        public const float InvulnerabilitySeconds = 3f; // GDD: 부활 후 3초간 무적

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public int Reputation { get; private set; }
        public bool IsInvulnerable { get; private set; }
        public bool IsReviving { get; private set; }
        public bool IsGameOver { get; private set; }

        private float _reviveTimer;
        private float _invulnerabilityTimer;

        public event Action OnDowned;   // HP 0 -> 평판 감소 시점
        public event Action OnRevived;  // 부활(HP 전체 회복 + 무적 시작) 시점
        public event Action OnGameOver; // 평판 0 -> 해고(회귀) 시점

        public ReputationSystem(int maxHp)
        {
            MaxHp = maxHp;
            CurrentHp = maxHp;
            Reputation = StartingReputation;
        }

        // 멘탈 관리 스탯 레벨업 등으로 최대 HP가 바뀔 때 호출한다.
        public void SetMaxHp(int maxHp, bool healToFull)
        {
            MaxHp = maxHp;
            CurrentHp = healToFull ? maxHp : Mathf.Min(CurrentHp, maxHp);
        }

        public void TakeDamage(int amount)
        {
            if (IsInvulnerable || IsReviving || IsGameOver || amount <= 0) return;

            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            if (CurrentHp == 0)
            {
                Down();
            }
        }

        public void Heal(int amount)
        {
            if (IsGameOver || amount <= 0) return;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        }

        // GDD 11번(밤 발각과 실패): 낮 전투의 HP0->부활 흐름을 거치지 않고 평판만 즉시 1 깎는다.
        // 밤 탐방 발각/시간초과 페널티에 사용한다.
        public void LoseReputationDirectly()
        {
            if (IsGameOver) return;

            Reputation--;
            OnDowned?.Invoke();

            if (Reputation <= 0)
            {
                IsGameOver = true;
                OnGameOver?.Invoke();
            }
        }

        private void Down()
        {
            Reputation--;
            OnDowned?.Invoke();

            if (Reputation <= 0)
            {
                IsGameOver = true;
                OnGameOver?.Invoke();
                return;
            }

            IsReviving = true;
            _reviveTimer = ReviveDelaySeconds;
        }

        // 매 프레임(Update) 또는 테스트 코드에서 원하는 delta로 호출해 타이머를 흘려보낸다.
        public void Tick(float deltaTime)
        {
            if (IsGameOver) return;

            if (IsReviving)
            {
                _reviveTimer -= deltaTime;
                if (_reviveTimer <= 0f)
                {
                    IsReviving = false;
                    CurrentHp = MaxHp;
                    IsInvulnerable = true;
                    _invulnerabilityTimer = InvulnerabilitySeconds;
                    OnRevived?.Invoke();
                }
                return;
            }

            if (IsInvulnerable)
            {
                _invulnerabilityTimer -= deltaTime;
                if (_invulnerabilityTimer <= 0f)
                {
                    IsInvulnerable = false;
                }
            }
        }

        // 평판 0으로 해고된 뒤 1층으로 회귀할 때 호출 (GDD 7번: 스탯/무기와 함께 완전히 초기화).
        public void ResetForNewRun()
        {
            Reputation = StartingReputation;
            CurrentHp = MaxHp;
            IsInvulnerable = false;
            IsReviving = false;
            IsGameOver = false;
            _reviveTimer = 0f;
            _invulnerabilityTimer = 0f;
        }
    }
}
