using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Networking.NetworkIdentity;

namespace Networking
{
    internal class BroadcastPacket : MethodPacket
    {
        public BroadcastPacket(IdentityId id, string methodName, bool shouldInvokeSynchronously, object[] methodArgs) : base(PacketId.BroadcastMethod, id, methodName, shouldInvokeSynchronously, methodArgs)
        {

        }

        public BroadcastPacket(List<object> args) : base(PacketId.BroadcastMethod, args)
        {
        }
    }
}
