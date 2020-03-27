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
        public object[] MethodArgs { get; private set; }

        public MethodPacket(PacketId packetId, int id, string methodName, object[] methodArgs) : base(packetId, id)
        {
            MethodName = methodName;
            MethodArgsCount = methodArgs.Count();
            MethodArgs = methodArgs;
            Data.Add(MethodName); 
            Data.Add(MethodArgsCount);
            if (MethodArgs != null) Data.AddRange(MethodArgs) ;
        }

        public MethodPacket(PacketId packetId, List<object> args) : base(packetId, args)
        {
            MethodName = args[0].ToString();
            MethodArgsCount = Convert.ToInt32(args[1]);
            MethodArgs = args.GetRange(2, MethodArgsCount).ToArray();
            args.RemoveRange(0, 2 + MethodArgsCount);
        }
    }
}
