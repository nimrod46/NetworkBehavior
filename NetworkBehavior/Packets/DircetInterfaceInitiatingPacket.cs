using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class DircetInterfaceInitiatingPacket : NetworkIdentityBasePacket
    {
        public DircetInterfaceInitiatingPacket(int id) : base(PacketId.DircetInterfaceInitiating, id)
        {
        }

        public DircetInterfaceInitiatingPacket(List<object> args) : base(PacketId.DircetInterfaceInitiating, args)
        {
        }
    }
}
