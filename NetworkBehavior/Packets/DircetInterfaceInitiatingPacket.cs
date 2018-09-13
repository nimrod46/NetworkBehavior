using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class DircetInterfaceInitiatingPacket : Packet
    {
        protected object id;
        public DircetInterfaceInitiatingPacket(NetworkBehavior net, int id) : base(net, PacketID.DircetInterfaceInitiating)
        {
            this.id = id;
            generateData();
        }

        protected override void generateData()
        {
            base.generateData();
            args.Add(id.ToString());
        }
    }
}
