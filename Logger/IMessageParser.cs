namespace Logging
{
    /// <summary>
    /// Defines a contract for classes responsible for parsing various types of input messages
    /// into human-readable string representations.
    /// </summary>
    public interface IMessageParser
    {
        /// <summary>
        /// Parses the provided message object into a human-readable string representation.
        /// </summary>
        /// <param name="message">The message object to be parsed.</param>
        /// <returns>A string representing the parsed message.</returns>
        string Parse(object message);
    }
}
