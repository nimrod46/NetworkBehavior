using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class BeginSynchronizationPacket : Packet
    {
        public BeginSynchronizationPacket() : base(PacketId.BeginSynchronization, false)
        {
        }

        public BeginSynchronizationPacket(List<object> args) : base(PacketId.BeginSynchronization, args)
        {
        }
    }
}
