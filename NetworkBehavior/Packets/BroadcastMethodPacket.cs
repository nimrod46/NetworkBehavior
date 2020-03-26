using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class BroadcastPacket : MethodPacket
    {
        public BroadcastPacket(int id, MethodInterceptionArgs args, bool invokeInServer) : base(PacketId.BroadcastMethod, id, args, invokeInServer)
        {

        }

        public BroadcastPacket(List<object> args) : base(PacketId.BroadcastMethod, args)
        {
        }
    }
}
