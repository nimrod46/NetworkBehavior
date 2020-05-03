using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            else if (type.IsValueType || typeof(string).IsAssignableFrom(type))
            {
                return Convert.ChangeType(value, type);
            }

            NetworkIdentity networkIdentity = NetworkIdentity.GetNetworkIdentityById(value);
            if (networkIdentity == null)
            {
                NetworkBehavior.PrintWarning("Cannot get network identity in \"Operations\"");
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
            return obj.ToString();
        }
    }
}
