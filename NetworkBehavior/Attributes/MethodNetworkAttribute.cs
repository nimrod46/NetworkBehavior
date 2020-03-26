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
        private struct MethodInfo
        {
            public MethodBase Method { get; set; }
            public bool ShouldInvokeSynchronously { get; set; }

            public MethodInfo(MethodBase method, bool shouldInvokeSynchronously)
            {
                Method = method;
                ShouldInvokeSynchronously = shouldInvokeSynchronously;
            }
        }

        private static readonly Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();
        internal delegate void networkingInvokeEvent(MethodInterceptionArgs args, PacketId packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity networkIdentity);
        internal static event networkingInvokeEvent onNetworkingInvoke;
        public NetworkInterface networkInterface = NetworkInterface.TCP;
        public bool shouldInvokeInServer = true;
        public bool shouldInvokeSynchronously = false;
        protected bool shouldInvokeImmediatelyIfHasAuthority = true;
        private PacketId packetID;

        internal MethodNetworkAttribute(PacketId packetID)
        {
            this.packetID = packetID;
        }

        public override sealed void OnInvoke(MethodInterceptionArgs args) //TODO: Remove static usage and use as instance instead
        {
            if (!(args.Instance as NetworkIdentity).hasInitialized)
            {
                NetworkBehavior.PrintWarning("MethodNetworkAttribute was called on none initialized identity");
                return;
            }
            
            lock (NetworkIdentity.scope)
            {
                if (!NetworkIdentity.interrupt)
                {
                    NetworkIdentity.interrupt = true;
                    base.OnInvoke(args);
                    return;
                }
                else
                {
                    if (!(args.Instance as NetworkIdentity).hasAuthority && !(args.Instance as NetworkIdentity).isInServer)
                    {
                        NetworkBehavior.PrintWarning(args.Method.Name + " was called on none authority identity");
                        return;
                    }
                    else
                    {
                        onNetworkingInvoke?.Invoke(args, packetID, networkInterface, shouldInvokeInServer, args.Instance as NetworkIdentity);
                    }
                }
            }

            if (shouldInvokeImmediatelyIfHasAuthority)
            {
                base.OnInvoke(args);
            }
        }
    

        public override void RuntimeInitialize(MethodBase method)
        {
            base.RuntimeInitialize(method);
            if (methods.ContainsKey(method.ReflectedType.Name + method.Name))
            {
                throw new Exception("MethodNetworkAttribute: Duplicate method name: " + method.Name);
            }
            methods.Add(method.ReflectedType.Name + method.Name, new MethodInfo(method, shouldInvokeSynchronously));
        }

        internal static void NetworkInvoke(NetworkIdentity net, MethodPacket packet)
        {
            MethodInfo methodInfo;
            Type t = net.GetType();
            while (!methods.TryGetValue(t.Name + packet.MethodName, out methodInfo))
            {
                t = t.BaseType;
            }

            int i = 0;
            foreach (ParameterInfo item in methodInfo.Method.GetParameters())
            {
                var v = packet.MethodArgs[i];
                if (typeof(NetworkIdentity).IsAssignableFrom(item.ParameterType))
                {
                    v = NetworkIdentity.entities[int.Parse(v + "")];
                }
                else
                {
                    v = Convert.ChangeType(v, methodInfo.Method.GetParameters().ToArray()[i].ParameterType);
                }
                packet.MethodArgs[i] = v;
                i++;
            }

            if (methodInfo.ShouldInvokeSynchronously)
            {
                lock (NetworkBehavior.synchronousActions)
                {
                    NetworkBehavior.synchronousActions.Add(() =>
                    {
                        lock (NetworkIdentity.scope)
                        {
                            NetworkIdentity.interrupt = false;
                            methodInfo.Method.Invoke(net, packet.MethodArgs.ToArray());
                        }
                    });
                }
            }
            else
            {
                lock (NetworkIdentity.scope)
                {
                    NetworkIdentity.interrupt = false;
                    methodInfo.Method.Invoke(net, packet.MethodArgs.ToArray());
                }
            }

        }
    }
}
