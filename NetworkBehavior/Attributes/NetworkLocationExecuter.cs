using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.ComponentModel.Design;
using System.Reflection;
using System.Threading;
using static Networking.NetworkIdentity;

namespace Networking
{
    internal class NetworkLocationExecuter : NetworkMemberExecuter
    {
        readonly LocationInfo location;
        internal NetworkLocationExecuter(LocationInfo location)
        {
            this.location = location;
        }

        internal override void InvokeMemberFromNetwork(NetworkIdentity networkIdentity, bool shouldInvokeSynchronously, params object[] args)
        {
            InvokeMemberFromNetwork(() =>
            {
                var v = args[0];
                if (typeof(NetworkIdentity).IsAssignableFrom(location.LocationType))
                {
                    v = NetworkIdentity.entities[IdentityId.FromLong(long.Parse(v + ""))];
                }
                else
                {
                    v = Convert.ChangeType(v, location.LocationType);
                }
                location.SetValue(networkIdentity, v);
            }, shouldInvokeSynchronously);
        }
    }
}
