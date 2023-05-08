using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Api.Core.Models;
using Api.Core.Services;
using Api.Modules.Customers.Models;
using Api.Modules.Items.Models;
using GeeksCoreLibrary.Core.Models;
using Newtonsoft.Json.Linq;

namespace Api.Modules.Customers.Interfaces
{
    /// <summary>
    /// Interface for operations related to Wiser users (users that can log in to Wiser).
    /// </summary>
    public interface IUsersService
    {
        /// <summary>
        /// Gets a list of all users for a customer.
        /// </summary>
        /// <param name="includeAdminUsers">Optional: Whether to also get the admin users from the main Wiser database. Default is false.</param>
        /// <returns>A list of <see cref="WiserItemModel">ItemModel</see>.</returns>
        Task<ServiceResult<List<FlatItemModel>>> GetAsync(bool includeAdminUsers = false);

        /// <summary>
        /// Method for logging in admin accounts.
        /// </summary>
        /// <param name="username">The e-mail address of the admin account.</param>
        /// <param name="password">The password of the admin account.</param>
        /// <param name="ipAddress">The IP address of the user that is trying to login.</param>
        /// <param name="totpPin">When the user is logging in with TOTP, then the PIN of the user should be entered here. </param>
        /// <returns>A populated <see cref="AdminAccountModel"/> if successful, a 401 error if not.</returns>
        Task<ServiceResult<AdminAccountModel>> LoginAdminAccountAsync(string username, string password, string ipAddress = null, string totpPin = null);

        /// <summary>
        /// Login a customer to Wiser. Normal users login with their username and password.
        /// Admin accounts login via their own credentials and can then login as any other user.
        /// </summary>
        /// <param name="username">The username of the user to login as.</param>
        /// <param name="password">The password of the user. Can be empty if logging in with an admin account.</param>
        /// <param name="encryptedAdminAccountId">Optional: The encrypted admin account ID.</param>
        /// <param name="subDomain">The Wiser sub domain used to access the site.</param>
        /// <param name="generateAuthenticationTokenForCookie">Optional: Indicate whether to generate a token for a login cookie so that the user stays login for a certain amount of time.</param>
        /// <param name="ipAddress">The IP address of the user.</param>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user to check for rights.</param>
        /// <param name="totpPin">When the user is logging in with TOTP, then the PIN of the user should be entered here. </param>
        /// <param name="totpBackupCode">When the user entered a backup code to access their account, instead of a PIN.</param>
        /// <returns>Either an unauthorized error, or the <see cref="UserModel"/> of the user that is trying to login.</returns>
        Task<ServiceResult<UserModel>> LoginCustomerAsync(string username, string password, string encryptedAdminAccountId = null, string subDomain = null, bool generateAuthenticationTokenForCookie = false, string ipAddress = null, ClaimsIdentity identity = null, string totpPin = null, string totpBackupCode = null);

        /// <summary>
        /// Sends a new password to a user.
        /// </summary>
        /// <param name="resetPasswordRequestModel">The information for the account to reset the password.</param>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user to check for rights.</param>
        /// <returns>Always returns true, unless an exception occurred.</returns>
        Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordRequestModel resetPasswordRequestModel, ClaimsIdentity identity);

        /// <summary>
        /// Method for validating a "Remember me" cookie.
        /// </summary>
        /// <param name="cookieValue">The exact contents of the cookie.</param>
        /// <param name="subDomain">The Wiser sub domain used to access the site.</param>
        /// <param name="ipAddress">Optional: The IP address of the user that is trying to login.</param>
        /// <param name="sessionId">Optional: The ID of the current session of the user.</param>
        /// <param name="encryptedAdminAccountId">Optional: The encrypted admin account ID.</param>
        /// <param name="identity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user to check for rights.</param>
        /// <returns>Whether the cookie is (still) valid. If it's not valid, an error will be returned. Otherwise the user ID will be returned (encrypted).</returns>
        Task<ServiceResult<ValidateCookieModel>> ValidateLoginCookieAsync(string cookieValue, string subDomain = null, string ipAddress = null, string sessionId = null, string encryptedAdminAccountId = null, ClaimsIdentity identity = null);

        /// <summary>
        /// Changes the password of a user.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        /// <param name="passwords">The old and new passwords of the user.</param>
        Task<ServiceResult<UserModel>> ChangePasswordAsync(ClaimsIdentity identity, ChangePasswordModel passwords);

