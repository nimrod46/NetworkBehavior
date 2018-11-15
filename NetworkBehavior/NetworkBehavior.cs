using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpApi;
using UdpApi;
using System.Web.Mvc;
using PostSharp.Aspects;
using PostSharp.Serialization;
using Networking;
using System.Reflection;

namespace Networking
{
    public enum PacketID
    {
        LobbyInfo,
        DircetInterfaceInitiating,
        BroadcastMethod,
        Command,
        SyncVar,
        Spawn,
        SpawnLocalPlayer,
        SpawnWithLocalAuthority,
        BeginSynchronization,
        NetworkIdentityDisconnected
        
    }

    public enum NetworkInterface
    {
        TCP,
        UDP
    }

    public struct EndPoint
    {
        public NetworkIdentity identity { get; set; }
        public string ip { get; set; }
        public int port { get; set; }

        public EndPoint(NetworkIdentity identity, string ip)
        {
            this.identity = identity;
            this.ip = ip;
            port = 0;
        }
    }


    public class NetworkBehavior
    {
        internal static Dictionary<string, Type> classes = new Dictionary<string, Type>();
        internal static NetworkBehavior instance;
        internal static Dictionary<int, EndPoint> clients = new Dictionary<int, EndPoint>();

        public NetworkIdentity player { get; private set; }
        public bool isLocalPlayerSpawned { get; set; }
        public string ip { get; set; }
        public int port = 1331;
        public int numberOfPlayer { get
            {
                return clients.Count;
            } }
        internal Server server { get; set; }
        internal Client client { get; set; }
        internal DirectServer directServer { get; set; }
        internal DirectClient directClient { get; set; }
        public bool isServer { get; private set; }
        public bool isConnected { get; private set; }
        public delegate void ConnectionLobbyAcceptedEventHandler(string ip, int port, long ping);
        public event ConnectionLobbyAcceptedEventHandler OnConnectionLobbyAcceptedEvent;
        public delegate void LobbyInfoEventHandler(string info);
        public event LobbyInfoEventHandler OnLobbyInfoEvent;
        public delegate void PlayerSynchronizedEventHandler(NetworkIdentity client);
        public event PlayerSynchronizedEventHandler OnPlayerSynchronized;

        public NetworkBehavior(NetworkIdentity player, string ip, int port)
        {
            instance = this;
            this.player = player;
            this.ip = ip;
            this.port = port;
            isLocalPlayerSpawned = false;
            player.OnBeginSynchronization += Player_OnBeginSynchronization;             
        }

        private void Player_OnBeginSynchronization()
        {
            BeginSynchronizationPacket packet = new BeginSynchronizationPacket(this);
            packet.Send(NetworkInterface.TCP);
        }

        public NetworkBehavior(NetworkIdentity player, int port)
        {
            instance = this;
            this.player = player;
            this.port = port;
            isLocalPlayerSpawned = false;   
        }
        
        private void registerEvents()
        {
            MethodNetworkAttribute.onNetworkingInvoke += MethodNetworkAttribute_onNetworkingInvoke;
            SyncVar.onNetworkingInvoke += SyncVar_onNetworkingInvoke;
        }

        private void Server_connectionAcceptedEvent(string ip, int port, long ping)
        {
            
        }

