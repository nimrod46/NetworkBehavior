using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Networking.NetworkIdentity;

namespace Networking
{
    internal class SyncVarPacket : NetworkIdentityBasePacket
    {
        public string LocationName { get; private set; }
        public object LocationValue { get; private set; }

        internal SyncVarPacket(IdentityId networkIdentityId, string locationName, object locationValue) : base (PacketId.SyncVar, networkIdentityId)
        {
            LocationName = locationName;
            LocationValue = locationValue;
            Data.Add(LocationName);
            Data.Add(LocationValue);
        }

        internal SyncVarPacket(List<object> args) : base(PacketId.SyncVar, args)
        {
            LocationName = args[0].ToString();
            LocationValue = args[1];
            args.RemoveRange(0, 2);
        }
    }
}
