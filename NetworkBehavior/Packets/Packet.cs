using Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal abstract class Packet
    {
        protected NetworkBehavior net;
        protected PacketID packetID;
        protected List<string> args = new List<string>();
        public Packet(NetworkBehavior net, PacketID packetID)
        {
            this.net = net;
            this.packetID = packetID;
        }

        protected virtual void generateData()
        {
            args.Add(((int)packetID).ToString());
        }

        internal List<string> GetArgs()
        {
            return new List<string>(args);
        }
        
        internal void Send(NetworkInterface networkInterface, params int[] ports)
        {
            if (net.isServer)
            {
                if (networkInterface == NetworkInterface.TCP)
                {
                    net.server.broadcast(args.ToArray(), ports);
                }
                else
                {
                    net.directBroadcast(args.ToArray(), ports);
                }
            }
            else
            {
                if (networkInterface == NetworkInterface.TCP)
                {
                    net.client.send(args.ToArray());
                }
                else
                {
                    net.directClient.send((int)packetID, args.ToArray());
                }
            }
        }

        internal void SendToAUser(NetworkInterface networkInterface, int port)
        {
            if (net.isServer)
            {
                if (networkInterface == NetworkInterface.TCP)
                {
                    net.server.sendToAUser(args.ToArray(), port);
                }
                else
                {
                    net.directServer.send(args.ToArray(), NetworkBehavior.clients[port].ip, NetworkBehavior.clients[port].port);
                }
            }
            else
            {
                throw new Exception("Client cannot send data to a specific user!");
            }
        }
    }
}
