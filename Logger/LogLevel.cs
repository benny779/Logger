using System;
using System.Diagnostics;

namespace Logging
{
    // The comments are based on https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel?view=dotnet-plat-ext-7.0

    /// <summary>
    /// Defines logging severity levels.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Logs that are used for interactive investigation during development.
        /// These logs should primarily contain information useful for debugging and have no long-term value.
        /// </summary>
        Debug = 0,
        /// <summary>
        /// Logs that track the general flow of the application. These logs should have long-term value.
        /// </summary>
        Info,
        /// <summary>
        /// Logs that highlight an abnormal or unexpected event in the application flow,
        /// but do not otherwise cause the application execution to stop.
        /// </summary>
        Warn,
        /// <summary>
        /// Logs that highlight when the current flow of execution is stopped due to a failure.
        /// These should indicate a failure in the current activity, not an application-wide failure.
        /// </summary>
        Error,
        /// <summary>
        /// Logs that describe an unrecoverable application or system crash,
        /// or a catastrophic failure that requires immediate attention.
        /// </summary>
        Critical
    }

    /// <summary>
    /// <see cref="LogLevel"/> extensions class.
    /// </summary>
    public static class LogLevelExtensions
    {
        /// <summary>
        /// Returns a 3-character string representing <see cref="LogLevel"/>.
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetShortName(this LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    return "DBG";
                case LogLevel.Info:
                    return "INF";
                case LogLevel.Warn:
                    return "WRN";
                case LogLevel.Error:
                    return "ERR";
                case LogLevel.Critical:
                    return "CRT";
                default:
                    throw new InvalidOperationException($"Invalid {nameof(LogLevel)} value.");
            }
        }


        /// <summary>
        /// Returns a <see cref="EventLogEntryType"/> object according to the <see cref="LogLevel"/> severity.
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static EventLogEntryType GetEventLogEntryType(this LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    return EventLogEntryType.Information;
                case LogLevel.Info:
                    return EventLogEntryType.Information;
                case LogLevel.Warn:
                    return EventLogEntryType.Warning;
                case LogLevel.Error:
                    return EventLogEntryType.Error;
                case LogLevel.Critical:
                    return EventLogEntryType.Error;
                default:
                    throw new InvalidOperationException($"Invalid {nameof(LogLevel)} value.");
            }
        }
    }
}

