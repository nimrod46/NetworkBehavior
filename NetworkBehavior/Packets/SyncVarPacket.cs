using PostSharp.Aspects;
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
        protected bool invokeInServer;
        internal SyncVarPacket(NetworkBehavior net, LocationInterceptionArgs locationArg, bool invokeInServer, int id) : base (net, PacketID.SyncVar)
        {
            this.net = net;
            this.locationArg = locationArg;
            this.id = id;
            this.invokeInServer = invokeInServer;
            generateData();
        }

        protected override void generateData()
        {
            base.generateData();
            args.Add(locationArg.Location.Name);
            args.Add(locationArg.Value.ToString());
            args.Add(invokeInServer.ToString());
            args.Add(id.ToString());
        }
    }
}
