using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Networking.NetworkIdentity;
using static NetworkingLib.Server;

namespace Networking
{
    internal class SyncObjectVars : NetworkIdentityBasePacket
    {
        public int SpawnParamCount { get; private set; }
        public object[] SpawnParams { get; private set; }

        internal SyncObjectVars(IdentityId id, params string[] spawnParams) : base(PacketId.SyncObjectVars, id)
        {
            SpawnParamCount = spawnParams.Count();
            SpawnParams = spawnParams;
            Data.Add(SpawnParamCount);
            Data.AddRange(SpawnParams);
        }

        public SyncObjectVars(List<object> args) : base(PacketId.SyncObjectVars, args)
        {
            SpawnParamCount = Convert.ToInt32(args[0]);
            SpawnParams = args.GetRange(1, SpawnParamCount).ToArray();
            args.RemoveRange(0, 1 + SpawnParamCount);
        }
    }
}
