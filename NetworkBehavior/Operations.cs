using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NetworkingLib.Server;

namespace Networking
{
    class Operations
    {
        public static object GetValueAsObject(Type type, object value)
        {
            if (typeof(Enum).IsAssignableFrom(type))
            {
                return Enum.Parse(type, value.ToString());
            }
            else if (typeof(EndPointId).IsAssignableFrom(type))
            {
                return EndPointId.FromLong(long.Parse(value.ToString()));
            }
            else if (type.IsValueType || typeof(string).IsAssignableFrom(type))
            {
                return Convert.ChangeType(value, type);
            }
            if(value.ToString() == "null")
            {
                return null;
            }
            NetworkIdentity networkIdentity = NetworkIdentity.GetNetworkIdentityById(value);
            if (networkIdentity == null)
            {
                NetworkBehavior.PrintWarning("Cannot get network identity in \"Operations\" from {0}", value);
            }
            return networkIdentity;
        }

        public static object GetObjectAsValue(object obj)
        {
            if (obj == null)
            {
                return "null";
            }
            else if (obj is NetworkIdentity)
            {
                return (obj as NetworkIdentity).Id.ToString();
            }
            else if(obj is Enum)
            {
                return Convert.ToInt32(obj);
            }
            else if(obj is EndPointId endPointId)
            {
                return endPointId.Id;
            }
            return obj.ToString();
        }
    }
}
