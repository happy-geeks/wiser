using System;
using Api.Modules.Templates.Enums;

namespace Api.Modules.Templates.Attributes;

/// <summary>
/// A custom attribute for properties for WTS settings, for dynamically generating a user interface.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class WtsPropertyAttribute : Attribute
{
    /// <summary>
    /// If field for the property should be visible.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// If field for the property is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// The name of the property that defines the id of the object.
    /// Required for properties using kendoComponent.Grid.
    /// For example: "TimeId" for RunSchemes.
    /// </summary>
    public string IdProperty { get; set; }

    /// <summary>
    /// The title of the field to show to the user.
    /// For example: "Service naam"
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The text to show under the field as help for the user.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The tab in which the field should be shown.
    /// For example: "Service", which means that the field should be shown in the tab with the name "Service".
    /// </summary>
    public string ConfigurationTab { get; set; }

    /// <summary>
    /// The kendo component to use for the field.
    /// </summary>
    public DataComponents DataComponent { get; set; }

    /// <summary>
    /// The kendo options to use for the field.
    /// </summary>
    public string KendoOptions { get; set; }

    /// <summary>
    /// Use datasource with the name of the property.
    /// For example: For the property "RunSchemes" the datasource "runSchemes" will be used.
    /// </summary>
    public bool UseDataSource { get; set; }

    /// <summary>
    /// Allows editing of the property.
    /// This can only be applied to properties of type list.
    /// This will automatically bind a change event in the kendoOptions.
    /// </summary>
    public bool AllowEdit { get; set; }

    /// <summary>
    /// Declares if the property depends on a dropdown field to be shown or hidden.
    /// For example: "Type", which means that the property depends on the dropdown field with the name "Type".
    /// This requires the property DependsOnValue to be set.
    /// </summary>
    public string DependsOnField { get; set; }

    /// <summary>
    /// Declares the value of the dropdown field on which the property depends to be shown.
    /// For example: "Continuous", which means that the property will be shown if the dropdown field has the value "Continuous".
    /// This requires the property DependsOnField to be set.
    /// </summary>
    public string[] DependsOnValue { get; set; }
    
    /// <summary>
    /// Indicates whether a textbox should be grayed out.
    /// For example: an ID field that must be visible but not editable.
    /// </summary>
    public bool IsDisabled { get; set; }
    
    /// <summary>
    /// If set, this field will be shown first. When multiple fields have this attribute, lower numbers are rendered first.
    /// Not currently in use.
    /// </summary>
    public int CustomUiOrder { get; set; }
    
    /// <summary>
    /// Path within the template is needed if the drop boxes might need to be changed or are dynamic.
    /// </summary>
    public string DropDownListDataSource { get; set; }
    
    /// <summary>
    /// Variable name to look for within the object found at the DropDownListDataSource is needed if the drop boxes might need to be changed or are dynamic.
    /// </summary>
    public string DropDownListDataVariableName { get; set; }
    
    
}