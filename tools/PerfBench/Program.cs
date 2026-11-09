// Two jobs. "bench" times SMT's data loading, navigation and log reader without the UI, from the map data SMT ships
// and from EVE-style logs it writes itself. "compare" sets the results of "SMT.exe --perftest" from two builds side
// by side. Scratch files go to %TEMP%\SmtPerfBench; EveManager is never constructed, since its constructor migrates
// settings and writes under %AppData%\SMT.
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using EVEDataUtils;
using SMT.EVEData;
using EveSystem = SMT.EVEData.System;

internal static class Program
{
    private const string Usage =
        "PerfBench bench [seconds]\n" +
        "    Times SMT's data loading, navigation and log reader without the UI. The log reader is fed\n" +
        "    EVE-style logs for the given number of seconds (20 unless given).\n" +
        "\n" +
        "PerfBench compare <before> <after>\n" +
        "    Compares the results of \"SMT.exe --perftest=<file>.json\" from two builds. Each side is one\n" +
        "    result file, or a folder of result files from several runs of the same build: timings differ\n" +
        "    by up to a quarter from run to run, and several runs show which differences are larger than that.";

    // EVEData copies its data\*.dat files next to the build output
    private static readonly string DataDir = Path.Combine(AppContext.BaseDirectory, "data");
    private static readonly string Work = Path.Combine(Path.GetTempPath(), "SmtPerfBench");

    private static int Main(string[] args)
    {
        // the same number format on every machine
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        if(args.Length == 3 && args[0] == "compare")
        {
            return Compare(args[1], args[2]);
        }

        int seconds = 20;
        if(args.Length >= 1 && args.Length <= 2 && args[0] == "bench" && (args.Length == 1 || int.TryParse(args[1], out seconds)))
        {
            List<EveSystem> systems = StartupData();
            GateJumps(systems);
            LogReader(seconds);
            return 0;
        }

        Console.WriteLine(Usage);
        return 1;
    }

    // ---------------------------------------------------------------- bench: startup data
    // The files MainWindow's constructor has EveManager read, in the same order, then the navigation tables.
    // Run in a fresh process, these are the cold costs SMT pays at startup.
    private static List<EveSystem> StartupData()
    {
        Console.WriteLine("startup data, as a fresh SMT process loads it:");
        Stopwatch total = Stopwatch.StartNew();
        Time("XmlSerializer for the region list", () => new XmlSerializer(typeof(List<MapRegion>)));
        List<MapRegion> regions = Time("MapLayout.dat", () => Serialization.DeserializeFromDisk<List<MapRegion>>(Path.Combine(DataDir, "MapLayout.dat")));
        Time("XmlSerializer for the system list", () => new XmlSerializer(typeof(List<EveSystem>)));
        List<EveSystem> systems = Time("Systems.dat", () => Serialization.DeserializeFromDisk<List<EveSystem>>(Path.Combine(DataDir, "Systems.dat")));
        Time("ShipTypes.dat", () => Serialization.DeserializeFromDisk<SerializableDictionary<string, string>>(Path.Combine(DataDir, "ShipTypes.dat")));
        Time("Navigation.InitNavigation", () =>
        {
            Navigation.InitNavigation(systems, new List<JumpBridge>());
            return 0;
        });
        Console.WriteLine($"  {"total",-36} {total.Elapsed.TotalMilliseconds,9:F1} ms   ({regions.Count} regions, {systems.Count} systems)");
        return systems;
    }

    private static T Time<T>(string label, Func<T> action)
    {
        long allocatedBefore = GC.GetTotalAllocatedBytes(true);
        Stopwatch stopwatch = Stopwatch.StartNew();
        T result = action();
        stopwatch.Stop();
        double allocatedMB = (GC.GetTotalAllocatedBytes(true) - allocatedBefore) / 1048576.0;
        Console.WriteLine($"  {label,-36} {stopwatch.Elapsed.TotalMilliseconds,9:F1} ms {allocatedMB,8:F1} MB allocated");
        return result;
    }

