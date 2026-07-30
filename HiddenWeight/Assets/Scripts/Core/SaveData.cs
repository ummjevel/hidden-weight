using System;
using System.Collections.Generic;

namespace HiddenWeight.Core
{
    [Serializable]
    public class SavedFragment
    {
        public string id;
        public string text;
    }

    // MonoBehaviour와 컬렉션 구현에 의존하지 않는 저장 DTO. 필드를 추가할 때 version을 올리고
    // SaveService.Migrate에서 이전 버전의 기본값을 채운다.
    [Serializable]
    public class SaveData
    {
        public int version = SaveService.CurrentVersion;
        public long savedAtUtcTicks;
        public int currentZone;
        public float checkpointX;
        public float checkpointY;
        public float checkpointZ;
        public int currency;
        public int healthShards;
        public bool hasAwareness;
        public bool hasClearedFracture;
        public List<int> skills = new List<int>();
        public List<SavedFragment> fragments = new List<SavedFragment>();
        public List<string> rewound = new List<string>();
        public List<string> visitedRooms = new List<string>();
        public List<string> clearedEncounters = new List<string>();
        public List<string> openedShortcuts = new List<string>();
        public List<string> takenRewards = new List<string>();
    }
}
