using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class SpawnWithLocalAuthorityPacket
    {
        protected NetworkBehavior net;
        protected Type instance;
        protected int ownerID;
        protected int id;
        public string data;
        protected PacketID packetID = PacketID.SpawnLocalPlayer;
        public SpawnWithLocalAuthorityPacket(NetworkBehavior net, Type instance, int ownerID, int id)
        {
            this.net = net;
            this.instance = instance;
            this.ownerID = ownerID;
            this.id = id;
            generateData();
        }

        protected virtual void generateData()
        {   
            data = ((int)packetID) + NetworkIdentity.packetSpiltter.ToString() + 
                instance.FullName + NetworkIdentity.argsSplitter.ToString() + 
                ownerID + NetworkIdentity.argsSplitter.ToString() + 
                id;
        }

        public virtual void send(NetworkInterface networkInterface)
        {
            net.sendToAPlayer(data, ownerID, networkInterface);
        }
    }
}
