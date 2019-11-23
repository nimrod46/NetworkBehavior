using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class BeginSynchronizationPacket : Packet
    {
        public BeginSynchronizationPacket() : base(PacketID.BeginSynchronization)
        {
            generateData();
        }

        protected override void generateData()
        {
            base.generateData();
        }
    }
}
