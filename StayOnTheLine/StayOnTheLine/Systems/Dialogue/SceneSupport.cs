using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using StayOnTheLine.Models;
using StayOnTheLine.Models.Dialogue;

namespace StayOnTheLine.Systems.Dialogue
{
    /// <summary>
    /// OOP Principle: ENCAPSULATION
    /// The in-world terminal clock. Runs faster than real time so the night visibly moves
    /// while the player reads (one real second = <c>speed</c> in-game seconds).
    /// </summary>
    public sealed class SceneClock
    {
        private readonly DateTime _start;
        private readonly double _speed;
        private readonly Stopwatch _elapsed = Stopwatch.StartNew();

        public SceneClock(DateTime start, double speed = 8)
        {
            _start = start;
            _speed = Math.Max(0.1, speed);
        }

        public DateTime Now => _start.AddSeconds(_elapsed.Elapsed.TotalSeconds * _speed);
    }

    /// <summary>
    /// Records-archive rows Stef shares with the player. Every change except the last one
    /// is tied to the player's own operator login - including edits made before calls came in.
    /// </summary>
    public static class AuditLogRepository
    {
        public static IReadOnlyList<AuditRecord> GetRecordsFor(string operatorName)
        {
            string op = "OP-" + (string.IsNullOrWhiteSpace(operatorName) ? "OPERATOR" : operatorName.Trim().ToUpperInvariant());
            return new List<AuditRecord>
            {
                new("S1 01:42", "INC-0216", "14 BELL ST", "PRE-CALL EDIT", op, true),
                new("S1 23:58", "INC-0101", "21 WEST ST", "POST-CLOSE EDIT", op, true),
                new("S2 00:41", "INC-0201", "WATER TOWER RD", "DISTANCE EDIT", op, true),
                new("S2 01:12", "INC-0245", "14 BELL ST", "ADDRESS EDIT", op, true),
                new("S2 01:47", "INC-0288", "[NO RECORD]", "DELETED", op, true),
                new("S3 02:16", "ARC-08", "LINE 08", "ACCESSED", "OP-K.NARCISO", false)
            };
        }
    }

    /// <summary>
    /// Writes a plain-text copy of the terminal thread next to the save slots ("keep a copy before
    /// anything changes again"). Returns the file path.
    /// </summary>
    public static class TranscriptArchiver
    {
        public static string Save(IEnumerable<ChatMessage> history, OperatorProfile? profile)
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves", "RecordCopies");
            Directory.CreateDirectory(dir);

            string slot = profile != null ? $"slot{profile.SlotIndex}" : "guest";
            string path = Path.Combine(dir, $"stef_thread_{slot}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            var lines = new List<string>
            {
                "RECORDS TERMINAL // PRIVATE COPY",
                $"OPERATOR: {profile?.OperatorName ?? "OPERATOR"}",
                $"COPIED: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                new string('-', 48)
            };
            // Only what appeared on the terminal; things said out loud at the desk were never logged
            lines.AddRange(history.Where(m => m.Channel == ChatChannel.Terminal).Select(m =>
            {
                string time = m.Timestamp?.ToString("HH:mm") ?? "--:--";
                string who = m.SpeakerName.Length > 0 ? m.SpeakerName + ": " : string.Empty;
                return $"[{time}] {who}{m.Text}";
            }));

            File.WriteAllLines(path, lines);
            return path;
        }
    }
}
