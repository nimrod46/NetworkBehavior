using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class SpawnWithLocalAuthorityPacket : SpawnPacket
    {
        internal SpawnWithLocalAuthorityPacket(NetworkBehavior net, Type instance, int ownerID, int id, params string[] spwanParams) : base(net, instance, id, ownerID, spwanParams)
        {
            packetID = PacketID.SpawnLocalPlayer;
        }    
    }
}
