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

        internal static readonly string appName = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
        internal static readonly string machineName = Environment.MachineName;
        internal static readonly string userName = Environment.UserName;
        public string AppName => appName;
        public string MachineName => machineName;
        public string UserName => userName;

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
