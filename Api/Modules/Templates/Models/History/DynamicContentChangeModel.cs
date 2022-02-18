using System.Reflection;
using Api.Modules.Templates.Helpers;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace Api.Modules.Templates.Models.History
{
    public class DynamicContentChangeModel
    {
        public string Component { get; set; }
        public string ComponentMode { get; set; }
        public string Property { get; set; }
        public object NewValue { get; set; }
        public object OldValue { get; set; }

        public DynamicContentChangeModel(string component, string property, object newValue, object oldValue, string componentMode)
        {
            this.Component = component;
            this.ComponentMode = componentMode;
            this.Property = property;
            this.NewValue = newValue;
            this.OldValue = oldValue;
        }

        public PropertyInfo GetProperty()
        {
            var newCmsSettings = ReflectionHelper.GetCmsSettingsTypeByComponentName(Component);
            return newCmsSettings.GetProperty(Property);
        }

        public CmsPropertyAttribute GetPropertyAttribute()
        {
            var property = GetProperty();
            return property.GetCustomAttribute<CmsPropertyAttribute>();
        }
    }
}
