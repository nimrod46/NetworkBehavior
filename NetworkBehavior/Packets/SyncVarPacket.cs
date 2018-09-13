﻿using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class SyncVarPacket : Packet
    {
        protected LocationInterceptionArgs locationArg;
        protected int id;
        public SyncVarPacket(NetworkBehavior net, LocationInterceptionArgs locationArg, int id) : base (net, PacketID.SyncVar)
        {
            this.net = net;
            this.locationArg = locationArg;
            this.id = id;
            generateData();
        }

        protected override void generateData()
        {
            base.generateData();
            args.Add(locationArg.Location.Name);
            args.Add(locationArg.Value.ToString());
            args.Add(id.ToString());
        }
    }
}