    // ---------------------------------------------------------------- bench: gate jumps
    // Navigation.GetSystemsXJumpsFrom, which works out each character's danger zone (1 to 6 jumps) and the storm areas.
    private static void GateJumps(List<EveSystem> systems)
    {
        Console.WriteLine();
        Console.WriteLine("systems within a number of gate jumps, starting from every 10th system:");
        Navigation.GetSystemsXJumpsFrom(new List<string>(), systems[0].Name, 6);
        for(int jumps = 1; jumps <= 6; jumps++)
        {
            List<double> milliseconds = new List<double>();
            long found = 0;
            for(int i = 0; i < systems.Count; i += 10)
            {
                long start = Stopwatch.GetTimestamp();
                found += Navigation.GetSystemsXJumpsFrom(new List<string>(), systems[i].Name, jumps).Count;
                milliseconds.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            }

            Console.WriteLine($"  {jumps} {(jumps == 1 ? "jump: " : "jumps:")} median {Median(milliseconds):F3} ms, longest {milliseconds.Max():F3} ms, {found / (double)milliseconds.Count:F0} systems on average");
        }
    }

    // ---------------------------------------------------------------- bench: log reader
    // LogFolderMonitor on 8 chat logs and 4 game logs, each written by a writer that holds its file open like the
    // EVE client does and adds a line every 0.5 to 4 s. Each line carries a number, so the delay from writing a line
    // to SMT handling it can be measured; the monitor is polled every 250 ms, as EveManager polls it.
    private static void LogReader(int seconds)
    {
        Console.WriteLine();
        Console.WriteLine("log reader, on logs written the way the EVE client writes them:");
        string folder = Path.Combine(Work, "logs");
        if(Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
        }

        Directory.CreateDirectory(folder);
        string session = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        List<EveLog> writers = new List<EveLog>();
        for(int i = 0; i < 8; i++)
        {
            writers.Add(new EveLog(Path.Combine(folder, $"Intel{i}_{session}_123456789.txt"), chat: true));
        }

        for(int i = 0; i < 4; i++)
        {
            writers.Add(new EveLog(Path.Combine(folder, $"{session}_12345678{i}.txt"), chat: false));
        }

        ConcurrentDictionary<long, long> written = new ConcurrentDictionary<long, long>();
        List<double> delays = new List<double>();
        void Handle(LogFileTail tail, List<string> lines)
        {
            foreach(string line in lines)
            {
                int numberAt = line.LastIndexOf('#');
                if(numberAt >= 0 && long.TryParse(line.AsSpan(numberAt + 1), out long number) && written.TryRemove(number, out long writtenAt))
                {
                    delays.Add(Stopwatch.GetElapsedTime(writtenAt).TotalMilliseconds);
                }
            }
        }

        void Fail(LogFileTail tail, Exception exception)
        {
            Console.WriteLine($"  {tail.FileName} failed: {exception.Message}");
        }

        using(LogFolderMonitor monitor = new LogFolderMonitor(folder, name => true))
        {
            // what following the logs costs while nothing is written: the same clock, so no poll looks for new files
            DateTime now = DateTime.UtcNow;
            monitor.Poll(now, Handle, Fail);
            const int quietPolls = 2000;
            long quietStart = Stopwatch.GetTimestamp();
            for(int i = 0; i < quietPolls; i++)
            {
                monitor.Poll(now, Handle, Fail);
            }

            double quietMicroseconds = Stopwatch.GetElapsedTime(quietStart).TotalMilliseconds * 1000 / quietPolls;
            Console.WriteLine($"  a poll of {monitor.Files.Count} files with nothing new: {quietMicroseconds:F1} microseconds");

            using CancellationTokenSource stop = new CancellationTokenSource();
            Task reader = Task.Run(async () =>
            {
                while(!stop.IsCancellationRequested)
                {
                    monitor.Poll(DateTime.UtcNow, Handle, Fail);
                    try
                    {
                        await Task.Delay(250, stop.Token);
                    }
                    catch(OperationCanceledException)
                    {
                    }
                }
            });

            using Process self = Process.GetCurrentProcess();
            TimeSpan cpuBefore = self.TotalProcessorTime;
            long allocatedBefore = GC.GetTotalAllocatedBytes();
            Random random = new Random(2026);
            int[] nextLineAt = writers.Select(_ => random.Next(500, 4000)).ToArray();
            Stopwatch clock = Stopwatch.StartNew();
            long lineNumber = 0;
            while(clock.Elapsed.TotalSeconds < seconds)
            {
                Thread.Sleep(50);
                for(int w = 0; w < writers.Count; w++)
                {
                    if(clock.ElapsedMilliseconds < nextLineAt[w])
                    {
                        continue;
                    }

                    lineNumber++;
                    written[lineNumber] = Stopwatch.GetTimestamp();
                    writers[w].Line(w < 8 ? $"[ 2026.11.09 12:00:00 ] Pilot > D-PNP9 red +3 #{lineNumber}" : $"[ 2026.11.09 12:00:00 ] (combat) <b>250</b> to Pith Usurper #{lineNumber}");
                    nextLineAt[w] = (int)clock.ElapsedMilliseconds + random.Next(500, 4000);
                }
            }

            // two more polls for the last lines
            Thread.Sleep(600);
            double elapsedSeconds = clock.Elapsed.TotalSeconds;
            self.Refresh();
            double cpuMilliseconds = (self.TotalProcessorTime - cpuBefore).TotalMilliseconds;
            double allocatedMB = (GC.GetTotalAllocatedBytes() - allocatedBefore) / 1048576.0;
            stop.Cancel();
            reader.Wait();

            delays.Sort();
            string delay = delays.Count == 0
                ? "no lines seen"
                : $"delay median {Median(delays):F0} ms, 95% {delays[Math.Min(delays.Count - 1, (int)(0.95 * delays.Count))]:F0} ms, longest {delays[delays.Count - 1]:F0} ms";
            Console.WriteLine($"  {seconds} s of traffic, polled every 250 ms: {lineNumber} lines written, {written.Count} never seen; {delay}");
            Console.WriteLine($"  this process, writers included: {cpuMilliseconds / elapsedSeconds:F1} ms of CPU and {allocatedMB / elapsedSeconds:F2} MB allocated per second");
        }

        foreach(EveLog writer in writers)
        {
            writer.Dispose();
        }

        // what looking for new files costs in a log folder that has collected years of logs (kept for the next run)
        const int oldLogs = 20000;
        string crowded = Path.Combine(Work, "logs-crowded");
        Directory.CreateDirectory(crowded);
        if(Directory.GetFiles(crowded).Length < oldLogs)
        {
            for(int i = 0; i < oldLogs; i++)
            {
                File.WriteAllText(Path.Combine(crowded, $"Corp_20200101_120000_{i}.txt"), "x");
            }
        }

        using(LogFolderMonitor crowdedMonitor = new LogFolderMonitor(crowded, name => true))
        {
            DateTime now = DateTime.UtcNow;
            long start = Stopwatch.GetTimestamp();
            crowdedMonitor.Poll(now, Handle, Fail);
            double first = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            List<double> later = new List<double>();
            for(int i = 0; i < 5; i++)
            {
                now = now.AddSeconds(2);
                start = Stopwatch.GetTimestamp();
                crowdedMonitor.Poll(now, Handle, Fail);
                later.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            }

            Console.WriteLine($"  looking for new files among {oldLogs} old logs: {first:F1} ms the first time, then {Median(later):F1} ms once a second");
        }
    }

