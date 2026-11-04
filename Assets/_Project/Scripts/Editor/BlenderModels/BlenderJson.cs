using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// A small JSON reader for the Blender layout files, whose nested number arrays (outlines) and fields that are either
    /// a name or a colour JsonUtility cannot map. Objects become <see cref="Dictionary{TKey,TValue}"/>, arrays
    /// <see cref="List{T}"/>, numbers double, plus string, bool and null. Editor only.
    /// </summary>
    public sealed class BlenderJson
    {
        private readonly string _text;
        private int _at;

        private BlenderJson(string text)
        {
            _text = text;
        }

        public static object Parse(string text)
        {
            var reader = new BlenderJson(text);
            object value = reader.Value();
            reader.Skip();
            if (reader._at != text.Length)
            {
                throw new System.FormatException($"Unexpected text at {reader._at}.");
            }

            return value;
        }

        public static Dictionary<string, object> Object(object value) => value as Dictionary<string, object>;

        public static List<object> Array(object value) => value as List<object> ?? new List<object>();

        public static float Number(object value, float fallback = 0f) => value is double number ? (float)number : fallback;

        public static string Text(object value) => value as string;

        public static bool Flag(object value) => value is bool flag && flag;

        /// <summary>A number array as a vector (missing parts are 0).</summary>
        public static Vector3 Vector(object value)
        {
            List<object> parts = Array(value);
            return new Vector3(parts.Count > 0 ? Number(parts[0]) : 0f, parts.Count > 1 ? Number(parts[1]) : 0f, parts.Count > 2 ? Number(parts[2]) : 0f);
        }

        public static object Field(Dictionary<string, object> map, string key) => map != null && map.TryGetValue(key, out object value) ? value : null;

        private object Value()
        {
            Skip();
            if (_at >= _text.Length)
            {
                throw new System.FormatException("Unexpected end of JSON.");
            }

            char c = _text[_at];
            switch (c)
            {
                case '{': return ReadObject();
                case '[': return ReadArray();
                case '"': return ReadString();
                case 't': Expect("true"); return true;
                case 'f': Expect("false"); return false;
                case 'n': Expect("null"); return null;
                default: return ReadNumber();
            }
        }

        private Dictionary<string, object> ReadObject()
        {
            var map = new Dictionary<string, object>();
            _at++;
            Skip();
            if (_text[_at] == '}')
            {
                _at++;
                return map;
            }

            while (true)
            {
                Skip();
                string key = ReadString();
                Skip();
                Expect(":");
                map[key] = Value();
                Skip();
                char c = _text[_at++];
                if (c == '}')
                {
                    return map;
                }

                if (c != ',')
                {
                    throw new System.FormatException($"Expected ',' or '}}' at {_at - 1}.");
                }
            }
        }

        private List<object> ReadArray()
        {
            var list = new List<object>();
            _at++;
            Skip();
            if (_text[_at] == ']')
            {
                _at++;
                return list;
            }

            while (true)
            {
                list.Add(Value());
                Skip();
                char c = _text[_at++];
                if (c == ']')
                {
                    return list;
                }

                if (c != ',')
                {
                    throw new System.FormatException($"Expected ',' or ']' at {_at - 1}.");
                }
            }
        }

        private string ReadString()
        {
            Expect("\"");
            var builder = new StringBuilder();
            while (_text[_at] != '"')
            {
                char c = _text[_at++];
                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }

                char escaped = _text[_at++];
                switch (escaped)
                {
                    case 'n': builder.Append('\n'); break;
                    case 't': builder.Append('\t'); break;
                    case 'r': builder.Append('\r'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'u':
                        builder.Append((char)int.Parse(_text.Substring(_at, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        _at += 4;
                        break;
                    default: builder.Append(escaped); break;
                }
            }

            _at++;
            return builder.ToString();
        }

        private double ReadNumber()
        {
            int start = _at;
            while (_at < _text.Length && "+-0123456789.eE".IndexOf(_text[_at]) >= 0)
            {
                _at++;
            }

            return double.Parse(_text.Substring(start, _at - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private void Expect(string word)
        {
            if (string.CompareOrdinal(_text, _at, word, 0, word.Length) != 0)
            {
                throw new System.FormatException($"Expected '{word}' at {_at}.");
            }

            _at += word.Length;
        }

        private void Skip()
        {
            while (_at < _text.Length && char.IsWhiteSpace(_text[_at]))
            {
                _at++;
            }
        }
    }
}
