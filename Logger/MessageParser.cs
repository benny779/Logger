using System;
using System.Data.SqlClient;
using System.Text;

namespace Logging
{
    internal class MessageParser : IMessageParser
    {
        public string Parse(object message)
        {
            switch (message)
            {
                case null:
                    return string.Empty;
                case Exception ex:
                    return ParseException(ex);
                case SqlCommand command:
                    return ParseSqlCommand(command);
                default:
                    return message.ToString();
            }
        }

        private static string ParseException(Exception exception)
        {
            var builder = new StringBuilder();
            builder.AppendException(exception);

            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                builder.AppendException(exception);
            }

            return builder.ToString();
        }

        private static string ParseSqlCommand(SqlCommand command)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Command type: {command.CommandType}");
            builder.AppendLine($"Command text: {command.CommandText}");

            ParseSqlParameters(command.Parameters, builder);

            return builder.ToString();
        }

        private static void ParseSqlParameters(SqlParameterCollection parameters, StringBuilder builder)
        {
            foreach (SqlParameter param in parameters)
            {
                builder.AppendLine($"Parameter: {param.ParameterName}, Value: {param.Value}");
            }
        }

    }
}
