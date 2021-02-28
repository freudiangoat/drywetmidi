using System;

namespace Melanchall.DryWetMidi.Devices.Platforms
{
    internal static class MidiOutLinuxApi
    {
        public static MidiOutApi.MIDIOUTCAPS GetDeviceInfo(IntPtr deviceId)
        {
            var device = MidiLinuxApi.GlobalSequencer.Value.GetClient((int)deviceId);

            return new MidiOutApi.MIDIOUTCAPS
            {
                szPname = device.Name,
                wMid = 0,
                wPid = 0,
                wTechnology = ConvertTechnology(device.Type),
                wChannelMask = 0,
                wNotes = 0,
                wVoices = 0,
                vDriverVersion = 0,
                dwSupport = 0,
            };
        }

        internal static uint GetDeviceCount() =>
            (uint)MidiLinuxApi.GlobalSequencer.Value.GetClients().Count;

        internal static void Connect(out IntPtr subscription, int uDeviceID)
        {
            var client = MidiLinuxApi.GlobalSequencer.Value.GetClient(uDeviceID);
            subscription = MidiLinuxApi.GlobalSequencer.Value.ConnectForSend(client, null);
            Console.WriteLine($"Output connect: DevID: {uDeviceID} ClientID: {client.Id} Sub: {subscription}");
        }

        internal static void SendMessage(IntPtr subscription, uint msg)
        {
            // Console.WriteLine($"Sending MIDI message via port {subscription}.");
            MidiLinuxApi.GlobalSequencer.Value.SendMsg(subscription, msg);
        }

        internal static void Close(IntPtr subscription)
        {
            MidiLinuxApi.GlobalSequencer.Value.Disconnect(subscription);
        }

        private static ushort ConvertTechnology(MidiLinuxApi.Sequencer.PortTypes type)
        {
            switch (type)
            {
                case MidiLinuxApi.Sequencer.PortTypes.MidiPort:
                    return 1;
                case MidiLinuxApi.Sequencer.PortTypes.Synthesizer:
                    return 2;
                case MidiLinuxApi.Sequencer.PortTypes.Wavetable:
                    return 6;
                case MidiLinuxApi.Sequencer.PortTypes.Software:
                    return 7;
            }

            return 0;
        }
    }
}