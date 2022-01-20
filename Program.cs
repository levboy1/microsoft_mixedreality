using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.MixedReality.WebRTC;

namespace mixedreality
{
    class Program
    {
        static PeerConnection pc;
        static string generatedSDP;
        static AudioTrackSource microphoneSource = null;
        static VideoTrackSource webcamSource = null;
        static Transceiver audioTransceiver = null;
        static LocalAudioTrack localAudioTrack = null;

        static async Task Main(string[] args)
        {
            try
            {
                pc = new PeerConnection();
                generatedSDP = string.Empty;
                Console.WriteLine("Peer conncetion object created.");

                pc.AudioTrackAdded += OnAudioTrackAdded;
                pc.AudioTrackRemoved += OnAudioTrackRemoved;
                pc.Connected += OnConnected;
                pc.DataChannelAdded += OnDataChannelAdded;
                pc.DataChannelRemoved += OnDataChannelRemoved;
                pc.IceCandidateReadytoSend += OnIceCandidateReadytoSend;
                pc.IceGatheringStateChanged += OnIceGatheringStateChanged;
                pc.IceStateChanged += OnIceStateChanged;
                pc.LocalSdpReadytoSend += OnLocalSdpReadytoSendAsync;
                pc.RenegotiationNeeded += OnRenegotiationNeeded;
                pc.TransceiverAdded += OnTransceiverAdded;
                pc.VideoTrackAdded += OnVideoTrackAdded;
                pc.VideoTrackRemoved += OnVideoTrackRemoved;
                
                Console.WriteLine("Event delegates added.");

                var config = new PeerConnectionConfiguration
                {
                    SdpSemantic = SdpSemantic.PlanB,
                    IceServers = new List<IceServer> {
                        new IceServer{ Urls = { "stun:stun.l.google.com:19302" } }
                    }
                };

                await pc.InitializeAsync(config);
                Console.WriteLine("Peer connection initialized.");
                await pc.AddDataChannelAsync("channel1", true, true, CancellationToken.None);

                microphoneSource = await DeviceAudioTrackSource.CreateAsync();
                var audioTrackConfig = new LocalAudioTrackInitConfig { trackName = "microphone_track" };
                localAudioTrack = LocalAudioTrack.CreateFromSource(microphoneSource, audioTrackConfig);

                audioTransceiver = pc.AddTransceiver(MediaKind.Audio);
                audioTransceiver.LocalAudioTrack = localAudioTrack;
                audioTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
                Console.WriteLine("Transreceiver and audio track added.");

                bool isSuccessful = pc.CreateOffer();
                if (!isSuccessful)
                    throw new Exception("Failed to create offer.");

                Console.ReadKey();

                string webCallsSdpContent = ReadSdpFromConsole();
                SdpMessage webCallsSDP = new SdpMessage();
                webCallsSDP.Type = SdpMessageType.Answer;
                webCallsSDP.Content = webCallsSdpContent.Replace("\\r\\n", "\r\n");
                Console.WriteLine();
                Console.WriteLine(webCallsSDP.Content);               

                await pc.SetRemoteDescriptionAsync(webCallsSDP);
                isSuccessful = pc.CreateAnswer();
                if (!isSuccessful)
                    throw new Exception("Failed to create answer.");

                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            localAudioTrack?.Dispose();
            microphoneSource?.Dispose();
            webcamSource?.Dispose();
            pc?.Close();
            pc?.Dispose();
        }



        private static void OnAudioTrackAdded(RemoteAudioTrack track)
        {
            Console.WriteLine("_________________________________________________");
            Console.WriteLine("OnAudioTrackAdded event triggered.");
            Console.WriteLine($"Track name: {track.Name}");
        }

        private static void OnAudioTrackRemoved(Transceiver transceiver, RemoteAudioTrack track)
        {
            Console.WriteLine("_________________________________________________");
            Console.WriteLine("OnAudioTrackRemoved event triggered.");
        }

        private static void OnConnected()
        {
            Console.WriteLine("_________________________________________________");
            Console.WriteLine($"OnConnected event triggered. Peer connection data channel count: {pc.DataChannels.Count} Transreceiver MlineIndex: {audioTransceiver.MlineIndex}");
        }

        private static void OnDataChannelRemoved(DataChannel channel)
        {
            Console.WriteLine("_________________________________________________");
            Console.WriteLine("OnDataChannelRemoved event triggered.");
        }

        private static void OnDataChannelAdded(DataChannel channel)
        {
            Console.WriteLine("_________________________________________________");
            Console.WriteLine("OnDataChannelAdded event triggered.");
        }

        private static void OnIceCandidateReadytoSend(IceCandidate candidate)
        {
            Console.WriteLine("_________________________________________________");
            Console.WriteLine($"OnIceCandidateReadytoSend event triggered. Ice candidate content:");
            Console.WriteLine(candidate.Content);
            generatedSDP = generatedSDP + $"a={candidate.Content}\\r\\n";
        }

        private static void OnIceGatheringStateChanged(IceGatheringState newState)
        {
            Console.WriteLine("_________________________________________________");
            Console.WriteLine($"OnIceGatheringStateChanged event triggered. New gathering state: {newState}");

            if (newState == IceGatheringState.Complete)
            {

                generatedSDP += "a=end-of-candidates\\r\\n";
                Console.WriteLine();
                Console.WriteLine(generatedSDP.Replace("\\r\\n", "\r\n"));
                Console.WriteLine();
                Console.WriteLine(generatedSDP);
            }
        }

        private static void OnIceStateChanged(IceConnectionState newState)
        {
            Console.WriteLine("_________________________________________________");
            Console.WriteLine($"OnIceStateChanged event triggered. New state: {newState} Transreceiver MlineIndex: {audioTransceiver.MlineIndex}");
        }

        private static void OnLocalSdpReadytoSendAsync(SdpMessage message)
        {
            Console.WriteLine("_________________________________________________");
            Console.WriteLine("OnLocalSdpReadytoSend event triggered.");
            string msg = message.Content.Replace("\r\n", "\\r\\n");
            generatedSDP = msg;
        }

        private static string ReadSdpFromConsole()
        {
            Console.WriteLine("\nEnter WebCalls API SDP content and hit <ENTER>.");
            string content = Console.ReadLine();
            return content;
        }

        private static void OnRenegotiationNeeded()
        {
            Console.WriteLine("_________________________________________________");
            Console.WriteLine($"OnRenegotiationNeeded event triggered.");
        }

        private static void OnTransceiverAdded(Transceiver transceiver)
        {
            Console.WriteLine("_________________________________________________");
            Console.WriteLine("OnTransceiverAdded event triggered.");
        }

        private static void OnVideoTrackRemoved(Transceiver transceiver, RemoteVideoTrack track)
        {
            Console.WriteLine("_________________________________________________");
            Console.WriteLine("OnVideoTrackRemoved event triggered.");
        }

        private static void OnVideoTrackAdded(RemoteVideoTrack track)
        {
            Console.WriteLine("_________________________________________________");
            Console.WriteLine("OnVideoTrackAdded event triggered.");
        }
    }
}
