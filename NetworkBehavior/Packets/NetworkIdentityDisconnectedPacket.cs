using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class NetworkIdentityDisconnectedPacket : Packet
    {
        private int playerID;
        public NetworkIdentityDisconnectedPacket(int playerID) : base(PacketID.NetworkIdentityDisconnected)
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
