using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class SpawnPacket
    {
        protected NetworkBehavior net;
        protected Type instance;
        protected object id;
        protected object ownerId;
        public string data;
        protected PacketID packetID = PacketID.Spawn;
        public SpawnPacket(NetworkBehavior net, Type instance, int id, int ownerId)
        {
            this.net = net;
            this.instance = instance;
            this.id = id;
            this.ownerId = ownerId;
            generateData();
        }

        protected virtual void generateData()
        {
            data = 
                ((int)packetID) + NetworkIdentity.packetSpiltter.ToString() +
                instance.FullName + NetworkIdentity.argsSplitter.ToString() + 
                ownerId + NetworkIdentity.argsSplitter.ToString() +
                id;
        }

        public virtual void send(NetworkInterface networkInterface)
        {
            net.send(data, networkInterface);
        }

        public virtual void send(NetworkInterface networkInterface, int port)
        {
            net.send(data, port, networkInterface);
        }

        public virtual void sendToAPlayer(NetworkInterface networkInterface, int port)
        {
            net.sendToAPlayer(data, port, networkInterface);
        }
    }
}
