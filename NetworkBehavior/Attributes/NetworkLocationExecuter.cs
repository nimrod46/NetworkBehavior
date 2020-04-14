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
        internal LocationInfo Location { get; }

        internal NetworkLocationExecuter(LocationInfo location)
        {
            this.Location = location;
        }


        internal override void InvokeMemberFromNetwork(NetworkIdentity networkIdentity, bool shouldInvokeSynchronously, params object[] args)
        {
            InvokeMemberFromNetwork(() =>
            {
                var v = args[0];
                if (typeof(NetworkIdentity).IsAssignableFrom(Location.LocationType))
                {
                    if (NetworkIdentity.entities.TryGetValue(IdentityId.FromLong(long.Parse(v + "")), out NetworkIdentity networkIdentityArg))
                    {
                        v = networkIdentityArg;
                    }
                    else
                    {
                        NetworkBehavior.PrintWarning("no NetworkIdentity with id {0} was found.", v.ToString());
                        return;
                    }
                }
                else
                {
                    v = Convert.ChangeType(v, Location.LocationType);
                }
                Location.SetValue(networkIdentity, v);
            }, shouldInvokeSynchronously);
        }
    }
}
