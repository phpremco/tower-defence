using System;
using System.IO;
using UnityEngine;

namespace Game.Platform
{
    /// <summary>
    /// Stores each key as a JSON file under a root folder. Writes are crash-safe: the new content goes to a temp file
    /// first and then replaces the save in one step, keeping the previous good save as a <c>.bak</c>. A file that cannot
    /// be read falls back to that backup, and a key with neither loads as missing, so a damaged save never stops the
    /// game at boot.
    /// </summary>
    public sealed class FileSaveService : ISaveService
    {
        private const string FileExtension = ".json";
        private const string TempExtension = ".tmp";
        private const string BackupExtension = ".bak";
        private const string DamagedSuffix = ".damaged";

        private readonly string _rootPath;

        public FileSaveService(string rootPath)
        {
            _rootPath = rootPath;
        }

        public void Save<T>(string key, T data)
        {
            Directory.CreateDirectory(_rootPath);
            string path = PathFor(key);
            string temp = path + TempExtension;
            File.WriteAllText(temp, JsonUtility.ToJson(data));
            if (File.Exists(path))
            {
                // Replace swaps the files in one step and keeps the old one as the backup, so a crash leaves either
                // the old save or the new one on disk, never a half-written file.
                File.Replace(temp, path, BackupPathFor(key));
            }
            else
            {
                File.Move(temp, path);
            }
        }

        public bool TryLoad<T>(string key, out T data)
        {
            string path = PathFor(key);
            if (TryRead(path, out data, out string problem))
            {
                return true;
            }

            if (problem == null)
            {
                // No file at all: a first launch, not a fault (an orphaned backup is still worth trying).
                return TryLoadBackup(key, out data, quiet: true);
            }

            KeepDamagedCopy(path);
            Debug.LogWarning($"Save '{key}' could not be read ({problem}); trying the backup. A copy was kept as '{Path.GetFileName(path)}{DamagedSuffix}'.");
            return TryLoadBackup(key, out data, quiet: false);
        }

        private bool TryLoadBackup<T>(string key, out T data, bool quiet)
        {
            string backup = BackupPathFor(key);
            if (TryRead(backup, out data, out string problem))
            {
                Debug.LogWarning($"Save '{key}' was restored from its backup.");
                return true;
            }

            if (!quiet || problem != null)
            {
                Debug.LogWarning(problem == null
                    ? $"Save '{key}' has no backup; starting with a fresh one."
                    : $"The backup of save '{key}' could not be read either ({problem}); starting with a fresh one.");
            }

            return false;
        }

        /// <summary>
        /// Reads and parses one file. Returns false with a null <paramref name="problem"/> when the file does not exist,
        /// and with a description when it exists but is empty, unreadable or not valid JSON.
        /// </summary>
        private static bool TryRead<T>(string path, out T data, out string problem)
        {
            data = default;
            problem = null;
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    problem = "the file is empty";
                    return false;
                }

                data = JsonUtility.FromJson<T>(json);
                if (data == null)
                {
                    problem = "the file holds no data";
                    return false;
                }

                return true;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is IOException || exception is UnauthorizedAccessException)
            {
                // JsonUtility reports malformed or truncated JSON as an ArgumentException.
                data = default;
                problem = exception.Message;
                return false;
            }
        }

        // The damaged file is about to be overwritten by the next save; a copy beside it lets a player or a support
        // request recover what was in it.
        private static void KeepDamagedCopy(string path)
        {
            try
            {
                File.Copy(path, path + DamagedSuffix, true);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Could not keep a copy of the damaged save: {exception.Message}");
            }
        }

        private string PathFor(string key) => Path.Combine(_rootPath, key + FileExtension);

        private string BackupPathFor(string key) => PathFor(key) + BackupExtension;
    }
}
