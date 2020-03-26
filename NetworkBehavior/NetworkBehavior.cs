using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Networking
{
    public enum PacketId
    {
        LobbyInfo,
        InitiateDircetInterface,
        DircetInterfaceInitiating,
        BroadcastMethod,
        Command,
        SyncVar,
        SpawnObject,
        BeginSynchronization,
    }

    public enum NetworkInterface
    {
        TCP,
        UDP
    }

    internal struct EndPoint
    {
        public string Ip { get; set; }
        public int TcpPort { get; set; }
        public int UdpPort { get; set; }

        public EndPoint(string ip, int tcpPort)
        {
            Ip = ip;
            TcpPort = tcpPort;
            UdpPort = 0;
        }
    }

    internal struct SocketInfo
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public NetworkInterface NetworkInterface { get; set; }

        public SocketInfo(string ip, int port, NetworkInterface networkInterface)
        {
            Ip = ip;
            Port = port;
            NetworkInterface = networkInterface;
        }
    }



    public abstract class NetworkBehavior
    {
        internal const string WARNING_MESSAGEP_REFIX = "NetworkBehavior lib WARNING: ";
        internal static List<Action> synchronousActions = new List<Action>();
        public delegate void LobbyInfoEventHandler(string info);
        public event LobbyInfoEventHandler OnLobbyInfoEvent;
        public delegate void IdentityInitializeEventHandler(NetworkIdentity client);
        public event IdentityInitializeEventHandler OnRemoteIdentityInitialize;
        public event IdentityInitializeEventHandler OnLocalIdentityInitialize;

        public readonly int serverPort;
        public int id;
        internal static Dictionary<string, Type> classes = new Dictionary<string, Type>();

        

        public NetworkBehavior(int serverPort)
        {
            this.serverPort = serverPort;
        }

        public void Start()
        {
            MethodNetworkAttribute.onNetworkingInvoke += MethodNetworkAttribute_onNetworkingInvoke;
            SyncVar.onNetworkingInvoke += SyncVar_onNetworkingInvoke;
        }
        protected abstract void SyncVar_onNetworkingInvoke(LocationInterceptionArgs args, PacketId packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity networkIdentity);

        protected abstract void MethodNetworkAttribute_onNetworkingInvoke(MethodInterceptionArgs args, PacketId packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity networkIdentity);
       
        protected virtual void ReceivedEvent(object[] args, string ip, int port)
        {
            try
            {
                SocketInfo info = new SocketInfo(ip, port, NetworkInterface.UDP);
                ParseArgs(args, info);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot parse packet: ");
                print(args);
                Console.WriteLine(e);
            }
        }

        private protected void ParseArgs(object[] args, SocketInfo socketInfo)
        {
            if (!int.TryParse(args[0].ToString(), out int packetID))
            {
                throw new Exception("Invalid packet recived, id: " + args[0]);
            }
            ParsePacket((PacketId) packetID, args, socketInfo);
        }

        private protected virtual void ParsePacket(PacketId packetId, object[] args, SocketInfo socketInfo) 
        {
            switch (packetId)
            {
                case PacketId.LobbyInfo:
                    LobbyInfoPacket lobbyInfoPacket = new LobbyInfoPacket(args.ToList());
                    ParseLobbyInfoPacket(lobbyInfoPacket, socketInfo);
                    break;
                //case PacketId.InitiateDircetInterface:
                //    break;
                //case PacketId.DircetInterfaceInitiating:
                //    packet = new DircetInterfaceInitiatingPacket(ref args);
                //    break;
                case PacketId.BroadcastMethod:
                    BroadcastPacket broadcastPacket = new BroadcastPacket(args.ToList());
                    ParseBroadcastPacket(broadcastPacket, socketInfo);
                    break;
                case PacketId.Command:
                    CommandPacket commandPacket = new CommandPacket(args.ToList());
                    ParseCommandPacket(commandPacket, socketInfo);
                    break;
                case PacketId.SyncVar:
                    SyncVarPacket syncVarPacket = new SyncVarPacket(args.ToList());
                    ParseSyncVarPacket(syncVarPacket, socketInfo);
                    break;
                case PacketId.SpawnObject:
                    SpawnObjectPacket spawnObjectPacket = new SpawnObjectPacket(args.ToList());
                    ParseSpawnObjectPacket(spawnObjectPacket, socketInfo);
                    break;
                //case PacketId.BeginSynchronization:
                //    packet = new BeginSynchronizationPacket(ref args);
                //    break;
                default:
                    throw new Exception("Invalid packet recived, id: " + args[0]);
            }
        }

        private protected virtual void ParseLobbyInfoPacket(LobbyInfoPacket lobbyInfoPacket, SocketInfo socketInfo)
        {
            OnLobbyInfoEvent?.Invoke(lobbyInfoPacket.Info);
        }

        private protected virtual void ParseSyncVarPacket(SyncVarPacket syncVarPacket, SocketInfo socketInfo)
        {
            if (TryGetNetworkIdentityByPacket(syncVarPacket, out NetworkIdentity identity))
            {
                SyncVar.NetworkInvoke(identity, syncVarPacket);
            }
        }

        private protected virtual void ParseSpawnObjectPacket(SpawnObjectPacket spawnObjectPacket, SocketInfo socketInfo)
        {
            ParseSpawnPacket(spawnObjectPacket, socketInfo);
        }

        private protected virtual void ParseSpawnPacket(SpawnPacket spawnPacket, SocketInfo socketInfo)
        {
            NetworkIdentity identity;
            object o = spawnObjectLocaly(spawnPacket.InstanceName);
            identity = o as NetworkIdentity;
            InitIdentityLocally(identity, spawnPacket.OwnerId, spawnPacket.NetworkIdentityId, spawnPacket.SpawnParams);
        }

        private protected virtual void ParseBroadcastPacket(BroadcastPacket broadcastPacket, SocketInfo socketInfo)
        {
            ParseMethodPacket(broadcastPacket, socketInfo);
        }

        private protected virtual void ParseCommandPacket(CommandPacket commandPacket, SocketInfo socketInfo)
        {
            ParseMethodPacket(commandPacket, socketInfo);
        }

        private protected virtual void ParseMethodPacket(MethodPacket methodPacket, SocketInfo socketInfo)
        {
            if (TryGetNetworkIdentityByPacket(methodPacket, out NetworkIdentity identity))
            {
                MethodNetworkAttribute.NetworkInvoke(identity, methodPacket);
            }
        }

        private protected bool TryGetNetworkIdentityByPacket(NetworkIdentityBasePacket networkIdentityPacket, out NetworkIdentity identity)
        {
            identity = GetNetworkIdentityById(networkIdentityPacket.NetworkIdentityId);
            if (identity == null)
            {
                PrintWarning("cannot get netIdentity from packet:");
                print(networkIdentityPacket.Data.ToArray());
                return false;
            }
            return true;
        }

        private void InvokeMethodLocaly(MethodPacket packet)
        {
            NetworkIdentity identity = GetNetworkIdentityById(packet.NetworkIdentityId);
            if (identity == null)
            {
                return;
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

        protected Type getNetworkClassTypeByName(string fullName)
        {
            if (classes.TryGetValue(fullName, out Type type))
            {
                return type;
            }
            return null;
        }

        protected virtual void InitIdentityLocally(NetworkIdentity identity, int ownerID, int id, params object[] valuesByFields)
        {
            identity.NetworkBehavior = this;
            identity.ownerId = ownerID;
            identity.id = id;
            identity.hasAuthority = ownerID == this.id;
            identity.isServerAuthority = ownerID == serverPort;
            if (valuesByFields != null && valuesByFields.Length != 0)
            {
                Dictionary<string, string> valuesByFieldsDict = valuesByFields.Select(v => v.ToString().Split('+')).ToDictionary(k => k[0], v => v[1]);
                SetObjectFieldsByValues(identity, valuesByFieldsDict);
                identity.hasFieldsBeenInitialized = true;
            }
            identity.PreformEvents();
            if (identity.hasAuthority)
            {
                OnLocalIdentityInitialize?.Invoke(identity);
            }
            else
            {
                OnRemoteIdentityInitialize?.Invoke(identity);
            }
        }

        const BindingFlags getBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        protected Dictionary<string, string> GetValuesByFieldsFromObject(object obj)
        {
            Type type = obj.GetType();
            List<MemberInfo> members = type.GetFields(getBindingFlags).Cast<MemberInfo>().Concat(type.GetProperties(getBindingFlags)).Where(prop => prop.Name.Length >= 5 && prop.Name.ToLower().Substring(0, 4).Equals("sync")).ToList();
            Dictionary<string, string> dic = members.ToDictionary(p => p.Name.ToString(), p =>
                (p is FieldInfo) ?
            typeof(NetworkIdentity).IsAssignableFrom(((FieldInfo)p).FieldType) ?
            ((FieldInfo)p).GetValue(obj) != null ? (((FieldInfo)p).GetValue(obj) as NetworkIdentity).id.ToString() : "null" :
            ((FieldInfo)p).GetValue(obj).ToString() :

            typeof(NetworkIdentity).IsAssignableFrom(((PropertyInfo)p).PropertyType) ? 
            ((PropertyInfo)p).GetValue(obj) != null ? (((PropertyInfo)p).GetValue(obj) as NetworkIdentity).id.ToString() : "null" :
            ((PropertyInfo)p).GetValue(obj).ToString()
            );
            return dic;
        }

        private void SetObjectFieldsByValues(object obj, Dictionary<string, string> valuesByFields)
        {
            Type type = obj.GetType();
            type.GetFields(getBindingFlags).Cast<MemberInfo>().Concat(type.GetProperties(getBindingFlags)).Where(prop => prop.Name.Length >= 5 && prop.Name.ToLower().Substring(0, 4).Equals("sync")).
                Where(p => valuesByFields.Keys.Contains(p.Name)).ToList().ForEach(p =>
                {
                    if (!valuesByFields[p.Name].Equals("null")) {
                        if (p is FieldInfo)
                        {
                            ((FieldInfo)p).SetValue(obj, Operations.getValueAsObject(((FieldInfo)p).FieldType.Name, valuesByFields[p.Name]));
                        }
                        else
                        {
                            ((PropertyInfo)p).SetValue(obj, Operations.getValueAsObject(((PropertyInfo)p).PropertyType.Name, valuesByFields[p.Name]));
                        }
                    }
                });
        }
    
        public NetworkIdentity GetNetworkIdentityById(int id)
        {
            if (!NetworkIdentity.entities.TryGetValue(id, out NetworkIdentity identity))
            {
                PrintWarning("no NetworkIdentity with id " + id + " was found.");
                    int i = identity.id;
                return null;
            }
            return identity;
        }

        internal static int GetIdByIpAndPort(string ip, int port)
        {
            return int.Parse(ip.Replace(".", "") + port.ToString());
        }

        internal static void print(object[] s)
        {
            foreach (object item in s)
            {
                Console.Write("!" + item.ToString() + "!");
            }
            Console.WriteLine();
        }

        public static void RunActionsSynchronously()
        {
            lock(synchronousActions)
            {
                foreach(Action action in synchronousActions)
                {
                    action.Invoke();
                }
                synchronousActions.Clear();
            }
        }

        internal static void PrintWarning(string message)
        {
            Console.Error.WriteLine(WARNING_MESSAGEP_REFIX + message);
        }
    }
}
