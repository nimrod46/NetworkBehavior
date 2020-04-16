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
    public class ServerBehavior : NetworkBehavior
    {
        private readonly object syncObj = new object();
        public delegate void ConnectionLobbyAcceptedEventHandler(EndPointId endPointId, long ping);
        public event ConnectionLobbyAcceptedEventHandler OnConnectionLobbyAcceptedEvent;
        public delegate void ClientSynchronizedEventHandler(EndPointId id);
        public event ClientSynchronizedEventHandler OnClientEventHandlerSynchronizedEvent;

        public bool IsRunning { get; set; }
        internal Dictionary<EndPointId, EndPoint> clients = new Dictionary<EndPointId, EndPoint>();
        internal List<EndPointId> clientsBeforeSync = new List<EndPointId>();
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
            localEndPointId = EndPointId.FromLong(serverPort);
        }

        public void Run()
        {
            server = new Server((int) serverEndPointId.Id, '~', '|');
            server.StartServer();
            server.OnReceivedEvent += Server_receivedEvent;

            Start();

            directServer = new DirectServer((int)serverEndPointId.Id + 1, '|');
            directServer.Start();
            directServer.OnReceivedEvent += ReceivedEvent;

            IsRunning = true;

            server.OnConnectionAcceptedEvent += Server_connectionAcceptedEvent;
            server.OnConnectionLobbyAcceptedEvent += Server_OnConnectionLobbyAcceptedEvent;
            server.OnClientDisconnectedEvent += Server_OnClientDisconnectedEvent;
        }

        private void Server_receivedEvent(object[][] data, EndPointId endPointId, SocketInfo socketInfo)
        {
            foreach (string[] s in data)
            {
                try
                {
                    ParseArgs(s, endPointId, socketInfo);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Cannot parse packet: ");
                    Print(s);
                    Console.WriteLine(e);
                }
            }
        }

        private void ReceivedEvent(object[] data, string ip, int port)
        {
            try
            {
                SocketInfo info = new SocketInfo(ip, port, NetworkInterfaceType.UDP);
                ParseArgs(data, GetEndPointIdByUdpPort(port), info);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot parse packet: ");
                Print(data);
                Console.WriteLine(e);
            }
        }

        private EndPointId GetEndPointIdByUdpPort(int udpPort)
        {
            EndPointId endPointId = EndPointId.InvalidIdentityId;
            foreach (var idAndEndPoint in clients)
            {
                if (idAndEndPoint.Value.UdpPort == udpPort)
                {
                    endPointId = idAndEndPoint.Key;
                    return endPointId;
                }
            }
            //PrintWarning("no client with udp port of: " + udpPort + " was found");
            return endPointId;
        }

        private protected override void ParsePacket(PacketId packetId, object[] args, EndPointId endPointId, SocketInfo socketInfo)
        {
            switch (packetId)
            {
                case PacketId.DircetInterfaceInitiating:
                    DircetInterfaceInitiatingPacket initiatingPacket = new DircetInterfaceInitiatingPacket(args.ToList());
                    EndPoint eP;
                    EndPointId clientId = initiatingPacket.EndPointId;
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
                    Synchronize(endPointId, new EndPoint(socketInfo.Ip, socketInfo.Port));
                    break;
                default:
                    base.ParsePacket(packetId, args, endPointId, socketInfo);
                    break;
            }
        }

        private protected override void ParseBroadcastPacket(BroadcastPacket broadcastPacket, EndPointId endPointId, SocketInfo socketInfo)
        {
            BroadcastPacket(broadcastPacket, endPointId, socketInfo);
            base.ParseBroadcastPacket(broadcastPacket, endPointId, socketInfo);
        }

        private protected override void ParseSyncVarPacket(SyncVarPacket syncVarPacket, EndPointId endPointId, SocketInfo socketInfo)
        {
            BroadcastPacket(syncVarPacket, endPointId, socketInfo);
            base.ParseSyncVarPacket(syncVarPacket, endPointId, socketInfo);
        }

        private void BroadcastPacket(Packet packet, EndPointId endPointId, SocketInfo socketInfo)
        {
            BroadcastPacket(packet, socketInfo.NetworkInterface, clientsBeforeSync.Concat(new EndPointId[] { endPointId }).ToArray());
        }

        protected override void InitIdentityLocally(NetworkIdentity identity, EndPointId ownerID, IdentityId id, params object[] valuesByFields)
        {
            identity.isInServer = true;
            base.InitIdentityLocally(identity, ownerID, id, valuesByFields);
        }

        private void Synchronize(EndPointId endPointId, EndPoint endPoint)
        {
            lock (clients)
            {
                Console.WriteLine("New player at: " + endPointId);
                SpawnObjectPacket spawnPacket;
                List<NetworkIdentity> identities = NetworkIdentity.entities.Values.ToList();
                identities.Sort();
                clients.Add(endPointId, endPoint);
                foreach (NetworkIdentity i in identities)
                {
                    if(i.IsDestroyed) { continue; }

                    Dictionary<string, string> valuesByFields = GetValuesByFieldsFromObject(i);
                    var args = valuesByFields.Select(k => k.Key + "+" + k.Value).ToArray();
                    spawnPacket = new SpawnObjectPacket(GetNetworkClassTypeByName(i.GetType().FullName), i.Id, i.OwnerId, args); //Spawn all existing clients in the remote client
                    SendPacketToAUser(spawnPacket, NetworkInterfaceType.TCP, endPointId);
                }
                //Console.WriteLine("Spawn all existing clients");

                InitiateDircetInterfacePacket initiateDircetInterface = new InitiateDircetInterfacePacket(endPointId);//Initiate dircet interface with the client
                SendPacketToAUser(initiateDircetInterface, NetworkInterfaceType.TCP, endPointId);
                //Console.WriteLine("Initiating dircet interface with the client");             
            }
        }

        protected override void OnInvokeBroadcastMethodNetworkly(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string methodName, object[] methodArgs)
        {
            if (!IsRunning)
            {
                throw new Exception("No connection exist!");
            }

            BroadcastPacket packet;
            packet = new BroadcastPacket(networkIdentity.Id, methodName, methodArgs);
            ParseBroadcastPacket(packet, EndPointId.InvalidIdentityId, new SocketInfo(null, serverPort, networkInterface));
        }

        protected override void OnInvokeCommandMethodNetworkly(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string methodName, object[] methodArgs, EndPointId? targetId = null)
        { 
            if (!IsRunning)
            {
                throw new Exception("No connection exist!");
            }
            targetId ??= networkIdentity.OwnerId;
            CommandPacket packet;
            packet = new CommandPacket(networkIdentity.Id, methodName, methodArgs);
            SendPacketToAUser(packet, networkInterface, (EndPointId)targetId);
        }

        protected override void OnInvokeLocationNetworkly(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string locationName, object locationValue)
        {
            if (!IsRunning)
            {
                throw new Exception("No connection exist!");
            }

            SyncVarPacket packet;
            packet = new SyncVarPacket(networkIdentity.Id, locationName, locationValue);
            BroadcastPacket(packet, networkInterface, clientsBeforeSync.ToArray());
        }

        public void SendLobbyInfo(EndPointId endPointId, string data)
        {
            if (!IsRunning)
            {
                throw new Exception("No connection exist!");
            }

            LobbyInfoPacket packet = new LobbyInfoPacket(data);
            BroadcastPacket(packet, NetworkInterfaceType.TCP, endPointId);
        }

        internal void DirectBroadcast(object[] args, params EndPointId[] clientsId)
        {
            lock (clients)
            {
                foreach (var client in clients)
                {
                    if (clientsId.Contains(client.Key))
                    {
                        continue;
                    }
                    if (client.Value.UdpPort == 0)
                    {
                        PrintWarning("cannot send packet as UDP for client: " + client.Key);
                        continue;
                    }
                    directServer.Send(args, client.Value.Ip, client.Value.UdpPort);
                }
            }
        }

        internal void BroadcastPacket(Packet packet, NetworkInterfaceType networkInterface, params EndPointId[] clientsId)
        {
            Broadcast(packet.Data.ToArray(), networkInterface, clientsId);
        }

        internal void Broadcast(object[] args, NetworkInterfaceType networkInterface, params EndPointId[] clientsId)
        {
            if (networkInterface == NetworkInterfaceType.TCP)
            {
                server.Broadcast(args, clientsId);
            }
            else
            {
                DirectBroadcast(args, clientsId);
            }
        }

        internal void SendPacketToAUser(Packet packet, NetworkInterfaceType networkInterface, EndPointId endPointId)
        {
            SendToAUser(packet.Data.ToArray(), networkInterface, endPointId);
        }

        internal void SendToAUser(object[] args, NetworkInterfaceType networkInterface, EndPointId endPointId)
        {
            if (networkInterface == NetworkInterfaceType.TCP)
            {
                server.SendToAUser(args, endPointId);
            }
            else
            {
                directServer.Send(args, clients[endPointId].Ip, clients[endPointId].UdpPort);
            }
        }

        private void Server_OnClientDisconnectedEvent(EndPointId endPointId)
        {
            ClientDsiconnected(endPointId);
        }

        private void Server_OnConnectionLobbyAcceptedEvent(EndPointId endPointId, long ping)
        {
            OnConnectionLobbyAcceptedEvent?.Invoke(endPointId, ping);
        }

        private void Server_connectionAcceptedEvent(EndPointId endPointId, long ping)
        {
            clientsBeforeSync.Add(endPointId);
        }

        private void ClientDsiconnected(EndPointId id)
        {
            Console.WriteLine("Client: " + id + " DISCONNECTED");
            lock (NetworkIdentity.entities)
            {
                List<NetworkIdentity> entitiesToDestroy = new List<NetworkIdentity>();
                for (int i = 0; i < NetworkIdentity.entities.Count; i++)
                {
                    if (NetworkIdentity.entities.Values.ElementAt(i).OwnerId == id)
                    {
                        entitiesToDestroy.Add(NetworkIdentity.entities.Values.ElementAt(i));
                    }
                }
                foreach (NetworkIdentity entity in entitiesToDestroy)
                {
                    entity.InvokeBroadcastMethodNetworkly(nameof(entity.Destroy));
                }
            }
        }

        public NetworkIdentity spawnWithServerAuthority(Type instance, NetworkIdentity identity = null)
        {
            return SpawnIdentity(instance, EndPointId.InvalidIdentityId, identity);
        }

        public NetworkIdentity spawnWithClientAuthority(Type instance, EndPointId clientId, NetworkIdentity identity = null)
        {
            return SpawnIdentity(instance, clientId, identity);

        }

        private NetworkIdentity SpawnIdentity(Type instance, EndPointId clientId, NetworkIdentity identity)
        {
            if (identity != null && identity.hasInitialized)
            {
                throw new Exception("Cannot spawn network instance that is already in use");
            }

            NetworkIdentity.lastId++;
            IdentityId id = NetworkIdentity.lastId;
            string[] args = null;
            if (identity != null)
            {
                Dictionary<string, string> valuesByFields = GetValuesByFieldsFromObject(identity);
                args = valuesByFields.Select(k => k.Key + "+" + k.Value).ToArray();
            }
            identity = Activator.CreateInstance(instance) as NetworkIdentity;
            EndPointId owner = EndPointId.InvalidIdentityId;
            if (clientId == EndPointId.InvalidIdentityId)
            {
                owner = serverEndPointId;
            }
            else
            {
                owner = clientId;
            }
            SpawnObjectPacket packet = new SpawnObjectPacket(instance, id, owner, args);
            BroadcastPacket(packet, NetworkInterfaceType.TCP, clientsBeforeSync.ToArray());
            InitIdentityLocally(identity, owner, id, args);
            return identity;
        }
    }
}
