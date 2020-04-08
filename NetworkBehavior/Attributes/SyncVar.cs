//using PostSharp.Aspects;
//using PostSharp.Serialization;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using PostSharp.Reflection;
//using System.Reflection;
//using System.Threading;

//namespace Networking
//{

//    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property,
//                      AllowMultiple = true, Inherited = false)
// ]
//    [PSerializable]
//    public class SyncVar : LocationInterceptionAspect
//    {
//        internal delegate void InvokeNetworklyEvent(PacketId packetID, NetworkIdentity networkIdentity, NetworkInterface networkInterface, string locationName, object locationValue);
//        internal static event InvokeNetworklyEvent OnInvokeLocationNetworkly;

//        public NetworkInterface networkInterface = NetworkInterface.TCP;
//        public bool shouldInvokeSynchronously = false;
//        public string hook = "";
//        public bool shouldInvokeNetworkly = true;
//        private PacketId packetId = PacketId.SyncVar;
//        private NetworkMemberExecuter<SyncVar> networkExecuter;
//        private LocationInfo locationInfo;
//        private string locationName;
//        private MethodInfo hookedMethod;

//        public SyncVar()
//        {
//        }

//        public override sealed void OnSetValue(LocationInterceptionArgs args)
//        {
//            NetworkIdentity networkIdentity = args.Instance as NetworkIdentity;
//            InvokeLocation(networkIdentity, () =>
//            { 
//                base.OnSetValue(args);
//                hookedMethod?.Invoke(networkIdentity, null);
//            }, () => {
//                object locationValue;
//                if (args.Value is NetworkIdentity)
//                {
//                    locationValue = ((args.Value as NetworkIdentity).id.ToString());
//                }
//                else
//                {
//                    locationValue = args.Value.ToString();
//                }
//                OnInvokeLocationNetworkly.Invoke(packetId, networkIdentity, networkInterface, locationName, locationValue);
//            });
//        }

//        protected virtual void InvokeLocation(NetworkIdentity networkIdentity, Action invokeLocally, Action invokeNetworkly)
//        {
//            networkExecuter.InvokeMember(networkIdentity, invokeLocally, invokeNetworkly);
//        }

//        public override void RuntimeInitialize(LocationInfo locationInfo)
//        {
//            base.RuntimeInitialize(locationInfo);
//            this.locationInfo = locationInfo;
//            locationName = locationInfo.DeclaringType.Name + ":" + locationInfo.Name;
//            networkExecuter = new NetworkMemberExecuter<SyncVar>(locationName, this, shouldInvokeSynchronously, true, shouldInvokeNetworkly);
//            if (typeof(NetworkIdentity).IsAssignableFrom(locationInfo.LocationType))
//            {
//                NetworkIdentity.prioritiesIdintities.Add(locationInfo.LocationType);
//            }
//            if (hook != "")
//            {
//                hookedMethod = locationInfo.DeclaringType.GetMethod(hook, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
//            }
//        }

//        protected virtual void InvokeLocationFromNetwork(NetworkIdentity networkIdentity, object licationValue)
//        {
//            var v = licationValue;
//            if (typeof(NetworkIdentity).IsAssignableFrom(locationInfo.LocationType))
//            {
//                v = NetworkIdentity.entities[int.Parse(v + "")];
//            }
//            else
//            {
//                v = Convert.ChangeType(v, locationInfo.LocationType);
//            }
//            networkExecuter.InvokeMemberFromNetwork(() => locationInfo.SetValue(networkIdentity, v));
//        }

//        internal static void NetworkInvoke(NetworkIdentity networkIdentity, SyncVarPacket packet)
//        {
//            NetworkMemberExecuter<SyncVar>.GetNetworkAttributeMemberByMemberName(packet.LocationName).InvokeLocationFromNetwork(networkIdentity, packet.LocationValue);
//        }
//    }
//}
