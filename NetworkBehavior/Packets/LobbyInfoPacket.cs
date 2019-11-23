using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class LobbyInfoPacket : Packet
    {
        public string info;
        public LobbyInfoPacket(string info) : base(PacketID.LobbyInfo)
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
