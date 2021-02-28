using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Melanchall.DryWetMidi.Core;

namespace Melanchall.DryWetMidi.Devices
{
    internal static class MidiLinuxApi
    {
        internal static Lazy<Sequencer> GlobalSequencer = new Lazy<Sequencer>(() => Sequencer.Open("drywetmidi"));

        #region Imports

        [DllImport("libasound.so", CharSet = CharSet.Ansi)]
        private static extern int snd_seq_open(ref IntPtr handle, string name, int streams, int mode);

        [DllImport("libasound.so")]
        private static extern int snd_seq_close(IntPtr handle);

        [DllImport("libasound.so", CharSet = CharSet.Ansi)]
        private static extern int snd_seq_set_client_name(IntPtr handle, string name);

        [DllImport("libasound.so")]
        private static extern int snd_seq_client_info_malloc(ref IntPtr info);

        [DllImport("libasound.so")]
        private static extern void snd_seq_client_info_free(IntPtr info);

        [DllImport("libasound.so")]
        private static extern int snd_seq_client_info_get_client(IntPtr info);

        [DllImport("libasound.so")]
        private static extern void snd_seq_client_info_set_client(IntPtr info, int client);

        [DllImport("libasound.so")]
        private static extern int snd_seq_query_next_client(IntPtr handle, IntPtr info);

        [DllImport("libasound.so")]
        private static extern int snd_seq_query_next_port(IntPtr handle, IntPtr info);

        [DllImport("libasound.so")]
        private static extern int snd_seq_port_info_malloc(ref IntPtr info);

        [DllImport("libasound.so")]
        private static extern void snd_seq_port_info_free(IntPtr info);

        [DllImport("libasound.so")]
        private static extern void snd_seq_port_info_set_client(IntPtr info, int client);

        [DllImport("libasound.so")]
        private static extern void snd_seq_port_info_set_port(IntPtr info, int port);

        [DllImport("libasound.so")]
        private static extern uint snd_seq_port_info_get_capability(IntPtr info);

        [DllImport("libasound.so")]
        private static extern uint snd_seq_port_info_get_type(IntPtr info);

        [DllImport("libasound.so")]
        private static extern int snd_seq_port_info_get_client(IntPtr info);

        [DllImport("libasound.so")]
        private static extern int snd_seq_port_info_get_port(IntPtr info);

        [DllImport("libasound.so")]
        private static extern IntPtr snd_seq_client_info_get_name(IntPtr info);

        [DllImport("libasound.so")]
        private static extern IntPtr snd_seq_port_info_get_name(IntPtr info);

        [DllImport("libasound.so")]
        private static extern int snd_seq_create_simple_port(IntPtr handle, string name, uint caps, uint type);

        [DllImport("libasound.so")]
        private static extern int snd_seq_connect_from(IntPtr handle, int myport, int src_client, int src_port);

        [DllImport("libasound.so")]
        private static extern int snd_seq_connect_to(IntPtr handle, int myport, int dst_client, int dst_port);

        [DllImport("libasound.so")]
        private static extern int snd_seq_poll_descriptors_count(IntPtr handle, short events);
        
        [DllImport("libasound.so")]
        private static extern int snd_seq_poll_descriptors(IntPtr handle, [In, Out] Pollfd[] descriptors, int numberPollDescriptors, short pollIn);

        [DllImport("libasound.so")]
        private static extern int snd_seq_event_input(IntPtr handle, ref IntPtr evt);

        [DllImport("libasound.so")]
        private static extern int snd_seq_event_output(IntPtr handle, ref SequencerEvent evt);

        [DllImport("libasound.so")]
        private static extern int snd_seq_drain_output(IntPtr handle);

        [DllImport("libasound.so")]
        private static extern int snd_midi_event_new(IntPtr bufsize, ref IntPtr midi);

        [DllImport("libasound.so")]
        private static extern void snd_midi_event_free(IntPtr midi);

