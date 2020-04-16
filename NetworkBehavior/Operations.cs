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
            if (typeof(NetworkIdentity).IsAssignableFrom(type))
            {
                return NetworkIdentity.GetNetworkIdentityById(value);
            }
            else if(typeof(Enum).IsAssignableFrom(type))
            {
                return Enum.Parse(type, value.ToString());
            }
            return Convert.ChangeType(value, type);
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
