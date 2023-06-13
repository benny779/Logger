using System;
using System.IO;

namespace Logging
{
    internal class LogEntry
    {
        public DateTime Timestamp;
        public LogLevel Level;
        public object Message;

        internal string formattedMessage = null;
        internal string fullFormattedMessage = null;

        public LogEntry(object message, LogLevel level)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Message = message;
        }

        public override string ToString()
        {
            return $"{nameof(LogEntry)}: {Timestamp} | {Level} | {Message}";
        }
    }
}
