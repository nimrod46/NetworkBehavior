using PostSharp.Aspects;
using PostSharp.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PostSharp.Reflection;
using System.Reflection;

namespace Networking
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
                      AllowMultiple = true, Inherited = false)  // Multiuse attribute.  
 ]
    [PSerializable]
    public class SyncVar : LocationInterceptionAspect
    {
        internal delegate void networkingInvokeEvent(LocationInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, int id);
        internal static event networkingInvokeEvent onNetworkingInvoke;
        internal static Dictionary<string, LocationInfo> fields = new Dictionary<string, LocationInfo>();
        internal static Dictionary<string, string> hooks = new Dictionary<string, string>();
        public NetworkInterface networkInterface = NetworkInterface.TCP;
        public string hook = "";
        private PacketID packetID = PacketID.SyncVar;

        public override void OnSetValue(LocationInterceptionArgs args)
        {
            base.OnSetValue(args);
            lock (NetworkIdentity.scope)
            {
                if (!NetworkIdentity.interrupt)
                {
                    NetworkIdentity.interrupt = true;
                    return;
                }
            }

            if ((args == null || !args.Location.LocationType.IsValueType) && args.Location.LocationType.Name != "String")
            {
                throw new Exception("Arguments cannot be none value type");
            }
            onNetworkingInvoke?.Invoke(args, packetID, networkInterface, (args.Instance as NetworkIdentity).id);
        }

        public override void RuntimeInitialize(LocationInfo locationInfo)
        {
            base.RuntimeInitialize(locationInfo);
            if (fields.ContainsKey(locationInfo.Name))
            {
                throw new Exception("SyncVar: Duplicate fields name: " + locationInfo.Name);
            }
            fields.Add(locationInfo.Name, locationInfo);
            if (hook != "")
            {
                hooks.Add(locationInfo.Name, hook);
            }
        }

        internal static void networkInvoke(NetworkIdentity net, object[] args)
        {
            LocationInfo field;
            string fieldName = args[0].ToString();
            List<object> temp = args.ToList();
            temp.RemoveAt(0);
            args = temp.ToArray();
            if (!fields.TryGetValue(fieldName, out field))
            {
                throw new Exception("SyncVar: no field name " + "\"" + fieldName + "\"" + " found.");
            }

            //  try
            // {
            object newArg = Operations.getValueAsObject(field.LocationType.Name, args[0]);

            lock (NetworkIdentity.scope)
            {
                NetworkIdentity.interrupt = false;
                field.SetValue(net, newArg);
            }
            string hookMethod;
            if (hooks.TryGetValue(fieldName, out hookMethod))
            {
                MethodInfo method = net.GetType().GetMethod(hookMethod);
                method.Invoke(net, null);
            }
            //  }
            // catch (Exception e)
            // {

            // throw e;
            // }
        }
    }
}
