using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class SpawnLocalPlayerPacket : SpawnPacket
    {
        internal SpawnLocalPlayerPacket(NetworkBehavior net, Type instance, int playerID, params string[] spawnParams) : base(net, instance, playerID, playerID, spawnParams)
        {
            packetID = PacketID.SpawnLocalPlayer;
            args.Clear();
            generateData();
        }

        protected override void generateData()
        {
            base.generateData();
            args.RemoveAt(0);
            args.Insert(0, ((int)packetID).ToString());
            Console.WriteLine(packetID);
        }
    }
}
