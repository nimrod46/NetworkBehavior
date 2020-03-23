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
        public BroadcastMethodPacket(MethodInterceptionArgs args, bool invokeInServer, bool alreadyInvokedInAuthority, int id) : base (args, invokeInServer, alreadyInvokedInAuthority, PacketID.BroadcastMethod, id)
        {

        }
    }
}
