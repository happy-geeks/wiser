using Api.Modules.EntityProperties.Enums;

namespace Api.Modules.EntityProperties.Models;

/// <summary>
/// A model for a dependency of an entity property within Wiser.
/// </summary>
public class EntityPropertyDependencyModel
{
    /// <summary>
    /// Gets or sets the name of the property it depends on.
    /// </summary>
    public string Field { get; set; }

    /// <summary>
    /// Gets or sets the type of dependency operator.
    /// </summary>
    public FilterOperators? Operator { get; set; }

    /// <summary>
    /// Gets or sets the value used in the operator.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Gets or sets the action it depends on.
    /// </summary>
    public DependencyActions? Action { get; set; }
}