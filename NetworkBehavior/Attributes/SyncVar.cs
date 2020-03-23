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

        internal delegate void networkingInvokeEvent(LocationInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity id);
        internal static event networkingInvokeEvent onNetworkingInvoke;
        private static Dictionary<string, VarInfo> fields = new Dictionary<string, VarInfo>();
        internal static Dictionary<string, MethodInfo> hooks = new Dictionary<string, MethodInfo>();
        public NetworkInterface networkInterface = NetworkInterface.TCP;
        public string hook = "";
        public bool invokeInServer = true;
        public bool shouldInvokeSynchronously = false;
        private PacketID packetID = PacketID.SyncVar;

        public override void OnSetValue(LocationInterceptionArgs args)
        {
            base.OnSetValue(args);

            if(!(args.Instance as NetworkIdentity).hasInitialized)
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
            }

            if (!(args.Instance as NetworkIdentity).hasAuthority)
            {
                return;
                throw new Exception("Cannot change sync var in an none authority identity");
            }

            if ((args == null || !args.Location.LocationType.IsValueType) && args.Location.LocationType.Name != "String")
            {
                throw new Exception("Arguments cannot be none value type");
            }
            onNetworkingInvoke?.Invoke(args, packetID, networkInterface, invokeInServer, args.Instance as NetworkIdentity);
        }

        public override void RuntimeInitialize(LocationInfo locationInfo)
        {
            base.RuntimeInitialize(locationInfo);
            if (fields.ContainsKey(locationInfo.Name))
            {
                throw new Exception("SyncVar: Duplicate fields name: " + locationInfo.Name);
            }
            fields.Add(locationInfo.Name, new VarInfo(locationInfo, shouldInvokeSynchronously));
            if (hook != "")
            {
                hooks.Add(locationInfo.Name, locationInfo.DeclaringType.GetMethod(hook, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
            }
        }

        internal static void networkInvoke(NetworkIdentity net, object[] args)
        {
            string fieldName = args[0].ToString();
            List<object> temp = args.ToList();
            temp.RemoveAt(0);
            args = temp.ToArray();
            if (!fields.TryGetValue(fieldName, out VarInfo field))
            {
                throw new Exception("SyncVar: no field name " + "\"" + fieldName + "\"" + " found.");
            }

            object newArg = Operations.getValueAsObject(field.LocationInfo.LocationType.Name, args[0]);
            if (!net.hasAuthority)
            {

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
                        field.LocationInfo.SetValue(net, newArg);
                    }
                }

            }

            if (hooks.TryGetValue(fieldName, out MethodInfo method))
            {
                if (method == null)
                {
                    throw new Exception("No hooked method: " + method.Name + " was found, please check the method name!");
                }

                if (field.ShouldInvokeSynchronously)
                {
                    lock (NetworkBehavior.synchronousActions)
                    {
                        NetworkBehavior.synchronousActions.Add(() => method.Invoke(net, null));
                    }
                }
                else
                {
                    method.Invoke(net, null);
                }
            }
        }
    }
}
