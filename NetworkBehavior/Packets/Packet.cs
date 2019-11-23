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
        public readonly PacketID packetID;
        public readonly List<string> args = new List<string>();
        public Packet(PacketID packetID)
        {
            this.packetID = packetID;
        }

        protected virtual void generateData()
        {
            args.Add(((int)packetID).ToString());
        }

        internal List<string> GetArgs()
        {
            return new List<string>(args);
        }
    }
}
