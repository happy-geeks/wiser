using System.Collections.Generic;

namespace Api.Modules.DigitalOcean.Models
{
 
#pragma warning disable CS1591
    public class PostDatabaseUserResponseModel
#pragma warning restore CS1591
    {
        /// <summary>
        /// List of users that have access to the Digital Ocean database
        /// </summary>
        public List<UserApiModel> Users { get; set; } = new();
    }
}
