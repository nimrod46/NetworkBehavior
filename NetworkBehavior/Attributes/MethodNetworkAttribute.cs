using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.ComponentModel.Design;
using System.Web.Mvc;
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
        internal delegate void networkingInvokeEvent(MethodInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, bool invokeInServer, bool haveBeenInvokedInAuthority, NetworkIdentity networkIdentity);
        internal static event networkingInvokeEvent onNetworkingInvoke;
        public NetworkInterface networkInterface = NetworkInterface.TCP;
        public bool shoudlInvokeOnNoneAuthority = true;
        public bool shouldInvokeInServer = true;
        public bool shouldInvokeSynchronously = false;
        protected bool shouldInvokeImmediatelyIfHasAuthority = true;
        private PacketID packetID;

        internal MethodNetworkAttribute(PacketID packetID)
        {
            this.packetID = packetID;
        }

        public override sealed void OnInvoke(MethodInterceptionArgs args)
        {
            if (!shoudlInvokeOnNoneAuthority && !((args.Instance as NetworkIdentity).hasAuthority && !(args.Instance as NetworkIdentity).isInServer))
            {
                base.OnInvoke(args);
                return;
            }
            bool shouldInvokeImmediately = shouldInvokeImmediatelyIfHasAuthority && (args.Instance as NetworkIdentity).hasAuthority;

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
                    if(shouldInvokeImmediately)
                    {
                        base.OnInvoke(args);
                    }
                }
            }
            onNetworkingInvoke?.Invoke(args, packetID, networkInterface, shouldInvokeInServer, shouldInvokeImmediately, args.Instance as NetworkIdentity);
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

        internal static void networkInvoke(NetworkIdentity net, object[] args)
        {
            MethodInfo methodInfo;
            string methodName = args[0].ToString();
            List<object> temp = args.ToList();
            temp.RemoveAt(0);
            temp.RemoveAt(temp.Count - 1);// remove shouldInvokeInServer
            temp.RemoveAt(temp.Count - 1);//remove shouldInvokeSynchronously
            args = temp.ToArray();
            Type t = net.GetType();
            while (!methods.TryGetValue(t.Name + methodName, out methodInfo))
            {
                t = t.BaseType;
            }
            
            object[] newArgs = null;
            if (args.Length != 0 && (string)args[0] != "")
            {
                newArgs = new object[args.Length];
                int i = 0;
                foreach (ParameterInfo item in methodInfo.Method.GetParameters())
                {
                    newArgs[i] = Operations.getValueAsObject(item.ParameterType.Name, args[i]);
                    i++;
                }
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
                            methodInfo.Method.Invoke(net, newArgs);
                        }
                    });
                }
            }
            else
            {
                lock (NetworkIdentity.scope)
                {
                    NetworkIdentity.interrupt = false;
                    methodInfo.Method.Invoke(net, newArgs);
                }
            }

        }
    }
}
