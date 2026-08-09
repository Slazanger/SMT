using System.Text;

namespace EVEDataUtils
{
    /// <summary>
    /// Writes files through a temporary file in the same directory and keeps one backup.
    /// </summary>
    public static class AtomicFile
    {
        public static string GetBackupPath(string fileName) => fileName + ".bak";

        public static void Write(string fileName, Action<Stream> writeAction)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
            ArgumentNullException.ThrowIfNull(writeAction);

            string fullPath = Path.GetFullPath(fileName);
            string directory = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(directory);

            string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
            string backupPath = GetBackupPath(fullPath);

            try
            {
                using(FileStream stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.WriteThrough))
                {
                    writeAction(stream);
                    stream.Flush(true);
                }

                if(File.Exists(fullPath))
                {
                    try
                    {
                        File.Replace(temporaryPath, fullPath, backupPath, true);
                    }
                    catch(PlatformNotSupportedException)
                    {
                        ReplaceFallback(temporaryPath, fullPath, backupPath);
                    }
                    catch(IOException)
                    {
                        ReplaceFallback(temporaryPath, fullPath, backupPath);
                    }
                }
                else
                {
                    File.Move(temporaryPath, fullPath);
                }
            }
            finally
            {
                if(File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        public static void WriteText(string fileName, Action<TextWriter> writeAction)
        {
            ArgumentNullException.ThrowIfNull(writeAction);

            Write(fileName, stream =>
            {
                using StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true);
                writeAction(writer);
                writer.Flush();
            });
        }

        public static void WriteAllLines(string fileName, IEnumerable<string> lines)
        {
            WriteText(fileName, writer =>
            {
                foreach(string line in lines)
                {
                    writer.WriteLine(line);
                }
            });
        }

        private static void ReplaceFallback(string temporaryPath, string fullPath, string backupPath)
        {
            File.Copy(fullPath, backupPath, true);
            File.Move(temporaryPath, fullPath, true);
        }
    }
}
