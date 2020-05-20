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
        internal SpawnObjectPacket(bool shouldInvokeSynchronously, Type instance, IdentityId id, EndPointId ownerId, bool spawnDuringSync, params string[] spawnParams) : base(PacketId.SpawnObject, shouldInvokeSynchronously, id, instance, ownerId, spawnDuringSync, spawnParams)
        {
        }

        public SpawnObjectPacket(List<object> args) : base(PacketId.SpawnObject, args)
        {
        }
    }
}
