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
        private readonly MethodBase method;
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
                v = Operations.GetValueAsObject(item.ParameterType, v);
                args[i] = v;
                i++;
            }
            InvokeMemberFromNetwork(() => method.Invoke(networkIdentity, args), shouldInvokeSynchronously);
        }
    }
}
