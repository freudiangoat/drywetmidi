using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Melanchall.DryWetMidi.Devices.Platforms
{
    internal static class MidiOutWinApi
    {
        #region Types

        [StructLayout(LayoutKind.Sequential)]
        public struct MIDIOUTCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public ushort wTechnology;
            public ushort wVoices;
            public ushort wNotes;
            public ushort wChannelMask;
            public uint dwSupport;

            public static MIDIOUTCAPS FromCommon(MidiOutApi.MIDIOUTCAPS caps) =>
                new MIDIOUTCAPS
                {
                    wMid = caps.wMid,
                    wPid = caps.wPid,
                    vDriverVersion = caps.vDriverVersion,
                    szPname = caps.szPname,
                    wTechnology = caps.wTechnology,
                    wVoices = caps.wVoices,
                    wNotes = caps.wNotes,
                    wChannelMask = caps.wChannelMask,
                    dwSupport = caps.dwSupport,
                };

            public MidiOutApi.MIDIOUTCAPS ToCommon() =>
                new MidiOutApi.MIDIOUTCAPS
                {
                    wMid = this.wMid,
                    wPid = this.wPid,
                    vDriverVersion = this.vDriverVersion,
                    szPname = this.szPname,
                    wTechnology = this.wTechnology,
                    wVoices = this.wVoices,
                    wNotes = this.wNotes,
                    wChannelMask = this.wChannelMask,
                    dwSupport = this.dwSupport,
                };
        }

        [Flags]
        public enum MIDICAPS : uint
        {
            MIDICAPS_VOLUME = 1,
            MIDICAPS_LRVOLUME = 2,
            MIDICAPS_CACHE = 4,
            MIDICAPS_STREAM = 8
        }

        #endregion

        #region Methods

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Ansi, EntryPoint = "midiOutGetDevCapsA", ExactSpelling = true)]
        public static extern uint midiOutGetDevCaps(IntPtr uDeviceID, ref MIDIOUTCAPS lpMidiOutCaps, uint cbMidiOutCaps);

        [DllImport("winmm.dll", CharSet = CharSet.Ansi, EntryPoint = "midiOutGetErrorTextA", ExactSpelling = true)]
        public static extern uint midiOutGetErrorText(uint mmrError, StringBuilder pszText, uint cchText);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiOutGetNumDevs();

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiOutOpen(out IntPtr lphmo, int uDeviceID, MidiWinApi.MidiMessageCallback dwCallback, IntPtr dwInstance, uint dwFlags);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiOutClose(IntPtr hmo);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiOutShortMsg(IntPtr hMidiOut, uint dwMsg);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiOutGetVolume(IntPtr hmo, ref uint lpdwVolume);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiOutSetVolume(IntPtr hmo, uint dwVolume);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiOutPrepareHeader(IntPtr hmo, IntPtr lpMidiOutHdr, int cbMidiOutHdr);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiOutUnprepareHeader(IntPtr hmo, IntPtr lpMidiOutHdr, int cbMidiOutHdr);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiOutLongMsg(IntPtr hmo, IntPtr lpMidiOutHdr, int cbMidiOutHdr);

        #endregion

        /// <summary>
        /// Processes MMRESULT which is return value of winmm functions.
        /// </summary>
        /// <param name="mmResult">MMRESULT which is return value of winmm function.</param>
        /// <exception cref="MidiDeviceException"><paramref name="mmResult"/> represents error code.</exception>
        public static void ProcessMmResult(uint mmResult)
        {
            if (mmResult == MidiWinApi.MMSYSERR_NOERROR)
                return;

            var stringBuilder = new StringBuilder((int)MidiWinApi.MaxErrorLength);
            var getErrorTextResult = midiOutGetErrorText(mmResult, stringBuilder, MidiWinApi.MaxErrorLength + 1);
            if (getErrorTextResult != MidiWinApi.MMSYSERR_NOERROR)
                throw new MidiDeviceException("Error occured during operation on device.");

            var errorText = stringBuilder.ToString();
            throw new MidiDeviceException(errorText);
        }
    }
}
