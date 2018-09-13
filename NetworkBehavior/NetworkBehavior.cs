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
                    var args = i.GetType().GetProperties().Where(prop => prop.Name.ToLower().Substring(0, 4).Equals("sync")).Select(p => p.GetValue(i)).ToArray().Cast<String>().ToArray();
                    
                    spawnPacket = new SpawnPacket(this, getNetworkClassTypeByName(i.GetType().FullName), i.id, i.ownerId, args); //Spawn all existing clients in the remote client
                    spawnPacket.SendToAUser(NetworkInterface.TCP, port);
                }
                //Console.WriteLine("Spawn all existing clients");
                NetworkIdentity identity = Activator.CreateInstance(player.GetType()) as NetworkIdentity;//Spawn the client player locally
                identity.id = port;
                identity.ownerId = port;
                identity.isInServer = true;
                identity.ThreadPreformEvents();
                clients.Add(port, new EndPoint(identity, ip));
               // Console.WriteLine("spawned the client player locally");
                SpawnLocalPlayerPacket packet = new SpawnLocalPlayerPacket(this, player.GetType(), port);//spawn the client player in the remote client
                packet.SendToAUser(NetworkInterface.TCP, port);
                //Console.WriteLine("spawned the client player in the remote client");

                spawnPacket = new SpawnPacket(this, player.GetType(), identity.id, identity.ownerId);//spawn the client player in all other clients
                spawnPacket.Send(NetworkInterface.TCP, port);
                //Console.WriteLine("spawned the client player in all other clients");
            }
        }


        private void SyncVar_onNetworkingInvoke(LocationInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, int id)
        {
            if (!isConnected)
            {
                throw new Exception("No connection exist!");
            }
          
            SyncVarPacket packet;
            switch (packetID)
            {
                case PacketID.SyncVar:
                    packet = new SyncVarPacket(this, args, id);
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

        private void MethodNetworkAttribute_onNetworkingInvoke(MethodInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, bool invokeInServer, int id)
        {
            if (!isConnected)
            {
                throw new Exception("No connection exist!");
            }
            MethodPacket packet;
            switch (packetID)
            {
                case PacketID.BroadcastMethod:
                    packet = new BroadcastMethodPacket(this, args, invokeInServer, id);
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
                    packet = new CommandPacket(this, args, id);
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
            //NetworkIdentity identity = Activator.CreateInstance(player.GetType()) as NetworkIdentity;
            //player = identity;
            player.id = port;
            player.ownerId = port;
            player.isServer = true;
            player.isInServer = true;
            player.hasAuthority = true;
            player.isLocalPlayer = true;
            player.ThreadPreformEvents();
            server.OnConnectionAcceptedEvent += Server_connectionAcceptedEvent;
            server.OnConnectionLobbyAcceptedEvent += Server_OnConnectionLobbyAcceptedEvent;
            server.OnClientDisconnectedEvent += Server_OnClientDisconnectedEvent;
        }

        private void Server_OnClientDisconnectedEvent(string ip, int port)
        {
            //NetworkIdentityDisconnectedPacket packet = new NetworkIdentityDisconnectedPacket(this, port);
            //packet.send(NetworkInterface.TCP);
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
                    SyncVar.networkInvoke(identity, args);
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
                    identity = o as NetworkIdentity;
                    identity.ownerId = int.Parse(args[1]);
                    if (identity.ownerId == player.id)
                    {
                        identity.hasAuthority = true;
                    }
                    identity.id = int.Parse(args[args.Length - 1]);
                    identity.isInServer = isServer;
                    Object[] objs = new object[args.Length - 1 - 2];
                    for (int i = 2; i < args.Length - 1; i++)
                    {
                        objs[i - 2] = args[i];
                    }
                    identity.ThreadPreformEvents(objs);
                    Console.WriteLine("New entity at: " + args[args.Length - 1] + " " + args[0]);
                    break;
                case (int)PacketID.SpawnLocalPlayer:
                    //o = spawnObjectLocaly(srts[0]);
                    //player = o as NetworkIdentity;
                    player.id = int.Parse(args[1]);
                    DircetInterfaceInitiatingPacket packet = new DircetInterfaceInitiatingPacket(this, player.id);
                    packet.Send(NetworkInterface.TCP);
                    player.ownerId = int.Parse(args[1]);
                    player.hasAuthority = true;
                    player.isInServer = isServer;
                    player.isLocalPlayer = true;
                    player.ThreadPreformEvents();
                    isLocalPlayerSpawned = true;
                    break;
                case (int)PacketID.SpawnWithLocalAuthority:
                    o = spawnObjectLocaly(args[0]);
                    identity = o as NetworkIdentity;
                    identity.ownerId = int.Parse(args[1]);
                    identity.id = int.Parse(args[2]);
                    identity.hasAuthority = true;
                    identity.isInServer = isServer;
                    identity.ThreadPreformEvents();
                    break;
                case (int)PacketID.BeginSynchronization:
                    Synchronize(ip, port);
                    break;
                //case (int)PacketID.NetworkIdentityDisconnected:            
                //  clientDsiconnected(int.Parse(srts[0]));
                // break;
                default:
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
            catch (Exception)
            {
                Console.WriteLine("Cannot find " + fullName + "class name, did you register the class?");
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

        public NetworkIdentity spawnWithServerAuthority(Type instance, params String[] args)
        {
            if (!isServer)
            {
                throw new Exception("Cannot spawn network instance on client");
            }
            int id = NetworkIdentity.lastId + 1;
            NetworkIdentity.lastId++;
            NetworkIdentity identity = Activator.CreateInstance(instance) as NetworkIdentity;
            identity.id = id;
            identity.ownerId = port;
            identity.hasAuthority = true;
            identity.isInServer = true;
            identity.ThreadPreformEvents(args);
            SpawnPacket packet = new SpawnPacket(this, instance, id, port, args);
            packet.Send(NetworkInterface.TCP);
            return identity;
        }

        public NetworkIdentity spawnWithClientAuthority(Type instance, int clientId, params String[] args)
        {
            if (!isServer)
            {
                throw new Exception("Cannot spawn network instance on client");
            }
            int id = NetworkIdentity.lastId + 1;
            NetworkIdentity.lastId++;
            NetworkIdentity identity = Activator.CreateInstance(instance) as NetworkIdentity;
            identity.id = id;
            identity.ownerId = clientId;
            identity.isInServer = true;
            identity.ThreadPreformEvents(args);
            SpawnPacket packet = new SpawnPacket(this, instance, id, clientId, args);
            packet.Send(NetworkInterface.TCP);
            return identity;
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

        private static NetworkIdentity getNetworkIdentityFromLastArg(ref string[] arg)
        {
            NetworkIdentity identity = getNetworkIdentityById(int.Parse(arg[arg.Length - 1] + ""));
            List<string> temp = arg.ToList();
            temp.RemoveAt(temp.Count - 1);
            arg = temp.ToArray();
            return identity;
        }

        internal static NetworkIdentity getNetworkIdentityById(int id)
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
