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

namespace Networking
{
    [AttributeUsage(AttributeTargets.Method,
                     AllowMultiple = true, Inherited = false)  // Multiuse attribute.  
]
    [PSerializable]
    public abstract class MethodNetworkAttribute : MethodInterceptionAspect
    {
        public NetworkInterface networkInterface = NetworkInterface.TCP;
        public bool networkInvokeOnNoneAuthority = true;
        public bool invokeInServer = true;
        internal delegate void networkingInvokeEvent(MethodInterceptionArgs args, PacketID packetID, NetworkInterface networkInterface, bool invokeInServer, NetworkIdentity networkIdentity);
        internal static event networkingInvokeEvent onNetworkingInvoke;
        internal static Dictionary<string, MethodBase> methods = new Dictionary<string, MethodBase>();
        internal PacketID packetID;
        internal MethodNetworkAttribute(PacketID packetID)
        {
            this.packetID = packetID;
        }

        public override sealed void OnInvoke(MethodInterceptionArgs args)
        {
           if (!args.Instance.GetType().Equals(args.Method.DeclaringType) && (args.Method.Attributes & MethodAttributes.NewSlot) != 0)
            {
                base.OnInvoke(args);
               // Console.WriteLine("Net method execute through base: " +args.Method.Name);
                return;
            }

            if (!networkInvokeOnNoneAuthority && !((args.Instance as NetworkIdentity).hasAuthority && !(args.Instance as NetworkIdentity).isInServer))
            {
                base.OnInvoke(args);
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
            }
            /*
            foreach (object o in args.Arguments)
            {
                if ((o == null || !o.GetType().IsValueType) && o?.GetType().Name != "String")
                {
                    throw new Exception("Arguments cannot be none value type");
                }
            }
            */
            onNetworkingInvoke?.Invoke(args, packetID, networkInterface, invokeInServer, args.Instance as NetworkIdentity);
        }

        public override void RuntimeInitialize(MethodBase method)
        {
            base.RuntimeInitialize(method);
            if (methods.ContainsKey(method.ReflectedType.Name + method.Name))
            {
                throw new Exception("MethodNetworkAttribute: Duplicate method name: " + method.Name);
            }
            methods.Add(method.ReflectedType.Name + method.Name, method);
        }

        internal static void networkInvoke(NetworkIdentity net, object[] args)
        {
            MethodBase method;
            string methodName = args[0].ToString();
            List<object> temp = args.ToList();
            temp.RemoveAt(0);
            temp.RemoveAt(temp.Count - 1);
            args = temp.ToArray();
            Type t = net.GetType();
            while (!methods.TryGetValue(t.Name + methodName, out method))
            {
                t = t.BaseType;
            }
           
            object[] newArgs = null;
            if (args.Length != 0 && (string)args[0] != "")
            {
                newArgs = new object[args.Length];
                int i = 0;
                foreach (ParameterInfo item in method.GetParameters())
                {
                    newArgs[i] = Operations.getValueAsObject(item.ParameterType.Name, args[i]);
                    i++;
                }
            }

            lock (NetworkIdentity.scope)
            {
                NetworkIdentity.interrupt = false;
                method.Invoke(net, newArgs);
            }
        }
    }
}
