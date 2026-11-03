using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace BalanceLab
{
    /// <summary>
    /// Just enough of Unity's text serialisation to read the ScriptableObject assets the generator writes: block
    /// mappings, block sequences, flow mappings such as <c>{x: 1, y: 2}</c> and plain scalars. Anything else Unity can
    /// write (anchors across documents, multi-line strings) is not needed for the balance numbers and is skipped.
    /// </summary>
    public sealed class YamlNode
    {
        public string Scalar;
        public Dictionary<string, YamlNode> Map;
        public List<YamlNode> List;

        /// <summary>A missing field reads as an empty scalar: Unity leaves out fields still at their default.</summary>
        public YamlNode this[string key] => Map != null && Map.TryGetValue(key, out YamlNode value) ? value : Missing;

        private static readonly YamlNode Missing = new YamlNode { Scalar = string.Empty };

        public bool Exists => !ReferenceEquals(this, Missing);

        public string Text => Scalar ?? string.Empty;

        public float Float => Text.Length == 0 ? 0f : float.Parse(Text, NumberStyles.Float, CultureInfo.InvariantCulture);

        public int Int => Text.Length == 0 ? 0 : int.Parse(Text, CultureInfo.InvariantCulture);

        public bool Bool => Text == "1";

        public string Guid => this["guid"]?.Text;

        public IReadOnlyList<YamlNode> Items => List ?? (IReadOnlyList<YamlNode>)Array.Empty<YamlNode>();
    }

    public static class UnityYaml
    {
        /// <summary>The fields of the MonoBehaviour document of an asset file (the part under "MonoBehaviour:").</summary>
        public static YamlNode ReadAsset(string path)
        {
            var lines = new List<string>();
            bool inside = false;
            foreach (string raw in File.ReadAllLines(path))
            {
                if (raw.StartsWith("--- ", StringComparison.Ordinal))
                {
                    inside = false;
                    continue;
                }

                if (raw == "MonoBehaviour:")
                {
                    inside = true;
                    continue;
                }

                if (inside && raw.Length > 0)
                {
                    lines.Add(raw);
                }
            }

            int index = 0;
            return ParseBlock(lines, ref index, 2);
        }

        private static int Indent(string line)
        {
            int i = 0;
            while (i < line.Length && line[i] == ' ')
            {
                i++;
            }

            return i;
        }

        // A block at the given indentation: a mapping of "key: value" lines, or a sequence of "- " lines. Unity writes
        // sequence items at the same indentation as their parent key.
        private static YamlNode ParseBlock(List<string> lines, ref int index, int indent)
        {
            if (index >= lines.Count)
            {
                return new YamlNode { Scalar = string.Empty };
            }

            string first = lines[index];
            if (Indent(first) == indent && first.Substring(indent).StartsWith("- ", StringComparison.Ordinal))
            {
                return ParseSequence(lines, ref index, indent);
            }

            var node = new YamlNode { Map = new Dictionary<string, YamlNode>() };
            while (index < lines.Count)
            {
                string line = lines[index];
                int current = Indent(line);
                if (current < indent || (current == indent && line.Substring(indent).StartsWith("- ", StringComparison.Ordinal)))
                {
                    break;
                }

                if (current > indent)
                {
                    index++;
                    continue;
                }

                string body = line.Substring(indent);
                int colon = body.IndexOf(':');
                if (colon < 0)
                {
                    index++;
                    continue;
                }

                string key = body.Substring(0, colon);
                string rest = body.Substring(colon + 1).Trim();
                index++;
                node.Map[key] = rest.Length > 0 ? ParseInline(rest) : ParseChild(lines, ref index, indent);
            }

            return node;
        }

        private static YamlNode ParseChild(List<string> lines, ref int index, int indent)
        {
            if (index >= lines.Count)
            {
                return new YamlNode { Scalar = string.Empty };
            }

            int next = Indent(lines[index]);
            bool sequence = lines[index].Substring(next).StartsWith("- ", StringComparison.Ordinal);
            if (sequence && next >= indent)
            {
                return ParseSequence(lines, ref index, next);
            }

            return next > indent ? ParseBlock(lines, ref index, next) : new YamlNode { Scalar = string.Empty };
        }

        private static YamlNode ParseSequence(List<string> lines, ref int index, int indent)
        {
            var node = new YamlNode { List = new List<YamlNode>() };
            while (index < lines.Count)
            {
                string line = lines[index];
                if (Indent(line) != indent || !line.Substring(indent).StartsWith("- ", StringComparison.Ordinal))
                {
                    break;
                }

                string item = line.Substring(indent + 2);
                int colon = item.IndexOf(':');
                bool flow = item.StartsWith("{", StringComparison.Ordinal);
                if (flow || colon < 0)
                {
                    node.List.Add(ParseInline(item));
                    index++;
                    continue;
                }

                // A mapping item: its first key sits on the dash line, the rest two columns further in.
                lines[index] = new string(' ', indent + 2) + item;
                node.List.Add(ParseBlock(lines, ref index, indent + 2));
            }

            return node;
        }

        private static YamlNode ParseInline(string text)
        {
            if (text == "[]")
            {
                return new YamlNode { List = new List<YamlNode>() };
            }

            if (!text.StartsWith("{", StringComparison.Ordinal))
            {
                return new YamlNode { Scalar = text };
            }

            var node = new YamlNode { Map = new Dictionary<string, YamlNode>() };
            string inner = text.Trim('{', '}');
            foreach (string pair in inner.Split(','))
            {
                int colon = pair.IndexOf(':');
                if (colon > 0)
                {
                    node.Map[pair.Substring(0, colon).Trim()] = new YamlNode { Scalar = pair.Substring(colon + 1).Trim() };
                }
            }

            return node;
        }
    }
}
