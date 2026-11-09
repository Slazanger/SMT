using System.Globalization;
using System.Text;
using EVEDataUtils;

namespace SMT.EVEData
{
    /// <summary>
    /// How long EVE log lines took to reach SMT: the time a line was read minus the time stamped on it (EVE time,
    /// whole seconds, so up to a second high). Logged every few minutes when there were lines, and at shutdown.
    /// </summary>
    internal sealed class LogReadDelays
    {
        private static readonly TimeSpan ReportInterval = TimeSpan.FromMinutes(5);
        private readonly object sync = new object();
        private readonly List<double> intel = new List<double>();
        private readonly List<double> gameLog = new List<double>();
        private DateTime nextReportUtc = DateTime.MinValue;

        public void Add(bool isIntel, string line, DateTime readUtc)
        {
            // "[ 2026.11.09 11:46:13 ] ..."
            if(line.Length < 23 || line[0] != '[' ||
               !DateTime.TryParseExact(line.AsSpan(2, 19), "yyyy.MM.dd HH:mm:ss", CultureInfo.InvariantCulture,
                                       DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime stamped))
            {
                return;
            }

            double seconds = (readUtc - stamped).TotalSeconds;
            if(seconds < -5 || seconds > 600)
            {
                // a wrong PC clock or an old line
                return;
            }

            lock(sync)
            {
                (isIntel ? intel : gameLog).Add(seconds);
            }
        }

        public void ReportIfDue(DateTime nowUtc)
        {
            if(nextReportUtc == DateTime.MinValue)
            {
                nextReportUtc = nowUtc + ReportInterval;
            }
            else if(nowUtc >= nextReportUtc)
            {
                Report();
                nextReportUtc = nowUtc + ReportInterval;
            }
        }

        public void Report()
        {
            string text;
            lock(sync)
            {
                if(intel.Count == 0 && gameLog.Count == 0)
                {
                    return;
                }

                text = $"Read delay since the last report: intel {Describe(intel)}; game logs {Describe(gameLog)} (EVE stamps whole seconds, so up to 1 s high)";
                intel.Clear();
                gameLog.Clear();
            }

            AppLog.Metrics("EVE log monitor", text);
        }

        private static string Describe(List<double> delays)
        {
            if(delays.Count == 0)
            {
                return "none";
            }

            delays.Sort();
            double Percentile(double p) => delays[Math.Min(delays.Count - 1, (int)(p * delays.Count))];
            return string.Format(CultureInfo.InvariantCulture, "{0} lines, median {1:0.0} s, 95% {2:0.0} s, max {3:0.0} s",
                                 delays.Count, Percentile(0.5), Percentile(0.95), delays[delays.Count - 1]);
        }
    }

    /// <summary>
    /// Follows one log file that the EVE client keeps open and appends to. It remembers a byte offset and hands back
    /// only complete new lines, decoded by the file's byte order mark (chat logs are UTF-16, game logs UTF-8).
    /// Sizes come from the file itself, an open handle or a path query, never from a directory listing: NTFS
    /// updates the directory entry of a file that is open for writing only when another process opens the file,
    /// which is why a FileSystemWatcher can miss appends.
    /// </summary>
    public sealed class LogFileTail : IDisposable
    {
        private const int ChunkBytes = 16 * 1024;

        private FileStream stream;
        private byte[] buffer;
        private int buffered;
        private long readPosition;
        private Encoding encoding;
        private int bomBytes;
        private int unitBytes = 1;
        private bool bigEndian;
        private bool checkLineStart = true;
        private bool atLineStart;
        private bool headerPending;
        private int headerDashLines;

        /// <param name="path">The log file.</param>
        /// <param name="startOffset">Where to start reading; content before it is never returned. At 0 the header is skipped.</param>
        public LogFileTail(string path, long startOffset)
        {
            FilePath = path;
            FileName = Path.GetFileName(path);
            StartOffset = startOffset;
            readPosition = startOffset;
            headerPending = startOffset == 0;
        }

        public string FilePath { get; }

        public string FileName { get; }

        /// <summary>
        /// The offset reading started from.
        /// </summary>
        public long StartOffset { get; }

        /// <summary>
        /// Set by the owner once it has handled the file's first lines, e.g. read its header.
        /// </summary>
        public bool Activated { get; set; }

        /// <summary>
        /// The character whose client writes the file: taken from the header when reading starts at 0, otherwise set by the owner.
        /// </summary>
        public string Listener { get; set; }

