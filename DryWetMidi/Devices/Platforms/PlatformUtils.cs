using System;
using System.Runtime.InteropServices;

namespace Melanchall.DryWetMidi.Devices.Platforms
{
    internal static class PlatformUtils
    {
        internal enum OS
        {
            Windows,
            Linux,
            OSX,
        }

        public static bool IsOSPlatform(OS os)
        {
#if NETFRAMEWORK
            return os == OS.Windows;
#else
            return RuntimeInformation.IsOSPlatform(ConvertOs(os));
#endif
        }

        public static T HandleByPlatform<T>(Func<T> windows, Func<T> linux = null, Func<T> osx = null)
        {
            if (IsOSPlatform(OS.Windows))
            {
                return windows();
            }
            else if (IsOSPlatform(OS.Linux) && linux != null)
            {
                return linux();
            }
            else if (IsOSPlatform(OS.OSX) && osx != null)
            {
                return osx();
            }

            throw new InvalidOperationException("Unsupported function the current operating system.");
        }

        public static void HandleByPlatform(Action windows, Action linux = null, Action osx = null)
        {
            if (IsOSPlatform(OS.Windows))
            {
                windows();
                return;
            }
            else if (IsOSPlatform(OS.Linux) && linux != null)
            {
                linux();
                return;
            }
            else if (IsOSPlatform(OS.OSX) && osx != null)
            {
                osx();
                return;
            }

            throw new InvalidOperationException("Unsupported function the current operating system.");
        }

#if NET || NETCOREAPP || NETSTANDARD
        private static OSPlatform ConvertOs(OS os)
        {
            switch (os)
            {
                case OS.Linux:
                    return OSPlatform.Linux;
                case OS.OSX:
                    return OSPlatform.OSX;
                case OS.Windows:
                    return OSPlatform.Windows;
            }

            throw new InvalidOperationException("Unsupported platform.");
        }
#endif
    }
}