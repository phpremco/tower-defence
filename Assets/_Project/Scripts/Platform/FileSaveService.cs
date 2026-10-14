using System.IO;
using UnityEngine;

namespace Game.Platform
{
    /// <summary>Stores each key as a JSON file under a root folder.</summary>
    public sealed class FileSaveService : ISaveService
    {
        private const string FileExtension = ".json";

        private readonly string _rootPath;

        public FileSaveService(string rootPath)
        {
            _rootPath = rootPath;
        }

        public void Save<T>(string key, T data)
        {
            Directory.CreateDirectory(_rootPath);
            File.WriteAllText(PathFor(key), JsonUtility.ToJson(data));
        }

        public bool TryLoad<T>(string key, out T data)
        {
            string path = PathFor(key);
            if (!File.Exists(path))
            {
                data = default;
                return false;
            }

            data = JsonUtility.FromJson<T>(File.ReadAllText(path));
            return true;
        }

        private string PathFor(string key) => Path.Combine(_rootPath, key + FileExtension);
    }
}
