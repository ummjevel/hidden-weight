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
        readonly Dictionary<string, string> _fragmentTexts = new Dictionary<string, string>();
        readonly HashSet<string> _visitedRooms = new HashSet<string>();

        // 아래 셋은 CONTENT_SYSTEM.md 6절 "초기화 규칙"이 영구 유지라고 정한 것들이다.
        // 정예·중간 보스·지역 보스 조우, 물리적으로 열린 숏컷, 한 번만 주는 고정 보상.
        readonly HashSet<string> _clearedEncounters = new HashSet<string>();
        readonly HashSet<string> _openedShortcuts = new HashSet<string>();
        readonly HashSet<string> _takenRewards = new HashSet<string>();

        // 일반 재화. CONTENT_SYSTEM.md 5절: 사망해도 유지되고 지역 재진입으로 초기화되지 않는다.
        // 소비처(상점·지도·업그레이드)는 아직 없고, 지금은 소형 획득물이 이동 유도선 역할을 하는 데 쓴다.
        public int Currency { get; private set; }

        public bool HasAwareness { get; private set; }
        public bool HasClearedFracture { get; private set; }
        public ZoneId CurrentZone { get; set; } = ZoneId.Prologue;
        public Vector3 LastCheckpoint { get; set; }
        public int FragmentCount => _fragments.Count;

        public IReadOnlyCollection<string> Fragments => _fragments;
        public IReadOnlyDictionary<string, string> FragmentTexts => _fragmentTexts;
        public IReadOnlyCollection<string> VisitedRooms => _visitedRooms;
        public int OpenedShortcutCount => _openedShortcuts.Count;

        public event System.Action<int, int> CurrencyChanged; // 획득량, 현재 총량
        public event System.Action<int> HealthShardsChanged;
        public event System.Action<string, string> FragmentCollected; // id, 표시 문장
        public event System.Action<string> RoomVisited;
        public event System.Action<string> ShortcutOpened;

        public void UnlockSkill(EmotionId id)
        {
            if (id != EmotionId.None) _skills.Add(id);
        }

        public bool HasSkill(EmotionId id) => _skills.Contains(id);

        public void AddCurrency(int amount)
        {
            if (amount <= 0) return;
            Currency += amount;
            CurrencyChanged?.Invoke(amount, Currency);
        }

        // 영구 성장 조각. 먹을 때마다 최대 체력이 1 늘어난다(CONTENT_SYSTEM.md 5절).
        public int HealthShards { get; private set; }

        public void AddHealthShard()
        {
            HealthShards++;
            HealthShardsChanged?.Invoke(HealthShards);
        }

        // 일회성 조우(정예·보스). 한 번 클리어하면 방을 다시 와도 다시 싸우지 않는다.
        public void MarkEncounterCleared(string id)
        {
            if (!string.IsNullOrEmpty(id)) _clearedEncounters.Add(id);
        }

        public bool IsEncounterCleared(string id)
            => !string.IsNullOrEmpty(id) && _clearedEncounters.Contains(id);

        // 물리적으로 열린 숏컷. 지역을 다시 들어와도 열린 채로 시작한다.
        public void MarkShortcutOpen(string id)
        {
            if (!string.IsNullOrEmpty(id) && _openedShortcuts.Add(id)) ShortcutOpened?.Invoke(id);
        }

        public bool IsShortcutOpen(string id)
            => !string.IsNullOrEmpty(id) && _openedShortcuts.Contains(id);

        // 고정 보상. 같은 보상을 두 번 주지 않는다(되감기로 복제하는 것도 여기서 막힌다).
        public bool TakeReward(string id)
            => !string.IsNullOrEmpty(id) && _takenRewards.Add(id);

        public bool IsRewardTaken(string id)
            => !string.IsNullOrEmpty(id) && _takenRewards.Contains(id);

        public void GrantAwareness() => HasAwareness = true;

        public void MarkFractureCleared() => HasClearedFracture = true;

        public bool CollectFragment(string id, string text = null)
        {
            if (string.IsNullOrEmpty(id) || !_fragments.Add(id)) return false;
            _fragmentTexts[id] = text ?? string.Empty;
            FragmentCollected?.Invoke(id, text ?? string.Empty);
            return true;
        }

        public bool HasFragment(string id) => _fragments.Contains(id);

        public void VisitRoom(string id)
        {
            if (!string.IsNullOrEmpty(id) && _visitedRooms.Add(id)) RoomVisited?.Invoke(id);
        }

        public bool HasVisitedRoom(string id)
            => !string.IsNullOrEmpty(id) && _visitedRooms.Contains(id);

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
            _fragmentTexts.Clear();
            _visitedRooms.Clear();
            _clearedEncounters.Clear();
            _openedShortcuts.Clear();
            _takenRewards.Clear();
            Currency = 0;
            HealthShards = 0;
            HasAwareness = false;
            HasClearedFracture = false;
            CurrentZone = ZoneId.Prologue;
            LastCheckpoint = Vector3.zero;
        }
    }
}
