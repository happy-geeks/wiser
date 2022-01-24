using System.Reflection;
using Api.Modules.Templates.Helpers;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace Api.Modules.Templates.Models.History
{
    public class DynamicContentChangeModel
    {
        public string Component { get; set; }
        public string Property { get; set; }
        public object NewValue { get; set; }
        public object OldValue { get; set; }

        public DynamicContentChangeModel(string component, string property, object newValue, object oldValue)
        {
            this.Component = component;
            this.Property = property;
            this.NewValue = newValue;
            this.OldValue = oldValue;
        }

        public PropertyInfo GetProperty()
        {
            var helper = new ReflectionHelper();
            var newCmsSettings = helper.GetCmsSettingsTypeByComponentName(Component);
            return newCmsSettings.GetProperty(Property);
        }

        public CmsPropertyAttribute GetPropertyAttribute()
        {
            var property = GetProperty();
            return property.GetCustomAttribute<CmsPropertyAttribute>();
        }
    }
}
