using System;
using System.IO;
using UnityEngine;

namespace HiddenWeight.Core
{
    public static class SaveService
    {
        public const int CurrentVersion = 1;
        const string FileName = "hidden-weight-save.json";
        const string TestFileName = "hidden-weight-save.tests.json";

        static ProgressState _boundProgress;
        public static string PathOverride { get; set; }
        public static string StoragePath => !string.IsNullOrEmpty(PathOverride)
            ? PathOverride
            : Path.Combine(Application.persistentDataPath, Application.isBatchMode ? TestFileName : FileName);
        static string BackupPath => StoragePath + ".bak";
        static string TempPath => StoragePath + ".tmp";

        public static bool HasSave => File.Exists(StoragePath) || File.Exists(BackupPath);

        public static void Bind(ProgressState progress)
        {
            Unbind();
            _boundProgress = progress;
            if (_boundProgress != null) _boundProgress.Changed += HandleProgressChanged;
        }

        public static void Unbind()
        {
            if (_boundProgress != null) _boundProgress.Changed -= HandleProgressChanged;
            _boundProgress = null;
        }

        static void HandleProgressChanged()
        {
            // 자동화 실행은 별도 테스트가 명시적으로 Save를 호출한다. 실제 개발 플레이와
            // 사용자 저장 파일을 테스트 진행 상태로 덮지 않는다.
            if (!Application.isBatchMode && _boundProgress != null) Save(_boundProgress);
        }

        public static bool Save(ProgressState progress)
        {
            if (progress == null) return false;
            try
            {
                string directory = Path.GetDirectoryName(StoragePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                string json = JsonUtility.ToJson(progress.CreateSaveData(), true);
                File.WriteAllText(TempPath, json);
                if (File.Exists(StoragePath))
                {
                    if (File.Exists(BackupPath)) File.Delete(BackupPath);
                    File.Replace(TempPath, StoragePath, BackupPath);
                }
                else File.Move(TempPath, StoragePath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SaveService] 저장하지 못했습니다: " + exception.Message);
                TryDelete(TempPath);
                return false;
            }
        }

        public static bool TryLoad(ProgressState progress)
        {
            if (progress == null) return false;
            if (TryRead(StoragePath, out var data) || TryRead(BackupPath, out data))
            {
                progress.Restore(data);
                return true;
            }
            return false;
        }

        static bool TryRead(string path, out SaveData data)
        {
            data = null;
            if (!File.Exists(path)) return false;
            try
            {
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null || data.version < 1 || data.version > CurrentVersion) return false;
                Migrate(data);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[SaveService] 저장 파일을 읽지 못했습니다: " + exception.Message);
                return false;
            }
        }

        static void Migrate(SaveData data)
        {
            // v1은 모든 컬렉션이 선택적이다. 손상되거나 초기 버전에서 빠진 배열은 빈 값으로 복구한다.
            data.skills ??= new System.Collections.Generic.List<int>();
            data.fragments ??= new System.Collections.Generic.List<SavedFragment>();
            data.rewound ??= new System.Collections.Generic.List<string>();
            data.visitedRooms ??= new System.Collections.Generic.List<string>();
            data.clearedEncounters ??= new System.Collections.Generic.List<string>();
            data.openedShortcuts ??= new System.Collections.Generic.List<string>();
            data.takenRewards ??= new System.Collections.Generic.List<string>();
            data.version = CurrentVersion;
        }

        public static void Delete()
        {
            TryDelete(StoragePath);
            TryDelete(BackupPath);
            TryDelete(TempPath);
        }

        static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception exception) { Debug.LogWarning("[SaveService] 파일을 지우지 못했습니다: " + exception.Message); }
        }
    }
}
