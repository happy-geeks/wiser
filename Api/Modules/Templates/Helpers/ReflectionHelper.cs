using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;

namespace Api.Modules.Templates.Helpers
{
    /// <summary>
    /// A helper class for the template module for reflection functions.
    /// </summary>
    public class ReflectionHelper
    {
        /// <summary>
        /// Get all possible components. These components should are retrieved from the assembly and any plugins and should have the base type CmsComponent&lt;CmsSettings, Enum&gt;
        /// </summary>
        /// <returns>Dictionary of type infos and object attributes of all the components found in the GCL.</returns>
        public static Dictionary<TypeInfo, CmsObjectAttribute> GetComponents()
        {
            var componentType = typeof(CmsComponent<CmsSettings, Enum>);
            var resultDictionary = new Dictionary<TypeInfo, CmsObjectAttribute>();
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.FullName!.StartsWith("GeeksCoreLibrary"));
            foreach (var assembly in loadedAssemblies)
            {
                var typeInfoList = assembly.DefinedTypes.Where(
                    type => type.BaseType is {IsGenericType: true}
                            && componentType.IsGenericType
                            && type.BaseType.GetGenericTypeDefinition() == componentType.GetGenericTypeDefinition()
                ).OrderBy(type => type.Name).ToList();

                foreach (var typeInfo in typeInfoList)
                {
                    resultDictionary.Add(typeInfo, typeInfo.GetCustomAttribute<CmsObjectAttribute>());
                }
            }

            return resultDictionary.OrderBy(c => c.Key.Name).ToDictionary(c => c.Key, c => c.Value);
        }

        /// <summary>
        /// Retrieve the Type of a given component name using reflection. This method will exclusively look through the GCL for components of the type CmsComponent&lt;CmsSettings, Enum&gt;.
        /// When no or multiple components are found an InvalidComponentException will be thrown.
        /// </summary>
        /// <param name="componentName">A string of the component that is to be retrieved using reflection.</param>
        /// <returns>The type of the current component.</returns>
        public static Type GetComponentTypeByName(string componentName)
        {
            var components = GetComponents();
            return components.FirstOrDefault(c => c.Key.FullName != null && c.Key.FullName.StartsWith($"{nameof(GeeksCoreLibrary)}.{nameof(GeeksCoreLibrary.Components)}") && String.Equals(c.Key.Name, componentName, StringComparison.OrdinalIgnoreCase)).Key;
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