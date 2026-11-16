using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// A local crash log without any service: every exception Unity logs is appended to <c>crash.log</c> in the given
    /// folder with the build version, the scene, the level and the time, so a playtester can send the file. The file is
    /// rotated to <c>crash.1.log</c> when it grows past its limit, and a burst of exceptions (one per frame in a broken
    /// Update) is rate-limited: after <see cref="BurstLimit"/> entries inside one window the rest only count, and the next
    /// written entry says how many were left out. Any other log type returns at the first check, so the handler costs
    /// nothing while the game runs normally. Writing never throws: a full disk must not turn one exception into two.
    /// </summary>
    public sealed class CrashLog
    {
        public const string FileName = "crash.log";
        public const string PreviousFileName = "crash.1.log";

        /// <summary>The file is rotated once it is larger than this.</summary>
        public const long DefaultMaxBytes = 256 * 1024;

        /// <summary>At most this many entries are written per window.</summary>
        public const int BurstLimit = 10;

        /// <summary>The length of the rate-limit window in seconds.</summary>
        public const double WindowSeconds = 60.0;

        private const int MaxStackLength = 4000;

        private readonly string _path;
        private readonly string _previousPath;
        private readonly long _maxBytes;
        private readonly Func<double> _clock;
        private readonly Func<CrashContext> _context;

        private double _windowStart = double.NegativeInfinity;
        private int _writtenInWindow;
        private int _suppressed;

        /// <param name="folder">Where crash.log lives (the persistent data folder in the game).</param>
        /// <param name="clock">Seconds since start, unscaled (the game passes <c>Time.realtimeSinceStartupAsDouble</c>).</param>
        /// <param name="context">Version, scene and level at the moment of the exception; only called when an entry is written.</param>
        public CrashLog(string folder, Func<double> clock, Func<CrashContext> context, long maxBytes = DefaultMaxBytes)
        {
            _path = Path.Combine(folder, FileName);
            _previousPath = Path.Combine(folder, PreviousFileName);
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _maxBytes = maxBytes;
        }

        public string FilePath => _path;

        /// <summary>Entries left out by the rate limit since the last written one.</summary>
        public int Suppressed => _suppressed;

        /// <summary>Signature of <c>Application.logMessageReceived</c>.</summary>
        public void Handle(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception)
            {
                return;
            }

            double now = _clock();
            if (now - _windowStart >= WindowSeconds)
            {
                _windowStart = now;
                _writtenInWindow = 0;
            }

            if (_writtenInWindow >= BurstLimit)
            {
                _suppressed++;
                return;
            }

            _writtenInWindow++;
            Write(Format(_context(), DateTime.UtcNow, condition, stackTrace, _suppressed));
            _suppressed = 0;
        }

        /// <summary>One entry: a header line with the time and where the game was, the message, then the stack.</summary>
        public static string Format(CrashContext context, DateTime utc, string condition, string stackTrace, int suppressedBefore)
        {
            var text = new StringBuilder(256);
            text.Append("=== ").Append(utc.ToString("yyyy-MM-dd HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture))
                .Append(" | version ").Append(context.Version)
                .Append(" | scene ").Append(Or(context.Scene))
                .Append(" | level ").Append(Or(context.Level))
                .Append('\n');
            if (suppressedBefore > 0)
            {
                text.Append("(").Append(suppressedBefore).Append(" earlier exceptions not written: rate limit)\n");
            }

            text.Append(condition).Append('\n');
            if (!string.IsNullOrEmpty(stackTrace))
            {
                text.Append(stackTrace.Length > MaxStackLength ? stackTrace.Substring(0, MaxStackLength) + "..." : stackTrace.TrimEnd()).Append('\n');
            }

            return text.Append('\n').ToString();
        }

        private void Write(string entry)
        {
            try
            {
                string folder = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                RotateIfFull();
                File.AppendAllText(_path, entry, Encoding.UTF8);
            }
            catch (IOException)
            {
                // A crash log that cannot be written is lost, never a second exception.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private void RotateIfFull()
        {
            var info = new FileInfo(_path);
            if (!info.Exists || info.Length < _maxBytes)
            {
                return;
            }

            if (File.Exists(_previousPath))
            {
                File.Delete(_previousPath);
            }

            File.Move(_path, _previousPath);
        }

        private static string Or(string value) => string.IsNullOrEmpty(value) ? "-" : value;
    }
}
