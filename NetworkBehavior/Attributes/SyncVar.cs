using PostSharp.Aspects;
using PostSharp.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PostSharp.Reflection;
using System.Reflection;
using System.Threading;

namespace Networking
{

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
                      AllowMultiple = true, Inherited = false)
 ]
    [PSerializable]
    public class SyncVar : LocationInterceptionAspect
    {
        private struct VarInfo
        {
            public LocationInfo LocationInfo { get; set; }
            public bool ShouldInvokeSynchronously { get; set; }

            public VarInfo(LocationInfo locationInfo, bool shouldInvokeSynchronously)
            {
                LocationInfo = locationInfo;
                ShouldInvokeSynchronously = shouldInvokeSynchronously;
            }
        }

        internal delegate void networkingInvokeEvent(LocationInterceptionArgs args, PacketId packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity id);
        internal static event networkingInvokeEvent onNetworkingInvoke;
        private static Dictionary<string, VarInfo> fields = new Dictionary<string, VarInfo>();
        public NetworkInterface networkInterface = NetworkInterface.TCP;
        public string hook = "";
        public bool isDisabled = false;
        public bool invokeInServer = true;
        public bool shouldInvokeSynchronously = false;
        private PacketId packetID = PacketId.SyncVar;
        private MethodInfo hookedMethod;

        public override void OnSetValue(LocationInterceptionArgs args)
        {
            base.OnSetValue(args);

            hookedMethod?.Invoke(args.Instance as NetworkIdentity, null);

            if (isDisabled)
            {
                return;
            }

            lock (NetworkIdentity.scope)
            {
                if (!NetworkIdentity.interrupt)
                {
                    NetworkIdentity.interrupt = true;
                    return;
                }
                else
                {
                    if (!(args.Instance as NetworkIdentity).hasAuthority && !(args.Instance as NetworkIdentity).isInServer)
                    {
                    }
                    else
                    {
                        if ((args.Instance as NetworkIdentity).hasInitialized)
                        {
                            onNetworkingInvoke?.Invoke(args, packetID, networkInterface, invokeInServer, args.Instance as NetworkIdentity);
                        }
                    }
                }
            }

        }

        public override void RuntimeInitialize(LocationInfo locationInfo)
        {
            base.RuntimeInitialize(locationInfo);
            if (fields.ContainsKey(locationInfo.DeclaringType.Name + locationInfo.Name))
            {
                throw new Exception("SyncVar: Duplicate fields name: " + locationInfo.Name);
            }
            fields.Add(locationInfo.DeclaringType.Name + locationInfo.Name, new VarInfo(locationInfo, shouldInvokeSynchronously));
            if (typeof(NetworkIdentity).IsAssignableFrom(locationInfo.LocationType))
            {
                NetworkIdentity.prioritiesIdintities.Add(locationInfo.LocationType);
            }
            if (hook != "")
            {
                hookedMethod = locationInfo.DeclaringType.GetMethod(hook, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            }
        }

        internal static void NetworkInvoke(NetworkIdentity net, SyncVarPacket packet)//TODO: Remove static usage and use as instance instead
        {
            VarInfo field;
            Type t = net.GetType();
            while (!fields.TryGetValue(t.Name + packet.LocationName, out field))
            {
                t = t.BaseType;
            }

            object newArg = Operations.getValueAsObject(field.LocationInfo.LocationType.Name, packet.LocationValue);//TODO: Remove and use Convert instead

            if (field.ShouldInvokeSynchronously)
            {
                lock (NetworkBehavior.synchronousActions)
                {
                    NetworkBehavior.synchronousActions.Add(() =>
                    {
                        lock (NetworkIdentity.scope)
                        {
                            NetworkIdentity.interrupt = false;
                            field.LocationInfo.SetValue(net, newArg);
                        }
                    });
                }
            }
            else
            {
                lock (NetworkIdentity.scope)
                {
                    NetworkIdentity.interrupt = false;
                    field.LocationInfo.SetValue(net, newArg);
                }
            }
        }
    }
}
