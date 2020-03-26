using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal abstract class NetworkIdentityBasePacket : Packet
    {
        public int NetworkIdentityId { get; private set; }

        public NetworkIdentityBasePacket(PacketId packetId, int networkIdentityId) : base(packetId)
        {
            NetworkIdentityId = networkIdentityId;
            Data.Add(NetworkIdentityId);
        }

        public NetworkIdentityBasePacket(PacketId packetId, List<object> args) : base(packetId, args)
        {
            NetworkIdentityId =  Convert.ToInt32(args[0]);
            args.RemoveAt(0);
        }
    }
}
