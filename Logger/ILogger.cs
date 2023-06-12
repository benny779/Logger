namespace Logging
{
    /// <summary>
    /// Represents a type used to performe logging.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// For debugging and development. Use with caution in production due to the high volume.
        /// </summary>
        /// <param name="message"></param>
        void Debug(object message);
        
        /// <summary>
        /// Tracks the general flow of the app. May have long-term value.
        /// </summary>
        /// <param name="message"></param>
        void Info(object message);

        /// <summary>
        /// For abnormal or unexpected events. Typically includes errors or conditions that don't cause the app to fail.
        /// </summary>
        /// <param name="message"></param>
        void Warn(object message);

        /// <summary>
        /// For errors and exceptions that cannot be handled.
        /// These messages indicate a failure in the current operation or request, not an app-wide failure.
        /// </summary>
        /// <param name="message"></param>
        void Error(object message);

        /// <summary>
        /// For failures that require immediate attention. Examples: data loss scenarios, out of disk space.
        /// </summary>
        /// <param name="message"></param>
        void Critical(object message);
    }
}
