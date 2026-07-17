using System.Text;

namespace EVEDataUtils
{
    /// <summary>
    /// Lightweight, thread-safe application logging that never throws back into SMT.
    /// </summary>
    public static class AppLog
    {
        private const long MaxLogSizeBytes = 2 * 1024 * 1024;
        private static readonly object SyncRoot = new object();
        private static string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SMT",
            "Logs");

        public static event Action<string, bool> StatusChanged;

        public static string CurrentLogFile => Path.Combine(logDirectory, "SMT.log");

        public static void Initialize(string directory)
        {
            if(!string.IsNullOrWhiteSpace(directory))
            {
                logDirectory = directory;
            }

            Info("Logging", "SMT logging initialized.");
        }

        public static void Info(string operation, string message)
        {
            Write("INFO", operation, message, false);
        }

        public static void Warning(string operation, string message)
        {
            Write("WARN", operation, message, true);
        }

        public static void Error(string operation, Exception exception)
        {
            string message = exception == null ? "Unknown error" : exception.ToString();
            Write("ERROR", operation, message, true);
        }

        public static void Error(string operation, string message)
        {
            Write("ERROR", operation, message, true);
        }

        private static void Write(string level, string operation, string message, bool isError)
        {
            string statusMessage = $"{operation}: {FirstLine(message)}";

            try
            {
                lock(SyncRoot)
                {
                    Directory.CreateDirectory(logDirectory);
                    RotateIfNeeded();

                    string entry = $"{DateTimeOffset.Now:O} [{level}] [{operation}] {message}{Environment.NewLine}";
                    File.AppendAllText(CurrentLogFile, entry, new UTF8Encoding(false));
                }
            }
            catch
            {
                // Logging must never cause an application failure.
            }

            try
            {
                StatusChanged?.Invoke(statusMessage, isError);
            }
            catch
            {
                // Status listeners are optional and must not affect application work.
            }
        }

        private static void RotateIfNeeded()
        {
            FileInfo currentLog = new FileInfo(CurrentLogFile);
            if(!currentLog.Exists || currentLog.Length < MaxLogSizeBytes)
            {
                return;
            }

            string previousLog = Path.Combine(logDirectory, "SMT.previous.log");
            File.Move(CurrentLogFile, previousLog, true);
        }

        private static string FirstLine(string value)
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                return "No details available";
            }

            int lineBreak = value.IndexOfAny(new[] { '\r', '\n' });
            return lineBreak < 0 ? value : value.Substring(0, lineBreak);
        }
    }
}
