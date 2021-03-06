﻿using System;
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

        protected override void InitIdentityLocally(NetworkIdentity identity, EndPointId ownerID, IdentityId id, bool spawnDuringSync, params object[] valuesByFields)
        {
            identity.isInServer = false;
            base.InitIdentityLocally(identity, ownerID, id, spawnDuringSync, valuesByFields);
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
                    hasSynchronized = true;
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

        internal override void OnInvokeBroadcastMethodNetworkly(BrodcastMethodEventArgs brodcastMethodEventArgs)
        {
            if (!IsConnected)
            {
                throw new Exception("No connection exist!");
            }
            brodcastMethodEventArgs.ShouldInvokeSynchronously ??= brodcastMethodEventArgs.NetworkInterface == NetworkInterfaceType.TCP;
            BroadcastPacket packet;
            packet = new BroadcastPacket(brodcastMethodEventArgs.NetworkIdentity.Id, brodcastMethodEventArgs.MethodName, brodcastMethodEventArgs.ShouldInvokeSynchronously.Value, brodcastMethodEventArgs.MethodArgs);
            Send(packet, brodcastMethodEventArgs.NetworkInterface);
            ParseBroadcastPacket(packet, false, localEndPointId, new SocketInfo(null, -1, brodcastMethodEventArgs.NetworkInterface));
        }
        internal override void OnInvokeCommandMethodNetworkly(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string methodName, object[] methodArgs, bool? shouldInvokeSynchronously = null, EndPointId? targetId = null)
        {
            if (!IsConnected)
            {
                throw new Exception("No connection exist!");
            }
            shouldInvokeSynchronously ??= networkInterface == NetworkInterfaceType.TCP;
            CommandPacket packet;
            packet = new CommandPacket(networkIdentity.Id, methodName, shouldInvokeSynchronously.Value, methodArgs);
            Send(packet, networkInterface);
        }

        internal override void OnInvokeLocationNetworkly(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string locationName, object locationValue, bool? shouldInvokeSynchronously = null)
        {
            if (!IsConnected)
            {
                throw new Exception("No connection exist!");
            }
            shouldInvokeSynchronously ??= networkInterface == NetworkInterfaceType.TCP;
            SyncVarPacket packet;
            packet = new SyncVarPacket(networkIdentity.Id, locationName, locationValue, shouldInvokeSynchronously.Value);
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
