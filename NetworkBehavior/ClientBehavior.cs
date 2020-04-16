using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Networking;
using NetworkingLib;
using static Networking.NetworkIdentity;
using static NetworkingLib.Server;

namespace Networking
{
    public class ClientBehavior : NetworkBehavior
    {
        public delegate void ServerDisconnectedEventHandler();
        public event ServerDisconnectedEventHandler OnServerDisconnected;

        public bool IsConnected { get; private set; }
        public readonly string serverIp;
        private Client client;
        private DirectClient directClient;


        public ClientBehavior(int serverPort, string serverIp) : base(serverPort)
        {
            this.serverIp = serverIp;
        }

        public void Connect()
        {
            client = new Client(serverIp, serverPort, '~', '|');
            client.OnReceivedEvent += Client_receivedEvent;
            client.OnConnectionLostEvent += Client_serverDisconnectedEvent;
            if (client.Connect(out long pingMs, out EndPointId endPointId))
            {
                Start(); 
                Console.WriteLine("Connection established with: " + pingMs + " ping ms");
                IsConnected = true;

                directClient = new DirectClient(serverIp, serverPort + 1, '|');
                directClient.OnReceivedEvent += ReceivedEvent;
                directClient.Start();
            }
        }

        private void ReceivedEvent(object[] data, string address, int port)
        {
            try
            {
                SocketInfo info = new SocketInfo(address, port, NetworkInterfaceType.UDP);
                ParseArgs(data, serverEndPointId, info);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot parse packet: ");
                Print(data);
                Console.WriteLine(e);
            }
        }

        protected override void InitIdentityLocally(NetworkIdentity identity, EndPointId ownerID, IdentityId id, params object[] valuesByFields)
        {
            identity.isInServer = false;
            base.InitIdentityLocally(identity, ownerID, id, valuesByFields);
        }

        private protected override void ParsePacket(PacketId packetId, object[] args, EndPointId endPointId, SocketInfo socketInfo) 
        {
            switch (packetId)
            {
                case PacketId.InitiateDircetInterface:
                    InitiateDircetInterfacePacket initiateDircetInterfacePacket = new InitiateDircetInterfacePacket(args.ToList());
                    localEndPointId = initiateDircetInterfacePacket.ClientId;
                    DircetInterfaceInitiatingPacket initiatingPacket = new DircetInterfaceInitiatingPacket(initiateDircetInterfacePacket.ClientId);
                    Send(initiatingPacket, NetworkInterfaceType.UDP);
                    break;
                default:
                    base.ParsePacket(packetId, args, endPointId, socketInfo);
                    break;
            }
        }
        public void Synchronize()
        {
            BeginSynchronizationPacket packet = new BeginSynchronizationPacket();
            Send(packet, NetworkInterfaceType.TCP);
        }

        private void Client_serverDisconnectedEvent(string ip, int port)
        {
            IsConnected = false;
            OnServerDisconnected?.Invoke();
        }

        private void Client_receivedEvent(object[][] data, string ip, int port)
        {
            foreach (string[] s in data)
            {
                try
                {
                    ParseArgs(s, serverEndPointId, new SocketInfo(ip, port, NetworkInterfaceType.TCP));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Cannot parse packet: ");
                    Print(s);
                    Console.WriteLine(e);
                }
            }
        }

        protected override void OnInvokeBroadcastMethodNetworkly(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string methodName, object[] methodArgs)
        {
            if (!IsConnected)
            {
                throw new Exception("No connection exist!");
            }

            BroadcastPacket packet;
            packet = new BroadcastPacket(networkIdentity.Id, methodName, methodArgs);
            Send(packet, networkInterface);
            ParseBroadcastPacket(packet, localEndPointId, new SocketInfo(null, -1, networkInterface));
        }
        protected override void OnInvokeCommandMethodNetworkly(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string methodName, object[] methodArgs, EndPointId? targetId = null)
        {
            if (!IsConnected)
            {
                throw new Exception("No connection exist!");
            }

            CommandPacket packet;
            packet = new CommandPacket(networkIdentity.Id, methodName, methodArgs);
            Send(packet, networkInterface);
        }

        protected override void OnInvokeLocationNetworkly(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string locationName, object locationValue)
        {
            if (!IsConnected)
            {
                throw new Exception("No connection exist!");
            }

            SyncVarPacket packet;
            packet = new SyncVarPacket(networkIdentity.Id, locationName, locationValue);
            Send(packet, networkInterface);
        }

        internal void Send(Packet packet, NetworkInterfaceType networkInterface)
        {
            if (networkInterface == NetworkInterfaceType.TCP)
            {
                client.Send(packet.Data.ToArray());
            }
            else
            {
                directClient.Send(packet.Data.ToArray());
            }
        }

        public bool StartLobbyClient(out long ping)
        {
            try
            {
                client = new Client(serverIp, serverPort, '~', '|');
            }
            catch (Exception e)
            {
                throw e;
            }
            client.OnReceivedEvent += Client_receivedEvent;
            return client.ConnectLobby(out ping);
        }
    }
}
