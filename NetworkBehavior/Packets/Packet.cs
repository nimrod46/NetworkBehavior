using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal abstract class Packet
    {
        public PacketId PacketId { get; set; }

        public List<object> Data { get; } = new List<object>();

        public Packet(PacketId packetId)
        {
            PacketId = packetId;
            Data.Add((int) PacketId);
        }

        public Packet(PacketId packetId, List<object> args) : this(packetId)
        {
            Data = new List<object>(args);
            args.RemoveAt(0);
        }
    }
}
