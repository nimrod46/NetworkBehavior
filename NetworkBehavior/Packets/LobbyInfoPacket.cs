using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class LobbyInfoPacket : Packet
    {
        public string info;
        public LobbyInfoPacket(NetworkBehavior net, string info) : base(net, PacketID.LobbyInfo)
        {
            this.info = info;
            generateData();
        }

        protected override void generateData()
        {
            base.generateData();
            args.Add(info);
        }
    }
}
