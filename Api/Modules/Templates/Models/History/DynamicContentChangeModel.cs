using System.Reflection;
using Api.Modules.Templates.Helpers;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace Api.Modules.Templates.Models.History
{
    /// <summary>
    /// Model class to have info about what changed in the Dynamic Content
    /// </summary>
    public class DynamicContentChangeModel
    {
        /// <summary>
        /// Gets or sets the component of the Dynamic Content that is changing
        /// </summary>
        public string Component { get; set; }
        
        /// <summary>
        /// Gets or sets the component mode of the Dynamic Content that is changing
        /// </summary>
        public string ComponentMode { get; set; }
        
        /// <summary>
        /// Gets or sets the property of the Dynamic Content that is changing
        /// </summary>
        public string Property { get; set; }
        
        /// <summary>
        /// Gets or sets the new value that the Dynamic Content is changing to
        /// </summary>
        public object NewValue { get; set; }
        
        /// <summary>
        /// Gets or sets the old value from which the Dynamic Content is changing to
        /// </summary>
        public object OldValue { get; set; }


#pragma warning disable CS1591
        public DynamicContentChangeModel(string component, string property, object newValue, object oldValue, string componentMode)
#pragma warning restore CS1591
        {
            this.Component = component;
            this.ComponentMode = componentMode;
            this.Property = property;
            this.NewValue = newValue;
            this.OldValue = oldValue;
        }

        private PropertyInfo GetProperty()
        {
            var newCmsSettings = ReflectionHelper.GetCmsSettingsTypeByComponentName(Component);
            return newCmsSettings.GetProperty(Property);
        }
        
        /// <summary>
        /// Gets the attribute of a property
        /// </summary>
        public CmsPropertyAttribute GetPropertyAttribute()
        {
            var property = GetProperty();
            return property.GetCustomAttribute<CmsPropertyAttribute>();
        }
    }
}
