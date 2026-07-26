using System.Collections.Generic;
using UnityEngine;
using HiddenWeight.Data;

namespace HiddenWeight.Core
{
    // 지역을 넘나들며 유지되는 진행도. MonoBehaviour가 아니라 GameManager가 들고 다니는 순수 C# 객체다.
    public class ProgressState
    {
        readonly HashSet<EmotionId> _skills = new HashSet<EmotionId>();
        readonly HashSet<string> _fragments = new HashSet<string>();
        readonly HashSet<string> _rewound = new HashSet<string>();

        public bool HasAwareness { get; private set; }
        public bool HasClearedFracture { get; private set; }
        public ZoneId CurrentZone { get; set; } = ZoneId.Prologue;
        public Vector3 LastCheckpoint { get; set; }
        public int FragmentCount => _fragments.Count;

        public void UnlockSkill(EmotionId id)
        {
            if (id != EmotionId.None) _skills.Add(id);
        }

        public bool HasSkill(EmotionId id) => _skills.Contains(id);

        public void GrantAwareness() => HasAwareness = true;

        public void MarkFractureCleared() => HasClearedFracture = true;

        public bool CollectFragment(string id) => _fragments.Add(id);

        public bool HasFragment(string id) => _fragments.Contains(id);

        // 기획서 EMOTION_SYSTEM 1.2절: 되감기는 영구 — 한 번 되돌리면 재방문(씬 재로드) 시에도
        // 유지된다. World의 Rewindable이 자기 persistentId로 기록하고 로드 시 복원한다.
        public void MarkRewound(string id)
        {
            if (!string.IsNullOrEmpty(id)) _rewound.Add(id);
        }

        public bool IsRewound(string id) => !string.IsNullOrEmpty(id) && _rewound.Contains(id);

        // 게이트가 요구하는 스킬이 None이면 조건 없이 열린다.
        public bool CanOpenGate(EmotionId required)
            => required == EmotionId.None || _skills.Contains(required);

        // 기획서 5.3절: 균열 클리어 후 자각을 갖춘 채 잔재로 백트래킹해야 열리는 최종 파편.
        public bool CanOpenFinalGate()
            => _skills.Contains(EmotionId.Rewind) && HasAwareness && HasClearedFracture;

        public void ResetAll()
        {
            _skills.Clear();
            _fragments.Clear();
            _rewound.Clear();
            HasAwareness = false;
            HasClearedFracture = false;
            CurrentZone = ZoneId.Prologue;
            LastCheckpoint = Vector3.zero;
        }
    }
}
