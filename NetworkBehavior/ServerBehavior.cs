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
    public class ServerBehavior : NetworkBehavior
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

        private void Server_receivedEvent(object[][] data, string ip, int port)
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

        private protected override void ParsePacket(PacketId packetId, object[] args, SocketInfo socketInfo)
        {
            switch (packetId)
            {
                case PacketId.DircetInterfaceInitiating:
                    DircetInterfaceInitiatingPacket initiatingPacket = new DircetInterfaceInitiatingPacket(args.ToList());
                    EndPoint eP;
                    int clientId = initiatingPacket.NetworkIdentityId;
                    if (clients.TryGetValue(clientId, out eP))
                    {
                        eP.UdpPort = socketInfo.Port;
                        clients[clientId] = eP;
                    }
                    else
                    {
                        PrintWarning("UDP NOT init for: " + clientId);
                    }
                    clientsBeforeSync.Remove(clientId);
                    OnClientEventHandlerSynchronizedEvent?.Invoke(clientId);
                    break;
                case PacketId.BeginSynchronization:
                    Synchronize(socketInfo.Ip, socketInfo.Port);
                    break;
                default:
                    base.ParsePacket(packetId, args, socketInfo);
                    break;
            }
        }

        private protected override void ParseBroadcastPacket(BroadcastPacket broadcastPacket, SocketInfo socketInfo)
        {
            BroadcastPacket(broadcastPacket, socketInfo);
            if (broadcastPacket.ShouldInvokeInServer)
            {
                base.ParseBroadcastPacket(broadcastPacket, socketInfo);
            }
        }

        private protected override void ParseSyncVarPacket(SyncVarPacket syncVarPacket, SocketInfo socketInfo)
        {
            BroadcastPacket(syncVarPacket, socketInfo);
            if (syncVarPacket.ShouldInvokeInServer)
            {
                base.ParseSyncVarPacket(syncVarPacket, socketInfo);
            }
        }

        private void BroadcastPacket(Packet packet, SocketInfo socketInfo)
        {
            BroadcastPacket(packet, socketInfo.NetworkInterface, clientsBeforeSync.Concat(new int[] { GetIdByIpAndPort(socketInfo.Ip, socketInfo.Port) }).ToArray());
        }

        protected override void InitIdentityLocally(NetworkIdentity identity, int ownerID, int id, params object[] valuesByFields)
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
                List<NetworkIdentity> identities = NetworkIdentity.entities.Values.ToList();
                identities.Sort();
                foreach (NetworkIdentity i in identities)
                {
                    Dictionary<string, string> valuesByFields = GetValuesByFieldsFromObject(i);
                    var args = valuesByFields.Select(k => k.Key + "+" + k.Value).ToArray();
                    spawnPacket = new SpawnObjectPacket(getNetworkClassTypeByName(i.GetType().FullName), i.id, i.ownerId, args); //Spawn all existing clients in the remote client
                    SendPacketToAUser(spawnPacket, NetworkInterface.TCP, ip, port);
                }
                int clientId = GetIdByIpAndPort(ip, port);
                clients.Add(clientId, new EndPoint(ip, port));
                //Console.WriteLine("Spawn all existing clients");

                InitiateDircetInterfacePacket initiateDircetInterface = new InitiateDircetInterfacePacket();//Initiate dircet interface with the client
                SendPacketToAUser(initiateDircetInterface, NetworkInterface.TCP, ip, port);
                //Console.WriteLine("Initiating dircet interface with the client");             
            }
        }

        internal void DirectBroadcast(object[] args, params int[] clientsId)
        {
            foreach (var client in clients)
            {
                if ( clientsId.Contains(client.Key))
                {
                    continue;
                }
                if(client.Value.UdpPort == 0)
                {
                    PrintWarning("cannot send packet as UDP for client: " + client.Key);
                    continue;
                }
                directServer.Send(args, client.Value.Ip, client.Value.UdpPort);
            }
        }

        internal void BroadcastPacket(Packet packet, NetworkInterface networkInterface, params int[] clientsId)
        {
            Broadcast(packet.Data.ToArray(), networkInterface, clientsId);
        }

        internal void Broadcast(object[] args, NetworkInterface networkInterface, params int[] clientsId)
        {
            if (networkInterface == NetworkInterface.TCP)
            {
                server.Broadcast(args, clientsId);
            }
            else
            {
                DirectBroadcast(args, clientsId);
            }
        }

        internal void SendPacketToAUser(Packet packet, NetworkInterface networkInterface, string ip, int port)
        {
            SendToAUser(packet.Data.ToArray(), networkInterface, ip, port);
        }

        internal void SendToAUser(object[] args, NetworkInterface networkInterface, string ip, int port)
        {
            if (networkInterface == NetworkInterface.TCP)
            {
                server.SendToAUser(args, ip, port);
            }
            else
            {
                directServer.Send(args, clients[GetIdByIpAndPort(ip, port)].Ip, clients[GetIdByIpAndPort(ip, port)].UdpPort);
            }
        }

        private void Server_OnClientDisconnectedEvent(string ip, int port)
        {
            ClientDsiconnected(GetIdByIpAndPort(ip, port));
        }

        private void Server_OnConnectionLobbyAcceptedEvent(string ip, int port, long ping)
        {
            OnConnectionLobbyAcceptedEvent?.Invoke(ip, port, ping);
        }

        private void Server_connectionAcceptedEvent(string ip, int port, long ping)
        {
            clientsBeforeSync.Add(GetIdByIpAndPort(ip, port));
        }

        protected override void SyncVar_onNetworkingInvoke(LocationInterceptionArgs args, PacketId packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity networkIdentity)
        {
            if (!IsRunning)
            {
                throw new Exception("No connection exist!");
            }

            SyncVarPacket packet;
            switch (packetID)
            {
                case PacketId.SyncVar:
                    packet = new SyncVarPacket(args, invokeInServer, networkIdentity.id);
                    BroadcastPacket(packet, networkInterface, clientsBeforeSync.ToArray());
                    break;
                default:
                    break;
            }
        }

        protected override void MethodNetworkAttribute_onNetworkingInvoke(MethodInterceptionArgs args, PacketId packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity networkIdentity)
        {
            if (!IsRunning)
            {
                throw new Exception("No connection exist!");
            }
            MethodPacket packet;
            switch (packetID)
            {
                case PacketId.BroadcastMethod:
                    packet = new BroadcastPacket(networkIdentity.id, args, invokeInServer);
                    BroadcastPacket(packet, networkInterface, clientsBeforeSync.ToArray());
                    break;
                case PacketId.Command:
                    packet = new CommandPacket(args, networkIdentity.id);
                    SendPacketToAUser(packet, networkInterface, clients[networkIdentity.ownerId].Ip, clients[networkIdentity.ownerId].TcpPort);
                    break;
                default:
                    break;
            }
        }

        public void SendLobbyInfo(string ip, int port, string data)
        {
            if (!IsRunning)
            {
                throw new Exception("No connection exist!");
            }

            LobbyInfoPacket packet = new LobbyInfoPacket(data);
            BroadcastPacket(packet, NetworkInterface.TCP, GetIdByIpAndPort(ip, port));
        }

        private void ClientDsiconnected(int id)
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
            BroadcastPacket(packet, NetworkInterface.TCP, clientsBeforeSync.ToArray());
            InitIdentityLocally(identity, owner, id, args);
            return identity;
        }
    }
}
