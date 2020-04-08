using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class LocationInfo
    {
        private readonly FieldInfo field;
        private readonly PropertyInfo property;
        public string Name { get; private set; }
        public object Value { get; private set; }
        public Type LocationType { get; private set; }
        public LocationInfo(FieldInfo field)
        {
            this.field = field;
            Name = field.Name;
            LocationType = field.FieldType;
        }

        public LocationInfo(PropertyInfo property)
        {
            this.property = property;
            Name = property.Name;
            LocationType = property.PropertyType;
        }

        public void SetValue(object instance, object value)
        {
            field?.SetValue(instance, value);
            property?.SetValue(instance, value);
        }

        public object GetValue(object instance)
        {
            if(field != null)
            {
                return field.GetValue(instance);
            }
            return property.GetValue(instance);
        }
    }
}
