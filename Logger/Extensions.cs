using System;
using System.Text;

namespace Logging
{
    internal static  class Extensions
    {
        public static StringBuilder AppendException(this StringBuilder builder, Exception exception)
        {
            builder.AppendLine($"Message: {exception.Message}");
            builder.AppendLine($"Source: {exception.Source}");
            builder.AppendLine($"StackTrace: {exception.StackTrace}");

            return builder;
        }
    }
}
