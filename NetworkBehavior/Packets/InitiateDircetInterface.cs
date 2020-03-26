using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class InitiateDircetInterfacePacket : Packet
    {
        public InitiateDircetInterfacePacket() : base(PacketId.InitiateDircetInterface)
        {
        }

        public InitiateDircetInterfacePacket(List<object> args) : base(PacketId.InitiateDircetInterface, args)
        {
        }
    }
}
