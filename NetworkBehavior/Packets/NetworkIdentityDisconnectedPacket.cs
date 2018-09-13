using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class NetworkIdentityDisconnectedPacket : Packet
    {
        private int playerID;
        public NetworkIdentityDisconnectedPacket(NetworkBehavior net , int playerID) : base(net, PacketID.NetworkIdentityDisconnected)
        {
            this.playerID = playerID;
            generateData();
        }

        protected override void generateData()
        {
            base.generateData();
            args.Add(playerID.ToString());
        }
    }
}
