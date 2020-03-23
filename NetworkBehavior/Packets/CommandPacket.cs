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
        public CommandPacket(MethodInterceptionArgs args, int id) : base (args, true, false, PacketID.Command, id)
        {

        }
    }
}
