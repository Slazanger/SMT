// Headless measurements of SMT hot paths. Side-effect free: never runs the EveManager
// constructor (which migrates settings and writes under %AppData%\SMT).
using System.Buffers;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using EVEDataUtils;
using SMT.EVEData;
using EveSystem = SMT.EVEData.System;

internal static class Program
{
    // EVEData copies its data\*.dat files next to the build output.
    private static readonly string DataDir = Path.Combine(AppContext.BaseDirectory, "data");
    // Scratch files (synthetic logs, fake chat-log folders) go to %TEMP%, never into the repo or %AppData%\SMT.
    private static string Work;

    private static int Main(string[] args)
    {
        Work = Path.Combine(Path.GetTempPath(), "SmtPerfBench");
        Directory.CreateDirectory(Work);
        string mode = args.Length > 0 ? args[0] : "help";
        switch(mode)
        {
            case "startup": Startup(); break;
            case "nav-alt": NavAlternatives(); break;
            case "json-export": JsonExport(); break;
            case "json-load": JsonLoad(); break;
            case "logs": Logs(); break;
            case "dirscan": DirScan(args.Length > 1 ? int.Parse(args[1]) : 20000); break;
            case "intel": Intel(); break;
            case "zkb": Zkb(); break;
            case "fsw-quiet": FswQuiet(args.Length > 1 ? int.Parse(args[1]) : 60); break;
            case "universe-gates": UniverseGates(); break;
            case "nav-check": NavCheck(args[1]); break;
            case "zkb-check": ZkbCheck(); break;
            default: Console.WriteLine("modes: startup nav-alt nav-check <baselineDir> json-export json-load logs dirscan [n] intel zkb fsw-quiet [thresholdSeconds] universe-gates"); return 1;
        }
        return 0;
    }

    // ---------------------------------------------------------------- helpers
    private static T Time<T>(string label, Func<T> f)
    {
        long alloc0 = GC.GetTotalAllocatedBytes(true);
        var sw = Stopwatch.StartNew();
        T r = f();
        sw.Stop();
        long alloc = GC.GetTotalAllocatedBytes(true) - alloc0;
        Console.WriteLine($"  {label,-62} {sw.Elapsed.TotalMilliseconds,9:F1} ms {alloc / 1048576.0,9:F1} MB alloc");
        return r;
    }

    private static double Median(List<double> xs)
    {
        xs.Sort();
        return xs[xs.Count / 2];
    }

    private static (double ms, double allocKb) Bench(Action a, int iters, int warmup = 3)
    {
        for(int i = 0; i < warmup; i++) a();
        var times = new List<double>();
        long alloc0 = GC.GetAllocatedBytesForCurrentThread();
        for(int i = 0; i < iters; i++)
        {
            var sw = Stopwatch.StartNew();
            a();
            sw.Stop();
            times.Add(sw.Elapsed.TotalMilliseconds);
        }
        long alloc = GC.GetAllocatedBytesForCurrentThread() - alloc0;
        return (Median(times), alloc / 1024.0 / iters);
    }

    private static List<EveSystem> LoadSystems() =>
        Serialization.DeserializeFromDisk<List<EveSystem>>(Path.Combine(DataDir, "Systems.dat"));

    private static List<MapRegion> LoadRegions() =>
        Serialization.DeserializeFromDisk<List<MapRegion>>(Path.Combine(DataDir, "MapLayout.dat"));

