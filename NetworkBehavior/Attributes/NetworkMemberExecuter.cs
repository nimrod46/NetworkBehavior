using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.ComponentModel.Design;
using PostSharp.Serialization;
using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using System.Reflection;
using System.Threading;

namespace Networking
{
    internal class NetworkMemberExecuter<T> where T : Aspect
    {
        internal static readonly Dictionary<string, T> networkAttributes = new Dictionary<string, T>();

        private readonly bool shouldInvokeSynchronously;
        private readonly bool shouldInvokeFromLocaly;
        private readonly bool shouldInvokeNetworkly;
        private string scope = "";
        public bool invokedFromNetwork;

        internal NetworkMemberExecuter(string memberName, T networkAttribute, bool shouldInvokeSynchronously, bool shouldInvokeFromLocaly, bool shouldInvokeNetworkly)
        {
            if (networkAttributes.ContainsKey(memberName))
            {
                throw new Exception("NetworkAttribute: Duplicate member name: " + memberName);
            }
            networkAttributes.Add(memberName, networkAttribute);
            this.shouldInvokeSynchronously = shouldInvokeSynchronously;
            this.shouldInvokeFromLocaly = shouldInvokeFromLocaly;
            this.shouldInvokeNetworkly = shouldInvokeNetworkly;
            invokedFromNetwork = false;
        }

        internal virtual void InvokeMember(NetworkIdentity networkIdentityInstance, Action action, Action invokeNetworkly)
        {
            lock (scope)
            {
                if (invokedFromNetwork)
                {
                    invokedFromNetwork = false;
                    action.Invoke();
                }
                else
                {
                    if(shouldInvokeFromLocaly)
                    {
                        action.Invoke();
                    }
                    if (!shouldInvokeNetworkly) return;

                    if (!networkIdentityInstance.hasInitialized) return;

                    if (!networkIdentityInstance.hasAuthority && !networkIdentityInstance.isInServer)
                    {
                        NetworkBehavior.PrintWarning("NetworkAttribute:InvokeNetworkly was called on none authority identity");
                    }
                    else
                    {
                        invokeNetworkly?.Invoke();
                    }
                }
            }
        }

        internal virtual void InvokeMemberFromNetwork(Action action)
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

        internal static T GetNetworkAttributeMemberByMemberName(string memberName)
        {
            if (!networkAttributes.TryGetValue(memberName, out T networkAttribute))
            { 
                throw new Exception("NetworkAttribute: No member with the name: " + memberName + " was found");
            }
            return networkAttribute;      
        }
    }
}
