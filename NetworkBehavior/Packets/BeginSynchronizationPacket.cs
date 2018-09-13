﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class BeginSynchronizationPacket : Packet
    {
        public BeginSynchronizationPacket(NetworkBehavior net) : base(net, PacketID.BeginSynchronization)
        {
            generateData();
        }

        protected override void generateData()
        {
            base.generateData();
        }
    }
}
