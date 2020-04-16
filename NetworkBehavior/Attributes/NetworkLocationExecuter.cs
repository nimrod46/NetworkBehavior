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
                v = Operations.GetValueAsObject(Location.LocationType, v);
                Location.SetValue(networkIdentity, v);
            }, shouldInvokeSynchronously);
        }
    }
}
