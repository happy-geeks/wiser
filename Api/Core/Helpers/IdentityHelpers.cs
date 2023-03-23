using System;
using System.Linq;
using System.Security.Claims;
using Api.Core.Models;

namespace Api.Core.Helpers
{
    /// <summary>
    /// Helpers for (Claim)Identities
    /// </summary>
    public static class IdentityHelpers
    {
        /// <summary>
        /// Gets the role(s) from a <see cref="ClaimsIdentity">ClaimsIdentity</see>.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> to get the role from.</param>
        /// <returns>The role name as <see cref="string"/></returns>
        public static string GetRoles(ClaimsIdentity claimsIdentity)
        {
            var roleClaims = claimsIdentity?.Claims.Where(claim => claim.Type == ClaimTypes.Role).Select(claim => claim.Value).ToList();
            if (roleClaims == null || !roleClaims.Any())
            {
                return null;
            }

            return String.Join(",", roleClaims);
        }

        /// <summary>
        /// Checks if a <see cref="ClaimsIdentity">ClaimsIdentity</see> contains a certain role.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> to get the role from.</param>
        /// <param name="role">The name of the role.</param>
        /// <returns>The role name as <see cref="string"/></returns>
        public static bool HasRole(ClaimsIdentity claimsIdentity, string role)
        {
            return claimsIdentity?.Claims.Where(claim => claim.Type == ClaimTypes.Role && !String.IsNullOrWhiteSpace(claim.Value)).SelectMany(claim => claim.Value.Split(',')).Any(r => String.Equals(r, role, StringComparison.OrdinalIgnoreCase)) ?? false;
        }

        /// <summary>
        /// Check whether a user has the administrator role.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the user to check.</param>
        /// <returns>A boolean indicating whether the supplied <see cref="ClaimsIdentity">ClaimsIdentity</see> contains the admin role.</returns>
        public static bool IsAdministrator(ClaimsIdentity claimsIdentity)
        {
            return HasRole(claimsIdentity, IdentityConstants.AdministratorRole);
        }

        /// <summary>
        /// Check whether a user has the AdminAccount role.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the user to check.</param>
        /// <returns>A boolean indicating whether the supplied <see cref="ClaimsIdentity">ClaimsIdentity</see> contains the admin role.</returns>
        public static bool IsAdminAccount(ClaimsIdentity claimsIdentity)
        {
            return HasRole(claimsIdentity, IdentityConstants.AdminAccountRole);
        }

        /// <summary>
        /// Check whether a user has the customer role.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the user to check.</param>
        /// <returns>A boolean indicating whether the supplied <see cref="ClaimsIdentity">ClaimsIdentity</see> contains the customer role.</returns>
        public static bool IsCustomer(ClaimsIdentity claimsIdentity)
        {
            return HasRole(claimsIdentity, IdentityConstants.CustomerRole);
        }

        /// <summary>
        /// Check whether the authenticated user is the same as the given username.
        /// </summary>
        /// <param name="claimsIdentity"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        public static bool IsUser(ClaimsIdentity claimsIdentity, string username)
        {
            var userName = GetUserName(claimsIdentity);
            return !String.IsNullOrWhiteSpace(userName) && userName.Equals(username, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get the name from a <see cref="ClaimsIdentity">ClaimsIdentity</see>. For the username, use <see cref="GetUserName"/> instead.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <returns></returns>
        public static string GetName(ClaimsIdentity claimsIdentity)
        {
            return claimsIdentity?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.GivenName)?.Value;
        }

        /// <summary>
        /// Get the username from a <see cref="ClaimsIdentity">ClaimsIdentity</see>.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <param name="useAdminAccountNameIfAvailable">Optional: Set to <see langword="true"/> to get the username of the logged in admin account, if applicable.</param>
        /// <returns></returns>
        public static string GetUserName(ClaimsIdentity claimsIdentity, bool useAdminAccountNameIfAvailable = false)
        {
            string result = null;
            if (useAdminAccountNameIfAvailable)
            {
                result = claimsIdentity?.Claims.FirstOrDefault(claim => claim.Type == IdentityConstants.AdminAccountName)?.Value;
            }

            if (String.IsNullOrEmpty(result))
            {
                result = claimsIdentity?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Name)?.Value;
            }
            else
            {
                result += " (Admin)";
            }

            return result;
        }

        /// <summary>
        /// Get the admin username from a <see cref="ClaimsIdentity">ClaimsIdentity</see>.
        /// If the user is not a Wiser admin account (from the main Wiser database), then this will return null.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <returns></returns>
        public static string GetAdminUserName(ClaimsIdentity claimsIdentity)
        {
            return claimsIdentity?.Claims.FirstOrDefault(claim => claim.Type == IdentityConstants.AdminAccountName)?.Value;
        }

        /// <summary>
        /// Get the email address from a <see cref="ClaimsIdentity">ClaimsIdentity</see>.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <returns></returns>
        public static string GetEmailAddress(ClaimsIdentity claimsIdentity)
        {
            return claimsIdentity?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Get the ID of a <see cref="ClaimsIdentity">ClaimsIdentity</see>.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <returns></returns>
        public static int GetClientId(ClaimsIdentity claimsIdentity)
        {
            return !Int32.TryParse(claimsIdentity?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value, out var result) ? 0 : result;
        }

        /// <summary>
        /// Get the ID of a <see cref="ClaimsIdentity">ClaimsIdentity</see>.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <returns></returns>
        public static ulong GetWiserUserId(ClaimsIdentity claimsIdentity)
        {
            return !UInt64.TryParse(claimsIdentity?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value, out var result) ? 0 : result;
        }

        /// <summary>
        /// Get the admin ID of a <see cref="ClaimsIdentity">ClaimsIdentity</see>.
        /// If the user is not a Wiser admin account (from the main Wiser database), then this will return 0.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <returns></returns>
        public static ulong GetWiserAdminId(ClaimsIdentity claimsIdentity)
        {
            return !UInt64.TryParse(claimsIdentity?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Sid)?.Value, out var result) ? 0 : result;
        }

        /// <summary>
        /// Get the sub domain from a <see cref="ClaimsIdentity">ClaimsIdentity</see>.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <returns></returns>
        public static string GetSubDomain(ClaimsIdentity claimsIdentity)
        {
            return claimsIdentity?.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.GroupSid)?.Value;
        }

        /// <summary>
        /// Get the sub domain from a <see cref="ClaimsIdentity">ClaimsIdentity</see>.
        /// </summary>
        /// <param name="claimsIdentity">The <see cref="ClaimsIdentity">ClaimsIdentity</see> of the authenticated user.</param>
        /// <returns></returns>
        public static bool IsTestEnvironment(ClaimsIdentity claimsIdentity)
        {
            return String.Equals("true", claimsIdentity?.Claims.FirstOrDefault(claim => claim.Type == HttpContextConstants.IsTestEnvironmentKey)?.Value, StringComparison.OrdinalIgnoreCase);
        }
    }
}