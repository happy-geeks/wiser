using System;
using System.Collections.Generic;
using Api.Modules.Customers.Interfaces;

namespace Api.Core.Models
{
    /// <summary>
    /// A model with settings for the API. This is meant to be used with the IOptions pattern.
    /// </summary>
    public class ApiSettings
    {
        /// <summary>
        /// Gets or sets the encryption key for encrypting and decrypting admin user IDs.
        /// </summary>
        public string AdminUsersEncryptionKey { get; set; }

        /// <summary>
        /// Gets or sets the encryption key for encrypting and decrypting the database password that is saved in easy_customers.
        /// </summary>
        public string DatabasePasswordEncryptionKey { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of times a user can attempt to login, before their IP address gets blocked.
        /// </summary>
        public int MaximumLoginAttemptsForUsers { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of times an admin can attempt to login, before their IP address gets blocked.
        /// </summary>
        public int MaximumLoginAttemptsForAdmins { get; set; }

        /// <summary>
        /// Gets or sets the properties that always need to be encrypted in JSON options objects, for fields and such.
        /// </summary>
        public List<string> JsonPropertiesToAlwaysEncrypt { get; set; }

        /// <summary>
        /// The amount of time to cache results from functions of <see cref="IUsersService"/> and <see cref="IWiserCustomersService"/>.
        /// </summary>
        public TimeSpan DefaultUsersCacheDuration { get; set; } = new(1, 0, 0);

        /// <summary>
        /// The app ID used for the Pusher server.
        /// </summary>
        public string PusherAppId { get; set; }

        /// <summary>
        /// The app key used for the Pusher server.
        /// </summary>
        public string PusherAppKey { get; set; }

        /// <summary>
        /// The app secret used for the Pusher server.
        /// </summary>
        public string PusherAppSecret { get; set; }

        /// <summary>
        /// The salt used to hash user ID for the Pusher server.
        /// </summary>
        public string PusherSalt { get; set; }

        /// <summary>
        /// The sub domain that should be used to login to the main wiser database (the one that contains the table "easy_customers"), when using multi tenancy.
        /// This value is not used when not using multi tenancy.
        /// </summary>
        public string MainSubDomain { get; set; } = "main";

        /// <summary>
        /// The fully qualified name of the certificate in the store of the server, of the certificate to use for IdentityServer4 (OAUTH2) authentication.
        /// </summary>
        public string SigningCredentialCertificate { get; set; }

        /// <summary>
        /// Gets or sets whether the NPM package 'terser' should be used when minifying scripts saved in the templates module.
        /// </summary>
        public bool UseTerserForTemplateScriptMinification { get; set; }

        /// <summary>
        /// Gets or sets the directory where the plugins are located.
        /// </summary>
        public string PluginsDirectory { get; set; }
    }
}