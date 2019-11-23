using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class BroadcastMethodPacket : MethodPacket
    {
        public BroadcastMethodPacket(MethodInterceptionArgs args, bool invokeInServer, int id) : base (args, invokeInServer, PacketID.BroadcastMethod, id)
        {

        }
    }
}
