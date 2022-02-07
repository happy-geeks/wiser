using System;
using System.Linq;
using System.Reflection;
using Api.Modules.Templates.Exceptions;
using GeeksCoreLibrary.Core.Cms;

namespace Api.Modules.Templates.Helpers
{
    public class ReflectionHelper
    {
        /// <summary>
        /// Retrieve the Type of a given component name using reflection. This method will exclusively look through the GCL for components of the type CmsComponent<CmsSettings, Enum>. 
        /// When no or multiple components are found an InvalidComponentException will be thrown.
        /// </summary>
        /// <param name="componentName">A string of the component that is to be retrieved using reflection.</param>
        /// <returns>The type of the current component.</returns>
        public static Type GetComponentTypeByName(string componentName)
        {
            var assembly = Assembly.GetAssembly(typeof(CmsComponent<,>));
            return assembly?.GetTypes().FirstOrDefault(t => t.FullName != null && t.FullName.StartsWith($"{nameof(GeeksCoreLibrary)}.{nameof(GeeksCoreLibrary.Components)}") && String.Equals(t.Name, componentName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the settingsType from the current CMSComponent. The settingstype is the model used for determining the properties and attributes of the components.
        /// </summary>
        /// <param name="component">The type of the component which settingstype should be retrieved.</param>
        /// <returns>
        /// The Type of the model belonging to this component.
        /// </returns>
        public static Type GetCmsSettingsType(Type component)
        {
            var info = (component.BaseType).GetTypeInfo();
            return info.GetGenericArguments()[0];
        }

        /// <summary>
        /// This method will retrieve the cms settings type by first retrieving the base component with the given name and then retrieve its cmssettings type.
        /// </summary>
        /// <param name="componentName">The name of the component whose settings type is to be retrieved</param>
        /// <returns>A Type of the cms settings that is bound to the component that matches the name.</returns>
        public static Type GetCmsSettingsTypeByComponentName (string componentName)
        {
            var component = GetComponentTypeByName(componentName);
            return GetCmsSettingsType(component);
        }
    }
}
