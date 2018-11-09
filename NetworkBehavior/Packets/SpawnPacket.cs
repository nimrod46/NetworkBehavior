using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class SpawnPacket : Packet
    {
        protected Type instance;
        protected object id;
        protected object ownerId;
        protected string[] spawnParams;
        internal SpawnPacket(NetworkBehavior net, Type instance, int id, int ownerId, params string[] spawnParams) : base(net, PacketID.Spawn)
        {
            this.net = net;
            this.instance = instance;
            this.id = id;
            this.ownerId = ownerId;
            this.spawnParams = spawnParams;
            generateData();
        }

        protected override void generateData()
        {
            base.generateData();
            args.Add(instance.FullName);
            args.Add(ownerId.ToString());
            if (spawnParams != null)
            {
                args.AddRange(spawnParams);
            }
            args.Add(id.ToString());
        }
    }
}
