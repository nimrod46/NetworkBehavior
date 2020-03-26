using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal class SyncVarPacket : NetworkIdentityBasePacket
    {
        public string LocationName { get; private set; }
        public string LocationValue { get; private set; }
        public bool ShouldInvokeInServer { get; private set; }

        internal SyncVarPacket(LocationInterceptionArgs locationArg, bool shouldInvokeInServer, int networkIdentityId) : base (PacketId.SyncVar, networkIdentityId)
        {
            LocationName = locationArg.Location.Name;
            if (locationArg.Value is NetworkIdentity)
            {
                LocationValue = ((locationArg.Value as NetworkIdentity).id.ToString());
            }
            else
            {
                LocationValue = locationArg.Value.ToString();
            }
            ShouldInvokeInServer = shouldInvokeInServer;
            Data.Add(LocationName);
            Data.Add(LocationValue);
            Data.Add(ShouldInvokeInServer);
        }

        internal SyncVarPacket(List<object> args) : base(PacketId.SyncVar, args)
        {
            LocationName = args[0].ToString();
            LocationValue = args[1].ToString();
            ShouldInvokeInServer = Convert.ToBoolean(args[2]);
            args.RemoveRange(0, 3);
        }
    }
}
