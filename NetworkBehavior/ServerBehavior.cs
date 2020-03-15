﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Networking;
using NetworkingLib;
using PostSharp.Aspects;

namespace Networking
{
    public class ServerBehavior : Networking.NetworkBehavior
    {
        public delegate void PlayerSynchronizedEventHandler(NetworkIdentity client);
        public event PlayerSynchronizedEventHandler OnPlayerSynchronized;
        public delegate void ConnectionLobbyAcceptedEventHandler(string ip, int port, long ping);
        public event ConnectionLobbyAcceptedEventHandler OnConnectionLobbyAcceptedEvent;
        internal Dictionary<int, EndPoint> clients = new Dictionary<int, EndPoint>();
        internal List<int> clientsBeforeSync = new List<int>();
        public int numberOfPlayer
        {
            get
            {
                return clients.Count;
            }
        }
        private Server server;
        private DirectServer directServer;

        public ServerBehavior(NetworkIdentity player, int serverPort) : base(player, serverPort)
        {
            isLocalPlayerSpawned = false;
        }

        public override void Run()
        {
            //try
            //{
                server = new Server(serverPort, '~', '|');
                server.StartServer();
                server.OnReceivedEvent += Server_receivedEvent;
                directServer = new DirectServer(serverPort + 1, '|');
                directServer.Start();
                directServer.OnReceivedEvent += ReceivedEvent;

                player.isInServer = true;

                InitIdentityLocally(player, serverPort, serverPort);

                server.OnConnectionAcceptedEvent += Server_connectionAcceptedEvent;
                server.OnConnectionLobbyAcceptedEvent += Server_OnConnectionLobbyAcceptedEvent;
                server.OnClientDisconnectedEvent += Server_OnClientDisconnectedEvent;
                base.Run();
            //}
            //catch (Exception e)
            //{
            //    throw e;
            //}

            
        }

        protected override void InitIdentityLocally(NetworkIdentity identity, int ownerID, int id, params string[] valuesByFields)
        {
            identity.isInServer = true;
            base.InitIdentityLocally(identity, ownerID, id, valuesByFields);
        }

        private void Synchronize(string ip, int port)
        {
            lock (player)
            {
                Console.WriteLine("New player at: " + GetIdByIpAndPort(ip, port));
                SpawnObjectPacket spawnPacket;
                foreach (NetworkIdentity i in NetworkIdentity.entities.Values)
                {
                    Dictionary<string, string> valuesByFields = GetValuesByFieldsFromObject(i);
                    var args = valuesByFields.Select(k => k.Key + "+" + k.Value).ToArray();
                    spawnPacket = new SpawnObjectPacket(getNetworkClassTypeByName(i.GetType().FullName), i.id, i.ownerId, args); //Spawn all existing clients in the remote client
                    SendToAUser(spawnPacket, NetworkInterface.TCP, ip, port);
                }
                //Console.WriteLine("Spawn all existing clients");
                int clientId = GetIdByIpAndPort(ip, port);
                NetworkIdentity identity = Activator.CreateInstance(player.GetType()) as NetworkIdentity;//Spawn the client player locally
                InitIdentityLocally(identity, clientId, clientId);
                // Console.WriteLine("spawned the client player locally");
                clients.Add(clientId, new EndPoint(identity, ip, port));
                SpawnLocalPlayerPacket spawnLocalPlayerPacket = new SpawnLocalPlayerPacket(player.GetType(), clientId);//spawn the client player in the remote client
                SendToAUser(spawnLocalPlayerPacket, NetworkInterface.TCP, ip, port);
                //Console.WriteLine("spawned the client player in the remote client");
                spawnPacket = new SpawnObjectPacket(player.GetType(), clientId, clientId);//spawn the client player in all other clients
                Send(spawnPacket, NetworkInterface.TCP, clientsBeforeSync.ToArray());
                //Console.WriteLine("spawned the client player in all other clients");

                
                clientsBeforeSync.Remove(GetIdByIpAndPort(ip, port));
                OnPlayerSynchronized?.Invoke(identity);
            }
        }

