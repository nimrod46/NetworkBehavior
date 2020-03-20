using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
    
    public class NetworkIdentity
    {
        public static Dictionary<int, NetworkIdentity> entities = new Dictionary<int, NetworkIdentity>();
        internal static readonly char packetSpiltter = '¥';
        internal static readonly char argsSplitter = '|';
        internal static bool interrupt = true;
        internal static object scope = new object();
        internal static int lastId = 0;

        public delegate void NetworkInitialize();
        public event NetworkInitialize OnNetworkInitializeEvent;
        public delegate void HasLocalAuthorityInitialize();
        public event HasLocalAuthorityInitialize OnHasLocalAuthorityInitializeEvent;
        public delegate void DestroyEventHandler(NetworkIdentity identity);
        public event DestroyEventHandler OnDestroyEvent;
        internal delegate void BeginSynchronization();
        internal event BeginSynchronization OnBeginSynchronization;

        public NetworkBehavior NetworkBehavior;
        public bool isServerAuthority = false;
        public bool hasAuthority = false;
        public bool isInServer = false;
        public bool hasInitialized = false;
        public bool hasFieldsBeenInitialized = false;
        public int id;
        public int ownerId;
        public NetworkIdentity()
        {
            if (!NetworkBehavior.classes.ContainsKey(GetType().FullName))
            {
                NetworkBehavior.classes.Add(GetType().FullName, GetType());
            }
        }

        internal void PreformEvents()
        {
            lock (entities)
            {
                if (!entities.ContainsKey(id))
                {
                    entities.Add(id, this);
                }
                if (hasAuthority)
                {
                    OnHasLocalAuthorityInitializeEvent?.Invoke();
                }
                hasInitialized = true;
                OnNetworkInitializeEvent?.Invoke();
            }
        }

        public void Synchronize()
        {
            OnBeginSynchronization?.Invoke();
        }
        
        public void Destroy()
        {
            DestroyLocally();
        }

        [BroadcastMethod]
        private void DestroyLocally()
        {
            OnDestroyEvent?.Invoke(this);
            entities.Remove(id);
        }

        [BroadcastMethod]
        public void SetAuthority(int newOwnerId)
        {
            if(newOwnerId == -1)
            {
                hasAuthority = isInServer;
                ownerId = NetworkBehavior.serverPort;
                isServerAuthority = true;
            }
            else
            {
                if (NetworkBehavior.GetNetworkIdentityById(newOwnerId).ownerId == newOwnerId)
                {
                    ownerId = newOwnerId;
                    hasAuthority = ownerId == id;
                    isServerAuthority = NetworkBehavior.serverPort == newOwnerId;
                }
                else
                {
                    throw new Exception("Invalid owner id was given");
                }
            }
        }
    }
}
