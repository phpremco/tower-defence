using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace BalanceLab
{
    /// <summary>Finds the generated data assets by GUID (from their .meta files) and by class, under Assets/_Project/Data.</summary>
    public sealed class AssetIndex
    {
        private static readonly Regex GuidLine = new Regex(@"^guid:\s*([0-9a-f]{32})", RegexOptions.Multiline);
        private static readonly Regex ClassLine = new Regex(@"m_EditorClassIdentifier:\s*\S+::(\S+)", RegexOptions.Multiline);

        private readonly Dictionary<string, string> _pathByGuid = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _guidByPath = new Dictionary<string, string>();
        private readonly Dictionary<string, List<string>> _pathsByClass = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, YamlNode> _cache = new Dictionary<string, YamlNode>();

        public AssetIndex(string dataRoot)
        {
            foreach (string asset in Directory.EnumerateFiles(dataRoot, "*.asset", SearchOption.AllDirectories))
            {
                string meta = asset + ".meta";
                if (!File.Exists(meta))
                {
                    continue;
                }

                Match guid = GuidLine.Match(File.ReadAllText(meta));
                if (!guid.Success)
                {
                    continue;
                }

                string full = Path.GetFullPath(asset);
                _pathByGuid[guid.Groups[1].Value] = full;
                _guidByPath[full] = guid.Groups[1].Value;
                Match cls = ClassLine.Match(ReadHead(asset));
                if (cls.Success)
                {
                    string name = cls.Groups[1].Value;
                    if (!_pathsByClass.TryGetValue(name, out List<string> list))
                    {
                        list = new List<string>();
                        _pathsByClass[name] = list;
                    }

                    list.Add(full);
                }
            }
        }

        public string PathOf(string guid) => guid != null && _pathByGuid.TryGetValue(guid, out string path) ? path : null;

        public string GuidOf(string path) => _guidByPath.TryGetValue(Path.GetFullPath(path), out string guid) ? guid : null;

        public IReadOnlyList<string> OfClass(string fullName) =>
            _pathsByClass.TryGetValue(fullName, out List<string> list) ? list : (IReadOnlyList<string>)Array.Empty<string>();

        public YamlNode Read(string path)
        {
            if (!_cache.TryGetValue(path, out YamlNode node))
            {
                node = UnityYaml.ReadAsset(path);
                _cache[path] = node;
            }

            return node;
        }

        public YamlNode ReadByGuid(string guid)
        {
            string path = PathOf(guid);
            return path == null ? null : Read(path);
        }

        public static string NameOf(string path) => Path.GetFileNameWithoutExtension(path);

        private static string ReadHead(string path)
        {
            using (var reader = new StreamReader(path))
            {
                var buffer = new char[2048];
                int read = reader.Read(buffer, 0, buffer.Length);
                return new string(buffer, 0, read);
            }
        }
    }
}
