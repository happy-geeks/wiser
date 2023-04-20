using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Api.Core.Helpers;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Customers.Interfaces;
using Api.Modules.Customers.Models;
using Api.Modules.Items.Models;
using Api.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Communication.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Models;
using Google.Authenticator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StringHelpers = Api.Core.Helpers.StringHelpers;

namespace Api.Modules.Customers.Services
{
    /// <summary>
    /// Service for operations related to Wiser users (users that can log in to Wiser).
    /// </summary>
    public class UsersService : IUsersService, IScopedService
    {
        private readonly IWiserCustomersService wiserCustomersService;
        private readonly ILogger<UsersService> logger;
        private readonly ApiSettings apiSettings;

        private const string WiserUserEntityType = "wiseruser";
        private const string UserLastLoginDateKey = "last_login";
        private const string UserLastLoginIpKey = "last_login_ip";
        private const string UserPasswordKey = "password";
        private const string UserUsernameKey = "username";
        private const string EmailAddressKey = "email_address";
        private const string UserActiveKey = "active";
        private const string UserLoginAttemptsKey = "attempts";
        private const string UserBlockedKey = "blocked";
        private const string UserRequirePasswordChangeKey = "require_password_change";
        private const string UserDashboardSettingsKey = "dashboard_settings";
        private const string UserGridSettingsGroupName = "grid_settings";
        private const string UserModuleSettingsGroupName = "module_settings";
        private const string UserPinnedModulesKey = "pinnedModules";
        private const string UserAutoLoadModulesKey = "autoLoadModules";

        private const string TotpEnabledKey = "totp_enabled";
        private const string TotpSecretKey = "totp_secret";
        private const string TotpRequiresSetupKey = "totp_requires_setup";
        private const string TotpBackupCodesGroupName = "totp_backup_codes";
        private const int AmountOfBackupCodesToGenerate = 10;

        private readonly IDatabaseConnection clientDatabaseConnection;
        private readonly IDatabaseConnection wiserDatabaseConnection;
        private readonly ITemplatesService templatesService;
        private readonly ICommunicationsService communicationsService;
        private readonly IRolesService rolesService;
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly GclSettings gclSettings;

