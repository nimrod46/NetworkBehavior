using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Networking.NetworkIdentity;
using static NetworkingLib.Server;

namespace Networking
{
    internal class DircetInterfaceInitiatingPacket : Packet
    {
        public EndPointId EndPointId { get; private set; }
        public DircetInterfaceInitiatingPacket(EndPointId endPointId) : base(PacketId.DircetInterfaceInitiating)
        {
            EndPointId = endPointId;
            Data.Add(EndPointId.Id);
        }

        public DircetInterfaceInitiatingPacket(List<object> args) : base(PacketId.DircetInterfaceInitiating, args)
        {
            EndPointId = EndPointId.FromLong(Convert.ToInt64(args[0]));
            args.RemoveAt(0);
        }
    }
}
