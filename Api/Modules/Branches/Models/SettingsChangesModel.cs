using GeeksCoreLibrary.Modules.Branches.Enumerations;

namespace Api.Modules.Branches.Models
{
    /// <inheritdoc />
    public class SettingsChangesModel : BranchChangesModel
    {
        /// <summary>
        /// Gets or sets the type of setting.
        /// </summary>
        public WiserSettingTypes Type { get; set; }
    }
}