    // A log as the EVE client writes it: the file held open with sharing and appended to, starting with a header that
    // names the character; chat logs in UTF-16 with a U+FEFF in front of every line, game logs in UTF-8.
    private sealed class EveLog : IDisposable
    {
        private readonly StreamWriter writer;
        private readonly string lineStart;

        public EveLog(string path, bool chat)
        {
            lineStart = chat ? ((char)0xFEFF).ToString() : string.Empty;
            writer = new StreamWriter(
                new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete),
                chat ? new UnicodeEncoding(false, true) : new UTF8Encoding(false))
            {
                AutoFlush = true,
            };
            Line("---------------------------------------------------------------");
            Line("  Listener:        Bench Pilot");
            Line("---------------------------------------------------------------");
        }

        public void Line(string text)
        {
            writer.Write(lineStart + text + "\r\n");
        }

        public void Dispose()
        {
            writer.Dispose();
        }
    }

    private static double Median(List<double> values)
    {
        List<double> sorted = values.OrderBy(value => value).ToList();
        return sorted[sorted.Count / 2];
    }

    // ---------------------------------------------------------------- compare
    // One line per value in the results. A value that repeats exactly needs no comment. For the rest, several runs per
    // side give a spread: values whose spreads don't overlap have moved, the others haven't. Timings, CPU, allocation
    // and GC counts are expected to wander, so with a single run per side they only count as moved beyond a quarter.
    private static int Compare(string before, string after)
    {
        List<Dictionary<string, object>> beforeRuns = LoadRuns(before);
        List<Dictionary<string, object>> afterRuns = LoadRuns(after);
        if(beforeRuns.Count == 0 || afterRuns.Count == 0)
        {
            Console.WriteLine($"no --perftest result files in {(beforeRuns.Count == 0 ? before : after)}");
            Console.WriteLine();
            Console.WriteLine(Usage);
            return 1;
        }

        Console.WriteLine($"before: {beforeRuns.Count} run(s), {before}");
        Console.WriteLine($"after:  {afterRuns.Count} run(s), {after}");
        Console.WriteLine();
        Console.WriteLine($"{"",-48} {"before",-26} {"after",-26} {"change",7}");

        List<string> keys = new List<string>();
        foreach(string key in beforeRuns.Concat(afterRuns).SelectMany(run => run.Keys))
        {
            if(!keys.Contains(key))
            {
                keys.Add(key);
            }
        }

        int lower = 0;
        int higher = 0;
        int changed = 0;
        foreach(string key in keys)
        {
            List<object> beforeValues = beforeRuns.Where(run => run.ContainsKey(key)).Select(run => run[key]).ToList();
            List<object> afterValues = afterRuns.Where(run => run.ContainsKey(key)).Select(run => run[key]).ToList();
            string left = beforeValues.Count == 0 ? "-" : Shorten(beforeValues[0].ToString());
            string right = afterValues.Count == 0 ? "-" : Shorten(afterValues[0].ToString());
            string change = string.Empty;
            string verdict = string.Empty;

            if(beforeValues.Count == 0 || afterValues.Count == 0)
            {
                verdict = beforeValues.Count == 0 ? "only after" : "only before";
                changed++;
            }
            else if(beforeValues.Concat(afterValues).All(value => value is double))
            {
                List<double> b = beforeValues.Cast<double>().OrderBy(value => value).ToList();
                List<double> a = afterValues.Cast<double>().OrderBy(value => value).ToList();
                left = Spread(b);
                right = Spread(a);
                double beforeMedian = b[b.Count / 2];
                double afterMedian = a[a.Count / 2];
                double ratio = beforeMedian == 0 ? double.NaN : (afterMedian - beforeMedian) / Math.Abs(beforeMedian);
                bool identical = b[0] == a[0] && b[b.Count - 1] == a[a.Count - 1] && b[0] == b[b.Count - 1];
                bool apart = a[0] > b[b.Count - 1] || a[a.Count - 1] < b[0];
                bool singleRuns = b.Count == 1 && a.Count == 1;
                if(!identical)
                {
                    change = double.IsNaN(ratio) ? string.Empty : ratio.ToString("+0%;-0%;0%");
                    if(!apart)
                    {
                        verdict = "within the spread between runs";
                    }
                    else if(!IsMeasurement(key))
                    {
                        verdict = "CHANGED";
                        changed++;
                    }
                    else if(singleRuns && Math.Abs(ratio) < 0.25)
                    {
                        verdict = "one run each: within the usual quarter";
                    }
                    else if(afterMedian < beforeMedian)
                    {
                        verdict = "lower";
                        lower++;
                    }
                    else
                    {
                        verdict = "HIGHER";
                        higher++;
                    }
                }
            }
            else if(beforeValues[0].ToString() != afterValues[0].ToString())
            {
                verdict = "CHANGED" + ListDifference(beforeValues[0].ToString(), afterValues[0].ToString());
                changed++;
            }

            Console.WriteLine($"{key,-48} {left,-26} {right,-26} {change,7}  {verdict}".TrimEnd());
        }

        Console.WriteLine();
        Console.WriteLine($"{changed} values changed, {higher} measurements higher, {lower} lower.");
        if(beforeRuns.Count == 1 || afterRuns.Count == 1)
        {
            Console.WriteLine("With one run on a side the spread between runs is unknown: give folders of several runs to be sure.");
        }

        return 0;
    }

    private static List<Dictionary<string, object>> LoadRuns(string path)
    {
        IEnumerable<string> files = Directory.Exists(path)
            ? Directory.GetFiles(path, "*.json").OrderBy(file => file, StringComparer.Ordinal)
            : File.Exists(path) ? new[] { path } : Array.Empty<string>();

        List<Dictionary<string, object>> runs = new List<Dictionary<string, object>>();
        foreach(string file in files)
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(file));
            if(document.RootElement.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            Dictionary<string, object> values = new Dictionary<string, object>();
            Flatten(document.RootElement, string.Empty, values);
            runs.Add(values);
        }

        return runs;
    }

    // { "regionSwitch": { "medianMs": 61.2 } } becomes "regionSwitch.medianMs" = 61.2; numbers as double, the rest as text
    private static void Flatten(JsonElement element, string prefix, Dictionary<string, object> values)
    {
        foreach(JsonProperty property in element.EnumerateObject())
        {
            string key = prefix.Length == 0 ? property.Name : prefix + "." + property.Name;
            switch(property.Value.ValueKind)
            {
                case JsonValueKind.Object when key == "startupMs":
                    // the marks count from process start: show what each phase took, and the total
                    double previous = 0;
                    foreach(JsonProperty mark in property.Value.EnumerateObject())
                    {
                        values[key + "." + mark.Name] = mark.Value.GetDouble() - previous;
                        previous = mark.Value.GetDouble();
                    }

                    values[key + ".total"] = previous;
                    break;
                case JsonValueKind.Object:
                    Flatten(property.Value, key, values);
                    break;
                case JsonValueKind.Number:
                    values[key] = property.Value.GetDouble();
                    break;
                case JsonValueKind.String:
                    values[key] = property.Value.GetString();
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    values[key] = property.Value.GetBoolean() ? "yes" : "no";
                    break;
            }
        }
    }

    // timings, CPU, allocation, GC counts and the redraws the 2 s timer adds wander from run to run; anything else
    // (redraws per action, elements on the map, what was found) should repeat exactly
    private static bool IsMeasurement(string key)
    {
        string name = key.Substring(key.LastIndexOf('.') + 1);
        return key.StartsWith("startupMs.", StringComparison.Ordinal) ||
               name.Contains("Ms", StringComparison.Ordinal) ||
               name.Contains("MB", StringComparison.Ordinal) ||
               name.StartsWith("gcGen", StringComparison.Ordinal) ||
               name.StartsWith("partialRedraws", StringComparison.Ordinal);
    }

    // "61.2", or "61.2 (58.9..66)" for several runs that differ: the median and the lowest and highest
    private static string Spread(List<double> sorted)
    {
        string median = sorted[sorted.Count / 2].ToString("0.###");
        return sorted[0] == sorted[sorted.Count - 1] ? median : $"{median} ({sorted[0]:0.###}..{sorted[sorted.Count - 1]:0.###})";
    }

    private static string Shorten(string text)
    {
        return text.Length <= 26 ? text : text.Substring(0, 23) + "...";
    }

    // for two space-separated lists of the same length (the hashes of the system names, the popup sizes): how many entries differ
    private static string ListDifference(string before, string after)
    {
        string[] b = before.Split(' ');
        string[] a = after.Split(' ');
        if(b.Length < 2 || b.Length != a.Length)
        {
            return string.Empty;
        }

        return $": {Enumerable.Range(0, b.Length).Count(i => b[i] != a[i])} of {b.Length} differ";
    }
}
