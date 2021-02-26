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