using System;
using System.Linq;

namespace Melanchall.DryWetMidi.Devices.Platforms
{
    internal static class MidiInLinuxApi
    {
        internal static void GetDeviceInfo(uint deviceIdx, out string name)
        {
            name = MidiLinuxApi.GlobalSequencer.Value.GetClient((int)deviceIdx)?.Name;
        }

        internal static uint GetDeviceCount() =>
            (uint)MidiLinuxApi.GlobalSequencer.Value.GetClients().Count();

        internal static void Subscribe(out IntPtr midiIn, int deviceIdx, MidiInApi.Callbacks callbacks)
        {
            var client = MidiLinuxApi.GlobalSequencer.Value.GetClient((int)deviceIdx);
            if (client == null)
            {
                throw new MidiDeviceException($"Unable to subscribe to the requested device {deviceIdx}");
            }

            midiIn = MidiLinuxApi.GlobalSequencer.Value.ConnectForReceive(client, callbacks);
            Console.WriteLine($"Input connect: DevID: {deviceIdx} ClientID: {client.Id} Sub: {midiIn}");
        }

        internal static void CloseSubscription(IntPtr subscription)
        {
            MidiLinuxApi.GlobalSequencer.Value.Disconnect(subscription);
        }

        internal static void StartSubscription(IntPtr subscription)
        {
            MidiLinuxApi.GlobalSequencer.Value.Start(subscription);
        }

        internal static void StopSubscription(IntPtr subscription)
        {
            MidiLinuxApi.GlobalSequencer.Value.Stop(subscription);
        }

        internal static void ResetSubscription(IntPtr subscription)
        {
            MidiLinuxApi.GlobalSequencer.Value.Reset(subscription);
        }
  }
}