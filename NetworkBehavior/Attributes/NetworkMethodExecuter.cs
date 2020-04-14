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
    internal class NetworkMethodExecuter : NetworkMemberExecuter
    {
        MethodBase method;
        internal NetworkMethodExecuter(MethodBase method) : base()
        {
            this.method = method;
        }

        internal override void InvokeMemberFromNetwork(NetworkIdentity networkIdentity, bool shouldInvokeSynchronously, params object[] args)
        {
            int i = 0;
            foreach (ParameterInfo item in method.GetParameters())
            {
                var v = args[i];
                if (typeof(NetworkIdentity).IsAssignableFrom(item.ParameterType))
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
                    v = Convert.ChangeType(v, item.ParameterType);
                }
                args[i] = v;
                i++;
            }
            InvokeMemberFromNetwork(() => method.Invoke(networkIdentity, args), shouldInvokeSynchronously);
        }
    }
}
