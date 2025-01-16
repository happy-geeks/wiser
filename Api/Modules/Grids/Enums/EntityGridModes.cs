namespace Api.Modules.Grids.Enums;

/// <summary>
/// An enum containing the different types of grids we can have.
/// </summary>
public enum EntityGridModes
{
    /// <summary>
    /// A normal entity grid, for showing a grid with linked entities of a certain type.
    /// </summary>
    Normal,

    /// <summary>
    /// A link overview, this is used for the grid that you see when adding a new link to a normal entity grid.
    /// </summary>
    LinkOverview,

    /// <summary>
    /// A grid with task history, for the tasks module that can be opened via a button in the header of Wiser.
    /// </summary>
    TaskHistory,

    /// <summary>
    /// The change history of an entity.
    /// </summary>
    ChangeHistory,

    /// <summary>
    /// A grid with a custom query.
    /// </summary>
    CustomQuery,

    /// <summary>
    /// The grid for the search module.
    /// </summary>
    SearchModule,

    /// <summary>
    /// A grid for viewing and editing all item details (fields) of a certain group.
    /// This is meant for dynamic item details that can be imported or created manually,
    /// but we don't know before hand what they will be exactly and they can be different per item.
    /// </summary>
    ItemDetailsGroup
}