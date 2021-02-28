using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Melanchall.DryWetMidi.Core;

namespace Melanchall.DryWetMidi.Devices.Platforms
{
    internal static class MidiInWinApi
    {
        private const int SysExBufferLength = 2048;
        private static IDictionary<IntPtr, IntPtr> _sysExHeaderPointers = new Dictionary<IntPtr, IntPtr>();

        #region Types

        [StructLayout(LayoutKind.Sequential)]
        internal struct MIDIINCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint dwSupport;

            public static MIDIINCAPS FromCommon(MidiInApi.MIDIINCAPS caps) =>
                new MIDIINCAPS
                {
                    wMid = caps.wMid,
                    wPid = caps.wPid,
                    vDriverVersion = caps.vDriverVersion,
                    szPname = caps.szPname,
                    dwSupport = 0
                };

            public MidiInApi.MIDIINCAPS ToCommon() =>
                new MidiInApi.MIDIINCAPS
                {
                    wMid = wMid,
                    wPid = wPid,
                    vDriverVersion = vDriverVersion,
                    szPname = szPname,
                };
        }

        #endregion

        #region Methods

        [DllImport("winmm.dll", SetLastError = true, CharSet = CharSet.Ansi, EntryPoint = "midiInGetDevCapsA", ExactSpelling = true)]
        public static extern uint midiInGetDevCaps(uint uDeviceID, ref MIDIINCAPS caps, uint cbMidiInCaps);

        [DllImport("winmm.dll", CharSet = CharSet.Ansi, EntryPoint = "midiInGetErrorTextA", ExactSpelling = true)]
        public static extern uint midiInGetErrorText(uint wError, StringBuilder lpText, uint cchText);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiInGetNumDevs();

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiInOpen(out IntPtr lphMidiIn, int uDeviceID, MidiWinApi.MidiMessageCallback dwCallback, IntPtr dwInstance, uint dwFlags);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiInClose(IntPtr hMidiIn);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiInStart(IntPtr hMidiIn);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiInStop(IntPtr hMidiIn);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiInReset(IntPtr hMidiIn);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiInPrepareHeader(IntPtr hMidiIn, IntPtr lpMidiInHdr, int cbMidiInHdr);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiInUnprepareHeader(IntPtr hMidiIn, IntPtr lpMidiInHdr, int cbMidiInHdr);

        [DllImport("winmm.dll", ExactSpelling = true)]
        public static extern uint midiInAddBuffer(IntPtr hMidiIn, IntPtr lpMidiInHdr, int cbMidiInHdr);

        #endregion

        #region Message Callback Methods

        internal static void Cleanup(IntPtr handle)
        {
            IntPtr sysExHeaderPointer;
            if (!_sysExHeaderPointers.TryGetValue(handle, out sysExHeaderPointer))
            {
                return;
            }

            UnprepareSysExBuffer(handle, sysExHeaderPointer);
        }

        internal static MidiWinApi.MidiMessageCallback GetMessageCallback(MidiInApi.Callbacks callbacks)
        {
            return (IntPtr hMidi, MidiMessage wMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2) =>
            {
                switch (wMsg)
                {
                    case MidiMessage.MIM_DATA:
                    case MidiMessage.MIM_MOREDATA:
                        OnShortMessage(callbacks, dwParam1.ToInt32());
                        break;

                    case MidiMessage.MIM_LONGDATA:
                        OnSysExMessage(hMidi, callbacks, dwParam1);
                        break;

                    case MidiMessage.MIM_ERROR:
                        byte statusByte, firstDataByte, secondDataByte;
                        MidiWinApi.UnpackShortEventBytes(dwParam1.ToInt32(), out statusByte, out firstDataByte, out secondDataByte);
                        callbacks.OnInvalidShortEventReceived?.Invoke(statusByte, firstDataByte, secondDataByte);
                        break;

                    case MidiMessage.MIM_LONGERROR:
                        callbacks.OnInvalidSysExEventReceived?.Invoke(MidiWinApi.UnpackSysExBytes(dwParam1));
                        break;
                }
            };
        }

        private static void OnShortMessage(MidiInApi.Callbacks callbacks, int message)
        {
            try
            {
                byte statusByte, firstDataByte, secondDataByte;
                MidiWinApi.UnpackShortEventBytes(message, out statusByte, out firstDataByte, out secondDataByte);

                var bytesToMidiEventConverter = new BytesToMidiEventConverter(2);
                var midiEvent = bytesToMidiEventConverter.Convert(statusByte, new[] { firstDataByte, secondDataByte });
                callbacks.OnEventReceived?.Invoke(midiEvent);

                var midiTimeCodeEvent = midiEvent as MidiTimeCodeEvent;
                if (midiTimeCodeEvent != null)
                    callbacks.OnRaiseTimeCode?.Invoke(midiTimeCodeEvent);
            }
            catch (Exception ex)
            {
                var exception = new MidiDeviceException($"Failed to parse short message.", ex);
                exception.Data.Add("Message", message);
                callbacks.OnError?.Invoke(exception);
            }
        }

        private static void OnSysExMessage(IntPtr handle, MidiInApi.Callbacks callbacks, IntPtr sysExHeaderPointer)
        {
            byte[] data = null;

            try
            {
                data = MidiWinApi.UnpackSysExBytes(sysExHeaderPointer);
                var midiEvent = new NormalSysExEvent(data);
                callbacks.OnEventReceived?.Invoke(midiEvent);

                UnprepareSysExBuffer(handle, sysExHeaderPointer);
                PrepareSysExBuffer(handle);
            }
            catch (Exception ex)
            {
                var exception = new MidiDeviceException($"Failed to parse system exclusive message.", ex);
                exception.Data.Add("Data", data);
                callbacks.OnError?.Invoke(exception);
            }
        }

        private static void PrepareSysExBuffer(IntPtr handle)
        {
            var header = new MidiWinApi.MIDIHDR
            {
                lpData = Marshal.AllocHGlobal(SysExBufferLength),
                dwBufferLength = SysExBufferLength,
                dwBytesRecorded = SysExBufferLength
            };

            var sysExHeaderPointer = Marshal.AllocHGlobal(MidiWinApi.MidiHeaderSize);
            _sysExHeaderPointers[handle] = sysExHeaderPointer;
            Marshal.StructureToPtr(header, sysExHeaderPointer, false);

            ProcessMmResult(MidiInWinApi.midiInPrepareHeader(handle, sysExHeaderPointer, MidiWinApi.MidiHeaderSize));
            ProcessMmResult(MidiInWinApi.midiInAddBuffer(handle, sysExHeaderPointer, MidiWinApi.MidiHeaderSize));
        }

        private static void UnprepareSysExBuffer(IntPtr handle, IntPtr headerPointer)
        {
            if (headerPointer == IntPtr.Zero)
                return;

            MidiInWinApi.midiInUnprepareHeader(handle, headerPointer, MidiWinApi.MidiHeaderSize);

            var header = (MidiWinApi.MIDIHDR)Marshal.PtrToStructure(headerPointer, typeof(MidiWinApi.MIDIHDR));
            Marshal.FreeHGlobal(header.lpData);
            Marshal.FreeHGlobal(headerPointer);
        }

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
            var getErrorTextResult = MidiInWinApi.midiInGetErrorText(mmResult, stringBuilder, MidiWinApi.MaxErrorLength + 1);
            if (getErrorTextResult != MidiWinApi.MMSYSERR_NOERROR)
                throw new MidiDeviceException("Error occured during operation on device.");

            var errorText = stringBuilder.ToString();
            throw new MidiDeviceException(errorText);
        }

        #endregion
    }
}
