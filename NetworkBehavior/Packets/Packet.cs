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

        public bool ShouldInvokeSynchronously { get; private set; }

        public Packet(PacketId packetId, bool shouldInvokeSynchronously)
        {
            PacketId = packetId;
            ShouldInvokeSynchronously = shouldInvokeSynchronously;
            Data.Add((int)PacketId);
            Data.Add(ShouldInvokeSynchronously);
        }

        public Packet(PacketId packetId, List<object> args)
        {
            PacketId = packetId;
            Data = new List<object>(args);
            ShouldInvokeSynchronously = bool.Parse(args[1].ToString());
            args.RemoveRange(0, 2);
        }
    }
}
