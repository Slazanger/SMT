using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace DataGen
{
    internal sealed class DataGenOptions
    {
        public bool ForceDownload { get; private set; }
        public bool Offline { get; private set; }
        public bool SkipDebugSvg { get; private set; }
        public string? SdeZipPath { get; private set; }

        public static DataGenOptions Parse(string[] args)
        {
            DataGenOptions options = new DataGenOptions();

            for(int i = 0; i < args.Length; i++)
            {
                switch(args[i])
                {
                    case "--force-download":
                        options.ForceDownload = true;
                        break;

                    case "--offline":
                        options.Offline = true;
                        break;

                    case "--skip-debug-svg":
                        options.SkipDebugSvg = true;
                        break;

                    case "--sde-zip":
                        if(i + 1 >= args.Length)
                        {
                            throw new ArgumentException("--sde-zip requires a path argument.");
                        }
                        options.SdeZipPath = args[++i];
                        break;

                    default:
                        throw new ArgumentException($"Unknown argument '{args[i]}'.");
                }
            }

            if(options.Offline && options.ForceDownload)
            {
                throw new ArgumentException("--offline and --force-download cannot be used together.");
            }

            return options;
        }
    }

    internal sealed class SdeDataPreparer
    {
        private const string LatestBuildUrl = "https://developers.eveonline.com/static-data/tranquility/latest.jsonl";
        private const string LatestJsonlZipUrl = "https://developers.eveonline.com/static-data/eve-online-static-data-latest-jsonl.zip";

        private static readonly string[] RequiredJsonlFiles =
        [
            "mapSolarSystems.jsonl",
            "mapRegions.jsonl",
            "mapConstellations.jsonl",
            "mapStargates.jsonl",
            "npcStations.jsonl",
            "types.jsonl",
            "mapStars.jsonl"
        ];

        private readonly string eveDataFolder;
        private readonly string generatedRoot;
        private readonly string cacheRoot;
        private readonly string inputRoot;
        private readonly string inputDataRoot;
        private readonly DataGenOptions options;

        public SdeDataPreparer(string eveDataFolder, DataGenOptions options)
        {
            this.eveDataFolder = Path.GetFullPath(eveDataFolder);
            this.generatedRoot = Path.Combine(this.eveDataFolder, "data", "generated-sde");
            this.cacheRoot = Path.Combine(generatedRoot, "cache");
            this.inputRoot = Path.Combine(generatedRoot, "input");
            this.inputDataRoot = Path.Combine(inputRoot, "data");
            this.options = options;
        }

        public async Task<string> PrepareAsync()
        {
            Directory.CreateDirectory(generatedRoot);
            Directory.CreateDirectory(cacheRoot);

            string extractedSdeRoot = await ResolveExtractedSdeAsync();
            SdeDataSet dataSet = SdeDataSet.Load(extractedSdeRoot);

            RecreateInputRoot();
            CopyManualInputs();

            SdeExportStats stats = SdeCsvExporter.Export(dataSet, inputDataRoot);
            int factionWarfareSystemCount = await ExportFactionWarfareSystemsAsync(dataSet);
            ValidateDotlanSystems(dataSet);
            ValidateCatalystTypes(dataSet);

            Console.WriteLine("Prepared SDE input:");
            Console.WriteLine($"  Systems: {stats.SolarSystemCount}");
            Console.WriteLine($"  Regions: {stats.RegionCount}");
            Console.WriteLine($"  Constellations: {stats.ConstellationCount}");
            Console.WriteLine($"  Jumps: {stats.JumpCount}");
            Console.WriteLine($"  NPC station systems: {stats.NpcStationSystemCount}");
            Console.WriteLine($"  Ship/item types: {stats.TypeCount}");
            Console.WriteLine($"  A0 blue-star systems: {stats.A0BlueStarSystemCount}");
            Console.WriteLine($"  Faction warfare systems: {factionWarfareSystemCount}");

            return inputRoot;
        }

        private async Task<string> ResolveExtractedSdeAsync()
        {
            if(!string.IsNullOrWhiteSpace(options.SdeZipPath))
            {
                string zipPath = Path.GetFullPath(options.SdeZipPath);
                if(!File.Exists(zipPath))
                {
                    throw new FileNotFoundException("The SDE zip file could not be found.", zipPath);
                }

                string localExtractRoot = Path.Combine(cacheRoot, "extracted", "local");
                ExtractZip(zipPath, localExtractRoot);
                return localExtractRoot;
            }

            SdeCacheMetadata metadata = LoadCacheMetadata();

            if(options.Offline)
            {
                string offlineExtractRoot = ResolveOfflineExtractRoot(metadata);
                ValidateExtractedSde(offlineExtractRoot);
                Console.WriteLine($"Using cached SDE from {offlineExtractRoot}");
                return offlineExtractRoot;
            }

            string latestBuild = await GetLatestBuildNumberAsync();
            string zipFileName = $"eve-online-static-data-{latestBuild}-jsonl.zip";
            string zipPathForBuild = Path.Combine(cacheRoot, zipFileName);
            string extractRootForBuild = Path.Combine(cacheRoot, "extracted", latestBuild);

            if(!options.ForceDownload && Directory.Exists(extractRootForBuild) && HasRequiredJsonlFiles(extractRootForBuild))
            {
                Console.WriteLine($"Using cached SDE build {latestBuild}");
                return extractRootForBuild;
            }

            if(!options.ForceDownload && File.Exists(zipPathForBuild))
            {
                Console.WriteLine($"Extracting cached SDE build {latestBuild}");
                ExtractZip(zipPathForBuild, extractRootForBuild);
                return extractRootForBuild;
            }

            SdeDownloadResult downloadResult = await DownloadLatestSdeAsync(zipPathForBuild, metadata);
            metadata.BuildNumber = latestBuild;
            metadata.ZipFileName = Path.GetFileName(downloadResult.ZipPath);
            metadata.ETag = downloadResult.ETag;
            metadata.LastModified = downloadResult.LastModified;
            metadata.DownloadedAtUtc = DateTime.UtcNow;

            ExtractZip(downloadResult.ZipPath, extractRootForBuild);
            metadata.ExtractedDirectory = Path.GetRelativePath(cacheRoot, extractRootForBuild);
            SaveCacheMetadata(metadata);

            return extractRootForBuild;
        }

        private static async Task<string> GetLatestBuildNumberAsync()
        {
            using HttpClient client = CreateHttpClient();
            using Stream stream = await client.GetStreamAsync(LatestBuildUrl);
            using StreamReader reader = new StreamReader(stream);

            string? line;
            while((line = await reader.ReadLineAsync()) != null)
            {
                if(string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                using JsonDocument document = JsonDocument.Parse(line);
                JsonElement root = document.RootElement;

                if(TryGetString(root, "_key", out string? key) && string.Equals(key, "sde", StringComparison.OrdinalIgnoreCase))
                {
                    if(TryGetString(root, "_value", out string? value))
                    {
                        return value;
                    }

                    if(TryGetString(root, "buildNumber", out string? buildNumber))
                    {
                        return buildNumber;
                    }
                }

                if(root.TryGetProperty("sde", out JsonElement sdeValue))
                {
                    return sdeValue.ValueKind == JsonValueKind.String
                        ? sdeValue.GetString()!
                        : sdeValue.GetRawText();
                }
            }

            throw new InvalidOperationException("Unable to determine the latest SDE build number.");
        }

        private static async Task<SdeDownloadResult> DownloadLatestSdeAsync(string targetZipPath, SdeCacheMetadata metadata)
        {
            Console.WriteLine("Downloading latest SDE JSONL zip...");

            using HttpClient client = CreateHttpClient();
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, LatestJsonlZipUrl);

            if(!string.IsNullOrWhiteSpace(metadata.ETag))
            {
                request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(metadata.ETag));
            }

            if(DateTimeOffset.TryParse(metadata.LastModified, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset lastModified))
            {
                request.Headers.IfModifiedSince = lastModified;
            }

            using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if(response.StatusCode == HttpStatusCode.NotModified)
            {
                string cachedZip = ResolveCachedZipPath(Path.GetDirectoryName(targetZipPath)!, metadata);
                if(File.Exists(cachedZip))
                {
                    return new SdeDownloadResult(cachedZip, metadata.ETag, metadata.LastModified);
                }
            }

            response.EnsureSuccessStatusCode();

            Directory.CreateDirectory(Path.GetDirectoryName(targetZipPath)!);
            string tempZipPath = targetZipPath + ".tmp";
            await using(Stream responseStream = await response.Content.ReadAsStreamAsync())
            await using(FileStream outputStream = File.Create(tempZipPath))
            {
                await responseStream.CopyToAsync(outputStream);
            }

            if(File.Exists(targetZipPath))
            {
                File.Delete(targetZipPath);
            }
            File.Move(tempZipPath, targetZipPath);

            string? etag = response.Headers.ETag?.Tag;
            string? lastModifiedHeader = response.Content.Headers.LastModified?.ToString("R", CultureInfo.InvariantCulture)
                ?? response.Headers.Date?.ToString("R", CultureInfo.InvariantCulture);

            return new SdeDownloadResult(targetZipPath, etag, lastModifiedHeader);
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SMT-DataGen/1.0 (https://github.com/Slazanger/SMT)");
            return client;
        }

        private string ResolveOfflineExtractRoot(SdeCacheMetadata metadata)
        {
            if(!string.IsNullOrWhiteSpace(metadata.ExtractedDirectory))
            {
                string extractedRoot = Path.Combine(cacheRoot, metadata.ExtractedDirectory);
                if(Directory.Exists(extractedRoot))
                {
                    return extractedRoot;
                }
            }

            string extractedParent = Path.Combine(cacheRoot, "extracted");
            if(Directory.Exists(extractedParent))
            {
                string? latestDirectory = Directory.EnumerateDirectories(extractedParent)
                    .OrderByDescending(Directory.GetLastWriteTimeUtc)
                    .FirstOrDefault(HasRequiredJsonlFiles);

                if(latestDirectory != null)
                {
                    return latestDirectory;
                }
            }

            throw new InvalidOperationException("No cached SDE extraction is available. Run DataGen once online or pass --sde-zip <path>.");
        }

        private static string ResolveCachedZipPath(string cacheRoot, SdeCacheMetadata metadata)
        {
            if(!string.IsNullOrWhiteSpace(metadata.ZipFileName))
            {
                return Path.Combine(cacheRoot, metadata.ZipFileName);
            }

            return string.Empty;
        }

        private static void ExtractZip(string zipPath, string extractRoot)
        {
            if(Directory.Exists(extractRoot))
            {
                Directory.Delete(extractRoot, recursive: true);
            }

            Directory.CreateDirectory(extractRoot);
            ZipFile.ExtractToDirectory(zipPath, extractRoot, overwriteFiles: true);
            ValidateExtractedSde(extractRoot);
        }

        private static void ValidateExtractedSde(string extractedRoot)
        {
            string[] missingFiles = RequiredJsonlFiles
                .Where(fileName => FindSdeFile(extractedRoot, fileName) == null)
                .ToArray();

            if(missingFiles.Length > 0)
            {
                throw new InvalidOperationException($"The extracted SDE is missing required JSONL files: {string.Join(", ", missingFiles)}");
            }
        }

        private static bool HasRequiredJsonlFiles(string extractedRoot)
        {
            return RequiredJsonlFiles.All(fileName => FindSdeFile(extractedRoot, fileName) != null);
        }

        private void RecreateInputRoot()
        {
            if(Directory.Exists(inputRoot))
            {
                Directory.Delete(inputRoot, recursive: true);
            }

            Directory.CreateDirectory(inputDataRoot);
        }

        private void CopyManualInputs()
        {
            string sourceDataRoot = Path.Combine(eveDataFolder, "data");
            CopyDirectory(Path.Combine(sourceDataRoot, "SourceMaps"), Path.Combine(inputDataRoot, "SourceMaps"));

            CopyOverlay(sourceDataRoot, "mapSolarSystemJumpsExtra.csv");
            CopyOverlay(sourceDataRoot, "iceSystems.csv");
            CopyOverlay(sourceDataRoot, "trigInvasionSystems.csv");
            CopyOverlay(sourceDataRoot, "JoveSystems.csv");
            CopyOverlay(sourceDataRoot, "joveGates.csv", "JoveGates.csv");
            CopyOverlay(sourceDataRoot, "POI.csv");
            CopyOverlay(sourceDataRoot, "Translation.csv");
        }

        private async Task<int> ExportFactionWarfareSystemsAsync(SdeDataSet dataSet)
        {
            List<EsiFactionWarfareSystem> factionWarfareSystems = await LoadFactionWarfareSystemsAsync();
            List<string> systemNames = factionWarfareSystems
                .Select(system => dataSet.SolarSystems.TryGetValue(system.SolarSystemId, out SdeSolarSystem? solarSystem) ? solarSystem.Name.En : null)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            if(systemNames.Count == 0)
            {
                throw new InvalidOperationException("ESI faction warfare systems did not map to any SDE solar systems.");
            }

            string outputPath = Path.Combine(inputDataRoot, "factionWarfareSystems.csv");
            await using StreamWriter writer = new StreamWriter(outputPath, append: false);
            await writer.WriteLineAsync("System");

            foreach(string systemName in systemNames)
            {
                await writer.WriteLineAsync(systemName);
            }

            return systemNames.Count;
        }

        private async Task<List<EsiFactionWarfareSystem>> LoadFactionWarfareSystemsAsync()
        {
            string cachePath = Path.Combine(cacheRoot, "fw-systems.json");

            if(options.Offline)
            {
                if(!File.Exists(cachePath))
                {
                    throw new InvalidOperationException("No cached ESI faction warfare systems are available. Run DataGen once online.");
                }

                return await ReadFactionWarfareCacheAsync(cachePath);
            }

            try
            {
                using HttpClient client = CreateHttpClient();
                await using Stream responseStream = await client.GetStreamAsync("https://esi.evetech.net/latest/fw/systems/?datasource=tranquility");
                List<EsiFactionWarfareSystem> systems = await JsonSerializer.DeserializeAsync<List<EsiFactionWarfareSystem>>(responseStream)
                    ?? throw new InvalidOperationException("ESI returned an empty faction warfare systems response.");

                string json = JsonSerializer.Serialize(systems, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(cachePath, json);
                return systems;
            }
            catch(Exception ex) when(File.Exists(cachePath))
            {
                Console.WriteLine($"  Warning: failed to refresh ESI faction warfare systems, using cache. {ex.Message}");
                return await ReadFactionWarfareCacheAsync(cachePath);
            }
        }

        private static async Task<List<EsiFactionWarfareSystem>> ReadFactionWarfareCacheAsync(string cachePath)
        {
            string json = await File.ReadAllTextAsync(cachePath);
            return JsonSerializer.Deserialize<List<EsiFactionWarfareSystem>>(json)
                ?? throw new InvalidOperationException("Cached ESI faction warfare systems file is empty.");
        }

        private void CopyOverlay(string sourceDataRoot, string sourceFileName, string? targetFileName = null)
        {
            string sourcePath = Path.Combine(sourceDataRoot, sourceFileName);
            if(!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("A required manual data overlay is missing.", sourcePath);
            }

            File.Copy(sourcePath, Path.Combine(inputDataRoot, targetFileName ?? sourceFileName), overwrite: true);
        }

        private static void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            if(!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException(sourceDirectory);
            }

            Directory.CreateDirectory(targetDirectory);

            foreach(string file in Directory.EnumerateFiles(sourceDirectory))
            {
                File.Copy(file, Path.Combine(targetDirectory, Path.GetFileName(file)), overwrite: true);
            }

            foreach(string directory in Directory.EnumerateDirectories(sourceDirectory))
            {
                CopyDirectory(directory, Path.Combine(targetDirectory, Path.GetFileName(directory)));
            }
        }

        private void ValidateDotlanSystems(SdeDataSet dataSet)
        {
            string rawMapDirectory = Path.Combine(inputDataRoot, "SourceMaps", "raw");
            List<string> missingSystems = new List<string>();

            foreach(string svgPath in Directory.EnumerateFiles(rawMapDirectory, "*_layout.svg"))
            {
                XmlDocument document = new XmlDocument
                {
                    XmlResolver = null
                };
                document.Load(svgPath);

                XmlNodeList? nodes = document.SelectNodes("//*[@Type='system']");
                if(nodes == null)
                {
                    continue;
                }

                foreach(XmlNode node in nodes)
                {
                    string? idValue = node.Attributes?["ID"]?.Value;
                    if(!long.TryParse(idValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long systemId))
                    {
                        continue;
                    }

                    if(!dataSet.SolarSystems.ContainsKey(systemId))
                    {
                        string name = node.Attributes?["Name"]?.Value ?? idValue ?? "unknown";
                        missingSystems.Add($"{Path.GetFileName(svgPath)}:{name} ({systemId})");
                    }
                }
            }

            if(missingSystems.Count > 0)
            {
                throw new InvalidOperationException($"Dotlan layouts reference systems missing from the SDE: {string.Join(", ", missingSystems.Take(20))}");
            }
        }

        private static void ValidateCatalystTypes(SdeDataSet dataSet)
        {
            string[] catalystTypeIds = ["89240", "89649", "89648", "89647", "89607"];
            string[] missingTypes = catalystTypeIds
                .Where(typeId => !dataSet.Types.ContainsKey(long.Parse(typeId, CultureInfo.InvariantCulture)))
                .ToArray();

            if(missingTypes.Length > 0)
            {
                Console.WriteLine($"  Warning: SDE is missing Catalyst-era type IDs still patched by EveManager: {string.Join(", ", missingTypes)}");
            }
        }

        private SdeCacheMetadata LoadCacheMetadata()
        {
            string metadataPath = GetMetadataPath();
            if(!File.Exists(metadataPath))
            {
                return new SdeCacheMetadata();
            }

            string json = File.ReadAllText(metadataPath);
            return JsonSerializer.Deserialize<SdeCacheMetadata>(json) ?? new SdeCacheMetadata();
        }

        private void SaveCacheMetadata(SdeCacheMetadata metadata)
        {
            string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GetMetadataPath(), json);
        }

        private string GetMetadataPath() => Path.Combine(cacheRoot, "sde-cache.json");

        private static bool TryGetString(JsonElement element, string propertyName, out string? value)
        {
            value = null;

            if(!element.TryGetProperty(propertyName, out JsonElement property))
            {
                return false;
            }

            value = property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : property.GetRawText();

            return !string.IsNullOrWhiteSpace(value);
        }

        internal static string? FindSdeFile(string root, string fileName)
        {
            return Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories).FirstOrDefault();
        }
    }

    internal sealed class SdeCacheMetadata
    {
        public string? BuildNumber { get; set; }
        public string? ZipFileName { get; set; }
        public string? ExtractedDirectory { get; set; }
        public string? ETag { get; set; }
        public string? LastModified { get; set; }
        public DateTime DownloadedAtUtc { get; set; }
    }

    internal readonly record struct SdeDownloadResult(string ZipPath, string? ETag, string? LastModified);

    internal sealed class SdeDataSet
    {
        public Dictionary<long, SdeSolarSystem> SolarSystems { get; } = new Dictionary<long, SdeSolarSystem>();
        public Dictionary<long, SdeRegion> Regions { get; } = new Dictionary<long, SdeRegion>();
        public Dictionary<long, SdeConstellation> Constellations { get; } = new Dictionary<long, SdeConstellation>();
        public Dictionary<long, SdeType> Types { get; } = new Dictionary<long, SdeType>();
        public List<SdeStargate> Stargates { get; } = new List<SdeStargate>();
        public List<SdeNpcStation> NpcStations { get; } = new List<SdeNpcStation>();
        public List<SdeStar> Stars { get; } = new List<SdeStar>();

        public static SdeDataSet Load(string extractedSdeRoot)
        {
            SdeDataSet dataSet = new SdeDataSet();

            foreach(SdeSolarSystem solarSystem in SdeJsonlReader.Read<SdeSolarSystem>(GetRequiredFile(extractedSdeRoot, "mapSolarSystems.jsonl")))
            {
                dataSet.SolarSystems[solarSystem.Id] = solarSystem;
            }

            foreach(SdeRegion region in SdeJsonlReader.Read<SdeRegion>(GetRequiredFile(extractedSdeRoot, "mapRegions.jsonl")))
            {
                dataSet.Regions[region.Id] = region;
            }

            foreach(SdeConstellation constellation in SdeJsonlReader.Read<SdeConstellation>(GetRequiredFile(extractedSdeRoot, "mapConstellations.jsonl")))
            {
                dataSet.Constellations[constellation.Id] = constellation;
            }

            dataSet.Stargates.AddRange(SdeJsonlReader.Read<SdeStargate>(GetRequiredFile(extractedSdeRoot, "mapStargates.jsonl")));
            dataSet.NpcStations.AddRange(SdeJsonlReader.Read<SdeNpcStation>(GetRequiredFile(extractedSdeRoot, "npcStations.jsonl")));
            dataSet.Stars.AddRange(SdeJsonlReader.Read<SdeStar>(GetRequiredFile(extractedSdeRoot, "mapStars.jsonl")));

            foreach(SdeType type in SdeJsonlReader.Read<SdeType>(GetRequiredFile(extractedSdeRoot, "types.jsonl")))
            {
                dataSet.Types[type.Id] = type;
            }

            return dataSet;
        }

        private static string GetRequiredFile(string extractedSdeRoot, string fileName)
        {
            string? path = SdeDataPreparer.FindSdeFile(extractedSdeRoot, fileName);
            if(path == null)
            {
                throw new FileNotFoundException($"Unable to find {fileName} in the extracted SDE.", fileName);
            }

            return path;
        }
    }

    internal static class SdeJsonlReader
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false
        };

        public static IEnumerable<T> Read<T>(string path)
        {
            int lineNumber = 0;

            foreach(string line in File.ReadLines(path))
            {
                lineNumber++;
                if(string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                T? value;
                try
                {
                    value = JsonSerializer.Deserialize<T>(line, JsonOptions);
                }
                catch(JsonException ex)
                {
                    throw new InvalidOperationException($"Failed to parse {Path.GetFileName(path)} line {lineNumber}.", ex);
                }

                if(value != null)
                {
                    yield return value;
                }
            }
        }
    }

    internal static class SdeCsvExporter
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static SdeExportStats Export(SdeDataSet dataSet, string dataRoot)
        {
            Directory.CreateDirectory(dataRoot);

            int solarSystemCount = ExportSolarSystems(dataSet, Path.Combine(dataRoot, "mapSolarSystems.csv"));
            int regionCount = ExportRegions(dataSet, Path.Combine(dataRoot, "mapRegions.csv"));
            int constellationCount = ExportConstellations(dataSet, Path.Combine(dataRoot, "mapConstellations.csv"));
            int jumpCount = ExportJumps(dataSet, Path.Combine(dataRoot, "mapSolarSystemJumps.csv"));
            int npcStationSystemCount = ExportNpcStations(dataSet, Path.Combine(dataRoot, "staStations.csv"));
            int typeCount = ExportTypes(dataSet, Path.Combine(dataRoot, "invTypes.csv"));
            int a0BlueStarSystemCount = ExportA0BlueStarSystems(dataSet, Path.Combine(dataRoot, "a0BlueStarSystems.csv"));

            return new SdeExportStats(
                solarSystemCount,
                regionCount,
                constellationCount,
                jumpCount,
                npcStationSystemCount,
                typeCount,
                a0BlueStarSystemCount);
        }

        private static int ExportSolarSystems(SdeDataSet dataSet, string path)
        {
            using StreamWriter writer = CreateWriter(path);
            writer.WriteLine("regionID,constellationID,solarSystemID,solarSystemName,x,y,z,xMin,xMax,yMin,yMax,zMin,zMax,luminosity,border,fringe,corridor,hub,international,regional,securityClass,security,factionID,radius");

            int count = 0;
            foreach(SdeSolarSystem system in dataSet.SolarSystems.Values.OrderBy(system => system.Id))
            {
                writer.WriteLine(string.Join(",", [
                    Format(system.RegionId),
                    Format(system.ConstellationId),
                    Format(system.Id),
                    CleanCsvValue(system.Name.En),
                    Format(system.Position.X),
                    Format(system.Position.Y),
                    Format(system.Position.Z),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    system.SecurityClass ?? string.Empty,
                    Format(system.SecurityStatus),
                    system.FactionId?.ToString(Invariant) ?? string.Empty,
                    Format(system.Radius)
                ]));
                count++;
            }

            return count;
        }

        private static int ExportRegions(SdeDataSet dataSet, string path)
        {
            using StreamWriter writer = CreateWriter(path);
            writer.WriteLine("regionID,regionName,x,y,z");

            int count = 0;
            foreach(SdeRegion region in dataSet.Regions.Values.OrderBy(region => region.Id))
            {
                writer.WriteLine(string.Join(",", [
                    Format(region.Id),
                    CleanCsvValue(region.Name.En),
                    Format(region.Position.X),
                    Format(region.Position.Y),
                    Format(region.Position.Z)
                ]));
                count++;
            }

            return count;
        }

        private static int ExportConstellations(SdeDataSet dataSet, string path)
        {
            using StreamWriter writer = CreateWriter(path);
            writer.WriteLine("regionID,constellationID,constellationName,x,y,z");

            int count = 0;
            foreach(SdeConstellation constellation in dataSet.Constellations.Values.OrderBy(constellation => constellation.Id))
            {
                writer.WriteLine(string.Join(",", [
                    Format(constellation.RegionId),
                    Format(constellation.Id),
                    CleanCsvValue(constellation.Name.En),
                    Format(constellation.Position.X),
                    Format(constellation.Position.Y),
                    Format(constellation.Position.Z)
                ]));
                count++;
            }

            return count;
        }

        private static int ExportJumps(SdeDataSet dataSet, string path)
        {
            using StreamWriter writer = CreateWriter(path);
            writer.WriteLine("fromRegionID,fromConstellationID,fromSolarSystemID,toSolarSystemID,toConstellationID,toRegionID");

            HashSet<(long FromSystemId, long ToSystemId)> jumps = new HashSet<(long FromSystemId, long ToSystemId)>();

            foreach(SdeStargate stargate in dataSet.Stargates)
            {
                if(!dataSet.SolarSystems.ContainsKey(stargate.SolarSystemId) ||
                   !dataSet.SolarSystems.ContainsKey(stargate.Destination.SolarSystemId))
                {
                    continue;
                }

                jumps.Add((stargate.SolarSystemId, stargate.Destination.SolarSystemId));
            }

            foreach((long fromSystemId, long toSystemId) in jumps.OrderBy(jump => jump.FromSystemId).ThenBy(jump => jump.ToSystemId))
            {
                SdeSolarSystem from = dataSet.SolarSystems[fromSystemId];
                SdeSolarSystem to = dataSet.SolarSystems[toSystemId];
                writer.WriteLine(string.Join(",", [
                    Format(from.RegionId),
                    Format(from.ConstellationId),
                    Format(from.Id),
                    Format(to.Id),
                    Format(to.ConstellationId),
                    Format(to.RegionId)
                ]));
            }

            return jumps.Count;
        }

        private static int ExportNpcStations(SdeDataSet dataSet, string path)
        {
            using StreamWriter writer = CreateWriter(path);
            writer.WriteLine("stationID,security,dockingCost,officeRentalCost,operationID,stationTypeID,corporationID,stationName,solarSystemID");

            HashSet<long> stationSystems = new HashSet<long>();
            foreach(SdeNpcStation station in dataSet.NpcStations.OrderBy(station => station.Id))
            {
                writer.WriteLine(string.Join(",", [
                    Format(station.Id),
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    station.OperationId?.ToString(Invariant) ?? string.Empty,
                    Format(station.TypeId),
                    station.OwnerId?.ToString(Invariant) ?? string.Empty,
                    string.Empty,
                    Format(station.SolarSystemId)
                ]));
                stationSystems.Add(station.SolarSystemId);
            }

            return stationSystems.Count;
        }

        private static int ExportTypes(SdeDataSet dataSet, string path)
        {
            using StreamWriter writer = CreateWriter(path);
            writer.WriteLine("typeID,groupID,typeName");

            int count = 0;
            foreach(SdeType type in dataSet.Types.Values.OrderBy(type => type.Id))
            {
                writer.WriteLine(string.Join(",", [
                    Format(type.Id),
                    Format(type.GroupId),
                    CleanCsvValue(type.Name.En)
                ]));
                count++;
            }

            return count;
        }

        private static int ExportA0BlueStarSystems(SdeDataSet dataSet, string path)
        {
            using StreamWriter writer = CreateWriter(path);
            writer.WriteLine("System");

            List<string> systemNames = dataSet.Stars
                .Where(star => star.Statistics.SpectralClass.StartsWith("A0", StringComparison.OrdinalIgnoreCase))
                .Select(star => dataSet.SolarSystems.TryGetValue(star.SolarSystemId, out SdeSolarSystem? system) ? system.Name.En : null)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            foreach(string systemName in systemNames)
            {
                writer.WriteLine(CleanCsvValue(systemName));
            }

            return systemNames.Count;
        }

        private static StreamWriter CreateWriter(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return new StreamWriter(path, append: false);
        }

        private static string Format(long value) => value.ToString(Invariant);

        private static string Format(double value) => value.ToString("R", Invariant);

        private static string Format(decimal value) => value.ToString(Invariant);

        private static string CleanCsvValue(string? value)
        {
            return (value ?? string.Empty).Replace(",", " ", StringComparison.Ordinal);
        }
    }

    internal readonly record struct SdeExportStats(
        int SolarSystemCount,
        int RegionCount,
        int ConstellationCount,
        int JumpCount,
        int NpcStationSystemCount,
        int TypeCount,
        int A0BlueStarSystemCount);

    internal sealed class SdeLocalizedName
    {
        [JsonPropertyName("en")]
        public string? EnValue { get; set; }

        [JsonIgnore]
        public string En => EnValue ?? string.Empty;
    }

    internal sealed class SdePosition
    {
        [JsonPropertyName("x")]
        public decimal X { get; set; }

        [JsonPropertyName("y")]
        public decimal Y { get; set; }

        [JsonPropertyName("z")]
        public decimal Z { get; set; }
    }

    internal sealed class SdeSolarSystem
    {
        [JsonPropertyName("_key")]
        public long Id { get; set; }

        [JsonPropertyName("constellationID")]
        public long ConstellationId { get; set; }

        [JsonPropertyName("name")]
        public SdeLocalizedName Name { get; set; } = new SdeLocalizedName();

        [JsonPropertyName("position")]
        public SdePosition Position { get; set; } = new SdePosition();

        [JsonPropertyName("radius")]
        public double Radius { get; set; }

        [JsonPropertyName("regionID")]
        public long RegionId { get; set; }

        [JsonPropertyName("securityClass")]
        public string? SecurityClass { get; set; }

        [JsonPropertyName("securityStatus")]
        public double SecurityStatus { get; set; }

        [JsonPropertyName("factionID")]
        public long? FactionId { get; set; }
    }

    internal sealed class SdeRegion
    {
        [JsonPropertyName("_key")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public SdeLocalizedName Name { get; set; } = new SdeLocalizedName();

        [JsonPropertyName("position")]
        public SdePosition Position { get; set; } = new SdePosition();
    }

    internal sealed class SdeConstellation
    {
        [JsonPropertyName("_key")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public SdeLocalizedName Name { get; set; } = new SdeLocalizedName();

        [JsonPropertyName("position")]
        public SdePosition Position { get; set; } = new SdePosition();

        [JsonPropertyName("regionID")]
        public long RegionId { get; set; }
    }

    internal sealed class SdeStargate
    {
        [JsonPropertyName("_key")]
        public long Id { get; set; }

        [JsonPropertyName("destination")]
        public SdeStargateDestination Destination { get; set; } = new SdeStargateDestination();

        [JsonPropertyName("solarSystemID")]
        public long SolarSystemId { get; set; }
    }

    internal sealed class SdeStargateDestination
    {
        [JsonPropertyName("solarSystemID")]
        public long SolarSystemId { get; set; }

        [JsonPropertyName("stargateID")]
        public long StargateId { get; set; }
    }

    internal sealed class SdeNpcStation
    {
        [JsonPropertyName("_key")]
        public long Id { get; set; }

        [JsonPropertyName("operationID")]
        public long? OperationId { get; set; }

        [JsonPropertyName("ownerID")]
        public long? OwnerId { get; set; }

        [JsonPropertyName("solarSystemID")]
        public long SolarSystemId { get; set; }

        [JsonPropertyName("typeID")]
        public long TypeId { get; set; }
    }

    internal sealed class SdeType
    {
        [JsonPropertyName("_key")]
        public long Id { get; set; }

        [JsonPropertyName("groupID")]
        public long GroupId { get; set; }

        [JsonPropertyName("name")]
        public SdeLocalizedName Name { get; set; } = new SdeLocalizedName();
    }

    internal sealed class SdeStar
    {
        [JsonPropertyName("_key")]
        public long Id { get; set; }

        [JsonPropertyName("solarSystemID")]
        public long SolarSystemId { get; set; }

        [JsonPropertyName("statistics")]
        public SdeStarStatistics Statistics { get; set; } = new SdeStarStatistics();
    }

    internal sealed class SdeStarStatistics
    {
        [JsonPropertyName("spectralClass")]
        public string SpectralClass { get; set; } = string.Empty;
    }

    internal sealed class EsiFactionWarfareSystem
    {
        [JsonPropertyName("solar_system_id")]
        public long SolarSystemId { get; set; }
    }
}
