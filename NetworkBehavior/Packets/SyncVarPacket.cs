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
        public bool ShouldInvokeSynchronously { get; private set; }

        internal SyncVarPacket(IdentityId networkIdentityId, string locationName, object locationValue, bool shouldInvokeSynchronously) : base (PacketId.SyncVar, networkIdentityId)
        {
            LocationName = locationName;
            LocationValue = locationValue;
            ShouldInvokeSynchronously = shouldInvokeSynchronously;
            Data.Add(LocationName);
            Data.Add(LocationValue);
            Data.Add(ShouldInvokeSynchronously);
        }

        internal SyncVarPacket(List<object> args) : base(PacketId.SyncVar, args)
        {
            LocationName = args[0].ToString();
            LocationValue = args[1];
            ShouldInvokeSynchronously = bool.Parse(args[2].ToString());
            args.RemoveRange(0, 3);
        }
    }
}
