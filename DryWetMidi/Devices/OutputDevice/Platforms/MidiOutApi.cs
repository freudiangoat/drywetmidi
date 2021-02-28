using System;
using System.Runtime.InteropServices;
using System.Text;
using Melanchall.DryWetMidi.Devices.Platforms;

namespace Melanchall.DryWetMidi.Devices
{
    internal static class MidiOutApi
    {
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
        }

        [Flags]
        public enum MIDICAPS : uint
        {
            MIDICAPS_VOLUME = 1,
            MIDICAPS_LRVOLUME = 2,
            MIDICAPS_CACHE = 4,
            MIDICAPS_STREAM = 8
        }

        public static void midiOutGetDevCaps(IntPtr uDeviceID, ref MIDIOUTCAPS lpMidiOutCaps)
        {
            var winCaps = MidiOutWinApi.MIDIOUTCAPS.FromCommon(lpMidiOutCaps);
            lpMidiOutCaps = PlatformUtils.HandleByPlatform(
                () => 
                {
                    var retVal = MidiOutWinApi.midiOutGetDevCaps(uDeviceID, ref winCaps, (uint)Marshal.SizeOf(winCaps));
                    MidiOutWinApi.ProcessMmResult(retVal);
                    return winCaps.ToCommon();
                },
                () => MidiOutLinuxApi.GetDeviceInfo(uDeviceID));
        }

        public static uint midiOutGetNumDevs() =>
            PlatformUtils.HandleByPlatform(
                () => MidiOutWinApi.midiOutGetNumDevs(),
                () => MidiOutLinuxApi.GetDeviceCount());

        public static void midiOutOpen(out IntPtr lphmo, int uDeviceID, MidiWinApi.MidiMessageCallback dwCallback, IntPtr dwInstance, uint dwFlags)
        {
            var ptr = IntPtr.Zero;
            PlatformUtils.HandleByPlatform(
                () => MidiOutWinApi.ProcessMmResult(MidiOutWinApi.midiOutOpen(out ptr, uDeviceID, dwCallback, dwInstance, dwFlags)),
                () => MidiOutLinuxApi.Connect(out ptr, uDeviceID));

            lphmo = ptr;
        }

        public static void midiOutClose(IntPtr hmo) =>
            PlatformUtils.HandleByPlatform(
                () => MidiOutWinApi.ProcessMmResult(MidiOutWinApi.midiOutClose(hmo)),
                () => MidiOutLinuxApi.Close(hmo));

        public static void midiOutShortMsg(IntPtr hMidiOut, uint dwMsg) =>
            PlatformUtils.HandleByPlatform(
                () => MidiOutWinApi.ProcessMmResult(MidiOutWinApi.midiOutShortMsg(hMidiOut, dwMsg)),
                () => MidiOutLinuxApi.SendMessage(hMidiOut, dwMsg));

        public static void midiOutGetVolume(IntPtr hmo, ref uint lpdwVolume)
        {
            var volume = lpdwVolume;
            PlatformUtils.HandleByPlatform(
                () => MidiOutWinApi.ProcessMmResult(MidiOutWinApi.midiOutGetVolume(hmo, ref volume)));

            lpdwVolume = volume;
        }

        public static void midiOutSetVolume(IntPtr hmo, uint dwVolume) =>
            PlatformUtils.HandleByPlatform(
                () => MidiOutWinApi.ProcessMmResult(MidiOutWinApi.midiOutSetVolume(hmo, dwVolume)));

        public static void midiOutPrepareHeader(IntPtr hmo, IntPtr lpMidiOutHdr, int cbMidiOutHdr) =>
            PlatformUtils.HandleByPlatform(
                () => MidiOutWinApi.ProcessMmResult(MidiOutWinApi.midiOutPrepareHeader(hmo, lpMidiOutHdr, cbMidiOutHdr)));

        public static uint midiOutUnprepareHeader(IntPtr hmo, IntPtr lpMidiOutHdr, int cbMidiOutHdr) =>
            PlatformUtils.HandleByPlatform(
                () => MidiOutWinApi.midiOutUnprepareHeader(hmo, lpMidiOutHdr, cbMidiOutHdr));

        public static void midiOutLongMsg(IntPtr hmo, IntPtr lpMidiOutHdr, int cbMidiOutHdr) =>
            PlatformUtils.HandleByPlatform(
                () => MidiOutWinApi.ProcessMmResult(MidiOutWinApi.midiOutLongMsg(hmo, lpMidiOutHdr, cbMidiOutHdr)));
    }
}