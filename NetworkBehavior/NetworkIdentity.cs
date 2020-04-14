using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static NetworkingLib.Server;

namespace Networking
{
    
    public class NetworkIdentity : IComparable<NetworkIdentity>
    {
        
        public struct IdentityId : IComparable
        {
            public long Id { get; set; }

            private IdentityId(long id)
            {
                Id = id;
            }

            public static bool operator ==(IdentityId i1, IdentityId i2)
            {
                return i1.Equals(i2);
            }

            public static bool operator !=(IdentityId i1, IdentityId i2)
            {
                return !i1.Equals(i2);
            }

            public static IdentityId operator ++(IdentityId i)
            {
                i.Id++;
                return i;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }

            public static IdentityId FromLong(long id)
            {
                return new IdentityId(id);
            }

            internal static IdentityId ZeroIdentityId = new IdentityId(0);

            internal static IdentityId InvalidIdentityId = new IdentityId(0);

            public override bool Equals(object obj)
            {
                return obj is IdentityId id &&
                       Id == id.Id;
            }

            public int CompareTo(object obj)
            {
                if (obj is IdentityId id)
                {
                    return Id.CompareTo(id.Id);
                }
                return 0;
            }

            public override string ToString()
            {
                return Id.ToString();
            }
        }

        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        internal delegate void InvokeBrodcastMethodNetworklyEvent(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string methodName, object[] methodArgs);
        internal static event InvokeBrodcastMethodNetworklyEvent OnInvokeBrodcastMethodMethodNetworkly;
        internal delegate void InvokeCommandMethodNetworklyEvent(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string methodName, object[] methodArgs, EndPointId? targetId = null);
        internal static event InvokeCommandMethodNetworklyEvent OnInvokeCommandMethodNetworkly;
        internal delegate void InvokeLocationNetworklyEvent(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string locationName, object locationValue);
        internal static event InvokeLocationNetworklyEvent OnInvokeLocationNetworkly;
        
        public static SortedList<IdentityId, NetworkIdentity> entities = new SortedList<IdentityId, NetworkIdentity>();
        internal static List<Type> prioritiesIdintities = new List<Type>();
        internal static readonly char packetSpiltter = '¥';
        internal static readonly char argsSplitter = '|';
        internal static IdentityId lastId = IdentityId.ZeroIdentityId;

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

        public EndPointId OwnerId { get; set; }
        public IdentityId Id { get; set; }

        public NetworkBehavior NetworkBehavior;
        public bool isServerAuthority = false;
        public bool hasAuthority = false;
        public bool isInServer = false;
        public bool hasInitialized = false;
        public bool hasFieldsBeenInitialized = false;
      
        internal bool isUsedAsVar;

