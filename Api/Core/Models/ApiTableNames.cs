namespace Api.Core.Models
{
    /// <summary>
    /// Names of tables that are only used in the Wiser API.
    /// </summary>
    public class ApiTableNames
    {
        /// <summary>
        /// The table that contains all tenants/tenants of Wiser.
        /// </summary>
        public const string WiserTenants = "easy_customers";

        /// <summary>
        /// The table that we use to store all request logs in.
        /// </summary>
        public const string ApiRequestLogs = "wiser_api_request_log";
    }
}