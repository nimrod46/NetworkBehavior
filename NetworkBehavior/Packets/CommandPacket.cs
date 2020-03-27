using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class CommandPacket : MethodPacket
    {
        public CommandPacket(int id, string methodName, object[] methodArgs) : base(PacketId.Command, id, methodName, methodArgs)
        {

        }

        public CommandPacket(List<object> args) : base(PacketId.Command, args)
        {
        }
    }
}
