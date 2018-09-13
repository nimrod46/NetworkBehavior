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
        public CommandPacket(NetworkBehavior net, MethodInterceptionArgs args, int id) : base (net, args, true, PacketID.Command, id)
        {

        }
    }
}
