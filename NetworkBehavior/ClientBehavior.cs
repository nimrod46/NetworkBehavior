﻿using System;
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
        public bool IsConnected { get; private set; }
        public readonly string serverIp;
        private Client client;
        private DirectClient directClient;

        public ClientBehavior(NetworkIdentity player, int serverPort, string serverIp) : base(player, serverPort)
        {
            this.serverIp = serverIp;
            isLocalPlayerSpawned = false;
        }

        public void Connect()
        {
            client = new Client(serverIp, serverPort, '~', '|');
            client.OnReceivedEvent += Client_receivedEvent;
            client.OnConnectionLostEvent += Client_serverDisconnectedEvent;
            if (client.Connect(out long pingMs))
            {
                Start();
                Console.WriteLine("Connection established with: " + pingMs + " ping ms");
                IsConnected = true;

                directClient = new DirectClient(serverIp, serverPort + 1, '|');
                directClient.OnReceivedEvent += ReceivedEvent;
                player.OnBeginSynchronization += Player_OnBeginSynchronization;
                directClient.Start();
            }
        }

        protected override void InitIdentityLocally(NetworkIdentity identity, int ownerID, int id, params string[] valuesByFields)
        {
            identity.isInServer = false;
            base.InitIdentityLocally(identity, ownerID, id, valuesByFields);
        }

        internal void Send(Packet packet, NetworkInterface networkInterface, params int[] ports)
        {
            if (networkInterface == NetworkInterface.TCP)
            {
                client.Send(packet.args.ToArray());
            }
            else
            {
                directClient.Send((int)packet.packetID, packet.args.ToArray());
            }
        }

        protected override void ParsePacketByPacketID(int packetID, string[] args, string ip, int port, NetworkInterface networkInterface, string[] originArgs)
        {
            switch (packetID)
            {
                case (int)PacketID.SpawnLocalPlayer:
                    base.ParsePacketByPacketID(packetID, args, ip, port, networkInterface, originArgs);
                    DircetInterfaceInitiatingPacket packet = new DircetInterfaceInitiatingPacket(player.id);
                    Send(packet, NetworkInterface.UDP);
                    break;
                default:
                    base.ParsePacketByPacketID(packetID, args, ip, port, networkInterface, originArgs);
                    break;
            }
        }

        private void Player_OnBeginSynchronization()
        {
            BeginSynchronizationPacket packet = new BeginSynchronizationPacket();
            Send(packet, NetworkInterface.TCP);
        }

        private void Client_serverDisconnectedEvent(string ip, int port)
        {
            IsConnected = false;
            player.ServerDisconnected();
        }

        private void Client_receivedEvent(string[][] data, string ip, int port)
        {
            foreach (string[] s in data)
            {
                try
                {
                    lock (scope)
                    {
                        ParsePacket(s, ip, port, NetworkInterface.TCP);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Cannot parse packet: ");
                    print(s);
                    Console.WriteLine(e);
                }
            }
        }

        protected override void SyncVar_onNetworkingInvoke(LocationInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity networkIdentity)
        {
            if (!IsConnected)
            {
                throw new Exception("No connection exist!");
            }
            SyncVarPacket packet;
            switch (packetID)
            {
                case PacketID.SyncVar:
                    packet = new SyncVarPacket(args, invokeInServer, networkIdentity.id);
                    Send(packet, networkInterface);
                    break;
                default:
                    break;
            }
        }

        protected override void MethodNetworkAttribute_onNetworkingInvoke(MethodInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity networkIdentity)
        {
            if (!IsConnected)
            {
                throw new Exception("No connection exist!");
            }
            MethodPacket packet;
            switch (packetID)
            {
                case PacketID.BroadcastMethod:
                    packet = new BroadcastMethodPacket(args, invokeInServer, networkIdentity.id);
                    Send(packet, networkInterface);
                    break;
                case PacketID.Command:
                    packet = new CommandPacket(args, networkIdentity.id);
                    Send(packet, networkInterface);
                    break;
                default:
                    break;
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
