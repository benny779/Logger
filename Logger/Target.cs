using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;

namespace Logging
{
    /// <summary>
    /// Abstract base class for target objects.
    /// </summary>
    public abstract class Target
    {
        /// <summary>
        /// Entries will be written from this level and above.
        /// </summary>
        public LogLevel LogLevel;

        internal string TargetIdentifier;
        internal bool Enabled = true;

        /// <summary>
        /// Creates a new instance of the <see cref="Target"/>.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <exception cref="ArgumentNullException"></exception>
        protected Target(string targetIdentifier)
        {
            Helper.ThrowIfNullOrEmpty(targetIdentifier, nameof(targetIdentifier));

            this.TargetIdentifier = targetIdentifier;
        }


        /// <summary>
        /// Write a log entry.
        /// </summary>
        /// <param name="entry"></param>
        internal abstract void Log(LogEntry entry);

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() => $"{TargetIdentifier}";
    }


    /// <summary>
    /// Class representing a file destination.
    /// </summary>
    public class TargetFile : Target
    {
        private readonly string _logFilePath;
        private readonly string _logFileName;
        private string LogFileFullPath => Path.Combine(_logFilePath, _logFileName ?? $"{DateTime.Today:yyyy-MM-dd}.log");


        /// <summary>
        /// Maximum log file size in bytes.
        /// </summary>
        public long MaxFileSizeBytes
        {
            get => _maxFileSizeBytes; set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(MaxFileSizeBytes));

