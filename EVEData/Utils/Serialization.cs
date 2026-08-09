using System.Xml;
using System.Xml.Serialization;

namespace EVEDataUtils
{
    public class Serialization
    {
        public static T DeserializeFromDisk<T>(string filename)
        {
            try
            {
                return DeserializeFile<T>(filename);
            }
            catch(Exception primaryException)
            {
                string backupFile = AtomicFile.GetBackupPath(filename);
                if(File.Exists(backupFile))
                {
                    try
                    {
                        T recovered = DeserializeFile<T>(backupFile);
                        AppLog.Warning("Load data", $"Recovered '{Path.GetFileName(filename)}' from its backup after: {primaryException.Message}");
                        return recovered;
                    }
                    catch(Exception backupException)
                    {
                        AppLog.Error("Load data", new AggregateException(primaryException, backupException));
                    }
                }
                else
                {
                    AppLog.Error("Load data", primaryException);
                }

                return default;
            }
        }

        public static void SerializeToDisk<T>(T obj, string fileName)
        {
            try
            {
                XmlSerializer xms = new XmlSerializer(typeof(T));
                AtomicFile.Write(fileName, stream => xms.Serialize(stream, obj));
            }
            catch(Exception exception)
            {
                AppLog.Error("Save data", exception);
                throw;
            }
        }

        private static T DeserializeFile<T>(string filename)
        {
            XmlSerializer xms = new XmlSerializer(typeof(T));

            using FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            using XmlReader xmlr = XmlReader.Create(fs, new XmlReaderSettings { XmlResolver = null });

            return (T)xms.Deserialize(xmlr);
        }
    }
}
