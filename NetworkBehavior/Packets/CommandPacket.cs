using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Networking.NetworkIdentity;

namespace Networking
{
    internal class CommandPacket : MethodPacket
    {
        public CommandPacket(IdentityId id, string methodName, object[] methodArgs) : base(PacketId.Command, id, methodName, methodArgs)
        {

        }

        public CommandPacket(List<object> args) : base(PacketId.Command, args)
        {
        }
    }
}
