using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using HiddenWeight.Data;

namespace HiddenWeight.EditorTools
{
    // Assets/ScriptableObjects/에 밸런스 데이터 에셋 12종을 생성한다.
    // 이미 있는 에셋은 값을 덮어쓰지 않고 그대로 건너뛴다 — 수동으로 조정한 밸런스를
    // 재실행 한 번으로 날려버리지 않기 위함이다 (기존 에셋을 찾으면 그 인스턴스를 그대로 반환한다).
    public static class DataAssetBuilder
    {
        const string Folder = "Assets/ScriptableObjects";
        const string SettingsFolder = "Assets/Settings";

        public static void Run()
        {
            EnsureFolder();

            var player = LoadOrCreate<PlayerData>($"{Folder}/PlayerData.asset", _ => { });
            // PlayerData의 필드 기본값 자체가 Task 2 표의 수치이므로 configure에서 할 일이 없다.

            var rewind = LoadOrCreate<EmotionData>($"{Folder}/Emotion_Rewind.asset", d =>
            {
                d.id = EmotionId.Rewind;
                d.displayName = "되감기";
                d.inputMode = SkillInput.Hold;
                d.channelTime = 1.0f;
                d.cooldown = 2f;
                d.range = 6f;
                d.moveSpeedMultiplier = 0f;
            });

            var hush = LoadOrCreate<EmotionData>($"{Folder}/Emotion_Hush.asset", d =>
            {
                d.id = EmotionId.Hush;
                d.displayName = "숨죽이기";
                d.inputMode = SkillInput.Hold;
                d.channelTime = 0f;
                d.cooldown = 0f;
                d.moveSpeedMultiplier = 0.65f;
                d.hushScale = 0.55f;
            });

            var foresight = LoadOrCreate<EmotionData>($"{Folder}/Emotion_Foresight.asset", d =>
            {
                d.id = EmotionId.Foresight;
                d.displayName = "예지";
                d.inputMode = SkillInput.Tap;
                d.cooldown = 3f;
                d.range = 8f;
                d.moveSpeedMultiplier = 1f;
                d.effectDuration = 1.5f;
                d.previewLeadTime = 2f;
            });

            var enemyResidue = LoadOrCreate<EnemyData>($"{Folder}/Enemy_Residue.asset", d =>
            {
                d.moveSpeed = 1.2f;
                ColorUtility.TryParseHtmlString("#6B5D52", out d.tint);
                d.wobbleAmplitude = 0f;
            });

            var enemyGaze = LoadOrCreate<EnemyData>($"{Folder}/Enemy_Gaze.asset", d =>
            {
                d.moveSpeed = 2.0f;
                ColorUtility.TryParseHtmlString("#7B5EA7", out d.tint);
                d.wobbleAmplitude = 0f;
            });

            var enemyFracture = LoadOrCreate<EnemyData>($"{Folder}/Enemy_Fracture.asset", d =>
            {
                d.moveSpeed = 1.6f;
                ColorUtility.TryParseHtmlString("#8FD9C4", out d.tint);
                // 균열 몬스터는 전부 EnemyPatrol을 끄고 시간 함수로 직접 움직이므로 이 값은
                // 실제로는 적용되지 않는다(Enemies/README.md 참고). 다른 지역과 같이 0으로 둔다.
                d.wobbleAmplitude = 0f;
            });

            var zonePrologue = LoadOrCreate<ZoneData>($"{Folder}/Zone_Prologue.asset", d =>
            {
                d.id = ZoneId.Prologue;
                d.displayName = "몽환의 우주";
                d.sceneName = "Zone_Prologue";
                // 잔재는 15룸 재설계 씬(Zone_Residue_Full)이 정식이다. 옛 4룸 씬은 남겨 두되
                // 정식 동선에서는 더 이상 지나가지 않는다.
                d.nextSceneName = "Zone_Residue_Full";
                d.grantedSkill = EmotionId.None;
                d.grantsAwareness = false; // 명시적으로 적어 둔다 (기본값과 같지만 의도를 드러내기 위함)
                d.awarenessStable = true;
                d.volumeProfile = LoadVolume("Prologue");
            });

            var zoneResidue = LoadOrCreate<ZoneData>($"{Folder}/Zone_Residue.asset", d =>
            {
                d.id = ZoneId.Residue;
                d.displayName = "잔재";
                d.sceneName = "Zone_Residue_Full";
                d.nextSceneName = "Zone_Gaze";
                d.grantedSkill = EmotionId.Rewind;
                d.grantsAwareness = false;
                d.awarenessStable = true;
                d.volumeProfile = LoadVolume("Residue");
            });

            var zoneGaze = LoadOrCreate<ZoneData>($"{Folder}/Zone_Gaze.asset", d =>
            {
                d.id = ZoneId.Gaze;
                d.displayName = "응시";
                d.sceneName = "Zone_Gaze";
                d.nextSceneName = "Zone_Fracture";
                d.grantedSkill = EmotionId.Hush;
                d.grantsAwareness = true;
                d.awarenessStable = true;
                d.volumeProfile = LoadVolume("Gaze");
            });

            var zoneFracture = LoadOrCreate<ZoneData>($"{Folder}/Zone_Fracture.asset", d =>
            {
                d.id = ZoneId.Fracture;
                d.displayName = "균열";
                d.sceneName = "Zone_Fracture";
                // 기획서 5.3절 백트래킹: 균열을 클리어하면 잔재로 되돌아간다.
                // 잔재를 두 번째로 밟았을 때 엔딩으로 보내는 판단은 ZoneTrigger가
                // Progress.HasClearedFracture로 한다(Task 6에서 이미 구현됨).
                // 균열 클리어 후 되돌아오는 곳도 15룸 씬이다(S3 재방문 동선).
                d.nextSceneName = "Zone_Residue_Full";
                d.grantedSkill = EmotionId.Foresight;
                d.grantsAwareness = false;
                d.awarenessStable = false; // 균열만 불안정
                d.volumeProfile = LoadVolume("Fracture");
            });

            LoadOrCreate<BalanceData>($"{Folder}/BalanceData.asset", d =>
            {
                d.player = player;
                d.emotions = new[] { rewind, hush, foresight };
                d.enemies = new[] { enemyResidue, enemyGaze, enemyFracture };
                d.zones = new[] { zonePrologue, zoneResidue, zoneGaze, zoneFracture };
                d.awarenessProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{SettingsFolder}/Volume_Awareness.asset");
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[DataAssetBuilder] 데이터 에셋 12개 생성/확인 완료");
        }

        // Task 1의 ProjectSetup이 만든 Volume_*.asset을 경로로 찾는다. GUID가 아니라 경로 기반이라
        // ProjectSetup을 재실행해도(= 새 GUID가 생겨도) 깨지지 않는다.
        static VolumeProfile LoadVolume(string zoneName)
            => AssetDatabase.LoadAssetAtPath<VolumeProfile>($"{SettingsFolder}/Volume_{zoneName}.asset");

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
        }

        // 경로에 이미 에셋이 있으면 그 값을 그대로 반환하고(덮어쓰지 않음), 없으면 새로 만들어
        // configure로 값을 채운 뒤 저장한다.
        static T LoadOrCreate<T>(string path, System.Action<T> configure) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // 이미 만들어져 있던 Enemy_Residue_Walker.asset에는 turnHesitationSeconds 필드가 없던
        // 시절에 저장된 값이 남아있어, 새로 추가한 필드가 기본값(0)으로 직렬화돼 있다.
        // ZoneSceneBuilder.ResidueEnemyData()의 Walker 케이스를 재실행하면 기존 에셋을 그대로
        // 반환하고 건드리지 않으므로(LoadData가 있으면 스킵), 여기서 한 번만 값을 채워 넣는다.
        [MenuItem("Hidden Weight/Fix/Apply Residue Walker Hesitation")]
        public static void ApplyResidueWalkerHesitation()
        {
            string path = $"{Folder}/Enemy_Residue_Walker.asset";
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (data == null)
            {
                Debug.LogError($"[DataAssetBuilder] {path} 를 찾을 수 없다.");
                return;
            }

            data.turnHesitationSeconds = 0.35f;
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("[DataAssetBuilder] Enemy_Residue_Walker.turnHesitationSeconds = 0.35 적용 완료");
        }
    }
}
