using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class SpawnObjectPacket : SpawnPacket
    {
        internal SpawnObjectPacket(Type instance, int id, int ownerId, params string[] spawnParams) : base(PacketId.SpawnObject, id, instance, ownerId, spawnParams)
        {
        }

        public SpawnObjectPacket(List<object> args) : base(PacketId.SpawnObject, args)
        {
        }
    }
}
