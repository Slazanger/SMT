using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SMT.Interop
{
    /// <summary>
    /// Win32 helper to flash a window caption and taskbar button.
    /// </summary>
    public static class FlashWindow
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        /// <summary>
        /// Flash both the window caption and taskbar button (FLASHW_CAPTION | FLASHW_TRAY).
        /// </summary>
        public const uint FLASHW_ALL = 3;

        private static FLASHWINFO Create_FLASHWINFO(IntPtr handle, uint flags, uint count, uint timeout)
        {
            FLASHWINFO fi = new FLASHWINFO();
            fi.cbSize = Convert.ToUInt32(Marshal.SizeOf(fi));
            fi.hwnd = handle;
            fi.dwFlags = flags;
            fi.uCount = count;
            fi.dwTimeout = timeout;
            return fi;
        }

        public static bool Flash(Window window, uint count)
        {
            if(Win2000OrLater)
            {
                FLASHWINFO fi = Create_FLASHWINFO(new WindowInteropHelper(window).Handle, FLASHW_ALL, count, 0);
                return FlashWindowEx(ref fi);
            }
            return false;
        }

        private static bool Win2000OrLater => Environment.OSVersion.Version.Major >= 5;
    }
}
