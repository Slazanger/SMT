using System;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace SMT.ResourceUsage
{
    public class ResourceLoader
    {
        // from http://stackoverflow.com/questions/347614/wpf-image-resources

        /// <summary>
        /// Load a resource WPF-BitmapImage (png, bmp, ...) from embedded resource defined as 'Resource' not as 'Embedded resource'.
        /// </summary>
        /// <param name="pathInApplication">Path without starting slash</param>
        /// <param name="assembly">Usually 'Assembly.GetExecutingAssembly()'. If not mentionned, I will use the calling assembly</param>
        /// <returns></returns>
        public static BitmapImage LoadBitmapFromResource(string pathInApplication, Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetCallingAssembly();
            }

            if (pathInApplication[0] == '/')
            {
                pathInApplication = pathInApplication.Substring(1);
            }
            try
            {
                return
                    new BitmapImage(
                        new Uri(
                            @"pack://application:,,,/" + assembly.GetName().Name + ";component/" + pathInApplication,
                            UriKind.Absolute));
            }
            catch
            {
                return null;
            }
        }
    }
}