using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.ComponentModel.Design;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;

namespace Networking
{
    internal abstract class NetworkMemberExecuter
    {

        private object scope = new object();
        public bool invokedFromNetwork;

        internal NetworkMemberExecuter()
        {
            invokedFromNetwork = false;
        }

        internal virtual void InvokeMemberFromLocal(NetworkIdentity networkIdentityInstance, Action invokeNetworkly)
        {
            lock (scope)
            {
                if (invokedFromNetwork)
                {
                    invokedFromNetwork = false;
                }

                else
                {
                    if (!networkIdentityInstance.hasInitialized) return;

                    invokeNetworkly?.Invoke();
                }
            }
        }

        protected virtual void InvokeMemberFromNetwork(Action action, bool shouldInvokeSynchronously)
        {
            if (shouldInvokeSynchronously)
            {
                lock (NetworkBehavior.synchronousActions)
                {
                    NetworkBehavior.synchronousActions.Add(() =>
                    {
                        lock (scope)
                        {
                            invokedFromNetwork = true;
                            action.Invoke();
                            invokedFromNetwork = false;
                        }
                    });
                }
            }
            else
            {
                lock (scope)
                {
                    invokedFromNetwork = true;
                    action.Invoke();
                    invokedFromNetwork = false;
                }
            }
        }

        internal abstract void InvokeMemberFromNetwork(NetworkIdentity networkIdentity, bool shouldInvokeSynchronously, params object[] args);
    }
}
