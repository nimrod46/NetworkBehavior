using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Networking.NetworkIdentity;
using static NetworkingLib.Server;

namespace Networking
{
    internal class SpawnObjectPacket : SpawnPacket
    {
        internal SpawnObjectPacket(Type instance, IdentityId id, EndPointId ownerId, params string[] spawnParams) : base(PacketId.SpawnObject, id, instance, ownerId, spawnParams)
        {
        }

        public SpawnObjectPacket(List<object> args) : base(PacketId.SpawnObject, args)
        {
        }
    }
}
