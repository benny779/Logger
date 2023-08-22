using System;

namespace Logging
{
    /// <summary>
    /// The exception that is thrown when a null reference
    /// is passed to a method that does not accept it as a valid argument.
    /// </summary>
    public class ArgumentEmptyException : ArgumentNullException
    {
        /// <summary>Initializes a new instance of the <see cref="ArgumentEmptyException"/> class with the
        /// name of the parameter that causes this exception.
        /// </summary>
        /// <param name="paramName"><inheritdoc cref="ArgumentNullException(string)"/></param>
        public ArgumentEmptyException(string paramName) : base(paramName)
        { }

        /// <summary>Initializes an instance of the <see cref="ArgumentEmptyException"/> class with a specified
        /// error message and the name of the parameter that causes this exception.</summary>
        /// <param name="paramName"><inheritdoc cref="ArgumentNullException(string)"/></param>
        /// <param name="message"><inheritdoc cref="ArgumentException(string)"/></param>
        public ArgumentEmptyException(string paramName, string message) : base(paramName, message)
        { }
    }
}
