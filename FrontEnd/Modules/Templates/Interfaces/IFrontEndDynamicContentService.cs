using System;
using System.Collections.Generic;
using System.Reflection;
using Api.Modules.Templates.Models.History;
using FrontEnd.Modules.Templates.Models;

namespace FrontEnd.Modules.Templates.Interfaces;

public interface IFrontEndDynamicContentService
{
    /// <summary>
    /// The method retrieves property attributes of a component and will divide the properties into the tabs and groups they belong to.
    /// </summary>
    /// <param name="component">The component from which the properties should be retrieved.</param>
    /// <param name="data">The data from database for this component.</param>
    /// <param name="componentMode">The selected mode of the component.</param>
    /// <returns>
    /// A list of <see cref="TabViewModel"/>.
    /// </returns>
    DynamicContentInformationViewModel GenerateDynamicContentInformationViewModel(Type component, Dictionary<string, object> data, string componentMode);

        
    /// <summary>
    /// The method retrieves property attributes of a component and will divide the properties into the tabs and groups they belong to.
    /// </summary>
    /// <param name="component">The component type.</param>
    /// <param name="properties">The properties of the component.</param>
    /// <param name="data">The data from database for this component.</param>
    /// <param name="componentMode">The selected mode of the component.</param>
    /// <returns>
    /// A list of <see cref="TabViewModel"/>.
    /// </returns>
    DynamicContentInformationViewModel GenerateDynamicContentInformationViewModel(Type component, IEnumerable<PropertyInfo> properties, Dictionary<string, object> data, string componentMode);

    List<(FieldViewModel OldVersion, FieldViewModel NewVersion)> GenerateChangesListForHistory(List<DynamicContentChangeModel> dynamicContentChanges);
}