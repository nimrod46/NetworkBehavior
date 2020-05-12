using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Networking.NetworkIdentity;
using static NetworkingLib.Server;

namespace Networking
{
    internal abstract class SpawnPacket : NetworkIdentityBasePacket
    {
        public string InstanceName { get; private set; }
        public EndPointId OwnerId { get; private set; }
        public int SpawnParamCount { get; private set; }
        public object[] SpawnParams { get; private set; }

        internal SpawnPacket(PacketId packetID, bool shouldInvokeSynchronously, IdentityId id, Type instance, EndPointId ownerId, params object[] spawnParams) : base(packetID, shouldInvokeSynchronously, id)
        {
            InstanceName = instance.FullName;
            OwnerId = ownerId;
            SpawnParamCount = spawnParams != null ? spawnParams.Count() : 0;
            SpawnParams = spawnParams;
            Data.Add(InstanceName);
            Data.Add(OwnerId.Id);
            Data.Add(SpawnParamCount);
            if (SpawnParams != null) Data.AddRange(SpawnParams) ;
        }

        public SpawnPacket(PacketId packetId, List<object> args) : base(packetId, args)
        {
            InstanceName = args[0].ToString();
            OwnerId = EndPointId.FromLong(Convert.ToInt64(args[1]));
            SpawnParamCount = Convert.ToInt32(args[2]);
            SpawnParams = args.GetRange(3, SpawnParamCount).ToArray();
            args.RemoveRange(0, 3 + SpawnParamCount);
        }
    }
}
