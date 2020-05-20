using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Networking.NetworkIdentity;
using static NetworkingLib.Server;

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
        SyncObjectVars,
        BeginSynchronization,
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

    public abstract class NetworkBehavior
    {
        internal const string WARNING_MESSAGEP_REFIX = "NetworkBehavior lib WARNING: ";
        internal static ConcurrentQueue<Action> synchronousActions = new ConcurrentQueue<Action>();
        public delegate void LobbyInfoEventHandler(string info);
        public event LobbyInfoEventHandler OnLobbyInfoEvent;
        public delegate void IdentityInitializeEventHandler(NetworkIdentity client);
        public event IdentityInitializeEventHandler OnRemoteIdentityInitialize;
        public event IdentityInitializeEventHandler OnLocalIdentityInitialize;
        public delegate void IdentityDestroyEventHandler(NetworkIdentity identity);
        public event IdentityDestroyEventHandler OnRemoteIdentityDestroy;
        public event IdentityDestroyEventHandler OnLocalIdentityDestroy;

        public readonly int serverPort;
        public readonly EndPointId serverEndPointId;
        public EndPointId localEndPointId;
        internal static Dictionary<string, Type> classes = new Dictionary<string, Type>();
        protected bool hasSynchronized;
        

        public NetworkBehavior(int serverPort)
        {
            this.serverPort = serverPort;
            this.serverEndPointId = EndPointId.FromLong(serverPort);
            hasSynchronized = false;
        }

        public void Start()
        {
            NetworkIdentity.OnInvokeBrodcastMethodMethodNetworkly += OnInvokeBroadcastMethodNetworkly;
            NetworkIdentity.OnInvokeCommandMethodNetworkly += OnInvokeCommandMethodNetworkly;
            NetworkIdentity.OnInvokeLocationNetworkly += OnInvokeLocationNetworkly; 
        }

        internal abstract void OnInvokeLocationNetworkly(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string locationName, object locationValue, bool? shouldInvokeSynchronously = null);

        internal abstract void OnInvokeBroadcastMethodNetworkly(BrodcastMethodEventArgs brodcastMethodEventArgs);

        internal abstract void OnInvokeCommandMethodNetworkly(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string methodName, object[] methodArgs, bool? shouldInvokeSynchronously = null, EndPointId? targetId = null);

        private protected void ParseArgs(object[] args, EndPointId endPointId, SocketInfo socketInfo)
        {
            if (!int.TryParse(args[0].ToString(), out int packetID))
            {
                throw new Exception("Invalid packet recived, id: " + args[0]);
            }
            ParsePacket((PacketId) packetID, args, endPointId, socketInfo);
        }

        private protected virtual void ParsePacket(PacketId packetId, object[] args, EndPointId endPointId, SocketInfo socketInfo) 
        {
            switch (packetId)
            {
                case PacketId.LobbyInfo:
                    LobbyInfoPacket lobbyInfoPacket = new LobbyInfoPacket(args.ToList());
                    ParseLobbyInfoPacket(lobbyInfoPacket, endPointId, socketInfo);
                    break;
                case PacketId.BroadcastMethod:
                    BroadcastPacket broadcastPacket = new BroadcastPacket(args.ToList());
                    ParseBroadcastPacket(broadcastPacket, broadcastPacket.ShouldInvokeSynchronously, endPointId, socketInfo);
                    break;
                case PacketId.Command:
                    CommandPacket commandPacket = new CommandPacket(args.ToList());
                    ParseCommandPacket(commandPacket, commandPacket.ShouldInvokeSynchronously, endPointId, socketInfo);
                    break;
                case PacketId.SyncVar:
                    SyncVarPacket syncVarPacket = new SyncVarPacket(args.ToList());
                    ParseSyncVarPacket(syncVarPacket, endPointId, socketInfo);
                    break;
                case PacketId.SpawnObject:
                    SpawnObjectPacket spawnObjectPacket = new SpawnObjectPacket(args.ToList());
                    ParseSpawnObjectPacket(spawnObjectPacket, endPointId, socketInfo);
                    break;
                case PacketId.SyncObjectVars:
                    SyncObjectVars syncObjectVars = new SyncObjectVars(args.ToList());
                    ParseSyncObjectVars(syncObjectVars, endPointId, socketInfo);
                    break;
                default:
                    throw new Exception("Invalid packet recived, id: " + args[0]);
            }
        }

        private protected virtual void ParseSyncObjectVars(SyncObjectVars syncObjectVars, EndPointId endPointId, SocketInfo socketInfo)
        {
            if (TryGetNetworkIdentityByPacket(syncObjectVars, out NetworkIdentity identity))
            {
                Dictionary<string, string> valuesByFieldsDict = syncObjectVars.SpawnParams.Select(v => v.ToString().Split('+')).ToDictionary(k => k[0], v => v[1]);
                SetObjectFieldsByValues(identity, valuesByFieldsDict);
                CallIdentityEvent(identity);
            }
            else if (socketInfo.NetworkInterface == NetworkInterfaceType.TCP)
            {
                PrintWarning("cannot get network identity from packet:");
                Print(syncObjectVars.Data.ToArray());
            }
            
        }

        private protected virtual void ParseLobbyInfoPacket(LobbyInfoPacket lobbyInfoPacket, EndPointId endPointId, SocketInfo socketInfo)
        {
            OnLobbyInfoEvent?.Invoke(lobbyInfoPacket.Info);
        }

        private protected virtual void ParseSyncVarPacket(SyncVarPacket syncVarPacket, EndPointId endPointId, SocketInfo socketInfo)
        {
            if (TryGetNetworkIdentityByPacket(syncVarPacket, out NetworkIdentity identity))
            {
                NetworkIdentity.NetworkSyncVarInvoke(identity, syncVarPacket, syncVarPacket.ShouldInvokeSynchronously);
            }
            else if(socketInfo.NetworkInterface == NetworkInterfaceType.TCP)
            {
                PrintWarning("cannot get network identity from packet:");
                Print(syncVarPacket.Data.ToArray());
            }
        }

        private protected virtual void ParseSpawnObjectPacket(SpawnObjectPacket spawnObjectPacket, EndPointId endPointId, SocketInfo socketInfo)
        {
            ParseSpawnPacket(spawnObjectPacket, socketInfo);
        }

        private protected virtual void ParseSpawnPacket(SpawnPacket spawnPacket, SocketInfo socketInfo)
        {
            NetworkIdentity identity;
            object o = SpawnObjectLocaly(spawnPacket.InstanceName);
            identity = o as NetworkIdentity;
            InitIdentityLocally(identity, spawnPacket.OwnerId, spawnPacket.NetworkIdentityId, spawnPacket.SpawnDuringSync, spawnPacket.SpawnParams);
        }

        private protected virtual void ParseBroadcastPacket(BroadcastPacket broadcastPacket, bool shouldInvokeSynchronously, EndPointId endPointId, SocketInfo socketInfo)
        {
            ParseMethodPacket(broadcastPacket, shouldInvokeSynchronously, socketInfo);
        }

        private protected virtual void ParseCommandPacket(CommandPacket commandPacket, bool shouldInvokeSynchronously, EndPointId endPointId, SocketInfo socketInfo)
        {
            ParseMethodPacket(commandPacket, shouldInvokeSynchronously, socketInfo);
        }

        private protected virtual void ParseMethodPacket(MethodPacket methodPacket, bool shouldInvokeSynchronously, SocketInfo socketInfo)
        {
            if (TryGetNetworkIdentityByPacket(methodPacket, out NetworkIdentity identity))
            {
                NetworkIdentity.NetworkMethodInvoke(identity, methodPacket, shouldInvokeSynchronously);
            }
            else if(socketInfo.NetworkInterface == NetworkInterfaceType.TCP)
            {
                PrintWarning("cannot get network identity from packet:");
                Print(methodPacket.Data.ToArray());
            }
        }

        private protected bool TryGetNetworkIdentityByPacket(NetworkIdentityBasePacket networkIdentityPacket, out NetworkIdentity identity)
        {
            identity = GetNetworkIdentityById(networkIdentityPacket.NetworkIdentityId);
            if (identity == null)
            {
                return false;
            }
            return true;
        }  

        private object SpawnObjectLocaly(string fullName)
        {
            try
            {
                return Activator.CreateInstance(GetNetworkClassTypeByName(fullName));
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot find " + fullName + "class name, did you register the class?");
                Console.WriteLine(e);
                Environment.Exit(0);
                return null;
            }
        }

        protected Type GetNetworkClassTypeByName(string fullName)
        {
            if (classes.TryGetValue(fullName, out Type type))
            {
                return type;
            }
            return null;
        }

        protected virtual void InitIdentityLocally(NetworkIdentity identity, EndPointId ownerId, IdentityId id, bool spawnDuringSync, params object[] valuesByFields)
        {
            identity.NetworkBehavior = this;
            identity.LocalEndPoint = localEndPointId;
            identity.OwnerId = ownerId;
            identity.Id = id;
            identity.hasAuthority = ownerId == this.localEndPointId;
            identity.isServerAuthority = ownerId == serverEndPointId;
            if (valuesByFields != null && valuesByFields.Length != 0)
            {
                Dictionary<string, string> valuesByFieldsDict = valuesByFields.Select(v => v.ToString().Split('+')).ToDictionary(k => k[0], v => v[1]);
                SetObjectFieldsByValues(identity, valuesByFieldsDict);
                identity.hasFieldsBeenInitialized = true;
            }
            identity.AddToEntities();
            if (!spawnDuringSync)
            {
                CallIdentityEvent(identity);
            }
        }

        private void CallIdentityEvent(NetworkIdentity identity)
        {
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
            ((FieldInfo)p).GetValue(obj) != null ? (((FieldInfo)p).GetValue(obj) as NetworkIdentity).Id.ToString() : "null" :
            ((FieldInfo)p).GetValue(obj)?.ToString() :

            typeof(NetworkIdentity).IsAssignableFrom(((PropertyInfo)p).PropertyType) ? 
            ((PropertyInfo)p).GetValue(obj) != null ? (((PropertyInfo)p).GetValue(obj) as NetworkIdentity).Id.ToString() : "null" :
            ((PropertyInfo)p).GetValue(obj)?.ToString()
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
                            ((FieldInfo)p).SetValue(obj, Operations.GetValueAsObject(((FieldInfo)p).FieldType, valuesByFields[p.Name]));
                        }
                        else
                        {
                            ((PropertyInfo)p).SetValue(obj, Operations.GetValueAsObject(((PropertyInfo)p).PropertyType, valuesByFields[p.Name]));
                        }
                    }
                });
        }
   
        internal static void Print(object[] s)
        {
            foreach (object item in s)
            {
                Console.Write("!" + item.ToString() + "!");
            }
            Console.WriteLine();
        }

        public void RunActionsSynchronously()
        {
            if (!hasSynchronized)
            {
                Console.WriteLine("WAITINGGGG: " + synchronousActions.Count);
                return;
            }

            while (synchronousActions.TryDequeue(out Action action))
            {
                action();
            }
        }

        internal void IdentityDestroy(NetworkIdentity networkIdentity)
        {
            if(networkIdentity.hasAuthority)
            {
                OnLocalIdentityDestroy?.Invoke(networkIdentity);
            }
            else
            {
                OnRemoteIdentityDestroy?.Invoke(networkIdentity);
            }
        }

        internal static void PrintWarning(string message, params object[] parameters)
        {
            Console.Error.WriteLine(WARNING_MESSAGEP_REFIX + message, parameters);
        }
    }
}