        private void Synchronize(string ip, int port)
        {
            lock (player)
            {
                Console.WriteLine("New player at: " + port);
                SpawnPacket spawnPacket;
                foreach (NetworkIdentity i in NetworkIdentity.entities.Values)
                {
                    Dictionary<string, string> valuesByFields = GetValuesByFieldsFromObject(i);
                    var args = valuesByFields.Select(k => k.Key + "+" + k.Value).ToArray();
                    spawnPacket = new SpawnPacket(this, getNetworkClassTypeByName(i.GetType().FullName), i.id, i.ownerId, args); //Spawn all existing clients in the remote client
                    spawnPacket.SendToAUser(NetworkInterface.TCP, port);
                }
                //Console.WriteLine("Spawn all existing clients");

                SpawnLocalPlayerPacket packet = new SpawnLocalPlayerPacket(this, player.GetType(), port);//spawn the client player in the remote client
                packet.SendToAUser(NetworkInterface.TCP, port);
                //Console.WriteLine("spawned the client player in the remote client");

                spawnPacket = new SpawnPacket(this, player.GetType(), port, port);//spawn the client player in all other clients
                spawnPacket.Send(NetworkInterface.TCP, port);
                //Console.WriteLine("spawned the client player in all other clients");

                NetworkIdentity identity = Activator.CreateInstance(player.GetType()) as NetworkIdentity;//Spawn the client player locally
                InitIdentityLocally(identity, port, port, false, false, true);
                clients.Add(port, new EndPoint(identity, ip));
                // Console.WriteLine("spawned the client player locally");
                OnPlayerSynchronized?.Invoke(identity);
            }
        }


        private void SyncVar_onNetworkingInvoke(LocationInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity networkIdentity)
        {
            if (!isConnected)
            {
                throw new Exception("No connection exist!");
            }
          
            SyncVarPacket packet;
            switch (packetID)
            {
                case PacketID.SyncVar:
                    packet = new SyncVarPacket(this, args, invokeInServer, networkIdentity.id);
                    if (isServer)
                    {
                        parsePacket(packet.GetArgs().ToArray(), null, 0, networkInterface);
                    }
                    else
                    {
                        packet.Send(networkInterface);
                    }
                    break;
                default:
                    break;
            }
        }

        private void MethodNetworkAttribute_onNetworkingInvoke(MethodInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity networkIdentity)
        {
            if (!isConnected)
            {
                throw new Exception("No connection exist!");
            }
            MethodPacket packet;
            switch (packetID)
            {
                case PacketID.BroadcastMethod:
                    packet = new BroadcastMethodPacket(this, args, invokeInServer, networkIdentity.id);
                    if (isServer)
                    {
                        parsePacket(packet.GetArgs().ToArray(), null, 0, networkInterface);
                    }
                    else
                    {
                        packet.Send(networkInterface);
                    }
                    break;
                case PacketID.Command:
                    packet = new CommandPacket(this, args, networkIdentity.id);
                    if (isServer)
                    {
                        packet.SendToAUser(networkInterface, networkIdentity.ownerId);
                    }
                    else
                    {
                        packet.Send(networkInterface);
                    }
                    break;
                default:
                    break;
            }
            
        }

        public bool StartLobbyClient(out long ping)
        {
            try
            {
                client = new Client(ip, port, '§', '|');
            }
            catch (Exception e)
            {

                throw e;
            }
            client.OnReceivedEvent += Client_receivedEvent;
            return client.connectLobby(out ping);
        }


        public void StartClient()
        {
            try
            {
                client = new Client(ip, port, '§', '|');
                client.connect();
                client.OnReceivedEvent += Client_receivedEvent;
                client.OnConnectionLostEvent += Client_serverDisconnectedEvent;
                directClient = new DirectClient(ip, port + 1, '|');
                directClient.start();
                directClient.OnReceivedEvent += receivedEvent;
            }
            catch (Exception e)
            {
                throw e;
            }

            isConnected = true;
            isServer = false;
            registerEvents();
        }

        public void StartServer()
        {
            try
            {
                server = new Server(port, '§', '|');
                server.startServer();
                server.OnReceivedEvent += Server_receivedEvent;
                directServer = new DirectServer(port + 1, '|');
                directServer.start();
                directServer.OnReceivedEvent += receivedEvent;
            }
            catch (Exception e)
            {
                throw e;
            }

            isConnected = true;
            isServer = true;
            registerEvents();

            player.isInServer = true;
            InitIdentityLocally(player, port, port, true, true, true);

            server.OnConnectionAcceptedEvent += Server_connectionAcceptedEvent;
            server.OnConnectionLobbyAcceptedEvent += Server_OnConnectionLobbyAcceptedEvent;
            server.OnClientDisconnectedEvent += Server_OnClientDisconnectedEvent;
        }

