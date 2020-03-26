using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class LobbyInfoPacket : Packet
    {
        public string Info { get; private set; }

        public LobbyInfoPacket(string info) : base(PacketId.LobbyInfo)
        {
            Info = info;
            Data.Add(Info);
        }

        public LobbyInfoPacket(List<object> args) : base(PacketId.LobbyInfo, args)
        {
            Info = args[0].ToString();
            args.RemoveAt(0);
        }
    }
}