        /// <summary>
        /// When the file last grew (UTC); MinValue until it does.
        /// </summary>
        public DateTime LastGrowthUtc { get; private set; } = DateTime.MinValue;

        /// <summary>
        /// The file has been deleted or moved away.
        /// </summary>
        public bool IsGone { get; private set; }

        /// <summary>
        /// Adds the complete lines written since the last call to <paramref name="lines"/> and returns how many.
        /// Lines lose their line break and the U+FEFF that EVE writes at the start of each chat line. A line still
        /// being written stays buffered until its line break arrives. A file that gets shorter is followed again
        /// from its new end. The file stays open between calls, so a call that finds nothing new costs one size query.
        /// </summary>
        public int ReadNewLines(List<string> lines, DateTime nowUtc)
        {
            try
            {
                stream ??= new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1, FileOptions.SequentialScan);
                long length = stream.Length;

                if(length < readPosition)
                {
                    // truncated or replaced: carry on from its current end
                    readPosition = length;
                    buffered = 0;
                    checkLineStart = true;
                    CloseStream();
                    return 0;
                }

                if(length == readPosition)
                {
                    return 0;
                }

                if(encoding == null)
                {
                    DetectEncoding();
                }

                if(checkLineStart)
                {
                    atLineStart = readPosition <= bomBytes || PrecededByLineFeed();
                    checkLineStart = false;
                }

                buffer ??= new byte[ChunkBytes];
                int added = 0;
                stream.Position = readPosition;
                while(true)
                {
                    if(buffered == buffer.Length)
                    {
                        Array.Resize(ref buffer, buffer.Length * 2);
                    }

                    int read = stream.Read(buffer, buffered, buffer.Length - buffered);
                    if(read <= 0)
                    {
                        break;
                    }

                    readPosition += read;
                    buffered += read;
                    added += TakeCompleteLines(lines);
                }

                LastGrowthUtc = nowUtc;
                return added;
            }
            catch(Exception exception) when(exception is FileNotFoundException || exception is DirectoryNotFoundException)
            {
                IsGone = true;
                CloseStream();
                return 0;
            }
            catch(IOException)
            {
                // locked or briefly unavailable: try again on the next poll
                CloseStream();
                return 0;
            }
        }

        public void Dispose()
        {
            CloseStream();
        }

