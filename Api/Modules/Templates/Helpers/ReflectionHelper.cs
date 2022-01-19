using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Api.Modules.Templates.Exceptions;
using GeeksCoreLibrary.Core.Cms;

namespace Api.Modules.Templates.Helpers
{
    public class ReflectionHelper
    {
        public ReflectionHelper ()
        {

        }

        /// <summary>
        /// Retrieve the Type of a given component name using reflection. This method will exclusively look through the GCL for components of the type CmsComponent<CmsSettings, Enum>. 
        /// When no or multiple components are found an InvalidComponentException will be thrown.
        /// </summary>
        /// <param name="componentName">A string of the component that is to be retrieved using reflection.</param>
        /// <returns>The type of the current component.</returns>
        public Type GetComponentTypeByName(string componentName)
        {
            var componentType = typeof(CmsComponent<CmsSettings, Enum>);
            var assembly = componentType.Assembly;

            var typeInfoList = assembly.DefinedTypes.Where(
                type =>
                type.Name == componentName
                && type.BaseType != null
                && type.BaseType.IsGenericType
                && componentType.IsGenericType
                && type.BaseType.GetGenericTypeDefinition() == componentType.GetGenericTypeDefinition()
            ).ToList();

            if (typeInfoList.Count == 0)
            {
                throw new InvalidComponentException("No components found with the provided name. (" + componentName + ")");
            }
            else if (typeInfoList.Count > 1)
            {
                throw new InvalidComponentException("Multiple components found with the same name. (" + componentName + ")");
            }

            return typeInfoList.First().AsType();
        }

        /// <summary>
        /// Gets the settingsType from the current CMSComponent. The settingstype is the model used for determining the properties and attributes of the components.
        /// </summary>
        /// <param name="component">The type of the component which settingstype should be retrieved.</param>
        /// <returns>
        /// The Type of the model belonging to this component.
        /// </returns>
        public Type GetCmsSettingsType(Type component)
        {
            var info = (component.BaseType).GetTypeInfo();
            return info.GetGenericArguments()[0];
        }

        /// <summary>
        /// This method will retrieve the cms settings type by first retrieving the base component with the given name and then retrieve its cmssettings type.
        /// </summary>
        /// <param name="componentName">The name of the component whose settings type is to be retrieved</param>
        /// <returns>A Type of the cms settings that is bound to the component that matches the name.</returns>
        public Type GetCmsSettingsTypeByComponentName (string componentName)
        {
            var component = GetComponentTypeByName(componentName);
            return GetCmsSettingsType(component);
        }
    }
}
