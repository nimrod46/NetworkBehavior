using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.ComponentModel.Design;
using PostSharp.Serialization;
using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using System.Reflection;
using System.Threading;

namespace Networking
{
    [AttributeUsage(AttributeTargets.Method,
                     AllowMultiple = true, Inherited = false)  // Multiuse attribute.  
]
    [PSerializable]
    public abstract class MethodNetworkAttribute : MethodInterceptionAspect
    {
        internal delegate void InvokeNetworklyEvent(PacketId packetID, NetworkIdentity networkIdentity, NetworkInterface networkInterface, string methodName, object[] methodArgs);
        internal static event InvokeNetworklyEvent OnInvokeMethodNetworkly;

        public NetworkInterface networkInterface = NetworkInterface.TCP;
        public bool shouldInvokeSynchronously = false;
        private PacketId packetId;
        private bool shouldInvokeFromLocaly;
        private NetworkMemberExecuter<MethodNetworkAttribute> networkExecuter;
        private MethodBase method;
        private string methodName;

        public MethodNetworkAttribute(PacketId packetId, bool shouldInvokeFromLocaly)
        {
            this.packetId = packetId;
            this.shouldInvokeFromLocaly = shouldInvokeFromLocaly;
        }

        public override sealed void OnInvoke(MethodInterceptionArgs args) 
        {
            NetworkIdentity networkIdentity = args.Instance as NetworkIdentity;
           
            InvokeMethod(networkIdentity, () => {

                base.OnInvoke(args);
            }, () =>
            { 
                List<object> methodArgs = new List<object>();
                foreach (object o in args.Arguments)
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
                OnInvokeMethodNetworkly.Invoke(packetId, networkIdentity, networkInterface, methodName, methodArgs.ToArray());
            });
        }

        protected virtual void InvokeMethod(NetworkIdentity networkIdentity, Action invokeLocally, Action invokeNetworkly)
        {
            networkExecuter.InvokeMember(networkIdentity, invokeLocally, invokeNetworkly);
        }

        public override void RuntimeInitialize(MethodBase method)
        {
            base.RuntimeInitialize(method);
            this.method = method;
            methodName = method.DeclaringType.Name + ":" + method.Name;
            networkExecuter = new NetworkMemberExecuter<MethodNetworkAttribute>(methodName, this, shouldInvokeSynchronously, shouldInvokeFromLocaly, true);
        }

        protected virtual void InvokeMethodFromNetwork(NetworkIdentity networkIdentity, object[] methodArgs)
        {
            int i = 0;
            foreach (ParameterInfo item in method.GetParameters())
            {
                var v = methodArgs[i];
                if (typeof(NetworkIdentity).IsAssignableFrom(item.ParameterType))
                {
                    v = NetworkIdentity.entities[int.Parse(v + "")];
                }
                else
                {
                    v = Convert.ChangeType(v, item.ParameterType);
                }
                methodArgs[i] = v;
                i++;
            }
            networkExecuter.InvokeMemberFromNetwork(() => method.Invoke(networkIdentity, methodArgs));
        }

        internal static void NetworkInvoke(NetworkIdentity networkIdentity, MethodPacket packet)
        {
            NetworkMemberExecuter<MethodNetworkAttribute>.GetNetworkAttributeMemberByMemberName(packet.MethodName).InvokeMethodFromNetwork(networkIdentity, packet.MethodArgs);
        }
    }
}
