using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class InitiateDircetInterface : Packet
    {
        public InitiateDircetInterface() : base(PacketID.InitiateDircetInterface)
        {
            generateData();
        }
    }
}
