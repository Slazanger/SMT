using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Xml;
using System.Xml.Serialization;

namespace SMT
{
    internal class Utils
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

        /// <summary>
        /// Create a colour from any string
        /// </summary>
        public static Color stringToColour(string str)
        {
            int hash = 0;

            foreach (char c in str.ToCharArray())
            {
                hash = c + ((hash << 5) - hash);
            }

            double R = (((byte)(hash & 0xff) / 255.0) * 80.0) + 127.0;
            double G = (((byte)((hash >> 8) & 0xff) / 255.0) * 80.0) + 127.0;
            double B = (((byte)((hash >> 16) & 0xff) / 255.0) * 80.0) + 127.0;

            return Color.FromArgb(100, (byte)R, (byte)G, (byte)B);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        public static string GetCaptionOfActiveWindow()
        {
            var strTitle = string.Empty;
            var handle = GetForegroundWindow();
            // Obtain the length of the text
            var intLength = GetWindowTextLength(handle) + 1;
            var stringBuilder = new StringBuilder(intLength);
            if (GetWindowText(handle, stringBuilder, intLength) > 0)
            {
                strTitle = stringBuilder.ToString();
            }
            return strTitle;
        }

        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}