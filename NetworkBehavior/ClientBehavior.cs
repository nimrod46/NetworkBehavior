using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Networking;
using NetworkingLib;
using PostSharp.Aspects;

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
            if (client.Connect(out long pingMs))
            {
                id = GetIdByIpAndPort(serverIp, client.GetPort());
                Start(); 
                Console.WriteLine("Connection established with: " + pingMs + " ping ms");
                IsConnected = true;

                directClient = new DirectClient(serverIp, serverPort + 1, '|');
                directClient.OnReceivedEvent += ReceivedEvent;
                directClient.Start();
            }
        }

        protected override void InitIdentityLocally(NetworkIdentity identity, int ownerID, int id, params object[] valuesByFields)
        {
            identity.isInServer = false;
            base.InitIdentityLocally(identity, ownerID, id, valuesByFields);
        }

        private protected override void ParsePacket(PacketId packetId, object[] args, SocketInfo socketInfo) 
        {
            switch (packetId)
            {
                case PacketId.InitiateDircetInterface:
                    DircetInterfaceInitiatingPacket initiatingPacket = new DircetInterfaceInitiatingPacket(id);
                    Send(initiatingPacket, NetworkInterface.UDP);
                    break;
                default:
                    base.ParsePacket(packetId, args, socketInfo);
                    break;
            }
        }
        public void Synchronize()
        {
            BeginSynchronizationPacket packet = new BeginSynchronizationPacket();
            Send(packet, NetworkInterface.TCP);
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
                    ParseArgs(s, new SocketInfo(ip, port, NetworkInterface.TCP));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Cannot parse packet: ");
                    print(s);
                    Console.WriteLine(e);
                }
            }
        }

        protected override void OnInvokeMethodNetworkly(PacketId packetID, NetworkIdentity networkIdentity, NetworkInterface networkInterface, string methodName, object[] methodArgs)
        {
            if (!IsConnected)
            {
                throw new Exception("No connection exist!");
            }

            MethodPacket packet;
            switch (packetID)
            {
                case PacketId.BroadcastMethod:
                    packet = new BroadcastPacket(networkIdentity.id, methodName, methodArgs);
                    Send(packet, networkInterface);
                    break;
                case PacketId.Command:
                    packet = new CommandPacket(networkIdentity.id, methodName, methodArgs);
                    Send(packet, networkInterface);
                    Console.WriteLine("SENDED: " + methodName);
                    break;
                default:
                    break;
            }
        }

        protected override void OnInvokeLocationNetworkly(PacketId packetID, NetworkIdentity networkIdentity, NetworkInterface networkInterface, string locationName, object locationValue)
        {
            if (!IsConnected)
            {
                throw new Exception("No connection exist!");
            }

            SyncVarPacket packet;
            switch (packetID)
            {
                case PacketId.SyncVar:
                    packet = new SyncVarPacket(networkIdentity.id, locationName, locationValue);
                    Send(packet, networkInterface);
                    break;
                default:
                    break;
            }
        }

        internal void Send(Packet packet, NetworkInterface networkInterface)
        {
            if (networkInterface == NetworkInterface.TCP)
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
