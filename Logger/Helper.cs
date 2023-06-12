using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logging
{
    internal static class Helper
    {
        public static void ThrowIfNull<T>(this T obj, string paramName) where T : class
        {
            if (obj == null)
                throw new ArgumentNullException(paramName);
        }

        public static void ThrowIfNullOrEmpty(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(paramName);
        }
    }

    /// <summary>
    /// TimeFormat Helper Class.
    /// </summary>
    public class TimeFormatBuilder
    {
        private readonly StringBuilder builder = new StringBuilder();

        /// <summary>
        /// Add a custom string.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public TimeFormatBuilder Add(string part)
        {
            builder.Append(part);
            return this;
        }

        /// <summary>
        /// Add Year.
        /// </summary>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public TimeFormatBuilder Year(string suffix = "") => Add("yyyy").Add(suffix);
        /// <summary>
        /// Add Month.
        /// </summary>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public TimeFormatBuilder Month(string suffix = "") => Add("MM").Add(suffix);
        /// <summary>
        /// Add Day.
        /// </summary>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public TimeFormatBuilder Day(string suffix = "") => Add("dd").Add(suffix);
        /// <summary>
        /// Add Hour.
        /// </summary>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public TimeFormatBuilder Hour(string suffix = "") => Add("HH").Add(suffix);
        /// <summary>
        /// Add Minute.
        /// </summary>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public TimeFormatBuilder Minute(string suffix = "") => Add("mm").Add(suffix);
        /// <summary>
        /// Add Second.
        /// </summary>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public TimeFormatBuilder Second(string suffix = "") => Add("ss").Add(suffix);
        /// <summary>
        /// Add Millisecond (3 digits).
        /// </summary>
        /// <param name="suffix"></param>
        /// <returns></returns>
        public TimeFormatBuilder Millisecond(string suffix = "") => Add("fff").Add(suffix);
        /// <summary>
        /// Add Millisecond.
        /// </summary>
        /// <param name="length"></param>
        /// <param name="suffix"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public TimeFormatBuilder Millisecond(int length, string suffix = "")
        {
            if (length < 1 || length > 7)
                throw new ArgumentOutOfRangeException(nameof(length), $"The {nameof(length)} value is out of range (1-7).");

            return Add(string.Join("", Enumerable.Repeat("f", length)))
                .Add(suffix);
        }


        /// <summary>
        /// Clear the Format Builder.
        /// </summary>
        /// <returns></returns>
        public TimeFormatBuilder Clear()
        {
            builder.Clear();
            return this;
        }


        /// <inheritdoc cref="StringBuilder.ToString()"/>
        public override string ToString() => builder.ToString();
    }
}
