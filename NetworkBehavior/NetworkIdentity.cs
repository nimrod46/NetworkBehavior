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

        internal delegate void InvokeBrodcastMethodNetworklyEvent(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string methodName, object[] methodArgs, bool? shouldInvokeSynchronously = null);
        internal static event InvokeBrodcastMethodNetworklyEvent OnInvokeBrodcastMethodMethodNetworkly;
        internal delegate void InvokeCommandMethodNetworklyEvent(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string methodNam, object[] methodArgs, bool? shouldInvokeSynchronously = null, EndPointId? targetId = null);
        internal static event InvokeCommandMethodNetworklyEvent OnInvokeCommandMethodNetworkly;
        internal delegate void InvokeLocationNetworklyEvent(NetworkIdentity networkIdentity, NetworkInterfaceType networkInterface, string locationName, object locationValue, bool? shouldInvokeSynchronously = null);
        internal static event InvokeLocationNetworklyEvent OnInvokeLocationNetworkly;
        
        public static SortedList<IdentityId, NetworkIdentity> entities = new SortedList<IdentityId, NetworkIdentity>();
        internal static Dictionary<Type, Dictionary<string, NetworkMethodExecuter>> methodsByType = new Dictionary<Type, Dictionary<string, NetworkMethodExecuter>>();
        internal static Dictionary<Type, Dictionary<string, NetworkLocationExecuter>> locationByType = new Dictionary<Type, Dictionary<string, NetworkLocationExecuter>>();
        internal static List<Type> prioritiesIdintities = new List<Type>();
        internal static readonly char packetSpiltter = '¥';
        internal static readonly char argsSplitter = '|';
        internal static IdentityId lastId = IdentityId.ZeroIdentityId;

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
        public bool IsDestroyed { get; set; }

        public bool isServerAuthority = false;
        public bool hasAuthority = false;
        public bool isInServer = false;
        public bool hasInitialized = false;
        public bool hasFieldsBeenInitialized = false;
        internal bool isUsedAsVar;

        public NetworkIdentity()
        {
            IsDestroyed = false;
            if (!NetworkBehavior.classes.ContainsKey(GetType().FullName))
            {
                NetworkBehavior.classes.Add(GetType().FullName, GetType());
            }

            RegisterMethodsAndLocations();

            isUsedAsVar = prioritiesIdintities.Any(t => t.IsAssignableFrom(GetType()));

        }

        public void InvokeBroadcastMethodNetworkly(string methodName, NetworkInterfaceType networkInterface = NetworkInterfaceType.TCP, bool? shouldInvokeSynchronously = null, params object[] args)
        {
            if (methodsByType.TryGetValue(GetType(), out Dictionary<string, NetworkMethodExecuter> d))
            {
                methodName = methodName + ":" + args.Length;
                if (d.TryGetValue(methodName, out NetworkMethodExecuter networkMemberExecuter))
                {
                    networkMemberExecuter.InvokeMemberFromLocal(this, () =>
                    {
                        List<object> methodArgs = new List<object>();
                        foreach (object o in args)
                        {
                            methodArgs.Add(Operations.GetObjectAsValue(o));

                        }
                        OnInvokeBrodcastMethodMethodNetworkly.Invoke(this, networkInterface, methodName, methodArgs.ToArray(), shouldInvokeSynchronously);
                    });
                    return;
                }
            }
            NetworkBehavior.PrintWarning("No method with name: {0} was not found", methodName);
        }

        public void InvokeBroadcastMethodNetworkly(string methodName, params object[] args)
        {
            InvokeBroadcastMethodNetworkly(methodName, NetworkInterfaceType.TCP, null, args);
        }

        public void InvokeBroadcastMethodNetworkly(string methodName, bool shouldInvokeSynchronously, params object[] args)
        {
            InvokeBroadcastMethodNetworkly(methodName, NetworkInterfaceType.TCP, shouldInvokeSynchronously, args);
        }

        public void InvokeCommandMethodNetworkly(string methodName, NetworkInterfaceType networkInterface = NetworkInterfaceType.TCP, bool? shouldInvokeSynchronously = null, EndPointId? targetId = null, params object[] args)
        {
            if (methodsByType.TryGetValue(GetType(), out Dictionary<string, NetworkMethodExecuter> d))
            {
                methodName = methodName + ":" + args.Length;
                if (d.TryGetValue(methodName, out NetworkMethodExecuter networkMemberExecuter))
                {
                    networkMemberExecuter.InvokeMemberFromLocal(this, () =>
                    {
                        List<object> methodArgs = new List<object>();
                        foreach (object o in args)
                        {
                            methodArgs.Add(Operations.GetObjectAsValue(o));
                        }
                        OnInvokeCommandMethodNetworkly.Invoke(this, networkInterface, methodName, methodArgs.ToArray(), shouldInvokeSynchronously, targetId);
                    });
                    return;
                }
            }
            NetworkBehavior.PrintWarning("No method with name: {0} was not found", methodName);
        }

        public void InvokeCommandMethodNetworkly(string methodName, EndPointId targetId, params object[] args)
        {
            InvokeCommandMethodNetworkly(methodName, NetworkInterfaceType.TCP, null, targetId, args);
        }

        public void InvokeCommandMethodNetworkly(string methodName, params object[] args)
        {
            InvokeCommandMethodNetworkly(methodName, NetworkInterfaceType.TCP, args);
        }

        public void InvokeCommandMethodNetworkly(string methodName, NetworkInterfaceType networkInterface, params object[] args)
        {
            InvokeCommandMethodNetworkly(methodName, networkInterface, null, null, args);
        }

        public void InvokeSyncVarNetworkly(string locationName, object value, NetworkInterfaceType networkInterface = NetworkInterfaceType.TCP)
        {
            if (locationByType.TryGetValue(GetType(), out Dictionary<string, NetworkLocationExecuter> d))
            {
                if (d.TryGetValue(locationName, out NetworkLocationExecuter networkMemberExecuter))
                {
                    networkMemberExecuter.InvokeMemberFromLocal(this, () =>
                    {
                        OnInvokeLocationNetworkly.Invoke(this, networkInterface, locationName, Operations.GetObjectAsValue(value));
                    });
                    return;
                }
            }
            NetworkBehavior.PrintWarning("No location with name: {0} was not found", locationName);
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
            if (!IsDestroyed)
            {
                IsDestroyed = true;
                OnDestroyEvent?.Invoke(this);
            }
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

        internal static void NetworkSyncVarInvoke(NetworkIdentity identity, SyncVarPacket syncVarPacket, bool shouldInvokeSynchronously)
        {
            if (locationByType.TryGetValue(identity.GetType(), out Dictionary<string, NetworkLocationExecuter> d))
            {
                if (d.TryGetValue(syncVarPacket.LocationName, out NetworkLocationExecuter memberExecuter))
                {
                    memberExecuter.InvokeMemberFromNetwork(identity, shouldInvokeSynchronously, syncVarPacket.LocationValue);
                    return;
                }
            }
            NetworkBehavior.PrintWarning("No location with name: {0} was not found", syncVarPacket.LocationName);
        }

        internal static void NetworkMethodInvoke(NetworkIdentity identity, MethodPacket methodPacket, bool shouldInvokeSynchronously)
        {
            if (methodsByType.TryGetValue(identity.GetType(), out Dictionary<string, NetworkMethodExecuter> d))
            {
                if (d.TryGetValue(methodPacket.MethodName, out NetworkMethodExecuter memberExecuter))
                {
                    memberExecuter.InvokeMemberFromNetwork(identity, shouldInvokeSynchronously, methodPacket.MethodArgs);
                    return;
                }
            }
            NetworkBehavior.PrintWarning("No location with name: {0} was not found", methodPacket.MethodName);
        }

    internal List<MemberInfo> GetSyncVars()
        {
            return GetType().GetFields(bindingFlags).Cast<MemberInfo>().Concat(GetType().GetProperties(bindingFlags)).Where(prop => prop.Name.Length >= 5 && prop.Name.ToLower().Substring(0, 4).Equals("sync")).ToList();
        }

        internal static NetworkIdentity GetNetworkIdentityById(IdentityId identityId)
        {
            if (!NetworkIdentity.entities.TryGetValue(identityId, out NetworkIdentity identity))
            {
                return null;
            }
            return identity;
        }

        internal static NetworkIdentity GetNetworkIdentityById(object identityId)
        {
            return GetNetworkIdentityById(IdentityId.FromLong(long.Parse(identityId.ToString())));
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
                return locationByType[other.GetType()].Values.Any(l => GetType().IsAssignableFrom(l.Location.GetType())) ? -1 : 1;
            }
            return -1;
        }

        private void RegisterMethodsAndLocations()
        {
            if (!methodsByType.ContainsKey(GetType()))
            {
                Dictionary<string, NetworkMethodExecuter> methodsByName = new Dictionary<string, NetworkMethodExecuter>();
                foreach (MethodBase method in GetType().GetMethods(bindingFlags))
                {
                    try
                    {
                        methodsByName.Add(method.Name + ":" + method.GetParameters().Length, new NetworkMethodExecuter(method));
                    }
                    catch (Exception)
                    {
                        // NetworkBehavior.PrintWarning("method overload named: " + method.Name);
                    }
                }
                methodsByType.Add(GetType(), methodsByName);
            }

            if (!locationByType.ContainsKey(GetType()))
            {
                Dictionary<string, NetworkLocationExecuter> locationsByName = new Dictionary<string, NetworkLocationExecuter>();
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
                        if (!prioritiesIdintities.Contains(location.LocationType))
                        {
                            prioritiesIdintities.Add(location.LocationType);
                        }
                    }
                    locationsByName.Add(member.Name, new NetworkLocationExecuter(location));
                }
                locationByType.Add(GetType(), locationsByName);
            }
        }
    }
}