    private static void InstallFakeEveManager(List<EveSystem> systems)
    {
        // EveManager without running its constructor; only NameToSystem is needed by
        // GetRangeBetweenSystems, which Navigation.InitNavigation calls per cached pair.
        var em = (EveManager)RuntimeHelpers.GetUninitializedObject(typeof(EveManager));
        var nts = new Dictionary<string, EveSystem>();
        foreach(EveSystem s in systems) nts[s.Name] = s;
        typeof(EveManager).GetField("<NameToSystem>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(em, nts);
        EveManager.Instance = em;
    }

    // ---------------------------------------------------------------- startup
    // Same order as MainWindow ctor -> EveManager.LoadFromDisk -> InitNavigation. Run in a fresh process.
    private static void Startup()
    {
        Console.WriteLine("startup data path (cold process):");
        var total = Stopwatch.StartNew();
        Time("XmlSerializer ctor List<MapRegion> (codegen)", () => new XmlSerializer(typeof(List<MapRegion>)));
        List<MapRegion> regions = Time("Deserialize MapLayout.dat (9.7 MB XML)", LoadRegions);
        Time("XmlSerializer ctor List<System> (codegen)", () => new XmlSerializer(typeof(List<EveSystem>)));
        List<EveSystem> systems = Time("Deserialize Systems.dat (5.0 MB XML)", LoadSystems);
        Time("Deserialize ShipTypes.dat", () => Serialization.DeserializeFromDisk<SerializableDictionary<string, string>>(Path.Combine(DataDir, "ShipTypes.dat")));
        // Before 2026-11-09 this also parsed JumpRangeCache.dat (35 MB XML, ~750 ms) and InitNavigation
        // recomputed its 1.03M distances in decimal (~780 ms); see docs/performance-research.md.
        InstallFakeEveManager(systems);
        Time("Navigation.InitNavigation (runtime jump table)", () => { Navigation.InitNavigation(systems, new List<JumpBridge>()); return 0; });
        Console.WriteLine($"  {"TOTAL",-62} {total.Elapsed.TotalMilliseconds,9:F1} ms   regions={regions.Count} systems={systems.Count}");
    }

    // ---------------------------------------------------------------- jump range alternatives
    private static void NavAlternatives()
    {
        List<EveSystem> systems = LoadSystems();
        InstallFakeEveManager(systems);

        Console.WriteLine("jump-range table (warm process):");
        for(int i = 0; i < 3; i++)
            Time("Navigation.InitNavigation (runtime jump table)", () => { Navigation.InitNavigation(systems, new List<JumpBridge>()); return 0; });

        // distance maths only: the same pairs, decimal+DecimalEx.Sqrt (what the old InitNavigation did per cached pair) vs double
        var table = BuildJumpTableGrid(systems, 10.0);
        var pairs = new List<(EveSystem a, EveSystem b)>();
        var byName = systems.ToDictionary(s => s.Name);
        foreach(var kv in table) foreach(var t in kv.Value) pairs.Add((byName[kv.Key], byName[t.sys]));

        Time($"decimal GetRangeBetweenSystems x {pairs.Count:N0}", () =>
        {
            decimal acc = 0;
            foreach(var p in pairs) acc += EveManager.Instance.GetRangeBetweenSystems(p.a.Name, p.b.Name);
            return acc;
        });
        Time($"double Math.Sqrt on cached doubles x {pairs.Count:N0}", () =>
        {
            double acc = 0;
            foreach(var p in pairs)
            {
                double dx = (double)p.a.ActualX - (double)p.b.ActualX, dy = (double)p.a.ActualY - (double)p.b.ActualY, dz = (double)p.a.ActualZ - (double)p.b.ActualZ;
                acc += Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
            return acc;
        });

    }

    private static Dictionary<string, List<(string sys, double ly)>> BuildJumpTableGrid(List<EveSystem> systems, double maxLy)
    {
        int n = systems.Count;
        var x = new double[n]; var y = new double[n]; var z = new double[n];
        var cells = new Dictionary<(int, int, int), List<int>>();
        for(int i = 0; i < n; i++)
        {
            EveSystem s = systems[i];
            x[i] = (double)s.ActualX; y[i] = (double)s.ActualY; z[i] = (double)s.ActualZ;
            // same target rules as Navigation.CreateStaticNavigationCache
            if(s.TrueSec > 0.45 || s.Region == "Pochven" || s.Name == "Zarzakh") continue;
            var key = ((int)Math.Floor(x[i] / maxLy), (int)Math.Floor(y[i] / maxLy), (int)Math.Floor(z[i] / maxLy));
            if(!cells.TryGetValue(key, out var list)) cells[key] = list = new List<int>();
            list.Add(i);
        }
        double max2 = maxLy * maxLy;
        var result = new Dictionary<string, List<(string, double)>>(n);
        for(int i = 0; i < n; i++)
        {
            int cx = (int)Math.Floor(x[i] / maxLy), cy = (int)Math.Floor(y[i] / maxLy), cz = (int)Math.Floor(z[i] / maxLy);
            List<(string, double)> links = null;
            for(int dx = -1; dx <= 1; dx++)
            for(int dy = -1; dy <= 1; dy++)
            for(int dz = -1; dz <= 1; dz++)
            {
                if(!cells.TryGetValue((cx + dx, cy + dy, cz + dz), out var cand)) continue;
                foreach(int j in cand)
                {
                    if(j == i) continue;
                    double ddx = x[i] - x[j], ddy = y[i] - y[j], ddz = z[i] - z[j];
                    double d2 = ddx * ddx + ddy * ddy + ddz * ddz;
                    if(d2 < max2 && d2 > 0)
                    {
                        (links ??= new List<(string, double)>()).Add((systems[j].Name, Math.Sqrt(d2)));
                    }
                }
            }
            if(links != null) result[systems[i].Name] = links;
        }
        return result;
    }

    // ---------------------------------------------------------------- System.Text.Json comparison
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        IncludeFields = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    private static void JsonExport()
    {
        List<EveSystem> systems = LoadSystems();
        List<MapRegion> regions = LoadRegions();
        File.WriteAllBytes(Path.Combine(Work, "Systems.json"), JsonSerializer.SerializeToUtf8Bytes(systems, JsonOpts));
        File.WriteAllBytes(Path.Combine(Work, "MapLayout.json"), JsonSerializer.SerializeToUtf8Bytes(regions, JsonOpts));
        foreach(string f in new[] { "Systems.json", "MapLayout.json" })
            Console.WriteLine($"  wrote {f}: {new FileInfo(Path.Combine(Work, f)).Length / 1048576.0:F1} MB");
    }

    private static void JsonLoad()
    {
        Console.WriteLine("System.Text.Json load of the same data (cold process, reflection-based, no source generator):");
        var total = Stopwatch.StartNew();
        var regions = Time("Deserialize MapLayout.json", () => JsonSerializer.Deserialize<List<MapRegion>>(File.ReadAllBytes(Path.Combine(Work, "MapLayout.json")), JsonOpts));
        var systems = Time("Deserialize Systems.json", () => JsonSerializer.Deserialize<List<EveSystem>>(File.ReadAllBytes(Path.Combine(Work, "Systems.json")), JsonOpts));
        Console.WriteLine($"  {"TOTAL",-62} {total.Elapsed.TotalMilliseconds,9:F1} ms   regions={regions.Count} systems={systems.Count} cells={regions.Sum(r => r.MapSystems.Values.Sum(m => m.CellPoints?.Count ?? 0))}");
    }

    // ---------------------------------------------------------------- game log tailing
    private static readonly string[] Victims = { "Dread Guristas Eliminator", "Pith Eradicator", "Dread Guristas Exterminator", "Pith Usurper", "Guristas Silencer" };

    private static string FakeCombatLine(Random r, DateTime t) =>
        $"[ {t:yyyy.MM.dd HH:mm:ss} ] (combat) <color=0xff00ffff><b>{r.Next(100, 900)}</b> <color=0x77ffffff><font size=10>to</font> <b><color=0xffffffff>{Victims[r.Next(Victims.Length)]}</b><font size=10><color=0x77ffffff> - Dread Guristas Large Smartbomb - Hits</font>";

    private static string MakeGameLog(string path, long targetBytes)
    {
        var r = new Random(42);
        var sb = new StringBuilder();
        sb.Append("------------------------------------------------------------\r\n  Gamelog\r\n  Listener: Bench Pilot\r\n  Session Started: 2026.11.06 21:34:57\r\n------------------------------------------------------------\r\n");
        DateTime t = new DateTime(2026, 11, 6, 21, 35, 0);
        while(sb.Length < targetBytes)
        {
            sb.Append(FakeCombatLine(r, t)).Append("\r\n");
            t = t.AddMilliseconds(500);
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        return path;
    }

    private static int CountLines(string path)
    {
        int n = 0;
        using var sr = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        while(sr.ReadLine() != null) n++;
        return n;
    }

    // Faithful replica of EveManager.ProcessGameLogFileChange for an already-seen file:
    // sniff BOM (open #1), reopen, skip fileReadFrom lines, parse the rest.
    private static int SmtGameLogEvent(string path, int fileReadFrom)
    {
        Encoding fe = Misc.GetEncoding(path);
        using var ifs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var file = new StreamReader(ifs, fe);
        for(int i = 0; i < fileReadFrom; i++) file.ReadLine();
        int parsed = 0;
        string line = file.ReadLine();
        while(line != null)
        {
            if(line != "" && line.StartsWith("["))
            {
                int typeStartPos = line.IndexOf("(") + 1;
                int typeEndPos = line.IndexOf(")");
                if(typeStartPos >= 1 && typeEndPos >= 1)
                {
                    string type = line.Substring(typeStartPos, typeEndPos - typeStartPos);
                    string text = Regex.Replace(line.Substring(typeEndPos + 1), "<.*?>", string.Empty);
                    if(type.Length + text.Length > 0) parsed++;
                }
            }
            line = file.ReadLine();
        }
        return parsed;
    }

    // First sight of a file: SMT reads it all to count lines, seeks back, then skips them again.
    private static int SmtGameLogFirstSight(string path)
    {
        Encoding fe = Misc.GetEncoding(path);
        using var ifs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var file = new StreamReader(ifs, fe);
        int fileReadFrom = 0;
        while(!file.EndOfStream) { file.ReadLine(); fileReadFrom++; }
        fileReadFrom--;
        file.BaseStream.Seek(0, SeekOrigin.Begin);
        file.DiscardBufferedData();
        for(int i = 0; i < fileReadFrom; i++) file.ReadLine();
        int n = 0;
        while(file.ReadLine() != null) n++;
        return n;
    }

    private static readonly Regex TagRegex = new("<[^>]*>", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // Byte-offset tailer: open, seek to the stored offset, read only the appended bytes,
    // decode with a persistent Decoder, split on '\n' keeping a partial tail.
    private sealed class Tail
    {
        public long Offset;
        public readonly Decoder Decoder = new UTF8Encoding(false).GetDecoder();
        public readonly StringBuilder Partial = new();
    }

    private static int TailEvent(string path, Tail st)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1, FileOptions.SequentialScan);
        long len = fs.Length;
        if(len < st.Offset) { st.Offset = 0; st.Partial.Clear(); st.Decoder.Reset(); }
        if(len == st.Offset) return 0;
        fs.Seek(st.Offset, SeekOrigin.Begin);
        int toRead = (int)Math.Min(len - st.Offset, 1 << 20);
        byte[] buf = ArrayPool<byte>.Shared.Rent(toRead);
        char[] chars = ArrayPool<char>.Shared.Rent(toRead + 2);
        int parsed = 0;
        try
        {
            int read = fs.Read(buf, 0, toRead);
            st.Offset += read;
            int nChars = st.Decoder.GetChars(buf, 0, read, chars, 0, flush: false);
            int start = 0;
            for(int i = 0; i < nChars; i++)
            {
                if(chars[i] != '\n') continue;
                int end = i > start && chars[i - 1] == '\r' ? i - 1 : i;
                string line;
                if(st.Partial.Length > 0) { st.Partial.Append(chars, start, end - start); line = st.Partial.ToString(); st.Partial.Clear(); }
                else line = new string(chars, start, end - start);
                start = i + 1;
                if(line.Length > 0 && line[0] == '[')
                {
                    int ts = line.IndexOf('(') + 1, te = line.IndexOf(')');
                    if(ts >= 1 && te >= 1)
                    {
                        string text = TagRegex.Replace(line.AsSpan(te + 1).ToString(), string.Empty);
                        if(text.Length >= 0) parsed++;
                    }
                }
            }
            if(start < nChars) st.Partial.Append(chars, start, nChars - start);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buf);
            ArrayPool<char>.Shared.Return(chars);
        }
        return parsed;
    }

    private static void Logs()
    {
        Console.WriteLine("game log, cost of ONE change event that appends 10 lines (median of 15):");
        Console.WriteLine($"  {"file size",-10} {"SMT re-read (ms / KB alloc)",-32} {"byte-offset tail (ms / KB alloc)",-34} {"SMT first sight (ms)"}");
        var r = new Random(7);
        foreach(double mb in new[] { 0.5, 1.0, 2.0, 4.0 })
        {
            string path = MakeGameLog(Path.Combine(Work, $"gamelog_{mb}MB.txt"), (long)(mb * 1048576));
            // append 10 lines so both readers see exactly the same new content
            int linesBefore = CountLines(path);
            long bytesBefore = new FileInfo(path).Length;
            var extra = new StringBuilder();
            for(int i = 0; i < 10; i++) extra.Append(FakeCombatLine(r, DateTime.Now)).Append("\r\n");
            File.AppendAllText(path, extra.ToString(), new UTF8Encoding(false));

            var smt = Bench(() => SmtGameLogEvent(path, linesBefore - 1), 15);
            var tail = Bench(() =>
            {
                var st = new Tail { Offset = bytesBefore };
                TailEvent(path, st);
            }, 15);
            var first = Bench(() => SmtGameLogFirstSight(path), 5, 1);
            Console.WriteLine($"  {mb,6:F1} MB  {smt.ms,8:F2} ms / {smt.allocKb,9:N0} KB        {tail.ms,8:F3} ms / {tail.allocKb,7:N0} KB          {first.ms,8:F1}");
            int check = SmtGameLogEvent(path, linesBefore - 1);
            int check2 = TailEvent(path, new Tail { Offset = bytesBefore });
            if(check != check2) Console.WriteLine($"    (parsed lines differ: smt={check} tail={check2})");
        }

        // whole-session model: a file growing to 4 MB at ~200 bytes per event (one line per write)
        Console.WriteLine();
        Console.WriteLine("whole-session model: file grows 0 -> 4 MB, one change event per ~2 lines appended:");
        string sess = Path.Combine(Work, "session.txt");
        MakeGameLog(sess, 0);
        var lines = new List<string>();
        var rr = new Random(1);
        long target = 4L * 1048576;
        long size = new FileInfo(sess).Length;
        while(size < target)
        {
            string l = FakeCombatLine(rr, DateTime.Now) + "\r\n";
            lines.Add(l);
            size += Encoding.UTF8.GetByteCount(l);
        }
        // sample the SMT cost at 20 points along the session and integrate
        int events = lines.Count / 2;
        double smtTotal = 0, tailTotal = 0;
        int samples = 20;
        using(var w = new StreamWriter(new FileStream(sess, FileMode.Append, FileAccess.Write, FileShare.ReadWrite), new UTF8Encoding(false)))
        {
            int written = 0;
            for(int sIdx = 1; sIdx <= samples; sIdx++)
            {
                int upTo = lines.Count * sIdx / samples;
                for(; written < upTo; written++) w.Write(lines[written]);
                w.Flush();
                int linesNow = CountLines(sess);
                // the event delivers the last two lines
                long lastTwo = Encoding.UTF8.GetByteCount(lines[written - 1]) + Encoding.UTF8.GetByteCount(lines[written - 2]);
                long offset = new FileInfo(sess).Length - lastTwo;
                var smt = Bench(() => SmtGameLogEvent(sess, linesNow - 2), 5, 1);
                var tail = Bench(() => { var st = new Tail { Offset = offset }; TailEvent(sess, st); }, 5, 1);
                smtTotal += smt.ms * (events / (double)samples);
                tailTotal += tail.ms * (events / (double)samples);
            }
        }
        Console.WriteLine($"  lines={lines.Count:N0} events={events:N0}: SMT total ~ {smtTotal / 1000:F1} s CPU, byte-offset tail ~ {tailTotal / 1000:F2} s CPU");
    }

    // ---------------------------------------------------------------- the 1.5 s "SUPER HACK" directory scan
    private static void DirScan(int nFiles)
    {
        string dir = Path.Combine(Work, $"chatlogs_{nFiles}");
        if(!Directory.Exists(dir) || Directory.GetFiles(dir).Length < nFiles)
        {
            Directory.CreateDirectory(dir);
            var old = DateTime.UtcNow.AddDays(-30);
            for(int i = 0; i < nFiles; i++)
            {
                string f = Path.Combine(dir, $"Corp_2026{i % 12 + 1:00}01_{i:000000}_123456789.txt");
                File.WriteAllText(f, "x");
                File.SetLastWriteTimeUtc(f, old);
            }
        }
        var res = Bench(() =>
        {
            DateTime recentThreshold = DateTime.UtcNow.AddMinutes(-10);
            int n = new DirectoryInfo(dir).EnumerateFiles("*.txt").Where(file => file.LastWriteTimeUtc >= recentThreshold).Count();
            if(n < 0) Console.WriteLine();
        }, 10, 2);
        Console.WriteLine($"one LogFileCacheTriggerAsync folder pass over {nFiles:N0} old files: {res.ms:F1} ms, {res.allocKb:N0} KB alloc (runs every 1.5 s per folder)");
    }

    // ---------------------------------------------------------------- FSW + touch loop on a quiet file
    // Replicates SMT's FileSystemWatcher (LastWrite|Size) plus the LogFileCacheTriggerAsync touch loop
    // (every 1.5 s, open+seek+close files whose ENUMERATED LastWriteTimeUtc is newer than a threshold;
    // SMT uses 10 min, scaled down here). A writer holds the file open like the EVE client does.
    private static void FswQuiet(int thresholdSeconds)
    {
        string dir = Path.Combine(Work, "fswtest");
        if(Directory.Exists(dir)) Directory.Delete(dir, true);
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "Intel_20261109_120000_123456789.txt");
        var t0 = Stopwatch.StartNew();
        string Ts() => $"{t0.Elapsed.TotalSeconds,7:F1}s";

        int fswEvents = 0;
        using var fsw = new FileSystemWatcher(dir) { Filter = "*.txt", NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size, InternalBufferSize = 32 * 1024 };
        fsw.Changed += (s, e) => { Interlocked.Increment(ref fswEvents); Console.WriteLine($"{Ts()}   FSW Changed {e.Name}"); };
        fsw.EnableRaisingEvents = true;

        var writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete), new UnicodeEncoding(false, true)) { AutoFlush = true };
        void Write(string what)
        {
            writer.WriteLine($"[ 2026.11.09 12:00:00 ] Pilot > {what}");
            var fi = new FileInfo(path); // path query
            var en = new DirectoryInfo(dir).EnumerateFiles("*.txt").First(); // directory enumeration, as the touch loop sees it
            long handleLen;
            using(var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) handleLen = fs.Length;
            Console.WriteLine($"{Ts()} WRITE '{what}': handle len={handleLen}  enumerated len={en.Length} lastWrite age={(DateTime.UtcNow - en.LastWriteTimeUtc).TotalSeconds:F0}s  path-query len={fi.Length}");
        }
        // note: the handle/path queries in Write() themselves open the file; measure the enumerated view BEFORE them
        void Probe(string label)
        {
            var en = new DirectoryInfo(dir).EnumerateFiles("*.txt").First();
            Console.WriteLine($"{Ts()} probe {label}: enumerated len={en.Length} lastWrite age={(DateTime.UtcNow - en.LastWriteTimeUtc).TotalSeconds:F0}s  FSW events so far={fswEvents}");
        }

        Console.WriteLine($"phase 1: FSW only, no touch loop, writer appends one line");
        writer.WriteLine("[ 2026.11.09 12:00:00 ] Pilot > line 0 (FSW only)");
        Thread.Sleep(3000);
        Probe("3 s after line 0 (no other opener)");

        using var cts = new CancellationTokenSource();
        int touches = 0;
        var touchTask = Task.Run(async () =>
        {
            while(!cts.IsCancellationRequested)
            {
                DateTime recent = DateTime.UtcNow.AddSeconds(-thresholdSeconds);
                foreach(FileInfo f in new DirectoryInfo(dir).EnumerateFiles("*.txt").Where(f => f.LastWriteTimeUtc >= recent))
                {
                    using var ifs = new FileStream(f.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    ifs.Seek(0, SeekOrigin.End);
                    touches++;
                }
                try { await Task.Delay(1500, cts.Token); } catch(OperationCanceledException) { }
            }
        });

        Console.WriteLine($"phase 2: touch loop on (threshold {thresholdSeconds}s)");
        Thread.Sleep(4000);
        Probe("after 4 s of touch loop");
        int before = fswEvents;
        writer.WriteLine("[ 2026.11.09 12:00:01 ] Pilot > line 1 (active channel)");
        Thread.Sleep(4000);
        Console.WriteLine($"{Ts()} line 1: FSW events during 4 s = {fswEvents - before}, touches so far={touches}");

        int quiet = thresholdSeconds + 30;
        Console.WriteLine($"phase 3: channel quiet for {quiet}s");
        int touchesAtQuietStart = touches;
        for(int s = 0; s < quiet; s += 15)
        {
            Thread.Sleep(15000);
            Console.WriteLine($"{Ts()}   quiet: touches since quiet start={touches - touchesAtQuietStart}");
        }
        Probe("end of quiet period");
        before = fswEvents;
        int touchesBefore = touches;
        writer.WriteLine("[ 2026.11.09 12:15:00 ] Pilot > line 2 (after the quiet period)");
        for(int s = 0; s < 20; s += 5)
        {
            Thread.Sleep(5000);
            Probe($"{s + 5} s after line 2");
        }
        Console.WriteLine($"RESULT: line 2 produced {fswEvents - before} FSW events and {touches - touchesBefore} touches in 20 s");
        cts.Cancel();
        touchTask.Wait();
        long finalLen;
        using(var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) finalLen = fs.Length;
        Console.WriteLine($"{Ts()} handle length now {finalLen} (an open-handle poller sees line 2 regardless)");
        writer.Dispose();
    }

    // ---------------------------------------------------------------- zKillboard filter equivalence
    // Replays random feed activity and compares, for every evaluation the CollectionView would make, the
    // old per-item filter (copy + Where + Take(30) + Contains) with a replica of MainWindow.ZKBFeedViewFilter
    // (cached set, rebuilt when the stream's count/first/last change or after InvalidateZKBVisibleKills).
    private static void ZkbCheck()
    {
        List<MapRegion> regions = LoadRegions();
        List<EveSystem> systems = LoadSystems();
        var r = new Random(99);
        var stream = new List<Kill>(); // index 0 = newest, as KillStream.Insert(0, kill)
        MapRegion region = regions.First(rg => rg.Name == "Delve");
        bool filterByRegion = true;

        bool Passes(Kill k) => !filterByRegion || region.IsSystemOnMap(k.SystemName);
        bool OldFilter(Kill item) => stream.ToList().Where(Passes).Take(30).ToList().Contains(item);

        var visible = new HashSet<Kill>();
        int visibleCount = -1;
        Kill visibleFirst = null, visibleLast = null;
        bool NewFilter(Kill item)
        {
            int count = stream.Count;
            Kill first = count > 0 ? stream[0] : null;
            Kill last = count > 0 ? stream[count - 1] : null;
            if(count != visibleCount || first != visibleFirst || last != visibleLast)
            {
                visible = new HashSet<Kill>(stream.Where(Passes).Take(30));
                visibleCount = count;
                visibleFirst = first;
                visibleLast = last;
            }
            return visible.Contains(item);
        }
        void Invalidate() => visibleCount = -1;

        long evaluations = 0, mismatches = 0;
        void Evaluate(Kill k) { evaluations++; if(OldFilter(k) != NewFilter(k)) mismatches++; }
        void Refresh() { foreach(Kill k in stream.ToList()) Evaluate(k); }

        for(int step = 0; step < 20000; step++)
        {
            int op = r.Next(100);
            if(op < 55)
            {
                // a new kill: the view evaluates just the added item (no invalidation)
                MapRegion where = r.Next(100) < 20 ? region : regions[r.Next(regions.Count)];
                var names = where.MapSystems.Keys.ToList();
                var k = new Kill { SystemName = r.Next(50) == 0 ? null : names[r.Next(names.Count)] };
                stream.Insert(0, k);
                Evaluate(k);
            }
            else if(op < 80)
            {
                // kills expiring from the back: the view drops them without filtering
                for(int n = Math.Min(stream.Count, r.Next(1, 4)); n > 0; n--) stream.RemoveAt(stream.Count - 1);
            }
            else if(op < 85)
            {
                filterByRegion = !filterByRegion; // ZKBFeedFilterViewChk_Checked invalidates
                Invalidate();
            }
            else if(op < 90)
            {
                region = regions[r.Next(regions.Count)]; // RegionUC_RegionChanged invalidates
                Invalidate();
            }
            else if(op < 95)
            {
                Refresh(); // a refresh the timer didn't start, e.g. a column sort (no invalidation)
            }
            else
            {
                Invalidate(); // UiRefreshTimer_Tick invalidates, then refreshes
                Refresh();
            }
        }
        Console.WriteLine($"zKillboard filter: {evaluations:N0} evaluations over 20,000 feed events, {mismatches} mismatches between old and new");
    }

    // ---------------------------------------------------------------- navigation regression check
    // <dir> holds routes.txt (and optionally table.txt) recorded from an earlier build with the same
    // DumpCapitalRoutes/DumpNavTable. tools/PerfBench/baseline/routes.txt was recorded at 529cc02, when the
    // table came from JumpRangeCache.dat; it is tied to that commit's Systems.dat. The current build's dumps
    // go to %TEMP%\SmtPerfBench\nav-check and are compared: identical routes, and for the table the same
    // destinations per system with ranges equal to rounding.
    private static void NavCheck(string dir)
    {
        List<EveSystem> systems = LoadSystems();
        InstallFakeEveManager(systems);
        var sw = Stopwatch.StartNew();
        Navigation.InitNavigation(systems, new List<JumpBridge>());
        Console.WriteLine($"  InitNavigation (runtime jump table) {sw.ElapsedMilliseconds} ms");
        string after = Path.Combine(Work, "nav-check");
        Directory.CreateDirectory(after);
        DumpNavTable(Path.Combine(after, "table.txt"));
        DumpCapitalRoutes(systems, Path.Combine(after, "routes.txt"));

        string tableBefore = Path.Combine(dir, "table.txt");
        if(File.Exists(tableBefore))
        {
            var before = ReadNavTable(tableBefore);
            var now = ReadNavTable(Path.Combine(after, "table.txt"));
            int setDiffs = Math.Abs(before.Count - now.Count);
            double maxRangeDiff = 0;
            foreach(var kv in before)
            {
                if(!now.TryGetValue(kv.Key, out var links) || !links.Keys.ToHashSet().SetEquals(kv.Value.Keys))
                {
                    setDiffs++;
                    continue;
                }
                foreach(var t in kv.Value) maxRangeDiff = Math.Max(maxRangeDiff, Math.Abs(t.Value - links[t.Key]));
            }
            Console.WriteLine($"  table: {setDiffs} systems with different destinations, largest range difference {maxRangeDiff:E2} LY");
        }

        string[] routesBefore = File.ReadAllLines(Path.Combine(dir, "routes.txt"));
        string[] routesNow = File.ReadAllLines(Path.Combine(after, "routes.txt"));
        var differing = routesBefore.Zip(routesNow).Where(p => p.First != p.Second).ToList();
        Console.WriteLine($"  capital routes: {differing.Count} of {routesBefore.Length} differ");
        foreach(var p in differing.Take(3)) Console.WriteLine($"    before: {p.First}{Environment.NewLine}    after:  {p.Second}");
    }

    private static Dictionary<string, Dictionary<string, double>> ReadNavTable(string file)
    {
        var result = new Dictionary<string, Dictionary<string, double>>();
        foreach(string line in File.ReadLines(file))
        {
            int tab = line.IndexOf('\t');
            var links = new Dictionary<string, double>();
            string rest = line.Substring(tab + 1);
            if(rest.Length > 0)
            {
                foreach(string e in rest.Split(';'))
                {
                    int eq = e.LastIndexOf('=');
                    links[e.Substring(0, eq)] = double.Parse(e.Substring(eq + 1), System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            result[line.Substring(0, tab)] = links;
        }
        return result;
    }

    // Every node's jumpable systems with ranges, read from Navigation's private MapNodes.
    private static void DumpNavTable(string file)
    {
        var mapNodes = (System.Collections.IDictionary)typeof(Navigation)
            .GetProperty("MapNodes", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);
        var lines = new List<string>();
        long links = 0;
        foreach(System.Collections.DictionaryEntry de in mapNodes)
        {
            object node = de.Value;
            var jumpable = (System.Collections.IEnumerable)node.GetType().GetProperty("JumpableSystems")!.GetValue(node);
            var entries = new List<string>();
            foreach(object jl in jumpable)
            {
                string sys = (string)jl.GetType().GetField("System")!.GetValue(jl);
                double ly = Convert.ToDouble(jl.GetType().GetField("RangeLY")!.GetValue(jl), System.Globalization.CultureInfo.InvariantCulture);
                entries.Add(sys + "=" + ly.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            }
            links += entries.Count;
            entries.Sort(StringComparer.Ordinal);
            lines.Add((string)de.Key + "\t" + string.Join(";", entries));
        }
        lines.Sort(StringComparer.Ordinal);
        File.WriteAllLines(file, lines);
        Console.WriteLine($"  table: {lines.Count} nodes, {links:N0} jump links -> {file}");
    }

    // Seeded capital routes across ranges, some with systems to avoid.
    private static void DumpCapitalRoutes(List<EveSystem> systems, string file)
    {
        var rnd = new Random(12345);
        var candidates = systems.Where(s => s.TrueSec <= 0.45 && s.Region != "Pochven" && s.Name != "Zarzakh").Select(s => s.Name).ToList();
        double[] ranges = { 5.0, 6.0, 7.0, 8.0, 10.0 };
        var lines = new List<string>();
        var sw = Stopwatch.StartNew();
        for(int i = 0; i < 200; i++)
        {
            string from = candidates[rnd.Next(candidates.Count)];
            string to = candidates[rnd.Next(candidates.Count)];
            double maxLy = ranges[rnd.Next(ranges.Length)];
            List<string> avoid = i % 4 == 0 ? new List<string> { candidates[rnd.Next(candidates.Count)], candidates[rnd.Next(candidates.Count)] } : null;
            List<Navigation.RoutePoint> route = Navigation.NavigateCapitals(from, to, maxLy, null, avoid);
            string r = route == null ? "null" : string.Join(",", route.Select(p => p.SystemName + "(" + p.LY.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture) + ")"));
            lines.Add($"{from}|{to}|{maxLy.ToString(System.Globalization.CultureInfo.InvariantCulture)}|{(avoid == null ? "" : string.Join("+", avoid))} -> {r}");
        }
        File.WriteAllLines(file, lines);
        Console.WriteLine($"  {lines.Count} capital routes ({lines.Count(l => !l.EndsWith("-> "))} non-empty) in {sw.ElapsedMilliseconds} ms -> {file}");
    }

    // ---------------------------------------------------------------- UniverseControl.Init gate de-duplication
    private struct GateHelper { public EveSystem from; public EveSystem to; }

    private static void UniverseGates()
    {
        List<EveSystem> systems = LoadSystems();
        var byName = systems.ToDictionary(s => s.Name);
        Console.WriteLine($"UniverseControl.Init gate list ({systems.Count} systems, {systems.Sum(s => s.Jumps.Count):N0} jump entries):");
        for(int run = 0; run < 2; run++)
        {
            // replica of UniverseControl.xaml.cs:361-385
            var cache = Time("current: linear scan of the gate list per jump entry", () =>
            {
                var universeSysLinksCache = new List<GateHelper>();
                foreach(EveSystem sys in systems)
                {
                    foreach(string jumpTo in sys.Jumps)
                    {
                        byName.TryGetValue(jumpTo, out EveSystem to);
                        bool needsAdd = true;
                        foreach(GateHelper gh in universeSysLinksCache)
                        {
                            if(((gh.from == sys) || (gh.to == sys)) && ((gh.from == to) || (gh.to == to))) { needsAdd = false; break; }
                        }
                        if(needsAdd) universeSysLinksCache.Add(new GateHelper { from = sys, to = to });
                    }
                }
                return universeSysLinksCache;
            });
            var alt = Time("HashSet of unordered (id, id) pairs", () =>
            {
                var seen = new HashSet<(long, long)>();
                var list = new List<GateHelper>();
                foreach(EveSystem sys in systems)
                {
                    foreach(string jumpTo in sys.Jumps)
                    {
                        if(!byName.TryGetValue(jumpTo, out EveSystem to)) continue;
                        var key = sys.ID < to.ID ? (sys.ID, to.ID) : (to.ID, sys.ID);
                        if(seen.Add(key)) list.Add(new GateHelper { from = sys, to = to });
                    }
                }
                return list;
            });
            Console.WriteLine($"  gates: current={cache.Count:N0} hashset={alt.Count:N0}");
        }
    }

    // ---------------------------------------------------------------- intel system matching
    private static void Intel()
    {
        List<EveSystem> systems = LoadSystems();
        var r = new Random(3);
        string[] vocab = { "nv", "clr", "gate", "+2", "red", "neut", "Sabre", "camp", "on", "in", "local", "spike", "Loki", "bubbled", "dscan", "ess", "Pilot", "Xander", "Kronos", "status" };
        var lines = new List<string>();
        for(int i = 0; i < 2000; i++)
        {
            var words = new List<string>();
            int nw = r.Next(3, 9);
            for(int k = 0; k < nw; k++) words.Add(vocab[r.Next(vocab.Length)]);
            string sys = systems[r.Next(systems.Count)].Name;
            words.Insert(r.Next(words.Count), r.Next(4) == 0 ? sys.Substring(0, Math.Min(3, sys.Length)) : sys);
            lines.Add(string.Join(" ", words));
        }
        int shortNames = systems.Count(s => s.Name.Length < 3);
        Console.WriteLine($"intel system matching over {lines.Count:N0} synthetic lines ({systems.Count} systems, {shortNames} names shorter than 3 chars):");

        List<string> Current(string intel)
        {
            var found = new List<string>();
            foreach(string s in intel.Split(' '))
            {
                if(s == "" || s.Length < 3) continue;
                foreach(EveSystem sys in systems)
                {
                    if(sys.Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) == 0 || s.IndexOf(sys.Name, StringComparison.OrdinalIgnoreCase) == 0)
                        found.Add(sys.Name);
                }
            }
            return found;
        }

        // same predicate, but only against names sharing the word's first three characters
        var buckets = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var shortList = new List<string>();
        foreach(EveSystem sys in systems)
        {
            if(sys.Name.Length < 3) { shortList.Add(sys.Name); continue; }
            string key = sys.Name.Substring(0, 3);
            if(!buckets.TryGetValue(key, out var l)) buckets[key] = l = new List<string>();
            l.Add(sys.Name);
        }
        List<string> Bucketed(string intel)
        {
            var found = new List<string>();
            foreach(string s in intel.Split(' '))
            {
                if(s == "" || s.Length < 3) continue;
                if(buckets.TryGetValue(s.Substring(0, 3), out var cand))
                    foreach(string name in cand)
                        if(name.StartsWith(s, StringComparison.OrdinalIgnoreCase) || s.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                            found.Add(name);
                foreach(string name in shortList)
                    if(s.StartsWith(name, StringComparison.OrdinalIgnoreCase)) found.Add(name);
            }
            return found;
        }

        int diffs = lines.Count(l => !Current(l).OrderBy(x => x).SequenceEqual(Bucketed(l).OrderBy(x => x)));
        var cur = Bench(() => { foreach(string l in lines) Current(l); }, 5, 1);
        var buck = Bench(() => { foreach(string l in lines) Bucketed(l); }, 5, 1);
        Console.WriteLine($"  current (all systems per word): {cur.ms / lines.Count * 1000:F1} µs/line");
        Console.WriteLine($"  3-char bucket index:            {buck.ms / lines.Count * 1000:F2} µs/line   (result differences: {diffs})");
    }

    // ---------------------------------------------------------------- zKillboard view filter
    private sealed class Kill { public string SystemName; }

    private static void Zkb()
    {
        List<MapRegion> regions = LoadRegions();
        List<EveSystem> systems = LoadSystems();
        MapRegion region = regions.First(rg => rg.Name == "Delve");
        var regionNames = region.MapSystems.Values.Select(m => m.Name).ToList();
        var regionSet = new HashSet<string>(regionNames);
        bool IsSystemOnMap(string name)
        {
            foreach(KeyValuePair<string, MapSystem> kvp in region.MapSystems)
                if(kvp.Value.Name == name) return true;
            return false;
        }

        Console.WriteLine($"zKillboard feed: one CollectionView.Refresh() with the region filter on (region {region.Name}, {region.MapSystems.Count} map systems, ~3% of kills in region):");
        foreach(int n in new[] { 250, 500, 750 })
        {
            var r = new Random(n);
            var stream = new List<Kill>(n);
            for(int i = 0; i < n; i++)
                stream.Add(new Kill { SystemName = r.Next(100) < 3 ? regionNames[r.Next(regionNames.Count)] : systems[r.Next(systems.Count)].Name });

            // replica of the MainWindow filter lambda, evaluated once per item by Refresh()
            bool CurrentFilter(object item)
            {
                var listToFilter = stream.ToList();
                var filteredItems = listToFilter.Where(k => IsSystemOnMap(k.SystemName)).Take(30).ToList();
                return filteredItems.Contains((Kill)item);
            }
            var cur = Bench(() => { int c = 0; foreach(Kill k in stream) if(CurrentFilter(k)) c++; }, 5, 1);
            var alt = Bench(() =>
            {
                var top = new HashSet<Kill>(stream.Where(k => regionSet.Contains(k.SystemName)).Take(30));
                int c = 0; foreach(Kill k in stream) if(top.Contains(k)) c++;
            }, 5, 1);
            Console.WriteLine($"  {n,4} kills: old per-item filter {cur.ms,8:F1} ms ({cur.allocKb / 1024:F1} MB alloc)   cached top-30 set {alt.ms,6:F3} ms");
        }
    }
}
