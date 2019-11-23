using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class SpawnLocalPlayerPacket : SpawnPacket
    {
        internal SpawnLocalPlayerPacket(Type instance, int id, params string[] spawnParams) : base(PacketID.SpawnLocalPlayer, instance, id, id, spawnParams)
        {
        }
    }
}
