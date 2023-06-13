using System;
using System.IO;

namespace Logging
{
    internal class LogEntry
    {
        public DateTime Timestamp;
        public LogLevel Level;
        public object Message;

        internal string formattedMessage;
        internal string fullFormattedMessage;

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
