using System.Collections.Generic;

namespace Api.Modules.DigitalOcean.Models
{
    /// <summary>
    /// Model for a Digital Ocean database,
    /// </summary>
    public class DatabaseApiModel
    {
        /// <summary>
        /// A unique ID that can be used to identify and reference a database cluster.
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// A unique, human-readable name referring to a database cluster.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// A slug representing the database engine used for the cluster. The possible values are: "pg" for PostgreSQL, "mysql" for MySQL, "redis" for Redis, and "mongodb" for MongoDB.
        /// </summary>
        public string Engine { get; set; }
        
        /// <summary>
        /// A string representing the version of the database engine in use for the cluster.
        /// </summary>
        public string Version { get; set; }
        
        /// <summary>
        /// The slug identifier for the region where the database cluster is located.
        /// </summary>
        public string Region { get; set; }
        
        /// <summary>
        /// An array of strings containing the names of databases created in the database cluster.
        /// </summary>
        public List<string> DatabaseNames { get; set; }
    }
}
