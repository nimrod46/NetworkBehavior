using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class Operations
    {
        public static object getValueAsObject(string typeName, object value)//TODO: Remove
        {
            object newArgs;
            switch (typeName)
            {
                case "String":
                    newArgs = value;
                    break;
                case "Char":
                    newArgs = char.Parse(value + "");
                    break;
                case "Boolean":
                    newArgs = bool.Parse(value + "");
                    break;
                case "UInt32":
                    newArgs = uint.Parse(value + "");
                    break;
                case "UInt64":
                    newArgs = ulong.Parse(value + "");
                    break;
                case "Int16":
                    newArgs = short.Parse(value + "");
                    break;
                case "UInt16":
                    newArgs = ushort.Parse(value + "");
                    break;
                case "Byte":
                    newArgs = byte.Parse(value + "");
                    break;
                case "SByte":
                    newArgs = sbyte.Parse(value + "");
                    break;
                case "Single":
                    newArgs = float.Parse(value + "");
                    break;
                case "Double":
                    newArgs = double.Parse(value + "");
                    break;
                case "Int32":
                    newArgs = int.Parse(value + "");
                    break;
                default:
                    if(NetworkIdentity.entities.Keys.Contains(int.Parse(value + "")))
                    {
                        newArgs = NetworkIdentity.entities[int.Parse(value + "")];
                    }
                    else
                    {
                        throw new Exception("No network identity found with id of: " + value);
                    }
                    break;
            }
            return newArgs;
        }
    }
}
