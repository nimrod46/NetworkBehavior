using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class SpawnLocalPlayerPacket
    {
        protected NetworkBehavior net;
        protected Type instance;
        protected int playerID;
        public string data;
        protected PacketID packetID = PacketID.SpawnLocalPlayer;
        public SpawnLocalPlayerPacket(NetworkBehavior net, Type instance, int playerID)
        {
            this.net = net;
            this.instance = instance;
            this.playerID = playerID;
            generateData();
        }

        protected virtual void generateData()
        {   
            data = ((int)packetID) + NetworkIdentity.packetSpiltter.ToString() + 
                instance.FullName + NetworkIdentity.argsSplitter.ToString() + 
                playerID;
        }

        public virtual void send(NetworkInterface networkInterface)
        {
            net.sendToAPlayer(data, playerID, networkInterface);
        }
    }
}