        /// <summary>
        /// Reads the header that starts every EVE log: the listener (the character whose client writes the file) and
        /// the offset just after its closing line of dashes. False if the header is missing or not complete yet.
        /// </summary>
        public static bool TryReadHeader(string path, out string listener, out long headerEnd)
        {
            listener = null;
            headerEnd = 0;
            byte[] head = ReadBytes(path, 0, 8192);
            Encoding fileEncoding = DetectEncoding(head, out int bom, out int unit, out bool isBigEndian);
            int dashLines = 0;
            int start = bom;
            for(int i = bom; i + unit <= head.Length; i += unit)
            {
                if(!IsLineFeed(head, i, unit, isBigEndian))
                {
                    continue;
                }

                string line = CleanLine(fileEncoding.GetString(head, start, i - start)).Trim();
                start = i + unit;
                if(line.StartsWith("Listener:", StringComparison.Ordinal))
                {
                    listener = line.Substring("Listener:".Length).Trim();
                }
                else if(line.StartsWith("---", StringComparison.Ordinal) && ++dashLines == 2)
                {
                    headerEnd = start;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The complete lines between two offsets, e.g. the part of a file written before it was followed.
        /// </summary>
        public static List<string> ReadLines(string path, long from, long to)
        {
            List<string> lines = new List<string>();
            if(to <= from)
            {
                return lines;
            }

            byte[] bytes = ReadBytes(path, 0, (int)Math.Min(to, int.MaxValue));
            Encoding fileEncoding = DetectEncoding(bytes, out int bom, out int unit, out bool isBigEndian);
            int start = (int)Math.Max(from, bom);
            if(unit == 2 && (start & 1) == 1)
            {
                start++;
            }

            for(int i = start; i + unit <= bytes.Length; i += unit)
            {
                if(IsLineFeed(bytes, i, unit, isBigEndian))
                {
                    lines.Add(CleanLine(fileEncoding.GetString(bytes, start, i - start)));
                    start = i + unit;
                }
            }

            return lines;
        }

        /// <summary>
        /// The session start in an EVE log file name ("Channel_20261108_123519_1117005149.txt" or
        /// "20261106_213457_1117005149.txt"), in UTC, or null if the name doesn't carry one.
        /// </summary>
        public static DateTime? SessionStartFromFileName(string fileName)
        {
            string[] parts = Path.GetFileNameWithoutExtension(fileName).Split('_');
            for(int i = parts.Length - 2; i >= 0; i--)
            {
                if(parts[i].Length == 8 && parts[i + 1].Length == 6 &&
                   DateTime.TryParseExact(parts[i] + parts[i + 1], "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                                          DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime started))
                {
                    return started;
                }
            }

            return null;
        }

        private int TakeCompleteLines(List<string> lines)
        {
            int added = 0;
            int start = 0;
            for(int i = 0; i + unitBytes <= buffered; i += unitBytes)
            {
                if(!IsLineFeed(buffer, i, unitBytes, bigEndian))
                {
                    continue;
                }

                if(atLineStart)
                {
                    string line = CleanLine(encoding.GetString(buffer, start, i - start));
                    if(!headerPending)
                    {
                        lines.Add(line);
                        added++;
                    }
                    else
                    {
                        SkipHeaderLine(line);
                    }
                }

                // a tail that started mid-line drops that partial first line
                atLineStart = true;
                start = i + unitBytes;
            }

            if(start > 0)
            {
                Buffer.BlockCopy(buffer, start, buffer, 0, buffered - start);
                buffered -= start;
            }

            return added;
        }

        private void SkipHeaderLine(string line)
        {
            string trimmed = line.Trim();
            if(trimmed.StartsWith("Listener:", StringComparison.Ordinal))
            {
                Listener = trimmed.Substring("Listener:".Length).Trim();
            }
            else if(trimmed.StartsWith("---", StringComparison.Ordinal) && ++headerDashLines == 2)
            {
                headerPending = false;
            }
        }

        private void DetectEncoding()
        {
            byte[] head = new byte[4];
            stream.Position = 0;
            int read = stream.Read(head, 0, head.Length);
            encoding = DetectEncoding(head.AsSpan(0, read).ToArray(), out bomBytes, out unitBytes, out bigEndian);

            if(readPosition < bomBytes)
            {
                readPosition = bomBytes;
            }

            if(unitBytes == 2 && (readPosition & 1) == 1)
            {
                // never start inside a UTF-16 code unit; the partial line is dropped
                readPosition--;
                checkLineStart = true;
            }
        }

        private bool PrecededByLineFeed()
        {
            byte[] previous = new byte[unitBytes];
            stream.Position = readPosition - unitBytes;
            return stream.Read(previous, 0, unitBytes) == unitBytes && IsLineFeed(previous, 0, unitBytes, bigEndian);
        }

        private void CloseStream()
        {
            stream?.Dispose();
            stream = null;
        }

        private static bool IsLineFeed(byte[] bytes, int index, int unit, bool isBigEndian)
        {
            if(unit == 1)
            {
                return bytes[index] == 0x0A;
            }

            return isBigEndian ? bytes[index] == 0 && bytes[index + 1] == 0x0A : bytes[index] == 0x0A && bytes[index + 1] == 0;
        }

        private static string CleanLine(string line)
        {
            if(line.Length > 0 && line[line.Length - 1] == '\r')
            {
                line = line.Substring(0, line.Length - 1);
            }

            if(line.Length > 0 && line[0] == '﻿')
            {
                line = line.Substring(1);
            }

            return line;
        }

        private static Encoding DetectEncoding(byte[] head, out int bom, out int unit, out bool isBigEndian)
        {
            isBigEndian = false;
            if(head.Length >= 2 && head[0] == 0xFF && head[1] == 0xFE)
            {
                bom = 2;
                unit = 2;
                return Encoding.Unicode;
            }

            if(head.Length >= 2 && head[0] == 0xFE && head[1] == 0xFF)
            {
                bom = 2;
                unit = 2;
                isBigEndian = true;
                return Encoding.BigEndianUnicode;
            }

            unit = 1;
            bom = head.Length >= 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF ? 3 : 0;
            return Encoding.UTF8;
        }

        private static byte[] ReadBytes(string path, long from, int count)
        {
            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1, FileOptions.SequentialScan);
            fs.Position = from;
            byte[] bytes = new byte[(int)Math.Max(0, Math.Min(count, fs.Length - from))];
            int total = 0;
            while(total < bytes.Length)
            {
                int read = fs.Read(bytes, total, bytes.Length - total);
                if(read <= 0)
                {
                    break;
                }

                total += read;
            }

            return total == bytes.Length ? bytes : bytes.AsSpan(0, total).ToArray();
        }
    }

    /// <summary>
    /// Follows the session log files in one EVE log folder (Chatlogs or Gamelogs). A name-only scan once a second
    /// finds new files; every poll reads what was appended to the files it follows, each kept open. Files that
    /// existed when it started are followed from their end; files that appear later from just after their header.
    /// </summary>
    public sealed class LogFolderMonitor : IDisposable
    {
        private readonly Dictionary<string, LogFileTail> tails = new Dictionary<string, LogFileTail>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> tooOld = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Func<string, bool> follow;
        private readonly List<string> lines = new List<string>();
        private DateTime nextScanUtc = DateTime.MinValue;
        private bool firstScanDone;

        /// <param name="folder">The log folder.</param>
        /// <param name="follow">Which file names to follow; asked again on every scan until it says yes, so filters may change.</param>
        public LogFolderMonitor(string folder, Func<string, bool> follow)
        {
            Folder = folder;
            this.follow = follow;
        }

        public string Folder { get; }

        public TimeSpan ScanInterval { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// EVE starts new log files at every login and the daily downtime logs everyone out, so a file whose name is
        /// older than this can't be written any more and is never followed.
        /// </summary>
        public TimeSpan MaxSessionAge { get; set; } = TimeSpan.FromHours(26);

        public IReadOnlyCollection<LogFileTail> Files => tails.Values;

        /// <summary>
        /// Scans for new files when due and calls <paramref name="onNewLines"/> for every followed file that grew.
        /// The list passed in is reused: copy what you keep.
        /// </summary>
        public void Poll(DateTime nowUtc, Action<LogFileTail, List<string>> onNewLines)
        {
            if(nowUtc >= nextScanUtc)
            {
                Scan(nowUtc);
                nextScanUtc = nowUtc + ScanInterval;
            }

            List<string> retired = null;
            foreach(LogFileTail tail in tails.Values)
            {
                lines.Clear();
                if(tail.ReadNewLines(lines, nowUtc) > 0)
                {
                    onNewLines(tail, lines);
                }

                if(tail.IsGone)
                {
                    (retired ??= new List<string>()).Add(tail.FileName);
                }
            }

            if(retired != null)
            {
                foreach(string name in retired)
                {
                    tails[name].Dispose();
                    tails.Remove(name);
                }
            }
        }

        public void Dispose()
        {
            foreach(LogFileTail tail in tails.Values)
            {
                tail.Dispose();
            }

            tails.Clear();
        }

        private void Scan(DateTime nowUtc)
        {
            // sessions that have aged out can't be written any more: let go of their handles
            List<string> aged = null;
            foreach(LogFileTail tail in tails.Values)
            {
                DateTime? tailStarted = LogFileTail.SessionStartFromFileName(tail.FileName);
                if(tailStarted.HasValue && nowUtc - tailStarted.Value > MaxSessionAge)
                {
                    (aged ??= new List<string>()).Add(tail.FileName);
                }
            }

            if(aged != null)
            {
                foreach(string name in aged)
                {
                    tails[name].Dispose();
                    tails.Remove(name);
                    tooOld.Add(name);
                }
            }

            IEnumerable<string> paths;
            try
            {
                paths = Directory.EnumerateFiles(Folder, "*.txt");
            }
            catch(Exception exception) when(exception is IOException || exception is UnauthorizedAccessException)
            {
                return;
            }

            foreach(string path in paths)
            {
                string name = Path.GetFileName(path);
                if(tails.ContainsKey(name) || tooOld.Contains(name))
                {
                    continue;
                }

                DateTime? started = LogFileTail.SessionStartFromFileName(name);
                if(started.HasValue && nowUtc - started.Value > MaxSessionAge)
                {
                    tooOld.Add(name);
                    continue;
                }

                if(!follow(name))
                {
                    continue;
                }

                try
                {
                    // the path query sees the real size even while EVE holds the file open
                    long size = new FileInfo(path).Length;
                    long start = size;
                    if(firstScanDone)
                    {
                        // a file that appeared while running: read from just after its header (or from 0, skipping it)
                        start = LogFileTail.TryReadHeader(path, out _, out long headerEnd) ? headerEnd : 0;
                    }

                    tails[name] = new LogFileTail(path, start);
                }
                catch(IOException)
                {
                    // gone again or locked; the next scan tries again
                }
            }

            firstScanDone = true;
        }
    }
}
