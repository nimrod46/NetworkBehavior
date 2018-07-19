using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class SyncVarPacket
    {
        protected NetworkBehavior net;
        protected LocationInterceptionArgs args;
        protected string data;
        protected int id;
        protected PacketID packetID = PacketID.SyncVar;
        public SyncVarPacket(NetworkBehavior net, LocationInterceptionArgs args, int id)
        {
            this.net = net;
            this.args = args;
            this.id = id;
            generateData();
        }

        protected virtual void generateData()
        {
            data = args.Value.ToString().Replace(NetworkIdentity.packetSpiltter.ToString(), "").Replace(NetworkIdentity.argsSplitter.ToString(), "");
            data = 
                ((int)packetID) + NetworkIdentity.packetSpiltter.ToString() + 
                args.Location.Name + NetworkIdentity.argsSplitter.ToString() + 
                data + NetworkIdentity.argsSplitter.ToString() + 
                id;
        }

        public virtual void send(NetworkInterface networkInterface)
        {
            net.send(data, networkInterface);
        }
    }
}