        public NetworkIdentity()
        {
            if (!NetworkBehavior.classes.ContainsKey(GetType().FullName))
            {
                NetworkBehavior.classes.Add(GetType().FullName, GetType());
            }
            foreach (MethodBase method in GetType().GetMethods(bindingFlags))
            {
                try
                {
                    methodsByClass.Add(method.Name + ":" + method.GetParameters().Length, new NetworkMethodExecuter(method));
                }
                catch (Exception)
                {
                    //NetworkBehavior.PrintWarning("method overload named: " + method.Name);
                }
            }

            foreach (MemberInfo member in GetSyncVars())
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
                    if (!prioritiesIdintities.Contains(location.LocationType)) {
                        prioritiesIdintities.Add(location.LocationType);
                    }
                }
                locationByClass.Add(member.Name, new NetworkLocationExecuter(location));
            }
            isUsedAsVar = prioritiesIdintities.Any(t => t.IsAssignableFrom(GetType()));
        }

        public void InvokeBroadcastMethodNetworkly(string methodName, NetworkInterfaceType networkInterface = NetworkInterfaceType.TCP, params object[] args)
        {
            methodName = methodName + ":" + args.Length;
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
                            methodArgs.Add((o as NetworkIdentity).Id.ToString());
                        }
                        else
                        {
                            methodArgs.Add(o.ToString());
                        }
                    }
                    OnInvokeBrodcastMethodMethodNetworkly.Invoke(this, networkInterface, methodName, methodArgs.ToArray());
                });
            }
            else
            {
                NetworkBehavior.PrintWarning("No method with name: {0} was not found", methodName);
            }
        }

        public void InvokeBroadcastMethodNetworkly(string methodName, params object[] args)
        {
            InvokeBroadcastMethodNetworkly(methodName, NetworkInterfaceType.TCP, args);
        }

        public void InvokeCommandMethodNetworkly(string methodName, NetworkInterfaceType networkInterface = NetworkInterfaceType.TCP, EndPointId? targetId = null, params object[] args)
        {
            methodName = methodName + ":" + args.Length;
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
                            methodArgs.Add((o as NetworkIdentity).Id.ToString());
                        }
                        else
                        {
                            methodArgs.Add(o.ToString());
                        }
                    }
                    OnInvokeCommandMethodNetworkly.Invoke(this, networkInterface, methodName, methodArgs.ToArray(), targetId);
                });
            }
            else
            {
                NetworkBehavior.PrintWarning("No method with name: {0} was not found", methodName);
            }
        }

        public void InvokeCommandMethodNetworkly(string methodName, EndPointId? targetId, params object[] args)
        {
            InvokeCommandMethodNetworkly(methodName, NetworkInterfaceType.TCP, targetId, args);
        }

        public void InvokeCommandMethodNetworkly(string methodName, params object[] args)
        {
            InvokeCommandMethodNetworkly(methodName, NetworkInterfaceType.TCP, null, args);
        }

        public void InvokeCommandMethodNetworkly(string methodName, NetworkInterfaceType networkInterface, params object[] args)
        {
            InvokeCommandMethodNetworkly(methodName, networkInterface, null, args);
        }

        public void InvokeSyncVarNetworkly(string locationName, object value, NetworkInterfaceType networkInterface = NetworkInterfaceType.TCP)
        {
            if (locationByClass.TryGetValue(locationName, out NetworkLocationExecuter networkMemberExecuter))
            {
                networkMemberExecuter.InvokeMemberFromLocal(this, () =>
                {
                    if (value is NetworkIdentity)
                    {
                        value = ((value as NetworkIdentity).Id.ToString());
                    }
                    else
                    {
                        value = value.ToString();
                    }
                    OnInvokeLocationNetworkly.Invoke(this, networkInterface, locationName, value);
                });
            }
            else
            {
                NetworkBehavior.PrintWarning("No location with name: {0} was not found", locationName);
            }
        }

        public void UpdateSyncVars()
        {
          
        }

        internal void PreformEvents()
        {
            lock (entities)
            {
                if (!entities.ContainsKey(Id))
                {
                    entities.Add(Id, this);
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
            entities.Remove(Id);
        }

        //public void SetAuthority(EndPointId newOwnerId)
        //{
        //    InvokeBroadcastMethodNetworkly(nameof(SetAuthority), newOwnerId);
        //    if (newOwnerId == EndPointId.InvalidIdentityId)
        //    {
        //        hasAuthority = isInServer;
        //        OwnerId = NetworkBehavior.serverEndPointId;
        //        isServerAuthority = true;
        //    }
        //    else
        //    {
        //        if (NetworkBehavior.GetNetworkIdentityById(newOwnerId).OwnerId == newOwnerId)
        //        {
        //            OwnerId = newOwnerId;
        //            hasAuthority = OwnerId == Id;
        //            isServerAuthority = NetworkBehavior.serverEndPointId == newOwnerId;
        //        }
        //        else
        //        {
        //            throw new Exception("Invalid owner id was given");
        //        }
        //    }
        //}

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
            else
            {
                NetworkBehavior.PrintWarning("No location with name: {0} was not found", methodPacket.MethodName);
            }
        }

        internal List<MemberInfo> GetSyncVars()
        {
            return GetType().GetFields(bindingFlags).Cast<MemberInfo>().Concat(GetType().GetProperties(bindingFlags)).Where(prop => prop.Name.Length >= 5 && prop.Name.ToLower().Substring(0, 4).Equals("sync")).ToList();
        }

        public int CompareTo(NetworkIdentity other)
        {
            if(other.GetType() == GetType())
            {
                return 0;
            }

            if(!isUsedAsVar)
            {
                return 1;
            }

            if (isUsedAsVar == other.isUsedAsVar)
            {
                return other.locationByClass.Values.Any(l => GetType().IsAssignableFrom(l.Location.GetType())) ? -1 : 1;
            }
            return -1;
        }
    }
}
