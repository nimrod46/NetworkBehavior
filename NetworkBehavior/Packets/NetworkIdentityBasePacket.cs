using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Networking.NetworkIdentity;

namespace Networking
{
    internal abstract class NetworkIdentityBasePacket : Packet
    {
        public IdentityId NetworkIdentityId { get; private set; }

        public NetworkIdentityBasePacket(PacketId packetId, IdentityId networkIdentityId) : base(packetId)
        {
            NetworkIdentityId = networkIdentityId;
            Data.Add(NetworkIdentityId.Id);
        }

        public NetworkIdentityBasePacket(PacketId packetId, List<object> args) : base(packetId, args)
        {
            NetworkIdentityId = IdentityId.FromLong(Convert.ToInt64(args[0]));
            args.RemoveAt(0);
        }
    }
}
