using System;
using System.Runtime.InteropServices;
using Melanchall.DryWetMidi.Devices.Platforms;

namespace Melanchall.DryWetMidi.Devices
{
    internal static class MidiTimerApi
    {
        #region Types

        [StructLayout(LayoutKind.Sequential)]
        public struct TIMECAPS
        {
            public uint wPeriodMin;
            public uint wPeriodMax;
        }

        public delegate void TimeProc(uint uID, uint uMsg, uint dwUser, uint dw1, uint dw2);

        #endregion

        #region Constants

        public const uint TIME_ONESHOT = 0;
        public const uint TIME_PERIODIC = 1;

        #endregion

        #region Methods

        public static uint timeGetDevCaps(ref TIMECAPS timeCaps, uint sizeTimeCaps)
        {
            if (PlatformUtils.IsOSPlatform(PlatformUtils.OS.Windows))
            {
                var winTimeCaps = default(MidiTimerWinApi.TIMECAPS);
                winTimeCaps.wPeriodMin = timeCaps.wPeriodMin;
                winTimeCaps.wPeriodMax = timeCaps.wPeriodMax;

                var retVal = MidiTimerWinApi.timeGetDevCaps(ref winTimeCaps, sizeTimeCaps);

                timeCaps.wPeriodMin = winTimeCaps.wPeriodMin;
                timeCaps.wPeriodMax = winTimeCaps.wPeriodMax;
                return retVal;
            }

            throw new InvalidOperationException("Unsupported Operating System.");
        }

        public static uint timeBeginPeriod(uint uPeriod)
        {
            if (PlatformUtils.IsOSPlatform(PlatformUtils.OS.Windows))
            {
                return MidiTimerWinApi.timeBeginPeriod(uPeriod);
            }

            throw new InvalidOperationException("Unsupported Operating System.");
        }

        public static uint timeEndPeriod(uint uPeriod)
        {
            if (PlatformUtils.IsOSPlatform(PlatformUtils.OS.Windows))
            {
                return MidiTimerWinApi.timeEndPeriod(uPeriod);
            }

            throw new InvalidOperationException("Unsupported Operating System.");
        }

        public static uint timeSetEvent(uint uDelay, uint uResolution, TimeProc lpTimeProc, IntPtr dwUser, uint fuEvent)
        {
            if (PlatformUtils.IsOSPlatform(PlatformUtils.OS.Windows))
            {
                MidiTimerWinApi.TimeProc winTimeProc = (uID, uMsg, dwUsr, dw1, dw2) => lpTimeProc(uID, uMsg, dwUsr, dw1, dw2);
                return MidiTimerWinApi.timeSetEvent(uDelay, uResolution, winTimeProc, dwUser, fuEvent);
            }

            throw new InvalidOperationException("Unsupported Operating System.");
        }

        public static uint timeKillEvent(uint uTimerID)
        {
            if (PlatformUtils.IsOSPlatform(PlatformUtils.OS.Windows))
            {
                return MidiTimerWinApi.timeKillEvent(uTimerID);
            }

            throw new InvalidOperationException("Unsupported Operating System.");
        }

        #endregion
    }
}