                _maxFileSizeBytes = value;
            }
        }
        private long _maxFileSizeBytes;

        /// <summary>
        /// Maximum number of lines.
        /// </summary>
        public int MaxFilesLines
        {
            get => _maxFileSize; set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(MaxFilesLines));

                _maxFileSize = value;
            }
        }
        private int _maxFileSize;

        /// <summary>
        /// Creates a new instance of the <see cref="TargetFile"/> class.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <param name="logFilePath">If null, will be set to <see cref="Directory.GetCurrentDirectory"/>.</param>
        /// <param name="logFileName">If null, will be set to the current day.</param>
        /// <param name="level">Minimum severity level.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public TargetFile(string targetIdentifier, string logFilePath = null, string logFileName = null, LogLevel level = LogLevel.Info) : base(targetIdentifier)
        {
            Helper.ThrowIfEmpty(logFilePath, nameof(logFilePath));

            this._logFilePath = logFilePath ?? Directory.GetCurrentDirectory();
            this._logFileName = logFileName;
            LogLevel = level;
            
            if (!Directory.Exists(this._logFilePath))
                Directory.CreateDirectory(this._logFilePath);
        }

        internal override void Log(LogEntry entry)
        {
            if (MaxFilesLines + MaxFileSizeBytes > 0)
                FileMaintenance();

            try
            {
                using (var writer = File.AppendText(LogFileFullPath))
                    writer.WriteLine(entry.FullFormattedMessage);
            }
            catch
            {
                // ignored
            }
        }


        #region FileMaintenance
        private void FileMaintenance()
        {
            bool wasMaintenance = false;

            if (MaxFilesLines > 0)
                wasMaintenance = FileMaintenanceByLines(LogFileFullPath, MaxFilesLines);

            if (!wasMaintenance && MaxFileSizeBytes > 0)
                FileMaintenanceByBytes(LogFileFullPath, MaxFileSizeBytes);
        }

        private static bool FileMaintenanceByLines(string filePath, int maxLines)
        {
            try
            {
                var linesInFile = File.Exists(filePath)
                    ? File.ReadAllLines(filePath).Length
                    : 0;

                if (linesInFile > maxLines - 1)
                    return ArchiveFile(filePath);

            }
            catch
            {
                // ignored
            }

            return false;
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private static bool FileMaintenanceByBytes(string filePath, long maxBytes)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var fileSize = fileInfo.Exists ? fileInfo.Length : 0;

                if (fileSize > maxBytes)
                    return ArchiveFile(filePath);
            }
            catch
            {
                // ignored
            }

            return false;
        }
        private static string GetNextFileName(string filePath)
        {
            if (!File.Exists(filePath)) return filePath;

            var file = new FileInfo(filePath);
            var dirPath = file.DirectoryName;
            var fileName = Path.GetFileNameWithoutExtension(file.Name);
            var fileExt = file.Extension;

            var count = Directory.GetFiles(dirPath, $"{fileName}*{fileExt}").Length;
            var newFileName = $"{fileName}_{count.ToString().PadLeft(3, '0')}{fileExt}";
            var newFilePath = Path.Combine(dirPath, newFileName);

            return newFilePath;
        }
        private static bool ArchiveFile(string filePath)
        {
            try
            {
                var nextFilePath = GetNextFileName(filePath);
                if (File.Exists(nextFilePath))
                    File.Delete(nextFilePath);

                File.Move(filePath, nextFilePath);
                return true;
            }
            catch
            {
                // ignored
            }

            return false;
        }
        #endregion


        /// <inheritdoc cref="Target.ToString"/>
        public override string ToString() => $"{TargetIdentifier}: {LogFileFullPath}";
    }

    /// <summary>
    /// Class representing a database destination.
    /// </summary>
    public class TargetDatabase : Target
    {
        private const int SqlConnectionDefaultTimeout = 2;
        private string _connectionString;

        /// <summary>
        /// Creates a new instance of the <see cref="TargetDatabase"/> class.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <param name="connectionString"></param>
        /// <param name="level">Minimum severity level.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TargetDatabase(string targetIdentifier, string connectionString, LogLevel level = LogLevel.Warn) : base(targetIdentifier)
        {
            this._connectionString = connectionString;
            LogLevel = level;
            Validate();
        }
        /// <summary>
        /// Creates a new instance of the <see cref="TargetDatabase"/> class.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <param name="dbConnection">A connection to get the ConnectionString from.</param>
        /// <param name="level">Minimum severity level.</param>
        public TargetDatabase(string targetIdentifier, IDbConnection dbConnection, LogLevel level = LogLevel.Warn)
            : this(targetIdentifier, dbConnection.ConnectionString, level)
        { }

        private void Validate()
        {
            Helper.ThrowIfNullOrEmpty(_connectionString, nameof(_connectionString));

            _connectionString = ValidateConnectionString(_connectionString);
        }
        private static string ValidateConnectionString(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);

            if (string.IsNullOrEmpty(builder.DataSource) || string.IsNullOrEmpty(builder.InitialCatalog))
                throw new ArgumentException("Invalid connection string. The DataSource and InitialCatalog properties must be specified.");

            if (!builder.IntegratedSecurity)
            {
                if (string.IsNullOrEmpty(builder.UserID) ^ string.IsNullOrEmpty(builder.Password))
                    throw new ArgumentException("Invalid connection string. UserID or Password is missing.");

                if (string.IsNullOrEmpty(builder.UserID) && string.IsNullOrEmpty(builder.Password))
                    builder.IntegratedSecurity = true;
            }

            builder.ConnectTimeout = SqlConnectionDefaultTimeout;
            return builder.ConnectionString;
        }

        internal override void Log(LogEntry entry)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var command = new SqlCommand("INSERT INTO LogEntries (App, Machine, Username, Timestamp, Level, Category, Message) VALUES (@App, @Machine, @Username, @Timestamp, @Level, @Category, @Message)", connection);
                    command.Parameters.AddWithValue("@App", General.AppName);
                    command.Parameters.AddWithValue("@Machine", General.MachineName);
                    command.Parameters.AddWithValue("@Username", General.UserName);
                    command.Parameters.AddWithValue("@Timestamp", entry.Timestamp);
                    command.Parameters.AddWithValue("@Level", entry.Level.ToString());
                    command.Parameters.AddWithValue("@Message", entry.FormattedMessage);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                // ignored
            }
        }


        /// <summary>
        /// Returns the DDL for the log table.
        /// </summary>
        /// <returns></returns>
        public static string GetLogTableDdl() => @"
