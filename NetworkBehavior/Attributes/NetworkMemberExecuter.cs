using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.ComponentModel.Design;
using System.Reflection;
using System.Threading;

namespace Networking
{
    internal abstract class NetworkMemberExecuter
    {

        private string scope = "";
        public bool invokedFromNetwork;

        internal NetworkMemberExecuter()
        {
            //if (networkAttributes.ContainsKey(memberName))
            //{
            //    throw new Exception("NetworkAttribute: Duplicate member name: " + memberName);
            //}
            //networkAttributes.Add(memberName, networkAttribute);
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

                    //if (!networkIdentityInstance.hasAuthority && !networkIdentityInstance.isInServer)
                    //{
                    //    NetworkBehavior.PrintWarning("NetworkAttribute:InvokeNetworkly was called on none authority identity");
                    //}
                    //else
                    {
                        invokeNetworkly?.Invoke();
                    }
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
                }
            }
        }

        internal abstract void InvokeMemberFromNetwork(NetworkIdentity networkIdentity, bool shouldInvokeSynchronously, params object[] args);

        //internal static T GetNetworkAttributeMemberByMemberName(string memberName)
        //{
        //    if (!networkAttributes.TryGetValue(memberName, out T networkAttribute))
        //    { 
        //        throw new Exception("NetworkAttribute: No member with the name: " + memberName + " was found");
        //    }
        //    return networkAttribute;      
        //}
    }
}
