using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
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
#pragma warning disable SYSLIB0001
                return Encoding.UTF7;
#pragma warning restore SYSLIB0001
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
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static Model3DGroup CreateTriangle(Point3D p0, Point3D p1, Point3D p2, Brush fill)
        {
            MeshGeometry3D triMesh = new MeshGeometry3D();
            triMesh.Positions.Add(p0);
            triMesh.Positions.Add(p1);
            triMesh.Positions.Add(p2);
            triMesh.TriangleIndices.Add(0);
            triMesh.TriangleIndices.Add(1);
            triMesh.TriangleIndices.Add(2);

            // calculate the normal for the tri
            Vector3D v0 = new Vector3D(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            Vector3D v1 = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            Vector3D normal = Vector3D.CrossProduct(v0, v1);

            triMesh.Normals.Add(normal);
            triMesh.Normals.Add(normal);
            triMesh.Normals.Add(normal);

            Material material = new DiffuseMaterial(fill);
            GeometryModel3D model = new GeometryModel3D(triMesh, material);
            Model3DGroup group = new Model3DGroup();
            group.Children.Add(model);
            return group;
        }

        public static ModelVisual3D CreateCube(double x, double y, double z, double size, Brush fill)
        {
            Model3DGroup cube = new Model3DGroup();

            Point3D p0 = new Point3D(x, y, z);
            Point3D p1 = new Point3D(x + size, y, z);
            Point3D p2 = new Point3D(x + size, y, z + size);
            Point3D p3 = new Point3D(x, y, z + size);
            Point3D p4 = new Point3D(x, y + size, z);
            Point3D p5 = new Point3D(x + size, y + size, z);
            Point3D p6 = new Point3D(x + size, y + size, z + size);
            Point3D p7 = new Point3D(x, y + size, z + size);

            //front
            cube.Children.Add(CreateTriangle(p3, p2, p6, fill));
            cube.Children.Add(CreateTriangle(p3, p6, p7, fill));

            //right
            cube.Children.Add(CreateTriangle(p2, p1, p5, fill));
            cube.Children.Add(CreateTriangle(p2, p5, p6, fill));

            //back
            cube.Children.Add(CreateTriangle(p1, p0, p4, fill));
            cube.Children.Add(CreateTriangle(p1, p4, p5, fill));

            //left
            cube.Children.Add(CreateTriangle(p0, p3, p7, fill));
            cube.Children.Add(CreateTriangle(p0, p7, p4, fill));

            //top
            cube.Children.Add(CreateTriangle(p7, p6, p5, fill));
            cube.Children.Add(CreateTriangle(p7, p5, p4, fill));

            //bottom
            cube.Children.Add(CreateTriangle(p2, p3, p0, fill));
            cube.Children.Add(CreateTriangle(p2, p0, p1, fill));

            ModelVisual3D model = new ModelVisual3D();
            model.Content = cube;
            return model;
        }
    }
}