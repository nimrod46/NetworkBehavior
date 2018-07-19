using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class NetworkIdentityDisconnectedPacket
    {
        protected NetworkBehavior net;
        private string data;
        protected PacketID packetID = PacketID.NetworkIdentityDisconnected;
        private int playerID;
        public NetworkIdentityDisconnectedPacket(NetworkBehavior net , int playerID)
        {
            this.net = net;
            this.playerID = playerID;
            generateData();
        }

        protected virtual void generateData()
        {
            data = ((int)packetID) + NetworkIdentity.packetSpiltter.ToString() +
                playerID + NetworkIdentity.argsSplitter.ToString();
        }

        public virtual void send(NetworkInterface networkInterface)
        {
            net.send(data, networkInterface);
        }
    }
}
