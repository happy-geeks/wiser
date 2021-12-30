using GeeksCoreLibrary.Core.Models;

namespace Api.Modules.Items.Models
{
    public class DisplayItemDetailModel : WiserItemDetailModel
    {
        /// <summary>
        /// Gets or sets the display name of the field. 
        /// </summary>
        public string DisplayName { get; set; }
    }
}