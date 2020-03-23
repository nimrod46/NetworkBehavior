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
    public class ServerBehavior : Networking.NetworkBehavior
    {
        private readonly object syncObj = new object();
        public delegate void ConnectionLobbyAcceptedEventHandler(string ip, int port, long ping);
        public event ConnectionLobbyAcceptedEventHandler OnConnectionLobbyAcceptedEvent;
        public delegate void ClientEventHandlerSynchronizedEventHandler(int id);
        public event ClientEventHandlerSynchronizedEventHandler OnClientEventHandlerSynchronizedEvent;

        public bool IsRunning { get; set; }
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

        public ServerBehavior(int serverPort) : base(serverPort)
        {
            id = serverPort;
        }

        public void Run()
        {
            server = new Server(serverPort, '~', '|');
            server.StartServer();
            server.OnReceivedEvent += Server_receivedEvent;

            Start();

            directServer = new DirectServer(serverPort + 1, '|');
            directServer.Start();
            directServer.OnReceivedEvent += ReceivedEvent;

            IsRunning = true;

            server.OnConnectionAcceptedEvent += Server_connectionAcceptedEvent;
            server.OnConnectionLobbyAcceptedEvent += Server_OnConnectionLobbyAcceptedEvent;
            server.OnClientDisconnectedEvent += Server_OnClientDisconnectedEvent;
        }

        protected override void InitIdentityLocally(NetworkIdentity identity, int ownerID, int id, params string[] valuesByFields)
        {
            identity.isInServer = true;
            base.InitIdentityLocally(identity, ownerID, id, valuesByFields);
        }

        private void Synchronize(string ip, int port)
        {
            lock (syncObj)
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
                int clientId = GetIdByIpAndPort(ip, port);
                clients.Add(clientId, new EndPoint(ip, port));
                //Console.WriteLine("Spawn all existing clients");

                InitiateDircetInterface initiateDircetInterface = new InitiateDircetInterface();//Initiate dircet interface with the client
                SendToAUser(initiateDircetInterface, NetworkInterface.TCP, ip, port);
                //Console.WriteLine("Initiated dircet interface with the client");

                clientsBeforeSync.Remove(clientId);
                OnClientEventHandlerSynchronizedEvent?.Invoke(clientId);
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
                        server.Broadcast(originArgs, clientsBeforeSync.ToArray().Length);
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
            if (!IsRunning)
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

        protected override void MethodNetworkAttribute_onNetworkingInvoke(MethodInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, bool invokeInServer, bool haveBeenInvokedInAuthority, NetworkIdentity networkIdentity)
        {
            if (!IsRunning)
            {
                throw new Exception("No connection exist!");
            }
            MethodPacket packet;
            switch (packetID)
            {
                case PacketID.BroadcastMethod:
                    packet = new BroadcastMethodPacket(args, invokeInServer, haveBeenInvokedInAuthority, networkIdentity.id);
                    ParsePacket(packet.GetArgs().ToArray(), null, 0, networkInterface);
                    break;
                case PacketID.Command:
                    packet = new CommandPacket(args, networkIdentity.id);
                    SendToAUser(packet, networkInterface, clients[networkIdentity.ownerId].Ip, clients[networkIdentity.ownerId].TcpPort);
                    break;
                default:
                    break;
            }
        }

        public void sendLobbyInfo(string ip, int port, string data)
        {
            if (!IsRunning)
            {
                throw new Exception("No connection exist!");
            }

            LobbyInfoPacket packet = new LobbyInfoPacket(data);
            Send(packet, NetworkInterface.TCP, GetIdByIpAndPort(ip, port));
        }

        private void clientDsiconnected(int id)
        {
            Console.WriteLine("Client: " + id + " DISCONNECTED");
            lock (NetworkIdentity.entities)
            {
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

        public NetworkIdentity spawnWithServerAuthority(Type instance, NetworkIdentity identity = null)
        {
            return SpawnIdentity(instance, -1, identity);
        }

        public NetworkIdentity spawnWithClientAuthority(Type instance, int clientId, NetworkIdentity identity = null)
        {
            return SpawnIdentity(instance, clientId, identity);

        }

        private NetworkIdentity SpawnIdentity(Type instance, int clientId, NetworkIdentity identity)
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
            int owner = 0;
            if (clientId == -1)
            {
                owner = serverPort;
            }
            else
            {
                owner = clientId;
            }
            SpawnObjectPacket packet = new SpawnObjectPacket(instance, id, owner, args);
            Send(packet, NetworkInterface.TCP, clientsBeforeSync.ToArray());
            InitIdentityLocally(identity, owner, id, args);
            return identity;
        }
    }
}
