using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logging
{
    /// <summary>
    /// A logger class to performe logging.
    /// </summary>
    public class Logger : ILogger
    {
        private readonly Dictionary<string, Target> _targets = new Dictionary<string, Target>();
        private bool _enabled = true;
        private string _timeFormat = "yyyy-MM-dd HH:mm:ss.fff"; //It should also be updated in the SetTimeFormat summary
        private bool _parallelismEnabled = true;
        private bool _logHistoryEnabled = false;
        private LimitedList<string> _logs;

        /// <summary>
        /// Creates a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <remarks>By default there are no targets. Use <see cref="AddTarget(Target)"/> to add.</remarks>
        public Logger() { }


        /// <inheritdoc cref="ILogger.Debug(object)"/>
        public void Debug(object message) => InternalLog(message, LogLevel.Debug);

        /// <inheritdoc cref="ILogger.Info(object)"/>
        public void Info(object message) => InternalLog(message, LogLevel.Info);

        /// <inheritdoc cref="ILogger.Warn(object)"/>
        public void Warn(object message) => InternalLog(message, LogLevel.Warn);

        /// <inheritdoc cref="ILogger.Error(object)"/>
        public void Error(object message) => InternalLog(message, LogLevel.Error);

        /// <inheritdoc cref="ILogger.Critical(object)"/>
        public void Critical(object message) => InternalLog(message, LogLevel.Critical);


        private void InternalLog(object message, LogLevel level)
        {
            if (!_enabled) return;

            var entry = new LogEntry(message, level);

            FormatLogEntryMessage(entry);

            InternalLog(entry);

            if (_logHistoryEnabled)
                _logs.Add(entry.fullFormattedMessage);
        }
        private void InternalLog(LogEntry entry)
        {
            var targetsToLog = _targets.Where(t => t.Value.enabled && entry.Level >= t.Value.LogLevel);

            if (_parallelismEnabled)
                Parallel.ForEach(targetsToLog, target => target.Value.Log(entry));
            else
                foreach (var target in targetsToLog) target.Value.Log(entry);
        }

        private void FormatLogEntryMessage(LogEntry entry)
        {
            entry.formattedMessage = GetMessageStringInternal(entry.Message);
            entry.fullFormattedMessage = GetMessageString(entry);
        }
        private string GetMessageString(LogEntry entry)
        {
            string timestamp = entry.Timestamp.ToString(_timeFormat);
            string levelString = entry.Level.GetShortName();
            string messageString = entry.formattedMessage ?? GetMessageStringInternal(entry.Message);

            return $"{timestamp} [{levelString}] {messageString}";
        }
        private string GetMessageStringInternal(object message)
        {
            if (message is null)
                return string.Empty;

            if (message is Exception ex)
            {
                var exMessages = new StringBuilder();
                exMessages.AppendLine(ex.Message);

                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    exMessages.AppendLine(ex.Message);
                }

                return exMessages.ToString();
            }

            return message.ToString();
        }


        /// <summary>
        /// Execute action on target if it exists.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <param name="action"></param>
        /// <exception cref="ArgumentNullException"></exception>
        private void TargetAction(string targetIdentifier, Action<Target> action)
        {
            Helper.ThrowIfNullOrEmpty(targetIdentifier, nameof(targetIdentifier));

            if (_targets.ContainsKey(targetIdentifier))
                action(_targets[targetIdentifier]);
        }


        /// <summary>
        /// Adds a target to the logger. If the identifier already exists, the target will be replaced.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Logger AddTarget(Target target)
        {
            if (target is null)
                throw new ArgumentNullException(nameof(target));

            var targetExist = _targets.ContainsKey(target.targetIdentifier);
            if (!targetExist)
                _targets.Add(target.targetIdentifier, target);
            else
                _targets[target.targetIdentifier] = target;

            return this;
        }
        /// <summary>
        /// Removes a target.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <returns><see langword="true"/> if the target is succesfully found and removed; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool RemoveTarget(string targetIdentifier)
        {
            Helper.ThrowIfNullOrEmpty(targetIdentifier, nameof(targetIdentifier));

            return _targets.Remove(targetIdentifier);
        }
        /// <summary>
        /// Removes all targets.
        /// </summary>
        public void RemoveAllTargets() => _targets.Clear();




        /// <summary>
        /// Sets the target state to Enabled.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void EnableTarget(string targetIdentifier) => TargetAction(targetIdentifier, t => t.enabled = true);
        /// <summary>
        /// Sets the target state to Disabled.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void DisableTarget(string targetIdentifier) => TargetAction(targetIdentifier, t => t.enabled = false);

        /// <summary>
        /// Sets the specified target minimum severity level.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <param name="level"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void SetTargetLogLevel(string targetIdentifier, LogLevel level) => TargetAction(targetIdentifier, t => t.LogLevel = level);


        /// <summary>
        /// Enable parallelism mode (Default mode). Logging will occur using <see cref="Parallel"/>.
        /// </summary>
        /// <returns></returns>
        public Logger EnableParallelism()
        {
            _parallelismEnabled = true;
            return this;
        }

        /// <summary>
        /// Disable parallelism mode. Logging will occur using the normal <see langword="foreach"/> statement.
        /// </summary>
        /// <returns></returns>
        public Logger DisableParallelism()
        {
            _parallelismEnabled = false;
            return this;
        }

        /// <summary>
        /// Enable the logger.
        /// </summary>
        public void Enable() => _enabled = true;
        /// <summary>
        /// Disable the logger.
        /// </summary>
        public void Disable() => _enabled = false;

        /// <summary>
        /// Specifies the time format for logging.
        /// </summary>
        /// <remarks>Default format is <c>"yyyy-MM-dd HH:mm:ss.fff"</c>.</remarks>
        /// <param name="format"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Logger SetTimeFormat(string format)
        {
            Helper.ThrowIfNullOrEmpty(format, nameof(format));

            _timeFormat = format;
            return this;
        }
        /// <summary>
        /// Specifies the time format for logging.
        /// </summary>
        /// <remarks>Default format is <c>"yyyy-MM-dd HH:mm:ss.fff"</c>.</remarks>
        /// <param name="formatBuilder"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public Logger SetTimeFormat(TimeFormatBuilder formatBuilder)
        {
            Helper.ThrowIfNull(formatBuilder, nameof(formatBuilder));

            _timeFormat = formatBuilder.ToString();
            return this;
        }


        /// <summary>
        /// Enable log history. All formatted logs messages will be saved.
        /// </summary>
        /// <param name="historyCapacity"></param>
        /// <returns></returns>
        public Logger EnableLogHistory(int historyCapacity = 1000)
        {
            if (_logs is null)
                _logs = new LimitedList<string>(historyCapacity);

            _logHistoryEnabled = true;
            return this;
        }

        /// <summary>
        /// Disable log history. History will be deleted.
        /// </summary>
        public void DisableLogHistory()
        {
            _logHistoryEnabled = false;
            _logs = null;
        }

        /// <summary>
        /// Disable log history. History will remain.
        /// </summary>
        public void PauseLogHistory() => _logHistoryEnabled = false;

        /// <summary>
        /// Clears the history.
        /// </summary>
        public void ClearLogHistory() => _logs?.Clear();

        /// <summary>
        /// Retrieve log history lazily (if available).
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetLogHistory()
        {
            if (_logs is null)
                yield break;

            foreach (var log in _logs)
                yield return log;
        }
    }
}