CREATE TABLE dbo.LogEntries
(
	LogEntriesID	int				PRIMARY KEY IDENTITY(1, 1),
	App				varchar(64)		NULL,
	Machine			varchar(64)		NULL,
	Username		varchar(64)		NULL,
	Timestamp		datetime2		NULL,
	Level			varchar(32)		NULL,
	Category		varchar(64)		NULL,
	Message			varchar(MAX)	NULL
)";


        /// <inheritdoc cref="Target.ToString"/>
        public override string ToString()
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            return $"{TargetIdentifier}: {builder.DataSource},{builder.InitialCatalog}";
        }
    }

    /// <summary>
    /// Class representing the Windows Event Viewer destination.
    /// </summary>
    public class TargetEventViewer : Target
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TargetEventViewer"/> class.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <param name="level">Minimum severity level.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TargetEventViewer(string targetIdentifier, LogLevel level = LogLevel.Error) : base(targetIdentifier)
        {
            LogLevel = level;
        }

        internal override void Log(LogEntry entry)
        {
            try
            {
                using (var eventLog = new EventLog("Application"))
                {
                    eventLog.Source = General.AppName;
                    eventLog.WriteEntry(entry.FormattedMessage, entry.Level.GetEventLogEntryType());
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    /// <summary>
    /// Class representing the <see cref="Console"/> destination.
    /// </summary>
    public class TargetConsole : Target
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TargetConsole"/> class.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <param name="level">Minimum severity level.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TargetConsole(string targetIdentifier, LogLevel level = LogLevel.Debug) : base(targetIdentifier)
        {
            LogLevel = level;
            Validate();
        }

        private void Validate()
        {
            if (!Environment.UserInteractive)
                throw new InvalidOperationException("The environment must be UserInteractive when LogTargets includes Console");
        }

        internal override void Log(LogEntry entry)
        {
            Console.WriteLine(entry.FullFormattedMessage);
        }
    }

    /// <summary>
    /// Class representing the <see cref="Trace"/> destination.
    /// </summary>
    public class TargetTrace : Target
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TargetTrace"/> class.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <param name="level">Minimum severity level.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TargetTrace(string targetIdentifier, LogLevel level = LogLevel.Debug) : base(targetIdentifier)
        {
            LogLevel = level;
        }

        internal override void Log(LogEntry entry)
        {
            Trace.WriteLine(entry.FullFormattedMessage);
        }
    }

    /// <summary>
    /// Class representing an email destination.
    /// </summary>
    public class TargetEmail : Target
    {
        private readonly SmtpClient _smtpClient;
        private readonly MailMessage _mailMessage;

        /// <summary>
        /// Creates a new instance of the <see cref="TargetEmail"/> class.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <param name="smtpServer"></param>
        /// <param name="smtpPort"></param>
        /// <param name="from"></param>
        /// <param name="recipients">Multiple email addresses must be separated with a comma (",").</param>
        /// <param name="level">Minimum severity level.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="FormatException"></exception>
        public TargetEmail(string targetIdentifier, string smtpServer, int smtpPort, MailAddress from, string recipients, LogLevel level = LogLevel.Critical)
            : this(targetIdentifier,
                 new SmtpClient(smtpServer, smtpPort),
                 new MailMessage(from, new MailAddress(recipients)),
                 level)
        { }

        /// <summary>
        /// Creates a new instance of the <see cref="TargetEmail"/> class.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <param name="smtpClient"></param>
        /// <param name="from"></param>
        /// <param name="recipients">Multiple email addresses must be separated with a comma (",").</param>
        /// <param name="level">Minimum severity level.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="FormatException"></exception>
        public TargetEmail(string targetIdentifier, SmtpClient smtpClient, MailAddress from, string recipients, LogLevel level = LogLevel.Critical)
            : this(targetIdentifier,
                   smtpClient,
                   new MailMessage(from, new MailAddress(recipients)),
                   level)
        { }

        /// <summary>
        /// Creates a new instance of the <see cref="TargetEmail"/> class.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <param name="smtpClient"></param>
        /// <param name="mailMessage"></param>
        /// <param name="level">Minimum severity level.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TargetEmail(string targetIdentifier, SmtpClient smtpClient, MailMessage mailMessage, LogLevel level = LogLevel.Critical)
            : base(targetIdentifier)
        {
            _smtpClient = smtpClient;
            _mailMessage = mailMessage;
            LogLevel = level;
            Validate();
        }

        private void Validate()
        {
            if (_smtpClient is null)
                throw new InvalidOperationException("Invalid SmtpClient.");

            if (_mailMessage is null)
                throw new InvalidOperationException("Invalid MailMessage.");

            if (_mailMessage.From is null)
                throw new InvalidOperationException("A from address must be specified.");

            if (_mailMessage.To.Count < 1)
                throw new InvalidOperationException("A recipient must be specified.");
        }

        internal override void Log(LogEntry entry)
        {
            _mailMessage.Subject = $"{General.AppName} | {entry.Level}";
            _mailMessage.Priority = entry.Level >= LogLevel.Error ? MailPriority.High : MailPriority.Normal;
            _mailMessage.Body = entry.FormattedMessage;

            try
            {
                _smtpClient.Send(_mailMessage);
            }
            catch
            {
                // ignored
            }
        }

        /// <inheritdoc cref="Target.ToString"/>
        public override string ToString() => $"{TargetIdentifier}: {_smtpClient.Host}:{_smtpClient.Port}";
    }
}