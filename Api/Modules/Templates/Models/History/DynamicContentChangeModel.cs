using System.Reflection;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace Api.Modules.Templates.Models.History
{
    public class DynamicContentChangeModel
    {
        PropertyInfo property;
        object newValue;
        object oldValue;

        public DynamicContentChangeModel(PropertyInfo property, object newValue, object oldValue)
        {
            this.property = property;
            this.newValue = newValue;
            this.oldValue = oldValue;
        }

        public PropertyInfo GetPropertyInfo()
        {
            return property;
        }

        public CmsPropertyAttribute GetPropertyAttribute()
        {
            return property.GetCustomAttribute<CmsPropertyAttribute>();
        }

        public string GetPropertyInfoPrettyName()
        {
            return property.GetCustomAttribute<CmsPropertyAttribute>().PrettyName;
        }

        public object GetNewValue()
        {
            return newValue;
        }
        public object GetOldValue()
        {
            return oldValue;
        }
    }
}
