using System.Xml;
using System.Xml.Serialization;


namespace Utils
{
    public class Serialization
    {
        public static T DeserializeFromDisk<T>(string filename)
        {
            try
            {
                XmlSerializer xms = new XmlSerializer(typeof(T));

                FileStream fs = new FileStream(filename, FileMode.Open);
                XmlReader xmlr = XmlReader.Create(fs);

                return (T)xms.Deserialize(xmlr);
            }
            catch
            {
                return default(T);
            }
        }

        public static void SerializeToDisk<T>(T obj, string fileName)
        {
            XmlSerializer xms = new XmlSerializer(typeof(T));

            using (TextWriter tw = new StreamWriter(fileName))
            {
                xms.Serialize(tw, obj);
            }
        }
    }
}
