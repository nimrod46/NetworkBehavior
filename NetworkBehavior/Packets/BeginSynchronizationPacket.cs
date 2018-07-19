using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class BeginSynchronizationPacket
    {
        protected NetworkBehavior net;
        public string data;
        protected PacketID packetID = PacketID.BeginSynchronization;
        public BeginSynchronizationPacket(NetworkBehavior net)
        {
            this.net = net;
            generateData();
        }

        protected virtual void generateData()
        {
            data = ((int) packetID).ToString() + NetworkIdentity.packetSpiltter.ToString();
        }

        public virtual void send(int port)
        {
            net.send(data, port, NetworkInterface.TCP);
        }
    }
}
