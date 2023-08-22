using System;
using System.Data.SqlClient;
using System.Text;

namespace Logging
{
    internal class MessageParser : IMessageParser
    {
        public string Parse(object message)
        {
            if (message is null)
                return string.Empty;

            if (message is Exception ex)
                return ParseException(ex);

            if (message is SqlCommand command)
                return ParseSqlCommand(command);

            return message.ToString();
        }

        private string ParseException(Exception exception)
        {
            var builder = new StringBuilder();
            builder.AppendLine(exception.Message);

            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                builder.AppendLine(exception.Message);
            }

            return builder.ToString();
        }

        private string ParseSqlCommand(SqlCommand command)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Command type: {command.CommandType}");
            builder.AppendLine($"Command text: {command.CommandText}");

            ParseSqlParameters(command.Parameters, builder);

            return builder.ToString();
        }

        private void ParseSqlParameters(SqlParameterCollection parameters, StringBuilder builder)
        {
            foreach (SqlParameter param in parameters)
            {
                builder.AppendLine($"Parameter: {param.ParameterName}, Value: {param.Value}");
            }
        }

    }
}
