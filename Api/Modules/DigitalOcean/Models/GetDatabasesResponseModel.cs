using System.Collections.Generic;

namespace Api.Modules.DigitalOcean.Models
{
    public class GetDatabasesResponseModel
    {
        public List<DatabaseApiModel> Databases { get; set; }
    }
}
