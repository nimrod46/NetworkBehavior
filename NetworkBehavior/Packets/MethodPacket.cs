using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Networking.NetworkIdentity;

namespace Networking
{
    internal abstract class MethodPacket : NetworkIdentityBasePacket
    {
        
        public string MethodName { get; private set; }
        public int MethodArgsCount { get; private set; }
        public bool ShouldInvokeSynchronously { get; private set; }
        public object[] MethodArgs { get; private set; }

        public MethodPacket(PacketId packetId, IdentityId id, string methodName, bool shouldInvokeSynchronously, object[] methodArgs) : base(packetId, id)
        {
            MethodName = methodName;
            MethodArgsCount = methodArgs.Count();
            ShouldInvokeSynchronously = shouldInvokeSynchronously;
            MethodArgs = methodArgs;
            Data.Add(MethodName); 
            Data.Add(ShouldInvokeSynchronously); 
            Data.Add(MethodArgsCount);
            if (MethodArgs != null) Data.AddRange(MethodArgs) ;
        }

        public MethodPacket(PacketId packetId, List<object> args) : base(packetId, args)
        {
            MethodName = args[0].ToString();
            ShouldInvokeSynchronously = bool.Parse(args[1].ToString());
            MethodArgsCount = Convert.ToInt32(args[2]);
            MethodArgs = args.GetRange(3, MethodArgsCount).ToArray();
            args.RemoveRange(0, 3 + MethodArgsCount);
        }
    }
}
