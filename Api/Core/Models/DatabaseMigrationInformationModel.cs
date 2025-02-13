using System;

namespace Api.Core.Models;

/// <summary>
/// A model that contains information about a database migration.
/// </summary>
public class DatabaseMigrationInformationModel
{
    /// <summary>
    /// The unique identifier of the migration.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The display name of the migration.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// A description that explains what the migration does and anything you have to pay attention to when starting this migration.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Whether this migration needs to be triggered manually.
    /// </summary>
    public bool RequiresManualTrigger { get; set; }

    /// <summary>
    /// Whether this is a migration that does more than just updating the database schema.
    /// If this is <c>true</c>, it might also update the data of certain tables.
    /// If that is the case, it will be explained in the <see cref="Description"/>.
    /// </summary>
    public bool IsCustomMigration { get; set; }

    /// <summary>
    /// The date and time that the migration was last run.
    /// If this value is smaller than the <see cref="LastUpdateOn"/> value,
    /// the migration has been updated since it was last run and should be run again.
    /// </summary>
    public DateTime? LastRunOn { get; set; }

    /// <summary>
    /// The date and time that the migration was last updated.
    /// If this value is greater than the <see cref="LastRunOn"/> value,
    /// the migration has been updated since it was last run and should be run again.
    /// </summary>
    public DateTime LastUpdateOn { get; set; }
}