using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class DircetInterfaceInitiatingPacket
    {
        protected NetworkBehavior net;
        protected object id;
        public string data;
        protected PacketID packetID = PacketID.DircetInterfaceInitiating;
        public DircetInterfaceInitiatingPacket(NetworkBehavior net, int id)
        {
            this.net = net;
            this.id = id;
            generateData();
        }

        protected virtual void generateData()
        {
            data =
                ((int)packetID) + NetworkIdentity.packetSpiltter.ToString() +
                id;
        }

        public virtual void send()
        {
            net.send(data, NetworkInterface.UDP);
        }
    }
}
