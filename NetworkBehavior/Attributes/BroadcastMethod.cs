﻿using System;
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

namespace Networking
{
    [AttributeUsage(AttributeTargets.Method,
                     AllowMultiple = true, Inherited = false)  // Multiuse attribute.  
]
    [PSerializable]
    public class BroadcastMethod : MethodNetworkAttribute
    {
        public BroadcastMethod() : base(PacketId.BroadcastMethod)
        {

        }
    }
}
