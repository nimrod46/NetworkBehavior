using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class SyncVarPacket : Packet
    {
        protected LocationInterceptionArgs locationArg;
        protected int id;
        protected bool invokeInServer;
        internal SyncVarPacket(LocationInterceptionArgs locationArg, bool invokeInServer, int id) : base (PacketID.SyncVar)
        {
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
