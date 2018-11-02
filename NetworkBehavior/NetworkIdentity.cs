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
        internal static readonly char packetSpiltter = '¥';
        internal static readonly char argsSplitter = '|';
        internal static bool interrupt = true;
        internal static object scope = new object();
        internal static int lastId = 0;
        public static Dictionary<int, NetworkIdentity> entities = new Dictionary<int, NetworkIdentity>();

        internal delegate void NetworkInitialize();
        internal event NetworkInitialize OnNetworkInitializeEvent;
        internal delegate void HasLocalAuthorityInitialize();
        internal event HasLocalAuthorityInitialize OnHasLocalAuthorityInitializeEvent;
        internal delegate void LocalPlayerInitialize();
        internal event LocalPlayerInitialize OnLocalPlayerInitializeEvent;
        public delegate void BeginSynchronization();
        public event BeginSynchronization OnBeginSynchronization;

        public bool isServer = false;
        public bool hasAuthority = false;
        public bool isLocalPlayer = false;
        public bool isInServer = false;
        public bool hasInitialized = false;
        public int id;
        public int ownerId;
        public NetworkIdentity()
        {
            OnNetworkInitializeEvent += OnNetworkInitialize;
            OnHasLocalAuthorityInitializeEvent += OnHasLocalAuthorityInitialize;
            OnLocalPlayerInitializeEvent += OnLocalPlayerInitialize;
            if (!NetworkBehavior.classes.ContainsKey(GetType().FullName))
            {
                NetworkBehavior.classes.Add(GetType().FullName, GetType());
            }
        }



        internal void ThreadPreformEvents()
        {
            //new Thread(new ThreadStart(PreformEvents)).Start();
            PreformEvents();
        }

        private void PreformEvents()
        {
            lock (entities)
            {
                if (!entities.ContainsKey(id))
                {
                    entities.Add(id, this);
                }
                OnNetworkInitializeEvent?.Invoke();
                if (hasAuthority)
                {
                    OnHasLocalAuthorityInitializeEvent?.Invoke();
                }

                if (isLocalPlayer)
                {
                    OnLocalPlayerInitializeEvent?.Invoke();
                }

            }
            hasInitialized = true;
        }

        public virtual void OnNetworkInitialize()
        {
        }

        public virtual void OnHasLocalAuthorityInitialize()
        {
        }

        public virtual void OnServerDisconnected()
        {
        }

        public virtual void OnNetworkIdentityDisconnected()
        {
        }

        public virtual void OnLocalPlayerInitialize()
        {
        }

        public void Synchronize()
        {
            OnBeginSynchronization?.Invoke();
        }

        public virtual void OnDestroyed()
        {
        }

        internal void ServerDisconnected()
        {
            OnServerDisconnected();
        }

        [BroadcastMethod(networkInterface = NetworkInterface.TCP, invokeInServer = true)]
        internal void Disconnected()
        {
            OnNetworkIdentityDisconnected();
        }

        [BroadcastMethod(networkInterface = NetworkInterface.TCP, invokeInServer = true)]
        public void Destroy()
        {
            OnDestroyed();
            entities.Remove(id);
        }
    }
}
