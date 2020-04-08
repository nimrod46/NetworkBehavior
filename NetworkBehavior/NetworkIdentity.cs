using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
    
    public class NetworkIdentity : IComparable<NetworkIdentity>
    {
        
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        internal delegate void InvokeMethodNetworklyEvent(PacketId packetID, NetworkIdentity networkIdentity, NetworkInterface networkInterface, string methodName, object[] methodArgs);
        internal static event InvokeMethodNetworklyEvent OnInvokeMethodNetworkly;
        internal delegate void InvokeLocationNetworklyEvent(PacketId packetID, NetworkIdentity networkIdentity, NetworkInterface networkInterface, string locationName, object locationValue);
        internal static event InvokeLocationNetworklyEvent OnInvokeLocationNetworkly;

        public static SortedList<int, NetworkIdentity> entities = new SortedList<int, NetworkIdentity>();
        internal static List<Type> prioritiesIdintities = new List<Type>();
        internal static readonly char packetSpiltter = '¥';
        internal static readonly char argsSplitter = '|';
        internal static int lastId = 0;

        internal Dictionary<string, NetworkMethodExecuter> methodsByClass = new Dictionary<string, NetworkMethodExecuter>();
        internal Dictionary<string, NetworkLocationExecuter> locationByClass = new Dictionary<string, NetworkLocationExecuter>();
        public delegate void NetworkInitialize();
        public event NetworkInitialize OnNetworkInitializeEvent;
        public delegate void HasLocalAuthorityInitialize();
        public event HasLocalAuthorityInitialize OnHasLocalAuthorityInitializeEvent;
        public delegate void DestroyEventHandler(NetworkIdentity identity);
        public event DestroyEventHandler OnDestroyEvent;
        internal delegate void BeginSynchronization();
        internal event BeginSynchronization OnBeginSynchronization;

        public NetworkBehavior NetworkBehavior;
        public bool isServerAuthority = false;
        public bool hasAuthority = false;
        public bool isInServer = false;
        public bool hasInitialized = false;
        public bool hasFieldsBeenInitialized = false;
        public int id;
        public int ownerId;
        internal bool isUsedAsVar;
        public NetworkIdentity()
        {
            if (!NetworkBehavior.classes.ContainsKey(GetType().FullName))
            {
                NetworkBehavior.classes.Add(GetType().FullName, GetType());
            }
            isUsedAsVar = prioritiesIdintities.Any(t => t.IsAssignableFrom(GetType()));
            foreach (MethodBase method in GetType().GetMethods(bindingFlags))
            {
                try
                {
                    methodsByClass.Add(method.Name, new NetworkMethodExecuter(method));
                }
                catch (Exception)
                {
                    //NetworkBehavior.PrintWarning("method overload named: " + method.Name);
                }
            }

            foreach (MemberInfo member in GetType().GetFields(bindingFlags).Cast<MemberInfo>().Concat(GetType().GetProperties(bindingFlags)).Where(prop => prop.Name.Length >= 5 && prop.Name.ToLower().Substring(0, 4).Equals("sync")).ToList())
            {
                LocationInfo location = null;
                if (member is FieldInfo)
                {
                    location = new LocationInfo(member as FieldInfo);
                }
                else if (member is PropertyInfo)
                {
                    location = new LocationInfo(member as PropertyInfo);
                }
                if (typeof(NetworkIdentity).IsAssignableFrom(location.LocationType))
                {
                    prioritiesIdintities.Add(location.LocationType);
                }
                locationByClass.Add(member.Name, new NetworkLocationExecuter(location));
            }

        }

        public void InvokeBroadcastMethodNetworkly(string methodName, NetworkInterface networkInterface = NetworkInterface.TCP, params object[] args)
        {
            if (methodsByClass.TryGetValue(methodName, out NetworkMethodExecuter networkMemberExecuter))
            {
                networkMemberExecuter.InvokeMemberFromLocal(this, () =>
                {
                    List<object> methodArgs = new List<object>();
                    foreach (object o in args)
                    {
                        if (o == null)
                        {
                            methodArgs.Add("null");
                        }
                        else if (o is NetworkIdentity)
                        {
                            methodArgs.Add((o as NetworkIdentity).id.ToString());
                        }
                        else
                        {
                            methodArgs.Add(o.ToString());
                        }
                    }
                    OnInvokeMethodNetworkly.Invoke(PacketId.BroadcastMethod, this, networkInterface, methodName, methodArgs.ToArray());
                });
            }
        }

        public void InvokeBroadcastMethodNetworkly(string methodName, params object[] args)
        {
            InvokeBroadcastMethodNetworkly(methodName, NetworkInterface.TCP, args);
        }

        public void InvokeCommandMethodNetworkly(string methodName, NetworkInterface networkInterface = NetworkInterface.TCP, params object[] args)
        {
            if (methodsByClass.TryGetValue(methodName, out NetworkMethodExecuter networkMemberExecuter))
            {
                networkMemberExecuter.InvokeMemberFromLocal(this, () =>
                {
                    List<object> methodArgs = new List<object>();
                    foreach (object o in args)
                    {
                        if (o == null)
                        {
                            methodArgs.Add("null");
                        }
                        else if (o is NetworkIdentity)
                        {
                            methodArgs.Add((o as NetworkIdentity).id.ToString());
                        }
                        else
                        {
                            methodArgs.Add(o.ToString());
                        }
                    }
                    OnInvokeMethodNetworkly.Invoke(PacketId.Command, this, networkInterface, methodName, methodArgs.ToArray());
                });
            }
        }

        public void InvokeCommandMethodNetworkly(string methodName, params object[] args)
        {
            InvokeCommandMethodNetworkly(methodName, NetworkInterface.TCP, args);
        }

        public void InvokeSyncVarNetworkly(string locationName, object value, NetworkInterface networkInterface = NetworkInterface.TCP)
        {
            if (locationByClass.TryGetValue(locationName, out NetworkLocationExecuter networkMemberExecuter))
            {
                networkMemberExecuter.InvokeMemberFromLocal(this, () =>
                {
                    if (value is NetworkIdentity)
                    {
                        value = ((value as NetworkIdentity).id.ToString());
                    }
                    else
                    {
                        value = value.ToString();
                    }
                    OnInvokeLocationNetworkly.Invoke(PacketId.SyncVar, this, networkInterface, locationName, value);
                });
            }
        }

        public void UpdateSyncVars()
        {
          
        }

        internal void PreformEvents()
        {
            lock (entities)
            {
                if (!entities.ContainsKey(id))
                {
                    entities.Add(id, this);
                }
                if (hasAuthority)
                {
                    OnHasLocalAuthorityInitializeEvent?.Invoke();
                }
                hasInitialized = true;
                OnNetworkInitializeEvent?.Invoke();
            }
        }

        public void Synchronize()
        {
            OnBeginSynchronization?.Invoke();
        }
        
        public void Destroy()
        {
            InvokeBroadcastMethodNetworkly(nameof(Destroy));
            OnDestroyEvent?.Invoke(this);
            entities.Remove(id);
        }

        public void SetAuthority(int newOwnerId)
        {
            InvokeBroadcastMethodNetworkly(nameof(SetAuthority), newOwnerId);
            if (newOwnerId == -1)
            {
                hasAuthority = isInServer;
                ownerId = NetworkBehavior.serverPort;
                isServerAuthority = true;
            }
            else
            {
                if (NetworkBehavior.GetNetworkIdentityById(newOwnerId).ownerId == newOwnerId)
                {
                    ownerId = newOwnerId;
                    hasAuthority = ownerId == id;
                    isServerAuthority = NetworkBehavior.serverPort == newOwnerId;
                }
                else
                {
                    throw new Exception("Invalid owner id was given");
                }
            }
        }

        internal static void NetworkSyncVarInvoke(NetworkIdentity identity, SyncVarPacket syncVarPacket)
        {
            if(identity.locationByClass.TryGetValue(syncVarPacket.LocationName, out NetworkLocationExecuter memberExecuter))
            {
                memberExecuter.InvokeMemberFromNetwork(identity, false, syncVarPacket.LocationValue);
            }
        }

        internal static void NetworkMethodInvoke(NetworkIdentity identity, MethodPacket methodPacket)
        {
            if (identity.methodsByClass.TryGetValue(methodPacket.MethodName, out NetworkMethodExecuter memberExecuter))
            {
                memberExecuter.InvokeMemberFromNetwork(identity, false, methodPacket.MethodArgs);
            }
        }

        public int CompareTo(NetworkIdentity other)
        {
            if(isUsedAsVar == other.isUsedAsVar)
            {
                return 0;
            }
            else if(isUsedAsVar)
            {
                return -1;
            }
            return 1;
        }
    }
}
