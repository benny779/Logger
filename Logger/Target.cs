using System;
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

        internal string targetIdentifier;
        internal bool enabled = true;

        /// <summary>
        /// Creates a new instance of the <see cref="Target"/>.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public Target(string targetIdentifier)
        {
            Helper.ThrowIfNullOrEmpty(targetIdentifier, nameof(targetIdentifier));

            this.targetIdentifier = targetIdentifier;
        }


        /// <summary>
        /// Write a log entry.
        /// </summary>
        /// <param name="entry"></param>
        internal abstract void Log(LogEntry entry);

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() => $"{targetIdentifier}";
    }


    /// <summary>
    /// Class representing a file destination.
    /// </summary>
    public class TargetFile : Target
    {
        private readonly string logFilePath;
        private readonly string logFileName;
        private string LogFileFullPath => Path.Combine(logFilePath, logFileName ?? $"{DateTime.Today:yyyy-MM-dd}.log");


        /// <summary>
        /// Maximum log file size in bytes.
        /// </summary>
        public long MaxFileSizeBytes
        {
            get => maxFileSizeBytes; set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(MaxFileSizeBytes));

                maxFileSizeBytes = value;
            }
        }
        private long maxFileSizeBytes;

        /// <summary>
        /// Maximum number of lines.
        /// </summary>
        public int MaxFilesLines
        {
            get => maxFileSize; set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(MaxFilesLines));

                maxFileSize = value;
            }
        }
        private int maxFileSize;

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
            if (logFilePath == string.Empty)
                throw new ArgumentException($"{nameof(logFilePath)} cannot be empty.");

            this.logFilePath = logFilePath ?? Directory.GetCurrentDirectory();
            this.logFileName = logFileName;
            LogLevel = level;
        }

        internal override void Log(LogEntry entry)
        {
            if (MaxFilesLines + MaxFileSizeBytes > 0)
                FileMaintenance();

            try
            {
                using (var writer = File.AppendText(LogFileFullPath))
                    writer.WriteLine(entry.fullFormattedMessage);
            }
            catch { }
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
            catch { }

            return false;
        }
        private static bool FileMaintenanceByBytes(string filePath, long maxBytes)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                var fileSize = fileInfo.Exists ? fileInfo.Length : 0;

                if (fileSize > maxBytes)
                    return ArchiveFile(filePath);
            }
            catch { }

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
            catch { }

            return false;
        }
        #endregion


        /// <inheritdoc cref="Target.ToString"/>
        public override string ToString() => $"{targetIdentifier}: {LogFileFullPath}";
    }

    /// <summary>
    /// Class representing a database destination.
    /// </summary>
    public class TargetDatabase : Target
    {
        private const int SQLCONNECTION_DEFAULT_TIMEOUT = 2;
        private string connectionString;

        /// <summary>
        /// Creates a new instance of the <see cref="TargetDatabase"/> class.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <param name="connectionString"></param>
        /// <param name="level">Minimum severity level.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public TargetDatabase(string targetIdentifier, string connectionString, LogLevel level = LogLevel.Warn) : base(targetIdentifier)
        {
            this.connectionString = connectionString;
            LogLevel = level;
            Validate();
        }

        private void Validate()
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            connectionString = ValidateConnectionString(connectionString);
        }
        private string ValidateConnectionString(string connectionString)
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

            builder.ConnectTimeout = SQLCONNECTION_DEFAULT_TIMEOUT;
            return builder.ConnectionString;
        }

        internal override void Log(LogEntry entry)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    var command = new SqlCommand("INSERT INTO LogEntries (App, Machine, Username, Timestamp, Level, Category, Message) VALUES (@App, @Machine, @Username, @Timestamp, @Level, @Category, @Message)", connection);
                    command.Parameters.AddWithValue("@App", entry.AppName);
                    command.Parameters.AddWithValue("@Machine", entry.MachineName);
                    command.Parameters.AddWithValue("@Username", entry.UserName);
                    command.Parameters.AddWithValue("@Timestamp", entry.Timestamp);
                    command.Parameters.AddWithValue("@Level", entry.Level.ToString());
                    command.Parameters.AddWithValue("@Message", entry.formattedMessage);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }
            catch { }
        }

        /// <inheritdoc cref="Target.ToString"/>
        public override string ToString()
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return $"{targetIdentifier}: {builder.DataSource},{builder.InitialCatalog}";
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
                    eventLog.Source = entry.AppName;
                    eventLog.WriteEntry(entry.formattedMessage, entry.Level.GetEventLogEntryType());
                }
            }
            catch { }
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
            Console.WriteLine(entry.fullFormattedMessage);
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
            Trace.WriteLine(entry.fullFormattedMessage);
        }
    }

    /// <summary>
    /// Class representing an email destination.
    /// </summary>
    public class TargetEmail : Target
    {
        private readonly string SmtpServer;
        private readonly int SmtpPort;

        /// <summary>
        /// Creates a new instance of the <see cref="TargetEmail"/> class.
        /// </summary>
        /// <param name="targetIdentifier"></param>
        /// <param name="smtpServer"></param>
        /// <param name="smtpPort"></param>
        /// <param name="level">Minimum severity level.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NotImplementedException"></exception>
        public TargetEmail(string targetIdentifier, string smtpServer, int smtpPort, LogLevel level = LogLevel.Critical) : base(targetIdentifier)
        {
            // TODO: implement TargetEmail
            throw new NotImplementedException();

            SmtpServer = smtpServer;
            SmtpPort = smtpPort;
            LogLevel = level;
            Validate();
        }

        private void Validate()
        {
            Helper.ThrowIfNullOrEmpty(SmtpServer, nameof(SmtpServer));

            if (SmtpPort < 1 || SmtpPort > 65535)
                throw new ArgumentOutOfRangeException(nameof(SmtpPort));
        }

        internal override void Log(LogEntry entry)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="Target.ToString"/>
        public override string ToString() => $"{targetIdentifier}: {SmtpServer}:{SmtpPort}";
    }
}