        protected override void ParsePacketByPacketID(int packetID, string[] args, string ip, int port, NetworkInterface networkInterface, string[] originArgs)
        {
            switch (packetID)
            {
                case (int)PacketID.DircetInterfaceInitiating:
                    EndPoint eP;
                    int clientId = int.Parse(args[0]);
                    if (clients.TryGetValue(clientId, out eP))
                    {
                        eP.UdpPort = port;
                        clients[clientId] = eP;
                    }
                    else
                    {
                        Console.Error.WriteLine("UDP NOT init for: " + clientId);
                    }
                    break;
                case (int)PacketID.BroadcastMethod:
                    if (networkInterface == NetworkInterface.TCP)
                    {
                        server.Broadcast(originArgs, clientsBeforeSync.ToArray());
                    }
                    else
                    {
                        directBroadcast(originArgs, clientsBeforeSync.ToArray());
                    }

                    if (!bool.Parse(args[args.Length - 2]))
                    {
                        return;
                    }
                    base.ParsePacketByPacketID(packetID, args, ip, port, networkInterface, originArgs);
                    break;
                case (int)PacketID.SyncVar:
                    if (networkInterface == NetworkInterface.TCP)
                    {
                        server.Broadcast(originArgs, clientsBeforeSync.ToArray());
                    }
                    else
                    {
                        directBroadcast(originArgs, clientsBeforeSync.ToArray());
                    }

                    if (!bool.Parse(args[args.Length - 2]))
                    {
                        return;
                    }
                    base.ParsePacketByPacketID(packetID, args, ip, port, networkInterface, originArgs);
                    break;
                case (int)PacketID.BeginSynchronization:
                    Synchronize(ip, port);
                    break;
                default:
                    base.ParsePacketByPacketID(packetID, args, ip, port, networkInterface, originArgs);
                    break;
            }
        }

        internal void directBroadcast(string[] args, params int[] ports)
        {
            foreach (EndPoint endPoint in clients.Values)
            {
                if (endPoint.UdpPort == 0 || ports.Contains(endPoint.UdpPort))
                {
                    continue;
                }
                directServer.Send(args, endPoint.Ip, endPoint.UdpPort);
            }
        }

        internal void Send(Packet packet, NetworkInterface networkInterface, params int[] ports)
        {
            if (networkInterface == NetworkInterface.TCP)
            {
                server.Broadcast(packet.args.ToArray(), ports);
            }
            else
            {
                directBroadcast(packet.args.ToArray(), ports);
            }
        }

        internal void SendToAUser(Packet packet, NetworkInterface networkInterface, string ip, int port)
        {
            if (networkInterface == NetworkInterface.TCP)
            {
                server.SendToAUser(packet.args.ToArray(), ip, port);
            }
            else
            {
                directServer.Send(packet.args.ToArray(), clients[GetIdByIpAndPort(ip, port)].Ip, clients[GetIdByIpAndPort(ip, port)].UdpPort);
            }
        }

        private void Server_receivedEvent(string[][] data, string ip, int port)
        {
            foreach (string[] s in data)
            {
                try
                {
                    ParsePacket(s, ip, port, NetworkInterface.TCP);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Cannot parse packet: ");
                    print(s);
                    Console.WriteLine(e);
                }
            }
        }

        private void Server_OnClientDisconnectedEvent(string ip, int port)
        {
            clientDsiconnected(GetIdByIpAndPort(ip, port));
        }

        private void Server_OnConnectionLobbyAcceptedEvent(string ip, int port, long ping)
        {
            OnConnectionLobbyAcceptedEvent?.Invoke(ip, port, ping);
        }

        private void Server_connectionAcceptedEvent(string ip, int port, long ping)
        {
            clientsBeforeSync.Add(GetIdByIpAndPort(ip, port));
        }

