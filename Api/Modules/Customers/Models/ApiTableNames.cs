namespace Api.Modules.Customers.Models
{
    public class ApiTableNames
    {
        // API tables
        public const string Clients = "api_clients";
        public const string DeferredRequests = "api_requests";
        public const string AdminAccountsLoginAttempts = "api_customer_login_attempts";

        // Wiser tables
        public const string WiserCustomers = "easy_customers";
        public const string WiserUsers = "easy_users";
        public const string WiserUserLogs = "easy_users_log";
        public const string WiserUsersAuthenticationTokens = "easy_users_auth_tokens";
        public const string WiserUserRights = "easy_userrights";
        public const string WiserAdminAccounts = "admin_accounts";
        public const string Modules = "easy_modules";
    }
}
