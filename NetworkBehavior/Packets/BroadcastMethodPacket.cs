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
        public BroadcastPacket(IdentityId id, string methodName, object[] methodArgs) : base(PacketId.BroadcastMethod, id, methodName, methodArgs)
        {

        }

        public BroadcastPacket(List<object> args) : base(PacketId.BroadcastMethod, args)
        {
        }
    }
}