        /// <summary>
        /// Initializes a new instance of <see cref="UsersService"/>.
        /// </summary>
        public UsersService(IWiserCustomersService wiserCustomersService, IOptions<ApiSettings> apiSettings, IDatabaseConnection clientDatabaseConnection, ILogger<UsersService> logger, ITemplatesService templatesService, ICommunicationsService communicationsService, IOptions<GclSettings> gclSettings, IRolesService rolesService, IDatabaseHelpersService databaseHelpersService)
        {
            this.wiserCustomersService = wiserCustomersService;
            this.logger = logger;
            this.clientDatabaseConnection = clientDatabaseConnection;
            this.apiSettings = apiSettings.Value;
            this.templatesService = templatesService;
            this.communicationsService = communicationsService;
            this.rolesService = rolesService;
            this.databaseHelpersService = databaseHelpersService;
            this.gclSettings = gclSettings.Value;

            if (clientDatabaseConnection is ClientDatabaseConnection connection)
            {
                wiserDatabaseConnection = connection.WiserDatabaseConnection;
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<FlatItemModel>>> GetAsync(bool includeAdminUsers = false)
        {
            var result = new List<FlatItemModel>();
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();

            var query = $@"SELECT 
    user.id, 
	IFNULL(NULLIF(user.title, ''), username.value) AS name, 
    username.`value` AS username
FROM {WiserTableNames.WiserItem} AS user
JOIN {WiserTableNames.WiserItemDetail} AS username ON username.item_id = user.id AND username.`key` = '{UserUsernameKey}'
WHERE user.entity_type = '{WiserUserEntityType}'
ORDER BY username.`value` ASC";

            DataTable dataTable;
            if (includeAdminUsers)
            {
                dataTable = await wiserDatabaseConnection.GetAsync(query);
                if (dataTable.Rows.Count > 0)
                {
                    result.AddRange(dataTable.Rows.Cast<DataRow>().Select(dataRow => new FlatItemModel
                    {
                        Id = dataRow.Field<ulong>("id"),
                        Title = dataRow.Field<string>("name"),
                        Fields = new Dictionary<string, object>
                        {
                            { "username", dataRow.Field<string>("username") },
                            { "isAdmin", true },
                            { "group", "Admin" }
                        }
                    }));
                }
            }

            dataTable = await clientDatabaseConnection.GetAsync(query);

            if (dataTable.Rows.Count > 0)
            {
                result.AddRange(dataTable.Rows.Cast<DataRow>().Select(dataRow => new FlatItemModel
                {
                    Id = dataRow.Field<ulong>("id"),
                    Title = dataRow.Field<string>("name"),
                    Fields = new Dictionary<string, object>
                    {
                        { "username", dataRow.Field<string>("username") },
                        { "isAdmin", false },
                        { "group", "Klant" }
                    }
                }));
            }

            return new ServiceResult<List<FlatItemModel>>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<AdminAccountModel>> LoginAdminAccountAsync(string username, string password, string ipAddress = null, string totpPin = null)
        {
            if (String.IsNullOrWhiteSpace(username) || String.IsNullOrWhiteSpace(password))
            {
                return new ServiceResult<AdminAccountModel>
                {
                    ErrorMessage = "Invalid credentials",
                    StatusCode = HttpStatusCode.Unauthorized
                };
            }

            wiserDatabaseConnection.ClearParameters();
            wiserDatabaseConnection.AddParameter("username", username);

            var query = $@"SELECT
                            account.id,
                            username.value AS login,
                            account.title AS name,
                            password.value AS password,
                            IF(active.value = '1', TRUE, FALSE) AS active,
                            attempts.value AS attempts,
                            blocked.value AS blocked,
                            IFNULL(totpEnabled.value, '0') = '1' AS totpEnabled,
                            totpSecret.value as totpSecret,
                            IFNULL(totpRequiresSetup.value, '1') = '1' AS totpRequiresSetup
                        FROM {WiserTableNames.WiserItem} AS account
                        JOIN {WiserTableNames.WiserItemDetail} AS username ON username.item_id = account.id AND username.`key` = '{UserUsernameKey}' AND username.value = ?username
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS password ON password.item_id = account.id AND password.`key` = '{UserPasswordKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS active ON active.item_id = account.id AND active.`key` = '{UserActiveKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS attempts ON attempts.item_id = account.id AND attempts.`key` = '{UserLoginAttemptsKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS blocked ON blocked.item_id = account.id AND blocked.`key` = '{UserBlockedKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} totpEnabled on totpEnabled.item_id = account.id and totpEnabled.`key` = '{TotpEnabledKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} totpSecret on totpSecret.item_id = account.id and totpSecret.`key` = '{TotpSecretKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} totpRequiresSetup on totpRequiresSetup.item_id = account.id and totpRequiresSetup.`key` = '{TotpRequiresSetupKey}'
                        WHERE account.entity_type = '{WiserUserEntityType}'
                        LIMIT 1";

            var dataTable = await wiserDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<AdminAccountModel>
                {
                    ErrorMessage = "Invalid credentials",
                    StatusCode = HttpStatusCode.Unauthorized
                };
            }

            var savedPassword = dataTable.Rows[0].Field<string>("password");
            if (String.IsNullOrWhiteSpace(savedPassword) || !password.VerifySha512(savedPassword))
            {
                return new ServiceResult<AdminAccountModel>
                {
                    ErrorMessage = "Invalid credentials",
                    StatusCode = HttpStatusCode.Unauthorized
                };
            }

            var result = AdminAccountModel.FromDataRow(dataTable.Rows[0]);
            result.EncryptedId = result.Id.ToString().EncryptWithAes(apiSettings.AdminUsersEncryptionKey, useSlowerButMoreSecureMethod: true);

            // If TOTP is enabled and setup, but we don't have a PIN yet, then return the user without logging in, so that they get the screen for entering the PIN.
            if (result.TotpAuthentication.Enabled && !result.TotpAuthentication.RequiresSetup && String.IsNullOrWhiteSpace(totpPin))
            {
                return new ServiceResult<AdminAccountModel>(result);
            }

            // Handle TOTP authentication.
            var (success, _) = await HandleTotpAuthenticationAsync(totpPin, null, result.TotpAuthentication, result.Id, result.Name, clientDatabaseConnection);
            if (!success)
            {
                // If TOTP authentication failed, return invalid credentials error.
                await AddFailedLoginAttemptAsync(ipAddress, username);
                return new ServiceResult<AdminAccountModel>
                {
                    ErrorMessage = "Invalid credentials",
                    StatusCode = HttpStatusCode.Unauthorized
                };
            }

            return new ServiceResult<AdminAccountModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<UserModel>> LoginCustomerAsync(string username, string password, string encryptedAdminAccountId = null, string subDomain = null, bool generateAuthenticationTokenForCookie = false, string ipAddress = null, ClaimsIdentity identity = null, string totpPin = null, string totpBackupCode = null)
        {
            if (await UsernameIsBlockedAsync(username, clientDatabaseConnection, apiSettings.MaximumLoginAttemptsForUsers))
            {
                return new ServiceResult<UserModel>
                {
                    ErrorMessage = "Username is blocked due to too many failed login attempts",
                    StatusCode = HttpStatusCode.Unauthorized
                };
            }

            if (String.IsNullOrWhiteSpace(username) || (String.IsNullOrWhiteSpace(password) && String.IsNullOrWhiteSpace(encryptedAdminAccountId)))
            {
                await AddFailedLoginAttemptAsync(ipAddress, username);
                return new ServiceResult<UserModel>
                {
                    ErrorMessage = "Invalid credentials",
                    StatusCode = HttpStatusCode.Unauthorized
                };
            }

            // Check if the admin account ID is valid and still active.
            var validAdminAccount = await ValidateAdminAccountAsync(encryptedAdminAccountId, identity);

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("username", username);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);

            var query = $@"SELECT 
	                        user.id, 
	                        IFNULL(NULLIF(user.title, ''), username.value) AS name, 
	                        username.`value` AS username, 
	                        password.`value` AS password,
                            last_login_ip.value AS last_login_ip,
                            IF(last_login_date.value IS NULL, ?now, STR_TO_DATE(last_login_date.value, '%Y-%m-%d %H:%i:%s')) AS last_login_date,
                            IFNULL(require_password_change.value, '0') AS require_password_change,
                            IFNULL(role.role_name, '') AS role,
                            email.value AS emailAddress,
                            IFNULL(totpEnabled.value, '0') = '1' AS totpEnabled,
                            totpSecret.value as totpSecret,
                            IFNULL(totpRequiresSetup.value, '1') = '1' AS totpRequiresSetup
                        FROM {WiserTableNames.WiserItem} user
                        JOIN {WiserTableNames.WiserItemDetail} username ON username.item_id = user.id AND username.`key` = '{UserUsernameKey}' AND username.value = ?username
                        JOIN {WiserTableNames.WiserItemDetail} password ON password.item_id = user.id AND password.`key` = '{UserPasswordKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} last_login_ip ON last_login_ip.item_id = user.id AND last_login_ip.`key` = '{UserLastLoginIpKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} last_login_date ON last_login_date.item_id = user.id AND last_login_date.`key` = '{UserLastLoginDateKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} require_password_change ON require_password_change.item_id = user.id AND require_password_change.`key` = '{UserRequirePasswordChangeKey}'
                        LEFT JOIN {WiserTableNames.WiserUserRoles} userRole ON userRole.user_id = user.id
                        LEFT JOIN {WiserTableNames.WiserRoles} role ON role.id = userRole.role_id
                        LEFT JOIN {WiserTableNames.WiserItemDetail} email ON email.item_id = user.id AND email.`key` = '{EmailAddressKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} totpEnabled on totpEnabled.item_id = user.id and totpEnabled.`key` = '{TotpEnabledKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} totpSecret on totpSecret.item_id = user.id and totpSecret.`key` = '{TotpSecretKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} totpRequiresSetup on totpRequiresSetup.item_id = user.id and totpRequiresSetup.`key` = '{TotpRequiresSetupKey}'
                        WHERE user.entity_type = '{WiserUserEntityType}'
                        AND user.published_environment > 0";

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                await AddFailedLoginAttemptAsync(ipAddress, username);
                return new ServiceResult<UserModel>
                {
                    ErrorMessage = "Invalid credentials",
                    StatusCode = HttpStatusCode.Unauthorized
                };
            }

            UserModel user = null;

            // Find out if a user exists with the given credentials.
            foreach (DataRow dataRow in dataTable.Rows)
            {
                Int32.TryParse(dataRow.Field<string>("require_password_change"), out var requirePasswordChange);
                user = new UserModel
                {
                    Id = dataRow.Field<ulong>("id"),
                    Username = dataRow.Field<string>("username"),
                    Password = dataRow.Field<string>("password"),
                    Name = dataRow.Field<string>("name"),
                    LastLoginDate = dataRow.Field<DateTime?>("last_login_date"),
                    LastLoginIpAddress = dataRow.Field<string>("last_login_ip"),
                    RequirePasswordChange = requirePasswordChange > 0 && !validAdminAccount, // Only require to change the password if the actual user is logged in.
                    Role = dataRow.Field<string>("role"),
                    EmailAddress = dataRow.Field<string>("emailAddress"),
                    TotpAuthentication = new TotpAuthenticationModel
                    {
                        RequiresSetup = Convert.ToBoolean(dataRow["totpRequiresSetup"]),
                        Enabled = Convert.ToBoolean(dataRow["totpEnabled"]),
                        SecretKey = dataRow.Field<string>("totpSecret")
                    }
                };

                // If an admin account is logging in, we don't want to check the password, so just return the first user.
                // Otherwise find a user with the correct password.
                if (validAdminAccount || (!String.IsNullOrWhiteSpace(password) && password.VerifySha512(user.Password)))
                {
                    break;
                }

                // Set the user back to null, otherwise the code below will still accept the login.
                user = null;
            }

            // No user has been found, means the client supplied wrong credentials.
            if (user == null)
            {
                await AddFailedLoginAttemptAsync(ipAddress, username);
                return new ServiceResult<UserModel>
                {
                    ErrorMessage = "Invalid credentials",
                    StatusCode = HttpStatusCode.Unauthorized
                };
            }

            // If TOTP is enabled and setup, but we don't have a PIN yet, then return the user without logging in, so that they get the screen for entering the PIN.
            if (!validAdminAccount && user.TotpAuthentication.Enabled && !user.TotpAuthentication.RequiresSetup && String.IsNullOrWhiteSpace(totpPin) && String.IsNullOrWhiteSpace(totpBackupCode))
            {
                return new ServiceResult<UserModel>(user);
            }

            // Handle TOTP authentication.
            var otpStillRequiresSetup = false;
            if (!validAdminAccount)
            {
                (var otpSuccess, otpStillRequiresSetup) = await HandleTotpAuthenticationAsync(totpPin, totpBackupCode, user.TotpAuthentication, user.Id, user.Name, clientDatabaseConnection);
                if (!otpSuccess)
                {
                    // If TOTP authentication failed, return invalid credentials error.
                    await AddFailedLoginAttemptAsync(ipAddress, username);
                    return new ServiceResult<UserModel>
                    {
                        ErrorMessage = "Invalid credentials",
                        StatusCode = HttpStatusCode.Unauthorized
                    };
                }
            }

            // If the TOTP authentication still has to be setup by the user, return the user and don't fully login, so they get the screen for setting it up with the QR code.
            if (otpStillRequiresSetup)
            {
                return new ServiceResult<UserModel>(user);
            }

            if (generateAuthenticationTokenForCookie)
            {
                user.CookieValue = await GenerateNewCookieTokenAsync(user.Id);
            }

            // Update last login information of the user, if it's not an admin account.
            if (String.IsNullOrWhiteSpace(encryptedAdminAccountId))
            {
                await LogDateAndIpOfLoginAsync(ipAddress, user.Id);
            }

            user.EncryptedLoginLogId = (await LogSuccessfulLoginAttemptAsync(user.Id)).ToString().EncryptWithAes(apiSettings.AdminUsersEncryptionKey, useSlowerButMoreSecureMethod: true);

            await ResetFailedLoginAttemptAsync(username);

            return new ServiceResult<UserModel>(user);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordRequestModel resetPasswordRequestModel, ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("username", resetPasswordRequestModel.Username);
            clientDatabaseConnection.AddParameter("emailAddress", resetPasswordRequestModel.EmailAddress);

            var query = $@"SELECT 
	                        i.id, 
	                        IFNULL(NULLIF(i.title, ''), username.value) AS name, 
	                        username.`value` AS username
                        FROM {WiserTableNames.WiserItem} i
                        JOIN {WiserTableNames.WiserItemDetail} username ON username.item_id = i.id AND username.`key` = '{UserUsernameKey}' AND username.value = ?username
                        JOIN {WiserTableNames.WiserItemDetail} email ON email.item_id = i.id AND email.`key` = 'email_address' AND email.value = ?emailAddress
                        WHERE i.entity_type = '{WiserUserEntityType}'
                        AND i.published_environment > 0";
            var dataTable = await clientDatabaseConnection.GetAsync(query);

            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<bool>(true);
            }

            var password = SecurityHelpers.GenerateRandomPassword(12);
            clientDatabaseConnection.AddParameter("id", dataTable.Rows[0].Field<ulong>("id"));
            clientDatabaseConnection.AddParameter("password", password.ToSha512Simple());
            clientDatabaseConnection.AddParameter("username", dataTable.Rows[0].Field<string>("username"));

            query = $@"INSERT INTO {WiserTableNames.WiserItemDetail} (item_id, `key`, `value`) VALUES (?id, '{UserPasswordKey}', ?password)
                    ON DUPLICATE KEY UPDATE `value` = ?password;

                    INSERT INTO {WiserTableNames.WiserItemDetail} (item_id, `key`, `value`) VALUES (?id, '{UserRequirePasswordChangeKey}', '1')
                    ON DUPLICATE KEY UPDATE `value` = '1';
                    
                    UPDATE {WiserTableNames.WiserLoginAttempts} SET attempts = 0, blocked = 0 WHERE username = ?username;";
            await clientDatabaseConnection.ExecuteAsync(query);

            var mailTemplate = (await templatesService.GetTemplateByNameAsync("Wachtwoord vergeten", true)).ModelObject;
            mailTemplate.Content = mailTemplate.Content.Replace("{username}", resetPasswordRequestModel.Username).Replace("{password}", password).Replace("{subdomain}", resetPasswordRequestModel.SubDomain);

            await communicationsService.SendEmailAsync(resetPasswordRequestModel.EmailAddress, mailTemplate.Subject, mailTemplate.Content);

            return new ServiceResult<bool>(true);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<ValidateCookieModel>> ValidateLoginCookieAsync(string cookieValue, string subDomain = null, string ipAddress = null, string sessionId = null, string encryptedAdminAccountId = null, ClaimsIdentity identity = null)
        {
            if (String.IsNullOrWhiteSpace(cookieValue))
            {
                return new ServiceResult<ValidateCookieModel>
                {
                    ErrorMessage = "Cookie value is required",
                    StatusCode = HttpStatusCode.BadRequest
                };
            }

            var cookieValueParts = cookieValue.Split(':');
            if (cookieValueParts.Length != 2)
            {
                return new ServiceResult<ValidateCookieModel>
                {
                    ErrorMessage = "Invalid cookie value",
                    StatusCode = HttpStatusCode.BadRequest
                };
            }

            // Find the authentication token.
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("selector", cookieValueParts[0]);
            clientDatabaseConnection.AddParameter("now", DateTime.Now);

            var query = $@"SELECT 
                            token.hashed_validator, 
                            token.user_id,
	                        IFNULL(NULLIF(user.title, ''), username.value) AS name, 
                            username.value AS username,
                            last_login_ip.value AS last_login_ip,
                            IF(last_login_date.value IS NULL, ?now, STR_TO_DATE(last_login_date.value, '%Y-%m-%d %H:%i:%s')) AS last_login_date,
                            IFNULL(require_password_change.value, 0) AS require_password_change,
                            email.value AS emailAddress
                        FROM {WiserTableNames.WiserUsersAuthenticationTokens} token
                        JOIN {WiserTableNames.WiserItem} user ON user.id = token.user_id
                        JOIN {WiserTableNames.WiserItemDetail} username ON username.item_id = user.id AND username.`key` = '{UserUsernameKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} last_login_ip ON last_login_ip.item_id = user.id AND last_login_ip.`key` = '{UserLastLoginIpKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} last_login_date ON last_login_date.item_id = user.id AND last_login_date.`key` = '{UserLastLoginDateKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} require_password_change ON require_password_change.item_id = user.id AND require_password_change.`key` = '{UserRequirePasswordChangeKey}'
                        LEFT JOIN {WiserTableNames.WiserItemDetail} email ON email.item_id = user.id AND email.`key` = '{EmailAddressKey}'
                        WHERE token.selector = ?selector
                        AND token.expires > ?now";

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<ValidateCookieModel>(new ValidateCookieModel { Success = false, MessageOrValue = "Invalid cookie or the token expired.", UserData = null });
            }

            // Validate the authentication token.
            var hashedValidator = dataTable.Rows[0].Field<string>("hashed_validator");
            var userId = Convert.ToUInt64(dataTable.Rows[0]["user_id"]);

            if (!cookieValueParts[1].VerifySha512(hashedValidator))
            {
                return new ServiceResult<ValidateCookieModel>(new ValidateCookieModel { Success = false, MessageOrValue = "Invalid cookie token.", UserData = null });
            }

            var dataRow = dataTable.Rows[0];
            Int32.TryParse(dataRow.Field<string>("require_password_change"), out var requirePasswordChange);

            var user = new UserModel
            {
                Id = userId,
                Username = dataRow.Field<string>("username"),
                Name = dataRow.Field<string>("name"),
                LastLoginDate = dataRow.Field<DateTime?>("last_login_date"),
                LastLoginIpAddress = dataRow.Field<string>("last_login_ip"),
                RequirePasswordChange = requirePasswordChange > 0 && String.IsNullOrWhiteSpace(encryptedAdminAccountId), // Only require to change the password if the actual user is logged in.
                EmailAddress = dataRow.Field<string>("emailAddress")
            };

            // Update last login information of the user, if it's not an admin account.
            if (String.IsNullOrWhiteSpace(encryptedAdminAccountId))
            {
                await LogDateAndIpOfLoginAsync(ipAddress, userId);
            }

            user.Password = null;

            await ResetFailedLoginAttemptAsync(user.Username);

            return new ServiceResult<ValidateCookieModel>(new ValidateCookieModel
            {
                Success = true,
                MessageOrValue = userId.ToString().EncryptWithAes(apiSettings.AdminUsersEncryptionKey, useSlowerButMoreSecureMethod: true),
                UserData = user
            });
        }

        /// <inheritdoc />
        public async Task<ServiceResult<UserModel>> ChangePasswordAsync(ClaimsIdentity identity, ChangePasswordModel passwords)
        {
            if (passwords.OldPassword.Equals(passwords.NewPassword))
            {
                return new ServiceResult<UserModel>
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorMessage = "Provided old password and new password are not allowed to match"
                };
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("userId", IdentityHelpers.GetWiserUserId(identity));

            var query = $@"SELECT password.`value` AS password
                        FROM {WiserTableNames.WiserItem} user
                        JOIN {WiserTableNames.WiserItemDetail} password ON password.item_id = user.id AND password.`key` = '{UserPasswordKey}'
                        WHERE user.id = ?userId
                        AND user.entity_type = '{WiserUserEntityType}'
                        AND user.published_environment > 0";

            var dataTable = await clientDatabaseConnection.GetAsync(query);

            if (dataTable.Rows.Count == 0)
            {
                return new ServiceResult<UserModel>
                {
                    StatusCode = HttpStatusCode.NotFound,
                    ErrorMessage = "User not found"
                };
            }

            var oldPasswordHash = dataTable.Rows[0].Field<string>("password");
            if (!passwords.OldPassword.VerifySha512(oldPasswordHash))
            {
                return new ServiceResult<UserModel>
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    ErrorMessage = "Old password is incorrect."
                };
            }

            clientDatabaseConnection.AddParameter("password", passwords.NewPassword.ToSha512Simple());
            query = $@"UPDATE {WiserTableNames.WiserItemDetail} SET value = ?password WHERE item_id = ?userId AND `key` = '{UserPasswordKey}';
                    UPDATE {WiserTableNames.WiserItemDetail} SET value = '0' WHERE item_id = ?userId AND `key` = '{UserRequirePasswordChangeKey}'";
            await clientDatabaseConnection.ExecuteAsync(query);

            return new ServiceResult<UserModel>
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<UserModel>> ChangeEmailAddressAsync(ulong userId, string subDomain, string newEmailAddress, ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("userId", userId);
            clientDatabaseConnection.AddParameter("email", newEmailAddress);
            var query = $@"INSERT INTO {WiserTableNames.WiserItemDetail} (item_id, `key`, value)
                        VALUES (?userId, '{EmailAddressKey}', ?email)
                        ON DUPLICATE KEY UPDATE value = VALUES(value);";
            await clientDatabaseConnection.ExecuteAsync(query);

            return new ServiceResult<UserModel>
            {
                StatusCode = HttpStatusCode.NoContent
            };
        }

        /// <inheritdoc />
        public async Task<ServiceResult<UserModel>> GetUserDataAsync(ClaimsIdentity identity)
        {
            return await GetUserDataAsync(this, identity);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<UserModel>> GetUserDataAsync(IUsersService usersService, ClaimsIdentity identity)
        {
            var customer = await wiserCustomersService.GetSingleAsync(identity);
            if (customer.StatusCode != HttpStatusCode.OK)
            {
                return new ServiceResult<UserModel>
                {
                    ErrorMessage = customer.ErrorMessage,
                    StatusCode = customer.StatusCode
                };
            }

            var encryptionKey = customer.ModelObject.EncryptionKey;
            var userId = IdentityHelpers.GetWiserUserId(identity);

            clientDatabaseConnection.AddParameter("userId", userId);
            var query = $@"SELECT IF(value = '1', TRUE, FALSE) AS totpEnabled FROM {WiserTableNames.WiserItemDetail} WHERE item_id = ?userId AND `key` = '{TotpEnabledKey}'";
            var dataTable = await clientDatabaseConnection.GetAsync(query);
            var totpEnabled = dataTable.Rows.Count > 0 && Convert.ToBoolean(dataTable.Rows[0]["totpEnabled"]);

            var result = new UserModel
            {
                EncryptedId = IdentityHelpers.GetWiserUserId(identity).ToString().EncryptWithAesWithSalt(gclSettings.DefaultEncryptionKey, true),
                EncryptedCustomerId = customer.ModelObject.CustomerId.ToString().EncryptWithAesWithSalt(gclSettings.DefaultEncryptionKey, true),
                ZeroEncrypted = "0".EncryptWithAesWithSalt(encryptionKey, true),
                Id = userId,
                EmailAddress = await usersService.GetUserEmailAddressAsync(userId),
                CurrentBranchName = customer.ModelObject.Name,
                CurrentBranchId = customer.ModelObject.Id,
                CurrentBranchIsMainBranch = customer.ModelObject.Id == customer.ModelObject.CustomerId,
                TotpAuthentication = new TotpAuthenticationModel
                {
                    Enabled = totpEnabled
                }
            };

            if (result.CurrentBranchIsMainBranch)
            {
                result.MainBranchName = result.CurrentBranchName;
            }
            else
            {
                var productionCustomer = await wiserCustomersService.GetSingleAsync(customer.ModelObject.CustomerId);
                if (productionCustomer.StatusCode != HttpStatusCode.OK)
                {
                    return new ServiceResult<UserModel>
                    {
                        ErrorMessage = productionCustomer.ErrorMessage,
                        StatusCode = productionCustomer.StatusCode
                    };
                }

                result.MainBranchName = productionCustomer.ModelObject.Name;
            }

            var wiserSettings = await usersService.GetWiserSettingsForUserAsync(encryptionKey);
            result.FilesRootId = wiserSettings.FilesRootId;
            result.PlainFilesRootId = wiserSettings.PlainFilesRootId;
            result.ImagesRootId = wiserSettings.ImagesRootId;
            result.PlainImagesRootId = wiserSettings.PlainImagesRootId;
            result.TemplatesRootId = wiserSettings.TemplatesRootId;
            result.PlainTemplatesRootId = wiserSettings.PlainTemplatesRootId;
            result.MainDomain = wiserSettings.MainDomain;

            return new ServiceResult<UserModel>(result);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetSettingsAsync(ClaimsIdentity identity, string groupName, string uniqueKey, string defaultValue = null)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("userId", IdentityHelpers.GetWiserUserId(identity));
            clientDatabaseConnection.AddParameter("group", groupName);
            clientDatabaseConnection.AddParameter("key", uniqueKey);

            var query = $@"SELECT settings.long_value
                        FROM {WiserTableNames.WiserItem} AS `user`
                        JOIN {WiserTableNames.WiserItemDetail} AS settings ON settings.item_id = `user`.id AND settings.`key` = ?key AND settings.groupname = ?group
                        WHERE `user`.id = ?userId AND `user`.entity_type = '{WiserUserEntityType}'";

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            return new ServiceResult<string>(dataTable.Rows.Count == 0 ? defaultValue : dataTable.Rows[0].Field<string>("long_value"));
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetGridSettingsAsync(ClaimsIdentity identity, string uniqueKey)
        {
            return await GetSettingsAsync(identity, UserGridSettingsGroupName, uniqueKey);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<int>>> GetPinnedModulesAsync(ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("userId", IdentityHelpers.GetWiserUserId(identity));

            var query = $@" SELECT pinned.`value` AS pinnedModules
                            FROM {WiserTableNames.WiserItemDetail} AS pinned
                            WHERE pinned.item_id = ?userId
                            AND pinned.`key` = '{UserPinnedModulesKey}'";

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            var pinnedModules = new List<int>();
            if (dataTable.Rows.Count > 0)
            {
                var pinned = dataTable.Rows[0].Field<string>("pinnedModules");
                if (!String.IsNullOrWhiteSpace(pinned))
                {
                    pinnedModules = pinned.Split(',').Select(Int32.Parse).ToList();
                }
            }

            return new ServiceResult<List<int>>(pinnedModules);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<int>>> GetAutoLoadModulesAsync(ClaimsIdentity identity)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("userId", IdentityHelpers.GetWiserUserId(identity));

            var query = $@" SELECT autoLoad.`value` AS autoLoadModules
                            FROM {WiserTableNames.WiserItemDetail} AS autoLoad
                            WHERE autoLoad.item_id = ?userId
                            AND autoLoad.`key` = '{UserAutoLoadModulesKey}'";

            var dataTable = await clientDatabaseConnection.GetAsync(query);
            var autoLoadModules = new List<int>();
            if (dataTable.Rows.Count > 0)
            {
                var autoLoad = dataTable.Rows[0].Field<string>("autoLoadModules");
                if (!String.IsNullOrWhiteSpace(autoLoad))
                {
                    autoLoadModules = autoLoad.Split(',').Select(Int32.Parse).ToList();
                }
            }

            return new ServiceResult<List<int>>(autoLoadModules);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> SaveSettingsAsync(ClaimsIdentity identity, string groupName, string uniqueKey, JToken settings)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("userId", IdentityHelpers.GetWiserUserId(identity));
            clientDatabaseConnection.AddParameter("key", uniqueKey);
            clientDatabaseConnection.AddParameter("group", groupName);
            clientDatabaseConnection.AddParameter("settings", settings?.ToString(Formatting.None));

            var query = $@"INSERT INTO {WiserTableNames.WiserItemDetail} (item_id, groupname, `key`, long_value)
                        VALUES (?userId, ?group, ?key, ?settings)
                        ON DUPLICATE KEY UPDATE long_value = VALUES(long_value)";
            await clientDatabaseConnection.ExecuteAsync(query);

            return new ServiceResult<bool>(true);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> SaveGridSettingsAsync(ClaimsIdentity identity, string uniqueKey, JToken settings)
        {
            return await SaveSettingsAsync(identity, UserGridSettingsGroupName, uniqueKey, settings);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> SavePinnedModulesAsync(ClaimsIdentity identity, List<int> moduleIds)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("userId", IdentityHelpers.GetWiserUserId(identity));

            var query = $@"INSERT INTO {WiserTableNames.WiserItemDetail} (item_id, `key`, groupname, value)
                        VALUES (?userId, '{UserPinnedModulesKey}', '{UserModuleSettingsGroupName}', '{String.Join(",", moduleIds)}')
                        ON DUPLICATE KEY UPDATE value = VALUES(value)";
            await clientDatabaseConnection.ExecuteAsync(query);

            return new ServiceResult<bool>(true);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> SaveAutoLoadModulesAsync(ClaimsIdentity identity, List<int> moduleIds)
        {
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("userId", IdentityHelpers.GetWiserUserId(identity));

            var query = $@"INSERT INTO {WiserTableNames.WiserItemDetail} (item_id, `key`, groupname, value)
                        VALUES (?userId, '{UserAutoLoadModulesKey}', '{UserModuleSettingsGroupName}', '{String.Join(",", moduleIds)}')
                        ON DUPLICATE KEY UPDATE value = VALUES(value)";
            await clientDatabaseConnection.ExecuteAsync(query);

            return new ServiceResult<bool>(true);
        }

        /// <inheritdoc />
        public async Task<string> GetUserEmailAddressAsync(ulong id)
        {
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("id", id);
            var dataTable = await clientDatabaseConnection.GetAsync($@"SELECT email.value
                                                                            FROM {WiserTableNames.WiserItem} AS item 
                                                                            JOIN {WiserTableNames.WiserItemDetail} email ON email.item_id = item.id AND email.`key` = '{EmailAddressKey}'
                                                                            WHERE item.id = ?id");
            return dataTable.Rows.Count > 0 ? dataTable.Rows[0].Field<string>("value") : null;
        }

        /// <inheritdoc />
        public async Task<UserModel> GetWiserSettingsForUserAsync(string encryptionKey)
        {
            var result = new UserModel();
            var dataTable = await clientDatabaseConnection.GetAsync("SELECT `key`, `value` FROM easy_objects WHERE `key` IN ('W2FilesRootId', 'W2ImagesRootId', 'W2TemplatesRootId', 'maindomain', 'maindomain_wiser', 'requiressl')");
            if (dataTable.Rows.Count == 0)
            {
                return result;
            }

            var mainDomainWiser = "";
            var mainDomain = "";
            var requireSsl = false;
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var key = dataRow.Field<string>("key");
                var savedValue = dataRow.Field<string>("value");
                UInt64.TryParse(savedValue, out var value);

                var encryptedValue = value.ToString().EncryptWithAesWithSalt(encryptionKey, true);
                switch (key?.ToLowerInvariant())
                {
                    case "w2filesrootid":
                        result.FilesRootId = encryptedValue;
                        result.PlainFilesRootId = value;
                        break;
                    case "w2imagesrootid":
                        result.ImagesRootId = encryptedValue;
                        result.PlainImagesRootId = value;
                        break;
                    case "w2templatesrootid":
                        result.TemplatesRootId = encryptedValue;
                        result.PlainTemplatesRootId = value;
                        break;
                    case "maindomain_wiser":
                        mainDomainWiser = savedValue;
                        break;
                    case "maindomain":
                        mainDomain = savedValue;
                        break;
                    case "requiressl":
                        requireSsl = String.Equals("true", savedValue, StringComparison.OrdinalIgnoreCase);
                        break;
                }
            }

            result.MainDomain = !String.IsNullOrWhiteSpace(mainDomainWiser) ? mainDomainWiser : mainDomain;
            if (!String.IsNullOrWhiteSpace(result.MainDomain) && !result.MainDomain.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                result.MainDomain = $"{(requireSsl ? "https" : "http")}://{result.MainDomain}";
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<string> GenerateAndSaveNewRefreshTokenAsync(string cookieSelector, string subDomain, string ticket)
        {
            string refreshToken;
            using (var rng = RandomNumberGenerator.Create())
            {
                var data = new byte[30];
                rng.GetBytes(data);
                refreshToken = Convert.ToBase64String(data);
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("token", StringHelpers.CreateSha512Hash(refreshToken, String.Empty));
            clientDatabaseConnection.AddParameter("ticket", ticket);
            clientDatabaseConnection.AddParameter("expires", DateTime.Now.AddYears(1));
            clientDatabaseConnection.AddParameter("cookieSelector", cookieSelector);
            await clientDatabaseConnection.ExecuteAsync($"UPDATE {WiserTableNames.WiserUsersAuthenticationTokens} SET refresh_token = ?token, ticket = ?ticket, refresh_token_expires = ?expires WHERE selector = ?cookieSelector");

            return refreshToken;
        }

        /// <inheritdoc />
        public async Task<string> UseRefreshTokenAsync(string subDomain, string refreshToken)
        {
            // Get the ticket that corresponds with the refresh token.
            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("token", StringHelpers.CreateSha512Hash(refreshToken, String.Empty));
            clientDatabaseConnection.AddParameter("now", DateTime.Now);
            var dataTable = await clientDatabaseConnection.GetAsync($"SELECT ticket FROM {WiserTableNames.WiserUsersAuthenticationTokens} WHERE refresh_token = ?token AND refresh_token_expires > ?now");

            var result = dataTable.Rows.Count == 0 ? null : dataTable.Rows[0].Field<string>("ticket");

            // Delete the refresh token, so that it can only be used once.
            await clientDatabaseConnection.ExecuteAsync($"UPDATE {WiserTableNames.WiserUsersAuthenticationTokens} SET refresh_token = NULL, ticket = NULL, refresh_token_expires = NULL WHERE refresh_token = ?token");

            return result;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<long>> UpdateUserTimeActiveAsync(ClaimsIdentity identity, string encryptedLoginLogId)
        {
            var result = new ServiceResult<long>();

            // Try to decrypt the encrypted log ID.
            string decryptedLogIdAsString;
            try
            {
                decryptedLogIdAsString = encryptedLoginLogId.DecryptWithAes(apiSettings.AdminUsersEncryptionKey, useSlowerButMoreSecureMethod: true);
            }
            catch (Exception exception)
            {
                logger.LogError($"UpdateUserTimeActiveAsync: Error trying to decrypt encrypted log ID '{encryptedLoginLogId}': {exception}");
                result.StatusCode = HttpStatusCode.BadRequest;
                result.ErrorMessage = $"Error trying to decrypt encrypted log ID '{encryptedLoginLogId}'";
                return result;
            }

            // Now try to parse the decrypted log ID to a UInt64.
            if (!UInt64.TryParse(decryptedLogIdAsString, out var decryptedLogId))
            {
                logger.LogError($"UpdateUserTimeActiveAsync: Could not parse log ID '{decryptedLogId}' to a UInt64.");
                result.StatusCode = HttpStatusCode.BadRequest;
                result.ErrorMessage = $"Error trying to decrypt encrypted log ID '{encryptedLoginLogId}'";
                return result;
            }

            // If the parsing was successful, but the ID is 0.
            if (decryptedLogId == 0)
            {
                logger.LogError("UpdateUserTimeActiveAsync: Log ID should be higher than 0.");
                result.StatusCode = HttpStatusCode.BadRequest;
                result.ErrorMessage = "Log ID should be higher than 0";
                return result;
            }

            // Log ID parsed successfully and is higher than 0; proceed with function.
            var userId = IdentityHelpers.GetWiserUserId(identity);

            try
            {
                await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

                // Make sure the WiserLoginLog exists and is up-to-date.
                await KeepTablesUpToDateAsync(clientDatabaseConnection.ConnectedDatabaseForWriting ?? clientDatabaseConnection.ConnectedDatabase);
                await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {WiserTableNames.WiserLoginLog});

                clientDatabaseConnection.ClearParameters();
                clientDatabaseConnection.AddParameter("id", decryptedLogId);
                clientDatabaseConnection.AddParameter("user_id", userId);
                var timeActiveData = await clientDatabaseConnection.GetAsync($"SELECT time_active_in_seconds, time_active_changed_on FROM {WiserTableNames.WiserLoginLog} WHERE id = ?id AND user_id = ?user_id");
                if (timeActiveData.Rows.Count == 0)
                {
                    logger.LogError("UpdateUserTimeActiveAsync: Log ID '{decryptedLogId}' does not exist or doesn't belong to the current user.", decryptedLogId);
                    result.StatusCode = HttpStatusCode.BadRequest;
                    result.ErrorMessage = $"Log ID '{decryptedLogId}' does not exist or doesn't belong to the current user";
                    return result;
                }

                var timeActiveChangedOn = timeActiveData.Rows[0].Field<DateTime>("time_active_changed_on");
                var currentTimeActive = timeActiveData.Rows[0].Field<long>("time_active_in_seconds");
                var timeActive = currentTimeActive + Convert.ToInt64(Math.Floor(DateTime.Now.Subtract(timeActiveChangedOn).TotalSeconds));

                clientDatabaseConnection.ClearParameters();
                clientDatabaseConnection.AddParameter("time_active_in_seconds", timeActive);
                clientDatabaseConnection.AddParameter("time_active_changed_on", DateTime.Now);
                await clientDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.WiserLoginLog, decryptedLogId);

                result.ModelObject = timeActive;
                result.StatusCode = HttpStatusCode.OK;
                return result;
            }
            catch (Exception exception)
            {
                logger.LogError($"Error while updating user active time: {exception}");
                result.StatusCode = HttpStatusCode.BadRequest;
                result.ErrorMessage = "Error while updating user active time";
                return result;
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> ResetTimeActiveChangedAsync(ClaimsIdentity identity, string encryptedLoginLogId)
        {
            var result = new ServiceResult<bool>();

            // Try to decrypt the encrypted log ID.
            string decryptedLogIdAsString;
            try
            {
                decryptedLogIdAsString = encryptedLoginLogId.DecryptWithAes(apiSettings.AdminUsersEncryptionKey, useSlowerButMoreSecureMethod: true);
            }
            catch (Exception exception)
            {
                logger.LogError($"ResetTimeActiveChangedAsync: Error trying to decrypt encrypted log ID '{encryptedLoginLogId}': {exception}");
                result.StatusCode = HttpStatusCode.BadRequest;
                result.ErrorMessage = $"Error trying to decrypt encrypted log ID '{encryptedLoginLogId}'";
                return result;
            }

            // Now try to parse the decrypted log ID to a UInt64.
            if (!UInt64.TryParse(decryptedLogIdAsString, out var decryptedLoginLogId))
            {
                logger.LogError($"ResetTimeActiveChangedAsync: Could not parse log ID '{decryptedLoginLogId}' to a UInt64.");
                result.StatusCode = HttpStatusCode.BadRequest;
                result.ErrorMessage = $"Error trying to decrypt encrypted log ID '{encryptedLoginLogId}'";
                return result;
            }

            // If the parsing was successful, but the ID is 0.
            if (decryptedLoginLogId == 0)
            {
                logger.LogError("ResetTimeActiveChangedAsync: Log ID should be higher than 0.");
                result.StatusCode = HttpStatusCode.BadRequest;
                result.ErrorMessage = "Log ID should be higher than 0";
                return result;
            }

            // Log ID parsed successfully and is higher than 0; proceed with function.
            var userId = IdentityHelpers.GetWiserUserId(identity);

            try
            {
                await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

                // Make sure the WiserLoginLog exists and is up-to-date.
                await KeepTablesUpToDateAsync(clientDatabaseConnection.ConnectedDatabaseForWriting ?? clientDatabaseConnection.ConnectedDatabase);
                await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {WiserTableNames.WiserLoginLog});

                clientDatabaseConnection.ClearParameters();
                clientDatabaseConnection.AddParameter("id", decryptedLoginLogId);
                clientDatabaseConnection.AddParameter("user_id", userId);
                var currentTimeActive = await clientDatabaseConnection.GetAsync($"SELECT id FROM {WiserTableNames.WiserLoginLog} WHERE id = ?id AND user_id = ?user_id");
                if (currentTimeActive.Rows.Count == 0)
                {
                    logger.LogError($"ResetTimeActiveChangedAsync: Log ID '{decryptedLoginLogId}' does not exist or doesn't belong to the current user.");
                    result.StatusCode = HttpStatusCode.BadRequest;
                    result.ErrorMessage = $"Log ID '{decryptedLoginLogId}' does not exist or doesn't belong to the current user";
                    return result;
                }

                clientDatabaseConnection.ClearParameters();
                clientDatabaseConnection.AddParameter("time_active_changed_on", DateTime.Now);
                await clientDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.WiserLoginLog, decryptedLoginLogId);

                result.ModelObject = true;
                result.StatusCode = HttpStatusCode.OK;
                return result;
            }
            catch (Exception exception)
            {
                logger.LogError($"Error while updating time active changed on timestamp: {exception}");
                result.StatusCode = HttpStatusCode.BadRequest;
                result.ErrorMessage = "Error while updating time active changed on timestamp";
                return result;
            }
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<RoleModel>>> GetRolesAsync(bool includePermissions = false)
        {
            var results = await rolesService.GetRolesAsync(includePermissions);
            return new ServiceResult<List<RoleModel>>(results);
        }

        /// <inheritdoc />
        public async Task<ServiceResult<string>> GetDashboardSettingsAsync(ClaimsIdentity identity)
        {
            return await GetSettingsAsync(identity, String.Empty, UserDashboardSettingsKey, "[]");
        }

        /// <inheritdoc />
        public async Task<ServiceResult<bool>> SaveDashboardSettingsAsync(ClaimsIdentity identity, JToken settings)
        {
            return await SaveSettingsAsync(identity, String.Empty, UserDashboardSettingsKey, settings);
        }

        /// <summary>
        /// Validates an encrypted admin account ID.
        /// It checks if it contains a valid integer and then checks in the database if that ID actually exists and whether that user is still active.
        /// </summary>
        /// <param name="encryptedAdminAccountId">The encrypted admin account ID.</param>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user to check for rights.</param>
        /// <returns>A boolean indicating whether this admin account is valid and still active.</returns>
        private async Task<bool> ValidateAdminAccountAsync(string encryptedAdminAccountId, ClaimsIdentity identity)
        {
            if (String.IsNullOrWhiteSpace(encryptedAdminAccountId))
            {
                return false;
            }

            UInt64.TryParse(encryptedAdminAccountId.DecryptWithAes(apiSettings.AdminUsersEncryptionKey, useSlowerButMoreSecureMethod: true), out var decryptedAdminAccountId);

            return (await ValidateAdminAccountIdAsync(decryptedAdminAccountId, identity)).ModelObject;
        }

        /// <summary>
        /// Validates the ID of an admin account, to check if that user exists and is still active.
        /// </summary>
        /// <param name="id">The ID of the admin account.</param>
        /// <param name="identity">Optional: The <see cref="ClaimsIdentity"/> of the authenticated user.</param>
        /// <returns>A boolean, indicating whether this admin account is allowed to login or not.</returns>
        private async Task<ServiceResult<bool>> ValidateAdminAccountIdAsync(ulong id, ClaimsIdentity identity = null)
        {
            // If the authenticated user is not an administrator, don't return sensitive information.
            if (identity != null && !IdentityHelpers.IsAdministrator(identity))
            {
                return new ServiceResult<bool>
                {
                    ErrorMessage = "Only administrators are allowed to do this.",
                    StatusCode = HttpStatusCode.Unauthorized
                };
            }

            wiserDatabaseConnection.ClearParameters();
            wiserDatabaseConnection.AddParameter("id", id);

            var query = $@"SELECT NULL FROM {WiserTableNames.WiserItem} AS account
                        LEFT JOIN {WiserTableNames.WiserItemDetail} AS active ON active.item_id = account.id AND active.`key` = '{UserActiveKey}'
                        WHERE account.id = ?id
                        AND account.entity_type = '{WiserUserEntityType}'
                        AND (active.value IS NULL OR active.value = '1')";
            var result = await wiserDatabaseConnection.GetAsync(query);
            return new ServiceResult<bool>(result.Rows.Count > 0);
        }

        /// <summary>
        /// Generates a new token for a "remember me" cookie.
        /// </summary>
        /// <param name="userId">The ID of the user to generate the token for.</param>
        /// <returns>The value that should be saved in the cookie.</returns>
        private async Task<string> GenerateNewCookieTokenAsync(ulong userId)
        {
            var validator = SecurityHelpers.GenerateRandomPassword(30);
            var selector = SecurityHelpers.GenerateRandomPassword(30);

            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("selector", selector);
            clientDatabaseConnection.AddParameter("hashed_validator", validator.ToSha512Simple());
            clientDatabaseConnection.AddParameter("user_id", userId);
            clientDatabaseConnection.AddParameter("expires", DateTime.Now.AddYears(1));
            await clientDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.WiserUsersAuthenticationTokens, 0);

            return $"{selector}:{validator}";
        }

        /// <summary>
        /// Check in the database whether a certain username is blocked from logging in as a customer or admin account.
        /// </summary>
        /// <param name="username">The username to check.</param>
        /// <param name="connection"></param>
        /// <param name="maximumLoginAttemptsAllowed"></param>
        /// <param name="loginAttemptsTableName"></param>
        /// <returns>true = blocked, false = not blocked</returns>
        private static async Task<bool> UsernameIsBlockedAsync(string username, IDatabaseConnection connection, int maximumLoginAttemptsAllowed, string loginAttemptsTableName = WiserTableNames.WiserLoginAttempts)
        {
            if (String.IsNullOrWhiteSpace(username))
            {
                // Can't do anything if we have no username.
                return false;
            }

            await connection.EnsureOpenConnectionForReadingAsync();
            connection.ClearParameters();
            connection.AddParameter("username", username);

            var query = $"SELECT attempts, blocked FROM {loginAttemptsTableName} WHERE username = ?username";
            var dataTable = await connection.GetAsync(query);

            if (dataTable.Rows.Count == 0)
            {
                // No results means that this is it's first attempt and this it can't be blocked.
                return false;
            }

            var isBlocked = dataTable.Rows[0].Field<bool>("blocked");
            if (isBlocked)
            {
                // Already blocked before, no need to count the attempts again.
                return true;
            }

            var attempts = dataTable.Rows[0].Field<int>("attempts");
            if (maximumLoginAttemptsAllowed == 0 || attempts < maximumLoginAttemptsAllowed)
            {
                // Is still allowed more attempts.
                return false;
            }

            // Maximum attempts reached, but not blocked yet. Block it now.
            query = $"UPDATE {loginAttemptsTableName} SET blocked = 1 WHERE username = ?username";
            await connection.ExecuteAsync(query);

            return true;
        }

        /// <summary>
        /// Increases the failed login attempts counter for an IP address in the database.
        /// </summary>
        /// <param name="ipAddress">The IP address that is trying to login.</param>
        /// <param name="username">The username that is trying to login.</param>
        private async Task AddFailedLoginAttemptAsync(string ipAddress, string username)
        {
            if (String.IsNullOrWhiteSpace(ipAddress) && String.IsNullOrWhiteSpace(username))
            {
                // Can't do anything if we have no data.
                return;
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("ipAddress", ipAddress ?? "");
            clientDatabaseConnection.AddParameter("username", username);

            var query = $@"INSERT INTO {WiserTableNames.WiserLoginAttempts} (ip_address, attempts, username) VALUES (?ipAddress, 1, ?username)
                        ON DUPLICATE KEY UPDATE attempts = attempts + 1, ip_address = ?ipAddress";
            await clientDatabaseConnection.ExecuteAsync(query);
        }

        /// <summary>
        /// Reset the failed login attempts counter for an username in the database.
        /// </summary>
        /// <param name="username">The username that is trying to login.</param>
        private async Task ResetFailedLoginAttemptAsync(string username)
        {
            if (String.IsNullOrWhiteSpace(username))
            {
                // Can't do anything if we have no username.
                return;
            }

            await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
            clientDatabaseConnection.ClearParameters();
            clientDatabaseConnection.AddParameter("username", username);

            var query = $@"UPDATE {WiserTableNames.WiserLoginAttempts} SET attempts = 0 WHERE username = ?username";
            await clientDatabaseConnection.ExecuteAsync(query);
        }

        /// <summary>
        /// Logs the current date time and the IP address of the user at the moment they logged in.
        /// </summary>
        /// <param name="ipAddress">The IP address of the user.</param>
        /// <param name="userId">The ID of the user.</param>
        private async Task LogDateAndIpOfLoginAsync(string ipAddress, ulong userId)
        {
            try
            {
                var query = $@"INSERT INTO {WiserTableNames.WiserItemDetail} (item_id, `key`, value)
                            VALUES (?id, '{UserLastLoginDateKey}', ?now), (?id, '{UserLastLoginIpKey}', ?ip)
                            ON DUPLICATE KEY UPDATE value = VALUES(`value`);";

                await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();
                clientDatabaseConnection.ClearParameters();
                clientDatabaseConnection.AddParameter("ip", ipAddress ?? "");
                clientDatabaseConnection.AddParameter("id", userId);
                clientDatabaseConnection.AddParameter("now", DateTime.Now);
                await clientDatabaseConnection.ExecuteAsync(query);
            }
            catch (Exception exception)
            {
                logger.LogError($"Error while updating last login information of user: {exception}");
            }
        }

        /// <summary>
        /// Add an entry to the login log table.
        /// </summary>
        /// <param name="userId"></param>
        /// <exception cref="ArgumentException"></exception>
        private async Task<ulong> LogSuccessfulLoginAttemptAsync(ulong userId)
        {
            if (userId == 0)
            {
                throw new ArgumentException("User ID must be higher than 0.", nameof(userId));
            }

            try
            {
                await clientDatabaseConnection.EnsureOpenConnectionForReadingAsync();

                // Make sure the WiserLoginLog exists and is up-to-date.
                await KeepTablesUpToDateAsync(clientDatabaseConnection.ConnectedDatabaseForWriting ?? clientDatabaseConnection.ConnectedDatabase);
                await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> {WiserTableNames.WiserLoginLog});

                clientDatabaseConnection.ClearParameters();
                clientDatabaseConnection.AddParameter("user_id", userId);
                clientDatabaseConnection.AddParameter("added_on", DateTime.Now);
                clientDatabaseConnection.AddParameter("time_active_changed_on", DateTime.Now);

                var logId = await clientDatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.WiserLoginLog, 0UL);

                // Remove outdated logs (data is retained for 3 months).
                await clientDatabaseConnection.ExecuteAsync($"DELETE FROM {WiserTableNames.WiserLoginLog} WHERE added_on < DATE_SUB(NOW(), INTERVAL 3 MONTH)");

                return logId;
            }
            catch (Exception exception)
            {
                logger.LogError($"Error while logging successful login attempt: {exception}");
            }

            return 0UL;
        }

        /// <inheritdoc />
        public bool ValidateTotpPin(string key, string code)
        {
            var twoFactorAuthenticator = new TwoFactorAuthenticator();
            return twoFactorAuthenticator.ValidateTwoFactorPIN(key, code);
        }

        /// <inheritdoc />
        public string SetUpTotpAuthentication(string account, string key)
        {
            var twoFactorAuthenticator = new TwoFactorAuthenticator();
            var setupInfo = twoFactorAuthenticator.GenerateSetupCode("Wiser", account, key, false, 3);
            return setupInfo.QrCodeSetupImageUrl;
        }

        /// <inheritdoc />
        public async Task<ServiceResult<List<string>>> GenerateTotpBackupCodesAsync(ClaimsIdentity identity)
        {
            // First generate new codes.
            var results = new List<string>();
            for (var index = 0; index < AmountOfBackupCodesToGenerate; index++)
            {
                results.Add(SecurityHelpers.GenerateRandomPassword(8));
            }

            // Delete any remaining backup codes from the user.
            clientDatabaseConnection.AddParameter("groupName", TotpBackupCodesGroupName);
            clientDatabaseConnection.AddParameter("userId", IdentityHelpers.GetWiserUserId(identity));
            var query = $@"DELETE FROM {WiserTableNames.WiserItemDetail} WHERE item_id = ?userId AND groupname = ?groupName";
            await clientDatabaseConnection.ExecuteAsync(query);

            // Hash the new backup codes and then save them in the database.
            var queryBuilder = new List<string>();
            for (var index = 0; index < results.Count; index++)
            {
                var backupCode = results[index];
                clientDatabaseConnection.AddParameter($"key{index}", index);
                clientDatabaseConnection.AddParameter($"code{index}", backupCode.ToSha512ForPasswords());
                queryBuilder.Add($"(?userId, ?groupName, ?key{index}, ?code{index})");
            }

            query = $@"INSERT INTO {WiserTableNames.WiserItemDetail} (`item_id`, `groupname`, `key`, `value`)
VALUES {String.Join(", ", queryBuilder)}";
            await clientDatabaseConnection.ExecuteAsync(query);

            // Return the new backup codes to the user, this is the only time that the user can see them!
            return new ServiceResult<List<string>>(results);
        }

        /// <summary>
        /// Handle TOTP authentication for users and admins.
        /// </summary>
        /// <param name="totpPin">The TOTP PIN that the user entered, if any.</param>
        /// <param name="totpBackupCode">The TOTP backup code that the user entered, if any.</param>
        /// <param name="totpAuthentication">The <see cref="TotpAuthenticationModel"/> with the TOTP settings.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="userName">The full name of the user</param>
        /// <param name="connectionToUse">The database connection to use (wiserDatabaseConnection for admins and clientDatabaseConnection for normal users).</param>
        /// <returns></returns>
        private async Task<(bool Success, bool userIsStillSettingUp)> HandleTotpAuthenticationAsync(string totpPin, string totpBackupCode, TotpAuthenticationModel totpAuthentication, ulong userId, string userName, IDatabaseConnection connectionToUse)
        {
            if (!totpAuthentication.Enabled)
            {
                return (true, false);
            }

            string query;
            if (totpAuthentication.RequiresSetup && String.IsNullOrWhiteSpace(totpPin))
            {
                // If no PIN has been entered, it means the user still needs to scan the QR code for the setup, so generate that here.
                totpAuthentication.SecretKey = SecurityHelpers.GenerateRandomPassword(30);
                totpAuthentication.QrImageUrl = SetUpTotpAuthentication(userName, totpAuthentication.SecretKey);

                // Save the secret key in the user's account.
                connectionToUse.AddParameter("totpSecretKey", totpAuthentication.SecretKey.EncryptWithAes(gclSettings.DefaultEncryptionKey, useSlowerButMoreSecureMethod: true));
                connectionToUse.AddParameter("userId", userId);
                query = $@"INSERT INTO {WiserTableNames.WiserItemDetail} (item_id, `key`, `value`) VALUES (?userId, '{TotpSecretKey}', ?totpSecretKey)
ON DUPLICATE KEY UPDATE `value` = VALUES(value);";
                await connectionToUse.ExecuteAsync(query);

                return (true, true);
            }

            // If the user entered a backup code, check if it's valid and if so, allow the user to login once with this code and then delete the code.
            if (!String.IsNullOrWhiteSpace(totpBackupCode))
            {
                connectionToUse.AddParameter("userId", userId);
                connectionToUse.AddParameter("groupName", TotpBackupCodesGroupName);
                query = $@"SELECT id, value FROM {WiserTableNames.WiserItemDetail} WHERE item_id = ?userId AND groupname = ?groupName";
                var dataTable = await connectionToUse.GetAsync(query);
                if (dataTable.Rows.Count == 0)
                {
                    // User has no more backup codes, so deny the login attempt.
                    return (false, false);
                }

                var validCodeId = 0UL;
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var rowId = dataRow.Field<ulong>("id");
                    var databaseBackupCode = dataRow.Field<string>("value");
                    // ReSharper disable once InvertIf
                    if (totpBackupCode.VerifySha512(databaseBackupCode))
                    {
                        validCodeId = rowId;
                        break;
                    }
                }

                // The backup code that the user entered was not found, so deny the login attempt.
                if (validCodeId == 0)
                {
                    return (false, false);
                }

                // Backup code found, delete the code and allow the login attempt.
                connectionToUse.AddParameter("detailId", validCodeId);
                query = $"DELETE FROM {WiserTableNames.WiserItemDetail} WHERE id = ?detailId";
                await connectionToUse.ExecuteAsync(query);

                return (true, false);
            }

            // If we do have a PIN, validate it.
            var success = ValidateTotpPin(totpAuthentication.SecretKey.DecryptWithAes(gclSettings.DefaultEncryptionKey, useSlowerButMoreSecureMethod: true), totpPin);
            if (!success)
            {
                // Invalid PIN, return unauthorized error.
                return (false, true);
            }

            // Correct PIN, set requires setup to false.
            connectionToUse.AddParameter("userId", userId);
            query = $@"INSERT INTO {WiserTableNames.WiserItemDetail} (item_id, `key`, `value`) VALUES (?userId, '{TotpRequiresSetupKey}', '0')
ON DUPLICATE KEY UPDATE `value` = VALUES(value);";
            await connectionToUse.ExecuteAsync(query);

            return (true, false);
        }

        /// <summary>
        /// Checks if the MySQL tables for the login log is up-to-date.
        /// </summary>
        private async Task KeepTablesUpToDateAsync(string databaseName)
        {
            var lastTableUpdates = await databaseHelpersService.GetLastTableUpdatesAsync(databaseName);

            // Check if the login log table needs to be updated.
            if (!lastTableUpdates.ContainsKey(WiserTableNames.WiserLoginLog) || lastTableUpdates[WiserTableNames.WiserLoginLog] < new DateTime(2023, 2, 23))
            {
                // Add column.
                var column = new ColumnSettingsModel("time_active_in_seconds", MySqlDbType.Int64, notNull: true, defaultValue: "0", addAfterColumnName: "user_id");
                await databaseHelpersService.AddColumnToTableAsync(WiserTableNames.WiserLoginLog, column, false, databaseName);

                // Convert and drop the "time_active" column if it still exists.
                if (await databaseHelpersService.ColumnExistsAsync(WiserTableNames.WiserLoginLog, "time_active", databaseName))
                {
                    await ConvertTimeSpanToSecondsAsync(WiserTableNames.WiserLoginLog, databaseName, "time_active", "time_active_in_seconds");
                    await databaseHelpersService.DropColumnAsync(WiserTableNames.WiserLoginLog, "time_active", databaseName);
                }

                clientDatabaseConnection.ClearParameters();
                clientDatabaseConnection.AddParameter("tableName", WiserTableNames.WiserLoginLog);
                clientDatabaseConnection.AddParameter("lastUpdate", DateTime.Now);
                var lastUpdateData = await clientDatabaseConnection.GetAsync("SELECT `name` FROM `{WiserTableChanges}` WHERE `name` = ?tableName");
                if (lastUpdateData.Rows.Count == 0)
                {
                    await clientDatabaseConnection.ExecuteAsync($"INSERT INTO `{WiserTableNames.WiserTableChanges}` (`name`, last_update) VALUES (?tableName, ?lastUpdate)");
                }
                else
                {
                    await clientDatabaseConnection.ExecuteAsync($"UPDATE `{WiserTableNames.WiserTableChanges}` SET last_update = ?lastUpdate WHERE `name` = ?tableName LIMIT 1");
                }
            }
        }

        private async Task ConvertTimeSpanToSecondsAsync(string tableName, string databaseName, string oldColumnName, string newColumnName)
        {
            var queryDatabasePart = !String.IsNullOrWhiteSpace(databaseName) ? $"`{databaseName}`." : String.Empty;

            var convertDataTable = await clientDatabaseConnection.GetAsync($"SELECT id, `{oldColumnName}` FROM {queryDatabasePart}`{tableName}`");
            foreach (var dataRow in convertDataTable.Rows.Cast<DataRow>())
            {
                var seconds = Convert.ToInt32(Math.Floor(dataRow.Field<TimeSpan>(oldColumnName).TotalSeconds));

                clientDatabaseConnection.ClearParameters();
                clientDatabaseConnection.AddParameter("tableId", Convert.ToUInt64(dataRow["id"]));
                clientDatabaseConnection.AddParameter("seconds", seconds);
                await clientDatabaseConnection.ExecuteAsync($"UPDATE `{tableName}` SET `{newColumnName}` = ?seconds WHERE id = ?tableId");
            }
        }
    }
}