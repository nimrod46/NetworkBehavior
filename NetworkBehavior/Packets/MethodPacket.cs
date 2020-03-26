using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal abstract class MethodPacket : NetworkIdentityBasePacket
    {
        
        public string MethodName { get; private set; }
        public int MethodArgsCount { get; private set; }
        public List<object> MethodArgs { get; private set; } = new List<object>();
        public bool ShouldInvokeInServer { get; private set; }

        public MethodPacket(PacketId packetId, int id, MethodInterceptionArgs methodArgs, bool shouldInvokeInServer) : base(packetId, id)
        {
            MethodName = methodArgs.Method.Name;
            if (methodArgs.Arguments.Count != 0)
            {
                foreach (object o in methodArgs.Arguments)
                {
                    if (o == null)
                    {
                       MethodArgs.Add("null");
                    }
                    else if (o is NetworkIdentity)
                    {
                        MethodArgs.Add((o as NetworkIdentity).id.ToString());
                    }
                    else
                    {
                        MethodArgs.Add(o.ToString());
                    }
                }
            }
            MethodArgsCount = MethodArgs.Count;
            ShouldInvokeInServer = shouldInvokeInServer;
            Data.Add(MethodName); 
            Data.Add(MethodArgsCount);
            if (MethodArgs != null) Data.AddRange(MethodArgs) ;
            Data.Add(ShouldInvokeInServer);
        }

        public MethodPacket(PacketId packetId, List<object> args) : base(packetId, args)
        {
            MethodName = args[0].ToString();
            MethodArgsCount = Convert.ToInt32(args[1]);
            MethodArgs = args.GetRange(2, MethodArgsCount);
            ShouldInvokeInServer = Convert.ToBoolean(args[MethodArgsCount + 2]);
            args.RemoveRange(0, 1 + MethodArgsCount);
        }
    }
}
