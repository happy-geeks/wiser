using System.Collections.Generic;

namespace Api.Modules.DigitalOcean.Models
{
    public class PostDatabaseUserResponseModel
    {
        public List<UserApiModel> Users { get; set; } = new();
    }
}
