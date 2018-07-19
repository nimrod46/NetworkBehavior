using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class BroadcastMethodPacket : MethodPacket
    {
        public BroadcastMethodPacket(NetworkBehavior net, MethodInterceptionArgs args, bool invokeInServer, int id) : base (net, args, invokeInServer, PacketID.BroadcastMethod, id)
        {

        }
    }
}