        /// <summary>
        /// Changes the e-mail address of a user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="subDomain">The Wiser sub domain used to access the site.</param>
        /// <param name="newEmailAddress">The new e-mail address.</param>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        Task<ServiceResult<UserModel>> ChangeEmailAddressAsync(ulong userId, string subDomain, string newEmailAddress, ClaimsIdentity identity);

        /// <summary>
        /// Gets data for the logged in user, such as the encrypted ID (which is only valid for 1 hour), for use with json.jcl.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        /// <returns></returns>
        Task<ServiceResult<UserModel>> GetUserDataAsync(ClaimsIdentity identity);

        /// <summary>
        /// Gets data for the logged in user, such as the encrypted ID (which is only valid for 1 hour), for use with json.jcl.
        /// </summary>
        /// <param name="usersService">The <see cref="IUsersService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to other methods in this method.</param>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        /// <returns></returns>
        Task<ServiceResult<UserModel>> GetUserDataAsync(IUsersService usersService, ClaimsIdentity identity);

        /// <summary>
        /// Gets settings for the authenticated user for a specific group of settings.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        /// <param name="groupName">The group that the settings belong to.</param>
        /// <param name="uniqueKey">The unique key for the settings.</param>
        /// <param name="defaultValue">Optional: The default value to return if there is no setting saved with the given group name and key.</param>
        /// <returns>The settings as a JSON string.</returns>
        Task<ServiceResult<string>> GetSettingsAsync(ClaimsIdentity identity, string groupName, string uniqueKey, string defaultValue = null);

        /// <summary>
        /// Gets settings for a grid for the authenticated user, so that users can keep their state of all grids in Wiser.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        /// <param name="uniqueKey">The unique key for the grid settings. This should be unique for each grid in Wiser, so that no 2 grids use the same settings.</param>
        Task<ServiceResult<string>> GetGridSettingsAsync(ClaimsIdentity identity, string uniqueKey);

        /// <summary>
        /// Gets the pinned modules for the authenticated user, so that users can keep their state of the pinned modules in Wiser.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        Task<ServiceResult<List<int>>> GetPinnedModulesAsync(ClaimsIdentity identity);

        /// <summary>
        /// Gets the modules that should be auto loaded, for the authenticated user. These modules should be automatically started when the user logs in.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        Task<ServiceResult<List<int>>> GetAutoLoadModulesAsync(ClaimsIdentity identity);

        /// <summary>
        /// Saves settings for the authenticated user that belong to a specific group.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        /// <param name="groupName">The group that the settings belong to.</param>
        /// <param name="uniqueKey">The unique key for the grid settings. This should be unique for each grid in Wiser, so that no 2 grids use the same settings.</param>
        /// <param name="settings">A JSON object with the settings to save.</param>
        /// <returns>A boolean whether the save action was successful.</returns>
        Task<ServiceResult<bool>> SaveSettingsAsync(ClaimsIdentity identity, string groupName, string uniqueKey, JToken settings);

        /// <summary>
        /// Saves settings for a grid for the authenticated user, so that the next time the grid is loaded, the user keeps those settings.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        /// <param name="uniqueKey">The unique key for the grid settings. This should be unique for each grid in Wiser, so that no 2 grids use the same settings.</param>
        /// <param name="settings">A JSON object with the settings to save.</param>
        Task<ServiceResult<bool>> SaveGridSettingsAsync(ClaimsIdentity identity, string uniqueKey, JToken settings);

        /// <summary>
        /// Save the list of pinned modules to the user details, so that next time the user will see the same pinned modules.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        /// <param name="moduleIds">The list of module IDs that the user has pinned.</param>
        Task<ServiceResult<bool>> SavePinnedModulesAsync(ClaimsIdentity identity, List<int> moduleIds);

        /// <summary>
        /// Save the list of modules that should be automatically started when the user logs in, to the user details.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        /// <param name="moduleIds">The list of module IDs that the user has set as auto load.</param>
        Task<ServiceResult<bool>> SaveAutoLoadModulesAsync(ClaimsIdentity identity, List<int> moduleIds);

        /// <summary>
        /// Get the e-mail address of the user.
        /// </summary>
        /// <param name="id">The ID of the user.</param>
        Task<string> GetUserEmailAddressAsync(ulong id);

