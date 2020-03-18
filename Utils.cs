using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SMT
{
    internal class Utils
    {
        static public T DeserializeFromDisk<T>(string filename)
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

        public static Encoding GetEncoding(string filename)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76)
            {
                return Encoding.UTF7;
            }

            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
            {
                return Encoding.UTF8;
            }

            if (bom[0] == 0xff && bom[1] == 0xfe)
            {
                return Encoding.Unicode;
            }

            if (bom[0] == 0xfe && bom[1] == 0xff)
            {
                return Encoding.BigEndianUnicode;
            }

            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff)
            {
                return Encoding.UTF32;
            }
            return Encoding.Default;
        }

        static public void SerializToDisk<T>(T obj, string fileName)
        {
            XmlSerializer xms = new XmlSerializer(typeof(T));

            using (TextWriter tw = new StreamWriter(fileName))
            {
                xms.Serialize(tw, obj);
            }
        }
    }
}