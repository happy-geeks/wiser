using System.Reflection;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace Api.Modules.Templates.Models.History
{
    public class DynamicContentChangeModel
    {
        public PropertyInfo Property { get; set; }
        public object NewValue { get; set; }
        public object OldValue { get; set; }

        public DynamicContentChangeModel(PropertyInfo property, object newValue, object oldValue)
        {
            this.Property = property;
            this.NewValue = newValue;
            this.OldValue = oldValue;
        }

        public CmsPropertyAttribute GetPropertyAttribute()
        {
            return Property.GetCustomAttribute<CmsPropertyAttribute>();
        }

        public string GetPropertyInfoPrettyName()
        {
            return Property.GetCustomAttribute<CmsPropertyAttribute>().PrettyName;
        }
    }
}
