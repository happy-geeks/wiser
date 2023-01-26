using System.Collections.Generic;

namespace Api.Modules.DigitalOcean.Models
{
 
    public class PostDatabaseUserResponseModel
    {
        /// <summary>
        /// List of users that have access to the Digital Ocean database
        /// </summary>
        public List<UserApiModel> Users { get; set; } = new();
    }
}
