using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    abstract class MethodPacket
    {
        protected NetworkBehavior net;
        protected MethodInterceptionArgs args;
        protected bool invokeInServer;
        public string data;
        protected PacketID packetId;
        protected int id;
        public MethodPacket (NetworkBehavior net, MethodInterceptionArgs args, bool invokeInServer, PacketID packetId, int id)
        {
            this.net = net;
            this.args = args;
            this.invokeInServer = invokeInServer;
            this.packetId = packetId;
            this.id = id;
            generateData();
        }

        protected virtual void generateData()
        {
            data = "";
            if (args.Arguments.Count != 0)
            {
                foreach (object o in args.Arguments)
                {
                    if (o == null)
                    {
                        data += "null";
                    }
                    else if (o is NetworkIdentity)
                    {
                        data += (o as NetworkIdentity).id;
                    }
                    else
                    {
                        data += o.ToString().Replace(NetworkIdentity.packetSpiltter.ToString(), "").Replace(NetworkIdentity.argsSplitter.ToString(), "");
                    }
                    data += NetworkIdentity.argsSplitter;
                }
                data = data.Remove(data.Length - 1);
            }
            data = 
                ((int)packetId) + NetworkIdentity.packetSpiltter.ToString() + 
                args.Method.Name + NetworkIdentity.argsSplitter.ToString() + 
                data + NetworkIdentity.argsSplitter.ToString() + invokeInServer + NetworkIdentity.argsSplitter.ToString() +
                id;
        }

        public virtual void send(NetworkInterface networkInterface)
        {
            net.send(data, networkInterface);
        }
    }
}
