using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using PostSharp.Aspects;
using PostSharp.Serialization;
using Networking;
using System.Reflection;
using System.IO;
using NetworkingLib;

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
        public NetworkIdentity Identity { get; set; }
        public string Ip { get; set; }
        public int TcpPort { get; set; }
        public int UdpPort { get; set; }

        public EndPoint(NetworkIdentity identity, string ip, int tcpPort)
        {
            Identity = identity;
            Ip = ip;
            TcpPort = tcpPort;
            UdpPort = 0;
        }
    }


    public abstract class NetworkBehavior
    {
        internal static Dictionary<string, Type> classes = new Dictionary<string, Type>();
        public NetworkIdentity player { get; private set; }
        public readonly int serverPort;
        public bool isLocalPlayerSpawned { get; set; }
        public bool isConnected { get; protected set; }
        public delegate void LobbyInfoEventHandler(string info);
        public event LobbyInfoEventHandler OnLobbyInfoEvent;

        public NetworkBehavior(NetworkIdentity player, int serverPort)
        {
            //instance = this;
            this.player = player;
            this.serverPort = serverPort;
            isLocalPlayerSpawned = false;
        }

        private void registerEvents()
        {
            MethodNetworkAttribute.onNetworkingInvoke += MethodNetworkAttribute_onNetworkingInvoke;
            SyncVar.onNetworkingInvoke += SyncVar_onNetworkingInvoke;
        }
        protected abstract void SyncVar_onNetworkingInvoke(LocationInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity networkIdentity);

        protected abstract void MethodNetworkAttribute_onNetworkingInvoke(MethodInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity networkIdentity);
       

        public virtual void Run()
        {
            isConnected = true;
            registerEvents();
        }

        protected virtual void ReceivedEvent(string[] args, string ip, int port)
        {
            try
            {
                ParsePacket(args, ip, port, NetworkInterface.UDP);
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

        protected void ParsePacket(string[] args, string ip, int port, NetworkInterface networkInterface)
        {
            string[] orgArgs = args;
            if (!int.TryParse(args[0], out int packetID))
            {
                throw new Exception("Invalid packet recived, id: " + args[0]);
            }
            List<string> temp = args.ToList();
            temp.RemoveAt(0);
            args = temp.ToArray();
            ParsePacketByPacketID(packetID, args, ip , port, networkInterface, orgArgs);
        }

        protected virtual void ParsePacketByPacketID(int packetID, string[] args, string ip, int port, NetworkInterface networkInterface, string[] originArgs)
        {
            NetworkIdentity identity;
            switch (packetID)
            {
                case (int)PacketID.LobbyInfo:
                    OnLobbyInfoEvent?.Invoke(args[0]);
                    break;
                case (int)PacketID.BroadcastMethod:
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
                    break;
                case (int)PacketID.Spawn:
                    object o = spawnObjectLocaly(args[0]);
                    string[] valuesOfFields = new string[args.Length - 1 - 2];
                    for (int i = 2; i < args.Length - 1; i++)
                    {
                        valuesOfFields[i - 2] = args[i];
                    }
                    identity = o as NetworkIdentity;
                    InitIdentityLocally(identity, int.Parse(args[1]), int.Parse(args[args.Length - 1]), valuesOfFields);
                    break;
                case (int)PacketID.SpawnLocalPlayer:
                    valuesOfFields = new string[args.Length - 1 - 2];
                    for (int i = 2; i < args.Length - 1; i++)
                    {
                        valuesOfFields[i - 2] = args[i];
                    }

                    InitIdentityLocally(player, int.Parse(args[1]), int.Parse(args[1]), valuesOfFields);
                    isLocalPlayerSpawned = true;
                    Console.WriteLine("Spawned local player: " + player.id);
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

        private void invokeMethodLocaly(string[] srts)
        {
            NetworkIdentity identity = getNetworkIdentityFromLastArg(ref srts);
            if (identity == null)
            {
                return;
            }
            MethodNetworkAttribute.networkInvoke(identity, srts);
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

        protected virtual void InitIdentityLocally(NetworkIdentity identity, int ownerID, int id, params string[] valuesByFields)
        {
            identity.NetworkBehavior = this;
            identity.ownerId = ownerID;
            identity.id = id;
            identity.hasAuthority = ownerID == player.id;
            identity.isServerAuthority = ownerID == serverPort;
            identity.isLocalPlayer = id == player.id;
            if (valuesByFields != null && valuesByFields.Length != 0)
            {
                Dictionary<string, string> valuesByFieldsDict = valuesByFields.Select(v => v.Split('+')).ToDictionary(k => k[0], v => v[1]);
                SetObjectFieldsByValues(identity, valuesByFieldsDict);
                identity.hasFieldsBeenInitialized = true;
            }
            identity.PreformEvents();
        }

        const BindingFlags getBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        protected Dictionary<string, string> GetValuesByFieldsFromObject(object obj)
        {
            Type type = obj.GetType();
            return type.GetFields(getBindingFlags).Cast<MemberInfo>().Concat(type.GetProperties(getBindingFlags)).Where(prop => prop.Name.Length >= 5 && prop.Name.ToLower().Substring(0, 4).Equals("sync")).
                ToDictionary(p => p.Name.ToString(), p => (p is FieldInfo) ? ((FieldInfo) p).GetValue(obj).ToString() : ((PropertyInfo)p).GetValue(obj).ToString());
        }

        private void SetObjectFieldsByValues(object obj, Dictionary<string, string> valuesByFields)
        {
            Type type = obj.GetType();
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
                    if (SyncVar.hooks.ContainsKey(p.Name))
                    {
                        SyncVar.hooks[p.Name].Invoke(obj, null);
                    }
                });
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
            if (!NetworkIdentity.entities.TryGetValue(id, out NetworkIdentity identity))
            {
                Console.WriteLine("NetworkBehavior: no NetworkIdentity with id " + id + " was found.");
                throw new Exception();
                // return null;
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
    }
}