        private void Server_OnClientDisconnectedEvent(string ip, int port)
        {
            clientDsiconnected(port);
        }

        private void Server_OnConnectionLobbyAcceptedEvent(string ip, int port, long ping)
        {
            OnConnectionLobbyAcceptedEvent?.Invoke(ip, port, ping);
        }

        public void sendLobbyInfo(int port, string data)
        {
            if (!isServer)
            {
                throw new Exception("Only server can send info about the lobby!");
            }

            if (!isConnected)
            {
                throw new Exception("No connection exist!");
            }

            LobbyInfoPacket packet = new LobbyInfoPacket(this, data);
            packet.Send(NetworkInterface.TCP, port);
        }

        private void receivedEvent(string[] args, string ip, int port)
        {
            try
            {
                parsePacket(args, ip, port, NetworkInterface.UDP);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot parse packet: ");
                foreach (string s in args)
                {
                    print(args);
                }
                Console.WriteLine(e);
            }
        }

        private void parsePacket(string[] args, string ip, int port, NetworkInterface networkInterface)
        {
            string[] orgArgs = args;
            int packetID; object o; NetworkIdentity identity;
            if (!int.TryParse(args[0], out packetID))
            {
                throw new Exception("Invalid packet recived, id: " + args[0]);
            }
            List<string> temp = args.ToList();
            temp.RemoveAt(0);
            args = temp.ToArray();
            switch (packetID)
            {
                case (int)PacketID.LobbyInfo:
                    OnLobbyInfoEvent?.Invoke(args[0]);
                    break;
                case (int)PacketID.DircetInterfaceInitiating:
                    EndPoint eP;
                    if (clients.TryGetValue(int.Parse(args[0]), out eP))
                    {
                        eP.port = port;
                        clients[int.Parse(args[0])] = eP;
                    }
                    else
                    {
                        Console.Error.WriteLine("UDP NOT init for " + port);
                    }
                    break;
                case (int)PacketID.BroadcastMethod:
                    if (isServer)
                    {
                        if (networkInterface == NetworkInterface.TCP)
                        {
                            server.broadcast(orgArgs);
                        }
                        else
                        {
                            for (int i = 0; i < clients.Count; i++)
                            {
                                directServer.send(orgArgs, clients.Values.ElementAt(i).ip, clients.Values.ElementAt(i).port);
                            }
                        }
                        if (!bool.Parse(args[args.Length - 2]))
                        {
                            return;
                        }
                    }
                    invokeMethodLocaly(args);
                    break;
                case (int)PacketID.Command:
                    identity = getNetworkIdentityFromLastArg(ref args);
                    if (identity == null)
                    {
                        return;
                    }
                    MethodNetworkAttribute.networkInvoke(player, args);
                    break;
                case (int)PacketID.SyncVar:
                    identity = getNetworkIdentityFromLastArg(ref args);
                    if (identity == null)
                    {
                        return;
                    }
                    
                    SyncVar.networkInvoke(identity, args, this.isServer);
                    if (isServer)
                    {
                        if (networkInterface == NetworkInterface.TCP)
                        {
                            server.broadcast(orgArgs);
                        }
                        else
                        {
                            directBroadcast(orgArgs);
                        }
                    }
                    break;
                case (int)PacketID.Spawn:
                    o = spawnObjectLocaly(args[0]);
                    string[] valuesOfFields = new string[args.Length - 1 - 2];
                    for (int i = 2; i < args.Length - 1; i++)
                    {
                        valuesOfFields[i - 2] = args[i];
                    }
                    identity = o as NetworkIdentity;
                    InitIdentityLocally(identity, int.Parse(args[1]), int.Parse(args[args.Length - 1]), player.id == int.Parse(args[1]), false, this.port == int.Parse(args[args.Length - 1]), valuesOfFields);
                    break;
                case (int)PacketID.SpawnLocalPlayer:
                    valuesOfFields = new string[args.Length - 1 - 2];
                    for (int i = 2; i < args.Length - 1; i++)
                    {
                        valuesOfFields[i - 2] = args[i];
                    }
                    InitIdentityLocally(player, int.Parse(args[1]), int.Parse(args[1]), true, true, this.port == int.Parse(args[1]), valuesOfFields);
                    isLocalPlayerSpawned = true;
                    DircetInterfaceInitiatingPacket packet = new DircetInterfaceInitiatingPacket(this, player.id);
                    packet.Send(NetworkInterface.UDP);
                    Console.WriteLine("Spawned local player: " + player.id);
                    break;
                case (int)PacketID.BeginSynchronization:
                    Synchronize(ip, port);
                    break;
                //case (int)PacketID.NetworkIdentityDisconnected:            
                //  clientDsiconnected(int.Parse(srts[0]));
                // break;
                default:
                    Console.Error.WriteLine("Invalid packet has been received!");
                    print(args);
                    break;
            }
        }

