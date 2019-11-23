using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class SpawnObjectPacket : SpawnPacket
    {
        internal SpawnObjectPacket(Type instance, int id, int ownerId, params string[] spawnParams) : base(PacketID.Spawn, instance, id, ownerId, spawnParams)
        {
        }
    }
}
