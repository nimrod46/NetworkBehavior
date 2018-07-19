using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class LobbyInfoPacket
    {
        protected NetworkBehavior net;
        public string info;
        public string data;
        protected PacketID packetID = PacketID.LobbyInfo;
        public LobbyInfoPacket(NetworkBehavior net, string info)
        {
            this.net = net;
            this.info = info;
            generateData();
        }

        protected virtual void generateData()
        {
            data = ((int)packetID) + NetworkIdentity.packetSpiltter.ToString() + info;
        }

        public virtual void sendToAPlayer(int port)
        {
            net.sendToAPlayer(data, port, NetworkInterface.TCP);
        }
    }
}