        NetworkIdentity identity;
        private void invokeMethodLocaly(string[] srts)
        {
            identity = getNetworkIdentityFromLastArg(ref srts);
            if (identity == null)
            {
                return;
            }
            MethodNetworkAttribute.networkInvoke(identity, srts);
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

        public static void print(object[] s)
        {
            foreach (object item in s)
            {
                Console.Write("!" + item.ToString() + "!");
            }
            Console.WriteLine();
        }

        private void Client_serverDisconnectedEvent(string ip, int port)
        {
            isConnected = false;
            player.ServerDisconnected();
            Close();
        }

        private void Server_receivedEvent(string[][] data, string ip, int port)
        {
            foreach (string[] s in data)
            {
                try
                {
                    parsePacket(s, ip, port, NetworkInterface.TCP);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Cannot parse packet: ");
                    print(s);
                    Console.WriteLine(e);
                }
            }
        }

        private void Client_receivedEvent(string[][] data, string ip, int port)
        {
            foreach(string[] s in data)
            {           
                try
                {
                    parsePacket(s, ip, port, NetworkInterface.TCP);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Cannot parse packet: ");
                    print(s);
                    Console.WriteLine(e);
                }
            }
        }
        
        private object spawnObjectLocaly(string fullName)
        {
            try
            {
                return Activator.CreateInstance(getNetworkClassTypeByName(fullName));
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot find " + fullName + "class name, did you register the class?");
                Console.WriteLine(e);
                Environment.Exit(0);
                return null;
            }
        }

        private Type getNetworkClassTypeByName(string fullName)
        {
            Type type;
            if (classes.TryGetValue(fullName, out type))
            {
                return type;
            }
            return null;
        }

        public NetworkIdentity spawnWithServerAuthority(Type instance, NetworkIdentity identity)
        {
            if (!isServer)
            {
                throw new Exception("Cannot spawn network instance on client");
            }
            if (identity != null && identity.hasInitialized)
            {
                throw new Exception("Cannot spawn network instance that is already in use");
            }

            int id = NetworkIdentity.lastId + 1;
            NetworkIdentity.lastId++;
            string[] args =  null;
            if (identity != null)
            {
                Dictionary<string, string> valuesByFields = GetValuesByFieldsFromObject(identity);
                args = valuesByFields.Select(k => k.Key + "+" + k.Value).ToArray();
            } 
            else
            {
                identity = Activator.CreateInstance(instance) as NetworkIdentity;
            }
            SpawnPacket packet = new SpawnPacket(this, instance, id, port, args);
            packet.Send(NetworkInterface.TCP);
            InitIdentityLocally(identity, port, id, true, true, true, args);
            return identity;
        }

        private void InitIdentityLocally(NetworkIdentity identity, int ownerID, int id, bool hasAuthority, bool isLocalPlayer, bool isServerAuthority, params string[] valuesByFields)
        {
            identity.ownerId = ownerID;
            identity.hasAuthority = hasAuthority;
            identity.id = id;
            identity.isInServer = this.isServer;
            identity.isLocalPlayer = isLocalPlayer;
            identity.isServerAuthority = isServerAuthority;
            if (valuesByFields != null && valuesByFields.Length != 0)
            {
                Dictionary<string, string> valuesByFieldsDict = valuesByFields.Select(v => v.Split('+')).ToDictionary(k => k[0], v => v[1]);
                SetObjectFieldsByValues(identity, valuesByFieldsDict);
                identity.hasFieldsBeenInitialized = true;
            }
            identity.ThreadPreformEvents();
        }

        public NetworkIdentity spawnWithClientAuthority(Type instance, int clientId, NetworkIdentity identity)
        {
            if (!isServer)
            {
                throw new Exception("Cannot spawn network instance on client");
            }

            if (identity != null && identity.hasInitialized)
            {
                throw new Exception("Cannot spawn network instance that is already in use");
            }

            int id = NetworkIdentity.lastId + 1;
            NetworkIdentity.lastId++;
            string[] args =  null;
            if (identity != null)
            {
                Dictionary<string, string> valuesByFields = GetValuesByFieldsFromObject(identity);
                args = valuesByFields.Select(k => k.Key + "+" + k.Value).ToArray();
            }
            else
            {
                identity = Activator.CreateInstance(instance) as NetworkIdentity;
            }
            InitIdentityLocally(identity, clientId, id, false, false, true, args);
            SpawnPacket packet = new SpawnPacket(this, instance, id, clientId, args);
            packet.Send(NetworkInterface.TCP);
            return identity;
        }

            const BindingFlags getBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        private Dictionary<string, string> GetValuesByFieldsFromObject(object obj)
        {
            Type type = obj.GetType();
            return type.GetFields(getBindingFlags).Cast<MemberInfo>().Concat(type.GetProperties(getBindingFlags)).Where(prop => prop.Name.Length >= 5 && prop.Name.ToLower().Substring(0, 4).Equals("sync")).
                ToDictionary(p => p.Name.ToString(), p => (p is FieldInfo) ? ((FieldInfo) p).GetValue(obj).ToString() : ((PropertyInfo)p).GetValue(obj).ToString());
        }

        private void SetObjectFieldsByValues(object obj, Dictionary<string, string> valuesByFields)
        {
            Type type = obj.GetType(); ;
            type.GetFields(getBindingFlags).Cast<MemberInfo>().Concat(type.GetProperties(getBindingFlags)).Where(prop => prop.Name.Length >= 5 && prop.Name.ToLower().Substring(0, 4).Equals("sync")).
                Where(p => valuesByFields.Keys.Contains(p.Name)).ToList().ForEach(p =>
                {
                    if (p is FieldInfo)
                    {
                        ((FieldInfo)p).SetValue(obj, Convert.ChangeType(valuesByFields[p.Name], ((FieldInfo)p).FieldType));
                    }
                    else
                    {
                        ((PropertyInfo)p).SetValue(obj, Convert.ChangeType(valuesByFields[p.Name], ((PropertyInfo)p).PropertyType));
                    }
                });
        }
    
        internal void directBroadcast(string[] args, params int[] ports)
        {
            foreach (EndPoint endPoint in clients.Values)
            {
                if(endPoint.port == 0 || ports.Contains(endPoint.port))
                {
                    continue;
                }
                directServer.send(args, endPoint.ip, endPoint.port);
            }
        }

        private NetworkIdentity getNetworkIdentityFromLastArg(ref string[] arg)
        {
            NetworkIdentity identity = GetNetworkIdentityById(int.Parse(arg[arg.Length - 1] + ""));
            List<string> temp = arg.ToList();
            temp.RemoveAt(temp.Count - 1);
            arg = temp.ToArray();
            return identity;
        }

        public NetworkIdentity GetNetworkIdentityById(int id)
        {
            NetworkIdentity identity;
            if(!NetworkIdentity.entities.TryGetValue(id, out identity))
            {
                Console.WriteLine("NetworkBehavior: no NetworkIdentity with id " + id + " was found.");
                throw new Exception();
               // return null;
            }
            return identity;
        }

        public void Close()
        {
            if (isConnected)
            {
               if (isServer)
                {
                   
                } 
               else
                {
                    client.disconnect();
                    directClient.disconnect();
                }
            }
        }
    }
}
