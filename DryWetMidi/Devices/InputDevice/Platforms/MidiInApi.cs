using System;
using System.Runtime.InteropServices;
using System.Text;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Devices.Platforms;

namespace Melanchall.DryWetMidi.Devices
{
    internal static class MidiInApi
    {
        #region Types

        internal struct MIDIINCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            public string szPname;
        }

        internal delegate void OnEventReceived(MidiEvent evt);
        internal delegate void OnTimeCodeRaised(MidiTimeCodeEvent midiTimeCodeEvent);
        internal delegate void OnError(Exception ex);
        internal delegate void OnInvalidShortEventReceived(byte status, byte firstData, byte secondData);
        internal delegate void OnInvalidSysExEventReceived(byte[] data);

        internal struct Callbacks
        {
            internal OnEventReceived OnEventReceived;
            internal OnTimeCodeRaised OnRaiseTimeCode;
            internal OnError OnError;
            internal OnInvalidShortEventReceived OnInvalidShortEventReceived;
            internal OnInvalidSysExEventReceived OnInvalidSysExEventReceived;
        }

        #endregion

        #region Methods

        public static void midiInGetDevCaps(uint uDeviceID, out MIDIINCAPS caps)
        {
            var result = PlatformUtils.HandleByPlatform(
                () =>
                {
                    var winCaps = new MidiInWinApi.MIDIINCAPS();
                    MidiInWinApi.ProcessMmResult(MidiInWinApi.midiInGetDevCaps(uDeviceID, ref winCaps, (uint)Marshal.SizeOf(winCaps)));
                    return winCaps.ToCommon();
                },
                () =>
                {
                    Console.WriteLine("Getting device info");
                    string name;
                    MidiInLinuxApi.GetDeviceInfo(uDeviceID, out name);
                    var linuxCaps = new MIDIINCAPS
                    {
                        wMid = 0,
                        wPid = 0,
                        vDriverVersion = 0,
                        szPname = name,
                    };

                    Console.WriteLine($"Getting device info: {name}");

                    return linuxCaps;
                }
            );
            
            caps = result;
        }

        public static uint midiInGetNumDevs() =>
            PlatformUtils.HandleByPlatform(
                () => MidiInWinApi.midiInGetNumDevs(),
                () => MidiInLinuxApi.GetDeviceCount());

        public static void midiInOpen(out IntPtr lphMidiIn, int uDeviceID, Callbacks callbacks)
        {
            var midiIn = default(IntPtr);
            PlatformUtils.HandleByPlatform(
                () => MidiInWinApi.ProcessMmResult(MidiInWinApi.midiInOpen(out midiIn, uDeviceID, MidiInWinApi.GetMessageCallback(callbacks), IntPtr.Zero, MidiWinApi.CallbackFunction)),
                () => MidiInLinuxApi.Subscribe(out midiIn, uDeviceID, callbacks));

            lphMidiIn = midiIn;
        }

        public static void midiInClose(IntPtr hMidiIn) =>
            PlatformUtils.HandleByPlatform(
                () => MidiInWinApi.ProcessMmResult(MidiInWinApi.midiInClose(hMidiIn)),
                () => MidiInLinuxApi.CloseSubscription(hMidiIn));

        public static void midiInStart(IntPtr hMidiIn) =>
            PlatformUtils.HandleByPlatform(
                () => MidiInWinApi.ProcessMmResult(MidiInWinApi.midiInStart(hMidiIn)),
                () => MidiInLinuxApi.StartSubscription(hMidiIn));

        public static void midiInStop(IntPtr hMidiIn) =>
            PlatformUtils.HandleByPlatform(
                () => MidiInWinApi.ProcessMmResult(MidiInWinApi.midiInStop(hMidiIn)),
                () => MidiInLinuxApi.StopSubscription(hMidiIn));

        public static void midiInReset(IntPtr hMidiIn) =>
            PlatformUtils.HandleByPlatform(
                () => MidiInWinApi.ProcessMmResult(MidiInWinApi.midiInReset(hMidiIn)),
                () => MidiInLinuxApi.ResetSubscription(hMidiIn));

        #endregion
    }
}