        /// <summary>
        /// Get some settings for Wiser.
        /// </summary>
        /// <param name="encryptionKey">The encryption key of the customer.</param>
        /// <returns></returns>
        Task<UserModel> GetWiserSettingsForUserAsync(string encryptionKey);

        /// <summary>
        /// Generates a new refresh token and saves it in the database. The refresh token can be used to re-authenticate without having to enter credentials again.
        /// </summary>
        /// <param name="cookieSelector"></param>
        /// <param name="subDomain">The Wiser sub domain used to access the site.</param>
        /// <param name="ticket">The serialized ticket from the OWIN context.</param>
        /// <returns>The newly generated refresh token.</returns>
        Task<string> GenerateAndSaveNewRefreshTokenAsync(string cookieSelector, string subDomain, string ticket);

        /// <summary>
        /// Gets the ticket corresponding to a refresh token from the database and then deleted that refresh token, so that it can only be used once.
        /// </summary>
        /// <param name="subDomain">The Wiser sub domain used to access the site.</param>
        /// <param name="refreshToken">The refresh token.</param>
        /// <returns>The serialized ticket for OWIN context.</returns>
        Task<string> UseRefreshTokenAsync(string subDomain, string refreshToken);

        /// <summary>
        /// Updates the time the logged in user has been active.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        /// <param name="encryptedLoginLogId">The encrypted ID of the log table.</param>
        /// <returns>An <see cref="Int64"/> indicating how long the user has been active.</returns>
        Task<ServiceResult<long>> UpdateUserTimeActiveAsync(ClaimsIdentity identity, string encryptedLoginLogId);

        /// <summary>
        /// Resets the last time the "time active" field was updated to the current time.
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        /// <param name="encryptedLoginLogId">The encrypted ID of the log table.</param>
        /// <returns>A <see cref="bool"/> indicating if the request was successful.</returns>
        Task<ServiceResult<bool>> ResetTimeActiveChangedAsync(ClaimsIdentity identity, string encryptedLoginLogId);

        /// <summary>
        /// Gets all available roles for users.
        /// </summary>
        /// <param name="includePermissions">Optional: Whether to include all permissions that each role has. Default is <see langword="false"/>.</param>
        /// <returns>A list of <see cref="RoleModel"/> with all available roles that users can have.</returns>
        Task<ServiceResult<List<RoleModel>>> GetRolesAsync(bool includePermissions = false);

        /// <summary>
        /// Authenticates the TOTP code with the unique key of an user (2FA).
        /// </summary>
        /// <param name="key">Secret stored key</param>
        /// <param name="code">Code from authenticator app</param>
        /// <returns></returns>
        bool ValidateTotpPin(string key, string code);

        /// <summary>
        /// Setup TOTP authentication (2FA).
        /// </summary>
        /// <param name="account">Account-name (equal to e-mail address)</param>
        /// <param name="key">Secret stored key</param>
        /// <returns>QR Image URL</returns>
        string SetUpTotpAuthentication(string account, string key);

        /// <summary>
        /// (Re)generate backup codes for TOTP (2FA) authentication.
        /// This will delete any remaining backup codes from the user and generate new ones.
        /// They will be hashed before they're saved in the database and can therefor only be shown to the user once!
        /// </summary>
        /// <param name="identity">The <see cref="ClaimsIdentity"/> of the authenticated client.</param>
        /// <returns>A list with the new backup codes.</returns>
        Task<ServiceResult<List<string>>> GenerateTotpBackupCodesAsync(ClaimsIdentity identity);

        /// <summary>
        /// Gets the layout data of the dashboard of the currently logged in user.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <returns>A JSON string representing the layout data.</returns>
        Task<ServiceResult<string>> GetDashboardSettingsAsync(ClaimsIdentity identity);

        /// <summary>
        /// Saves the layout data of the dashboard to the currently logged in user.
        /// </summary>
        /// <param name="identity">The identity of the authenticated user.</param>
        /// <param name="settings">The JSON that represents the dashboard layout data.</param>
        /// <returns>A boolean whether the saving of the data was successful.</returns>
        Task<ServiceResult<bool>> SaveDashboardSettingsAsync(ClaimsIdentity identity, JToken settings);
    }
}