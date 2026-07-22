using System.Collections.Generic;

namespace HanGame.Common
{
    /// <summary>
    /// 한 번의 플레이 진행(런) 동안 유지되는 상태.
    /// 기획서 3.3: 회귀하면 전부 초기화된다(영구 성장 없음).
    /// </summary>
    public class RunState
    {
        public const int TotalFloors = 4;

        /// <summary>현재 층(1~4).</summary>
        public int Floor = 1;

        /// <summary>현재 단계(낮/밤).</summary>
        public FloorPhase Phase = FloorPhase.Day;

        /// <summary>남은 평판. 기본 3(권장).</summary>
        public int Reputation = 3;

        // 낮에 획득한 스탯 강화 누적 횟수. key: 강화 id.
        public readonly Dictionary<string, int> UpgradeStacks = new();

        // 보유 무기 id 목록. 시작 시 키보드 샷건만 보유.
        public readonly List<string> Weapons = new() { WeaponIds.KeyboardShotgun };

        // 결과 화면용 누적 통계.
        public int TasksProcessed;   // 처리한 업무 수
        public int PlayerLevel = 1;
        public int NightClears;      // 야간 탐방 성공 횟수
        public float PlayTime;       // 총 플레이 시간(초)

        /// <summary>해고·발각 시 첫 출근 날로 회귀. 기획서 3.2/3.3/11.9.</summary>
        public void ResetToFirstDay()
        {
            Floor = 1;
            Phase = FloorPhase.Day;
            Reputation = DefaultReputation;
            UpgradeStacks.Clear();
            Weapons.Clear();
            Weapons.Add(WeaponIds.KeyboardShotgun);
            PlayerLevel = 1;
            // 누적 통계(TasksProcessed 등)는 결과 화면 목적이므로 여기서 리셋하지 않는다.
        }

        public int DefaultReputation = 3;

        public bool HasWeapon(string id) => Weapons.Contains(id);

        public int UpgradeLevel(string id) => UpgradeStacks.TryGetValue(id, out var v) ? v : 0;

        public void AddUpgrade(string id)
        {
            UpgradeStacks[id] = UpgradeLevel(id) + 1;
        }

        public bool IsFinalFloor => Floor >= TotalFloors;
    }

    /// <summary>무기·스킬 식별자.</summary>
    public static class WeaponIds
    {
        public const string KeyboardShotgun = "keyboard_shotgun"; // 시작 무기
        public const string StaplerRapid = "stapler_rapid";       // 1층 밤 보상
        public const string TaskDelegate = "task_delegate";       // 2층 밤 보상(액티브)
        public const string ResignationNotice = "resignation";    // 3층 밤 보상(궁극기)
    }
}
