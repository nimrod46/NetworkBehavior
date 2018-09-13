using PostSharp.Aspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    internal abstract class MethodPacket : Packet
    {
        protected MethodInterceptionArgs methodArgs;
        protected bool invokeInServer;
        protected int id;
        public MethodPacket (NetworkBehavior net, MethodInterceptionArgs methodArgs, bool invokeInServer, PacketID packetId, int id) : base(net, packetId)
        {
            this.net = net;
            this.methodArgs = methodArgs;
            this.invokeInServer = invokeInServer;
            this.id = id;
            generateData();
        }

        protected override void generateData()
        {
            base.generateData();
            if (methodArgs.Arguments.Count != 0)
            {
                foreach (object o in methodArgs.Arguments)
                {
                    if (o == null)
                    {
                        args.Add("null");
                    }
                    else if (o is NetworkIdentity)
                    {
                        args.Add((o as NetworkIdentity).id.ToString());
                    }
                    else
                    {
                        args.Add(o.ToString());
                    }
                }
            }
            args.Insert(1, methodArgs.Method.Name);
            args.Add(invokeInServer.ToString());
            args.Add(id.ToString());
        }
    }
}