        [DllImport("libasound.so")]
        private static extern void snd_midi_event_init(IntPtr midi);

        [DllImport("libasound.so")]
        private static extern long snd_midi_event_decode(IntPtr midi, [Out] byte[] buf, long count, IntPtr evt);

        [DllImport("libasound.so")]
        private static extern long snd_midi_event_encode(IntPtr midi, [In] byte[] buf, long count, ref SequencerEvent evt);

        [DllImport("libasound.so")]
        private static extern int snd_midi_event_reset_decode(IntPtr midi);

        [DllImport("libasound.so")]
        private static extern int snd_midi_event_reset_encode(IntPtr midi);

        [DllImport("libasound.so", EntryPoint = "snd_strerror")]
        private static extern IntPtr _snd_strerror(int err);

        private static string snd_strerror(int err)
        {
            var ptr = _snd_strerror(err);
            return Marshal.PtrToStringAnsi(ptr);
        }


        [DllImport("libc", EntryPoint = "poll")]
        private static extern int sys_poll([In, Out] Pollfd[] fds, uint nfds, int timeout);

        #endregion

        #region Imported structures

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct RealTime
        {
            internal uint Seconds;
            internal uint NanoSeconds;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct SequencerTimestamp
        {
            [FieldOffset(0)]
            internal int Tick;

            [FieldOffset(0)]
            internal RealTime Time;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SequencerAddress
        {
            internal byte Client;
            internal byte Port;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct NoteEvent
        {
            internal byte Channel;
            internal byte Note;
            internal byte Velocity;
            internal byte OffVelocity;
            internal uint Duration; 
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct ControlEvent
        {
            [FieldOffset(0)]
            internal byte Channel;

            [FieldOffset(4)]
            internal uint Parameter;

            [FieldOffset(8)]
            internal int Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Raw8Event
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            internal byte[] Data;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Raw32Event
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            internal uint[] Data;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ExternalEvent
        {
            internal uint Length;

            internal IntPtr Ptr;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct QueueControlEvent
        {
            [FieldOffset(0)]
            internal byte Queue;

            [FieldOffset(4)]
            internal int Value;

            [FieldOffset(4)]
            internal SequencerTimestamp Time;

            [FieldOffset(4)]
            internal uint Position;

            [FieldOffset(4)]
            internal QueueSkew Skew;

            /*[FieldOffset(4)]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            internal uint[] Data32;

            [FieldOffset(4)]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            internal byte[] Data8;*/
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct QueueSkew
        {
            internal uint Value;
            internal uint Base;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct Connect
        {
            internal SequencerAddress Sender;
            internal SequencerAddress Destination;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct ResultData
        {
            internal int Event;
            internal int Result;
        }

        internal enum snd_seq_event_type : byte
        {
            /** system status; event data type = #snd_seq_result_t */
            SND_SEQ_EVENT_SYSTEM = 0,
            /** returned result status; event data type = #snd_seq_result_t */
            SND_SEQ_EVENT_RESULT,

            /** note on and off with duration; event data type = #snd_seq_ev_note_t */
            SND_SEQ_EVENT_NOTE = 5,
            /** note on; event data type = #snd_seq_ev_note_t */
            SND_SEQ_EVENT_NOTEON,
            /** note off; event data type = #snd_seq_ev_note_t */
            SND_SEQ_EVENT_NOTEOFF,
            /** key pressure change (aftertouch); event data type = #snd_seq_ev_note_t */
            SND_SEQ_EVENT_KEYPRESS,
            
            /** controller; event data type = #snd_seq_ev_ctrl_t */
            SND_SEQ_EVENT_CONTROLLER = 10,
            /** program change; event data type = #snd_seq_ev_ctrl_t */
            SND_SEQ_EVENT_PGMCHANGE,
            /** channel pressure; event data type = #snd_seq_ev_ctrl_t */
            SND_SEQ_EVENT_CHANPRESS,
            /** pitchwheel; event data type = #snd_seq_ev_ctrl_t; data is from -8192 to 8191) */
            SND_SEQ_EVENT_PITCHBEND,
            /** 14 bit controller value; event data type = #snd_seq_ev_ctrl_t */
            SND_SEQ_EVENT_CONTROL14,
            /** 14 bit NRPN;  event data type = #snd_seq_ev_ctrl_t */
            SND_SEQ_EVENT_NONREGPARAM,
            /** 14 bit RPN; event data type = #snd_seq_ev_ctrl_t */
            SND_SEQ_EVENT_REGPARAM,

            /** SPP with LSB and MSB values; event data type = #snd_seq_ev_ctrl_t */
            SND_SEQ_EVENT_SONGPOS = 20,
            /** Song Select with song ID number; event data type = #snd_seq_ev_ctrl_t */
            SND_SEQ_EVENT_SONGSEL,
            /** midi time code quarter frame; event data type = #snd_seq_ev_ctrl_t */
            SND_SEQ_EVENT_QFRAME,
            /** SMF Time Signature event; event data type = #snd_seq_ev_ctrl_t */
            SND_SEQ_EVENT_TIMESIGN,
            /** SMF Key Signature event; event data type = #snd_seq_ev_ctrl_t */
            SND_SEQ_EVENT_KEYSIGN,
                    
            /** MIDI Real Time Start message; event data type = #snd_seq_ev_queue_control_t */
            SND_SEQ_EVENT_START = 30,
            /** MIDI Real Time Continue message; event data type = #snd_seq_ev_queue_control_t */
            SND_SEQ_EVENT_CONTINUE,
            /** MIDI Real Time Stop message; event data type = #snd_seq_ev_queue_control_t */
            SND_SEQ_EVENT_STOP,
            /** Set tick queue position; event data type = #snd_seq_ev_queue_control_t */
            SND_SEQ_EVENT_SETPOS_TICK,
            /** Set real-time queue position; event data type = #snd_seq_ev_queue_control_t */
            SND_SEQ_EVENT_SETPOS_TIME,
            /** (SMF) Tempo event; event data type = #snd_seq_ev_queue_control_t */
            SND_SEQ_EVENT_TEMPO,
            /** MIDI Real Time Clock message; event data type = #snd_seq_ev_queue_control_t */
            SND_SEQ_EVENT_CLOCK,
            /** MIDI Real Time Tick message; event data type = #snd_seq_ev_queue_control_t */
            SND_SEQ_EVENT_TICK,
            /** Queue timer skew; event data type = #snd_seq_ev_queue_control_t */
            SND_SEQ_EVENT_QUEUE_SKEW,
            /** Sync position changed; event data type = #snd_seq_ev_queue_control_t */
            SND_SEQ_EVENT_SYNC_POS,

            /** Tune request; event data type = none */
            SND_SEQ_EVENT_TUNE_REQUEST = 40,
            /** Reset to power-on state; event data type = none */
            SND_SEQ_EVENT_RESET,
            /** Active sensing event; event data type = none */
            SND_SEQ_EVENT_SENSING,

            /** Echo-back event; event data type = any type */
            SND_SEQ_EVENT_ECHO = 50,
            /** OSS emulation raw event; event data type = any type */
            SND_SEQ_EVENT_OSS,

            /** New client has connected; event data type = #snd_seq_addr_t */
            SND_SEQ_EVENT_CLIENT_START = 60,
            /** Client has left the system; event data type = #snd_seq_addr_t */
            SND_SEQ_EVENT_CLIENT_EXIT,
            /** Client status/info has changed; event data type = #snd_seq_addr_t */
            SND_SEQ_EVENT_CLIENT_CHANGE,
            /** New port was created; event data type = #snd_seq_addr_t */
            SND_SEQ_EVENT_PORT_START,
            /** Port was deleted from system; event data type = #snd_seq_addr_t */
            SND_SEQ_EVENT_PORT_EXIT,
            /** Port status/info has changed; event data type = #snd_seq_addr_t */
            SND_SEQ_EVENT_PORT_CHANGE,

            /** Ports connected; event data type = #snd_seq_connect_t */
            SND_SEQ_EVENT_PORT_SUBSCRIBED,
            /** Ports disconnected; event data type = #snd_seq_connect_t */
            SND_SEQ_EVENT_PORT_UNSUBSCRIBED,

            /** user-defined event; event data type = any (fixed size) */
            SND_SEQ_EVENT_USR0 = 90,
            /** user-defined event; event data type = any (fixed size) */
            SND_SEQ_EVENT_USR1,
            /** user-defined event; event data type = any (fixed size) */
            SND_SEQ_EVENT_USR2,
            /** user-defined event; event data type = any (fixed size) */
            SND_SEQ_EVENT_USR3,
            /** user-defined event; event data type = any (fixed size) */
            SND_SEQ_EVENT_USR4,
            /** user-defined event; event data type = any (fixed size) */
            SND_SEQ_EVENT_USR5,
            /** user-defined event; event data type = any (fixed size) */
            SND_SEQ_EVENT_USR6,
            /** user-defined event; event data type = any (fixed size) */
            SND_SEQ_EVENT_USR7,
            /** user-defined event; event data type = any (fixed size) */
            SND_SEQ_EVENT_USR8,
            /** user-defined event; event data type = any (fixed size) */
            SND_SEQ_EVENT_USR9,

            /** system exclusive data (variable length);  event data type = #snd_seq_ev_ext_t */
            SND_SEQ_EVENT_SYSEX = 130,
            /** error event;  event data type = #snd_seq_ev_ext_t */
            SND_SEQ_EVENT_BOUNCE,
            /** reserved for user apps;  event data type = #snd_seq_ev_ext_t */
            SND_SEQ_EVENT_USR_VAR0 = 135,
            /** reserved for user apps; event data type = #snd_seq_ev_ext_t */
            SND_SEQ_EVENT_USR_VAR1,
            /** reserved for user apps; event data type = #snd_seq_ev_ext_t */
            SND_SEQ_EVENT_USR_VAR2,
            /** reserved for user apps; event data type = #snd_seq_ev_ext_t */
            SND_SEQ_EVENT_USR_VAR3,
            /** reserved for user apps; event data type = #snd_seq_ev_ext_t */
            SND_SEQ_EVENT_USR_VAR4,

            /** NOP; ignored in any case */
            SND_SEQ_EVENT_NONE = 255
        };

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private struct SequencerEvent
        {
            [FieldOffset(0)]
            public snd_seq_event_type Type;

            [FieldOffset(1)]
            public byte Flags;

            [FieldOffset(2)]
            public byte Tag;

            [FieldOffset(3)]
            public byte Queue;

            [FieldOffset(4)]
            public SequencerTimestamp Time;

            [FieldOffset(12)]
            public SequencerAddress Source;

            [FieldOffset(14)]
            public SequencerAddress Dest;

            [FieldOffset(16)]
            public NoteEvent Note;

            [FieldOffset(16)]
            public ControlEvent Control;

            /*[FieldOffset(16)]
            public Raw8Event Raw8;

            [FieldOffset(16)]
            public Raw32Event Raw32;*/

            [FieldOffset(16)]
            public ExternalEvent External;

            [FieldOffset(16)]
            public QueueControlEvent QueueControl;

            [FieldOffset(16)]
            public SequencerTimestamp Timestamp;

            [FieldOffset(16)]
            public SequencerAddress Address;

            [FieldOffset(16)]
            public Connect Connect;

            [FieldOffset(16)]
            public ResultData Result;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Pollfd
        {
            public int fd;
            public short events;
            public short revents;
        }

        #endregion

        internal class Sequencer : IDisposable
        {
            internal const int OpenOutput = 1;
            internal const int OpenInput = 2;
            internal const int OpenDuplex = OpenOutput | OpenInput;

            internal const int OpenModeBlocking = 0;
            internal const int OpenModeNonBlocking = 1;

            internal const uint PortReadCapability = 1 << 0;
            internal const uint PortSubsReadCapability = 1 << 5;
            internal const uint PortFullReadCapability = PortReadCapability | PortSubsReadCapability;

            internal const uint PortWriteCapability = 1 << 1;
            internal const uint PortSubsWriteCapability = 1 << 6;
            internal const uint PortFullWriteCapability = PortWriteCapability | PortSubsWriteCapability;

            internal const uint PortTypeGeneric = 1 << 1;
            internal const uint PortTypeApplication = 1 << 20;

            internal const short PollIn = 0x0001;
            internal const short PollOut = 0x0004;

            [Flags]
            internal enum PortTypes
            {
                Wavetable = 1 << 16,
                Software = 1 << 17,
                Synthesizer = 1 << 18,
                MidiPort = 1 << 19,
            }

            private readonly IDictionary<IntPtr, MidiInApi.Callbacks> _callbacks;
            private readonly IDictionary<string, BytesToMidiEventConverter> _midiEventReaders;
            private readonly IDictionary<int, byte> _connectedPorts;
            private readonly IDictionary<byte, int> _portToClient;
            private readonly IntPtr _handle;

            private Thread _listenThread;
            private HashSet<IntPtr> _activeCallbacks;
            private int _portIdx;
            private bool disposedValue;

            private Sequencer(IntPtr handle)
            {
                _handle = handle;
                _callbacks = new Dictionary<IntPtr, MidiInApi.Callbacks>();
                _midiEventReaders = new Dictionary<string, BytesToMidiEventConverter>();
                _connectedPorts = new Dictionary<int, byte>();
                _portToClient = new Dictionary<byte, int>();
                _activeCallbacks = new HashSet<IntPtr>();
            }

            ~Sequencer()
            {
                Dispose(disposing: false);
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            internal static Sequencer Open(string name)
            {
                var handle = default(IntPtr);
                var error = snd_seq_open(ref handle, "default", OpenDuplex, OpenModeBlocking);

                if (error != 0)
                {
                    throw new MidiDeviceException("Unable to open sequencer");
                }

                error = snd_seq_set_client_name(handle, name);

                if (error != 0)
                {
                    throw new MidiDeviceException("Unable to name the sequencer client");
                }

                return new Sequencer(handle);
            }

            internal IReadOnlyList<Client> GetClients()
            {
                var clientInfo = default(IntPtr);
                var portInfo = default(IntPtr);
                snd_seq_client_info_malloc(ref clientInfo);
                snd_seq_port_info_malloc(ref portInfo);

                snd_seq_client_info_set_client(clientInfo, -1);

                var clients = new List<Client>();
                var idx = 0u;
                while (snd_seq_query_next_client(_handle, clientInfo) >= 0)
                {
                    var clientId = snd_seq_client_info_get_client(clientInfo);

                    var clientNamePtr = snd_seq_client_info_get_name(clientInfo);
                    var clientName = Marshal.PtrToStringAuto(clientNamePtr);

                    snd_seq_port_info_set_client(portInfo, clientId);
                    snd_seq_port_info_set_port(portInfo, 0);
                    var portType = snd_seq_port_info_get_type(portInfo);

                    clients.Add(new Client(idx++, (byte)clientId, clientName, portType));
                }

                snd_seq_port_info_free(portInfo);
                snd_seq_client_info_free(clientInfo);

                return clients;
            }

            internal Client GetClient(int idx)
            {
                var clients = GetClients();
                if (idx >= clients.Count || idx < 0)
                {
                    return null;
                }

                return clients[idx];
            }

            internal IntPtr ConnectForReceive(Client client, MidiInApi.Callbacks? callback) =>
                Connect(client, callback, true);

            internal IntPtr ConnectForSend(Client client, MidiInApi.Callbacks? callback) =>
                Connect(client, callback, false);

            private IntPtr Connect(Client client, MidiInApi.Callbacks? callback, bool receive)
            {
                var portNumber = CreatePort();
                if (receive)
                {
                    ConnectPortReceive(portNumber, client.Id, 0);
                }
                else
                {
                    ConnectPortSend(portNumber, client.Id, 0);
                }

                _connectedPorts[client.Id] = portNumber;

                Console.WriteLine($"Connected via local port: {portNumber} to {client.Id}");
                _portToClient[portNumber] = client.Id;

                var ptr = new IntPtr(portNumber);

                if(callback != null)
                {
                    _callbacks[ptr] = callback.Value;
                }

                return ptr;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // TODO: dispose managed state (managed objects)
                    }

                    snd_seq_close(_handle);
                    disposedValue = true;
                }
            }

            private void ConnectPortReceive(int localPort, int remoteClient, int remotePort)
            {
                var err = snd_seq_connect_from(_handle, localPort, remoteClient, remotePort);

                if (err < 0)
                {
                    throw new MidiDeviceException($"Unable to connect port {localPort} to client {remoteClient}:{remotePort}");
                }
            }

            private void ConnectPortSend(int localPort, int remoteClient, int remotePort)
            {
                var err = snd_seq_connect_to(_handle, localPort, remoteClient, remotePort);

                if (err < 0)
                {
                    throw new MidiDeviceException($"Unable to connect port {localPort} to client {remoteClient}:{remotePort}");
                }
            }

            private byte CreatePort()
            {
                var idx = Interlocked.Increment(ref _portIdx);
                var portNumber = (byte)snd_seq_create_simple_port(
                    _handle,
                    $"port{idx}",
                    PortFullWriteCapability | PortFullReadCapability,
                    PortTypeApplication | PortTypeGeneric);

                Console.WriteLine($"Creating Port {portNumber}");

                if (portNumber < 0)
                {
                    throw new MidiDeviceException("Unable to create port");
                }

                return portNumber;
            }

            internal void Disconnect(IntPtr subscription)
            {
                if (_callbacks.ContainsKey(subscription))
                {
                    _callbacks.Remove(subscription);
                    _activeCallbacks.Remove(subscription);
                }

                var portNumber = (byte)subscription.ToInt32();
                if (_portToClient.ContainsKey(portNumber))
                {
                    var clientId = _portToClient[portNumber];
                    _connectedPorts.Remove(clientId);
                    _portToClient.Remove(portNumber);
                }
            }

            internal void Start(IntPtr subscription)
            {
                if (!_callbacks.ContainsKey(subscription))
                {
                    return;
                }

                _activeCallbacks.Add(subscription);
                StartListener();
            }

            internal void Stop(IntPtr subscription)
            {
                if (!_callbacks.ContainsKey(subscription))
                {
                    return;
                }

                _activeCallbacks.Remove(subscription);
            }

            internal void Reset(IntPtr subscription) =>
                Stop(subscription);

            private void StartListener()
            {
                if (_listenThread != null)
                {
                    return;
                }

                _listenThread = new Thread(EventPollLoop);
                _listenThread.Start();
            }

            private void EventPollLoop()
            {
                int currentCount = 0;
                Pollfd[] descriptors = null;
                var midi = IntPtr.Zero;
                snd_midi_event_new(new IntPtr(128), ref midi);

                while (_activeCallbacks.Count > 0)
                {
                    if (currentCount != _activeCallbacks.Count)
                    {
                        var numberPollDescriptors = snd_seq_poll_descriptors_count(_handle, PollIn);
                        descriptors = new Pollfd[numberPollDescriptors];

                        snd_seq_poll_descriptors(_handle, descriptors, numberPollDescriptors, PollIn);
                    }

                    if (sys_poll(descriptors, (uint)descriptors.Length, -1) < 0)
                    {
                        break;
                    }

                    int error = 0;
                    do
                    {
                        var evtPtr = IntPtr.Zero;
                        error = snd_seq_event_input(_handle, ref evtPtr);
                        if (error < 0)
                        {
                            break;
                        }

                        var rawEvent = (SequencerEvent)Marshal.PtrToStructure(evtPtr, typeof(SequencerEvent));

                        var source = "{rawEvent.Source.Client}:{rawEvent.Source.Port}";
                        MidiInApi.Callbacks callbacks;
                        BytesToMidiEventConverter midiEventReader;
                        if (!_midiEventReaders.TryGetValue(source, out midiEventReader))
                        {
                            midiEventReader = new BytesToMidiEventConverter(2);
                            _midiEventReaders[source] = midiEventReader;
                        }

                        if (_callbacks.TryGetValue(new IntPtr(rawEvent.Dest.Port), out callbacks))
                        {
                            var midiBytes = new byte[28];
                            snd_midi_event_decode(midi, midiBytes, midiBytes.Length, evtPtr);
                            snd_midi_event_reset_decode(midi);

                            try
                            {
                                var evt = midiEventReader.Convert(midiBytes);
                                callbacks.OnEventReceived?.Invoke(evt);
                            }
                            catch (Exception e)
                            {
                                callbacks.OnError?.Invoke(e);
                            }
                        }
                    } while (error > 0);
                }

                snd_midi_event_free(midi);

                _listenThread = null;
            }

            internal void SendMsg(IntPtr subscription, uint msg)
            {
                byte portNumber = (byte)subscription;
                int clientId;
                if (!_portToClient.TryGetValue(portNumber, out clientId))
                {
                    Console.WriteLine($"Unable to find destination clientID for local port {portNumber}.");
                    return;
                }

                var midi = IntPtr.Zero;
                // Console.WriteLine("Creating midi event.");
                long error = snd_midi_event_new(new IntPtr(128), ref midi);

                if (error < 0)
                {
                    Console.WriteLine($"Failed to create new midi event {error}");
                }

                snd_midi_event_init(midi);

                try
                {
                    var midiBytes = BitConverter.GetBytes(msg);
                    var seqEvent = default(SequencerEvent);
                    // Console.WriteLine("Encoding midi event.");
                    error = snd_midi_event_encode(midi, midiBytes, midiBytes.Length, ref seqEvent);

                    // Console.WriteLine($"Setting event properties. {error}");
                    seqEvent.Source.Port = portNumber;
                    seqEvent.Dest.Client = 254; // (byte)clientId;
                    seqEvent.Dest.Port = 253;
                    seqEvent.Queue = 253;

                    // Console.WriteLine($"SPort: {portNumber} DClient: {clientId} DPort: {0}");
                    if (error < 0)
                    {
                        throw new MidiDeviceException("Unable to encode sequencer event.");
                    }

                    // Console.WriteLine("Sending midi event.");
                    int err = snd_seq_event_output(_handle, ref seqEvent);

                    if (err < 0)
                    {
                        Console.WriteLine($"Output error: {snd_strerror(err)} ({err})");
                        Environment.FailFast("Failed");
                    }

                    err = snd_seq_drain_output(_handle);

                    if (err < 0)
                    {
                        Console.WriteLine($"Drain error: {snd_strerror(err)} ({err})");
                        Environment.FailFast("Failed");
                    }
                }
                finally
                {
                    snd_midi_event_free(midi);
                }
            }
        }

        internal class Client
        {
            internal Client(uint idx, byte id, string name, uint type)
            {
                Index = idx;
                Id = id;
                Name = name;
                Type = (Sequencer.PortTypes)type;
            }

            public uint Index { get; }

            public byte Id { get; }

            public string Name { get; }

            public Sequencer.PortTypes Type { get; }
        }
    }
}