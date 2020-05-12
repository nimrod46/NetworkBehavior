using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NetworkingLib.Server;

namespace Networking
{
    internal class InitiateDircetInterfacePacket : Packet
    {
        public EndPointId ClientId { get; private set; }

        public InitiateDircetInterfacePacket(EndPointId clientId) : base(PacketId.InitiateDircetInterface, false)
        {
            ClientId = clientId;
            Data.Add(ClientId.Id);
        }

        public InitiateDircetInterfacePacket(List<object> args) : base(PacketId.InitiateDircetInterface, args)
        {
            ClientId = EndPointId.FromLong(Convert.ToInt64(args[0]));
            args.RemoveAt(0);
        }

    }
}