        protected override void SyncVar_onNetworkingInvoke(LocationInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity networkIdentity)
        {
            if (!isConnected)
            {
                throw new Exception("No connection exist!");
            }

            SyncVarPacket packet;
            switch (packetID)
            {
                case PacketID.SyncVar:
                    packet = new SyncVarPacket(args, invokeInServer, networkIdentity.id);
                    ParsePacket(packet.GetArgs().ToArray(), null, 0, networkInterface);
                    break;
                default:
                    break;
            }
        }

        protected override void MethodNetworkAttribute_onNetworkingInvoke(MethodInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity networkIdentity)
        {
            if (!isConnected)
            {
                throw new Exception("No connection exist!");
            }
            MethodPacket packet;
            switch (packetID)
            {
                case PacketID.BroadcastMethod:
                    packet = new BroadcastMethodPacket(args, invokeInServer, networkIdentity.id);
                    ParsePacket(packet.GetArgs().ToArray(), null, 0, networkInterface);
                    break;
                case PacketID.Command:
                    packet = new CommandPacket(args, networkIdentity.id);
                    SendToAUser(packet, networkInterface, clients[networkIdentity.id].Ip, clients[networkIdentity.id].TcpPort);
                    break;
                default:
                    break;
            }
        }

        public void sendLobbyInfo(string ip, int port, string data)
        {
            if (!isConnected)
            {
                throw new Exception("No connection exist!");
            }

            LobbyInfoPacket packet = new LobbyInfoPacket(data);
            Send(packet, NetworkInterface.TCP, GetIdByIpAndPort(ip, port));
        }

        private void clientDsiconnected(int id)
        {
            Console.WriteLine("Player: " + id + " DISCONNECTED");
            lock (NetworkIdentity.entities)
            {
                NetworkIdentity.entities[id].Disconnected();
                List<NetworkIdentity> entitiesToDestroy = new List<NetworkIdentity>();
                for (int i = 0; i < NetworkIdentity.entities.Count; i++)
                {
                    if (NetworkIdentity.entities.Values.ElementAt(i).ownerId == id)
                    {
                        entitiesToDestroy.Add(NetworkIdentity.entities.Values.ElementAt(i));
                    }
                }
                foreach (NetworkIdentity entity in entitiesToDestroy)
                {
                    entity.Destroy();
                }
            }
        }

        public NetworkIdentity spawnWithServerAuthority(Type instance, NetworkIdentity identity)
        {
            if (identity != null && identity.hasInitialized)
            {
                throw new Exception("Cannot spawn network instance that is already in use");
            }

            int id = NetworkIdentity.lastId + 1;
            NetworkIdentity.lastId++;
            string[] args = null;
            if (identity != null)
            {
                Dictionary<string, string> valuesByFields = GetValuesByFieldsFromObject(identity);
                args = valuesByFields.Select(k => k.Key + "+" + k.Value).ToArray();
            }
            else
            {
                identity = Activator.CreateInstance(instance) as NetworkIdentity;
            }
            SpawnObjectPacket packet = new SpawnObjectPacket(instance, id, serverPort, args);
            Send(packet, NetworkInterface.TCP, clientsBeforeSync.ToArray());
            InitIdentityLocally(identity, serverPort, id, args);
            return identity;
        }

        public NetworkIdentity spawnWithClientAuthority(Type instance, int clientId, NetworkIdentity identity)
        {
            if (identity != null && identity.hasInitialized)
            {
                throw new Exception("Cannot spawn network instance that is already in use");
            }

            int id = NetworkIdentity.lastId + 1;
            NetworkIdentity.lastId++;
            string[] args = null;
            if (identity != null)
            {
                Dictionary<string, string> valuesByFields = GetValuesByFieldsFromObject(identity);
                args = valuesByFields.Select(k => k.Key + "+" + k.Value).ToArray();
            }
            else
            {
                identity = Activator.CreateInstance(instance) as NetworkIdentity;
            }
            InitIdentityLocally(identity, clientId, id, args);
            SpawnObjectPacket packet = new SpawnObjectPacket(instance, id, clientId, args);
            Send(packet, NetworkInterface.TCP, clientsBeforeSync.ToArray());
            return identity;
        }
    }
}