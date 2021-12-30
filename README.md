# Wiser
Wiser v3. This includes the API and the front-end projects.

## Requirements
### Software
1. Install [NodeJs](https://nodejs.org/en/) LTS.

### Compile front-end
1. Open PowerShell/CMD Window in the directory that contains the `FrontEnd.csproj` file (__NOT__ the root directory, that contains the `WiserCore.sln` file!).
1. Run `npm install`.
1. Run `node_modules\.bin\webpack --mode=development`.

### Setup secrets
1. Create 2 files named `appsettings-secrets.json`, one for the API and one for the front-end, somewhere outside of the project directory.
1. Open `appSettings.json` in both projects and save the directory to the secrets in the property `GCL.SecretsBaseDirectory`.
1. The `appsettings-secrets.json` files should look like this:
#### API
```json
{
  "GCL": {
    "connectionString": "", // The connection string to the main database for Wiser.
    "DefaultEncryptionKey": "", // The default encryption key that should be used for encrypting values with AES when no encryption key is given.
    "DefaultEncryptionKeyTripleDes": "",  // The default encryption key that should be used for encrypting values with Tripe DES when no encryption key is given.
    "evoPdfLicenseKey": "" // If you're going to use the PdfService, you need a license key for Evo PDF, or make your own implementation.
  },
  "Api": {
    "AdminUsersEncryptionKey": "", // The encryption key to use for encrypting IDs and other data for admin users.
    "DatabasePasswordEncryptionKey": "", // The encryption key that will be used for encrypting and saving passwords for connection strings to customer databases.
    "ClientSecret": "", // The secret for the default client for OAUTH2 authentication.
    "PusherAppId": "", // Some modules use pusher to send notifications to users. Enter the app ID for pusher here if you want to use that.
    "PusherAppKey": "", // The app key for pusher.
    "PusherAppSecret": "", // The app secret for pusher.
    "PusherSalt": "" // A salt to use when hashing event IDs for pusher.
  },
  "DigitalOcean": {
    "ClientId": "", // If you want to use the Digital Ocean API, enter the client ID for that here.
    "ClientSecret": "" // The secret for the Digital Ocean API.
  }
}
```
#### FrontEnd
```json
{
  "FrontEnd": {
    "ApiClientId": "wiser", // The client ID for OAUTH2 authentication with the API, this should (at the moment) always be "wiser".
    "ApiClientSecret": "", // The client secret for OAUTH2 authentication with the API, this should be the same value as "API.ClientSecret" in the appsettings-secrets.json of the API.
    "TrackJsToken": "", // If you want to use Track JS to track all errors, enter a token for that here. TrackJS will not be loaded if this is empty.
    "MarkerIoToken": "", // If you want to use Marker.io for allowing your users to send bug reports, enter the token for that here. Marker.io will not be loaded if this is empty.
    "ApiBaseUrl": "", // The base URL for the API.
    "WiserHostNames": [] // One or more host names for running the Wiser FrontEnd on. This is needed to figure out the sub domain for multi tenancy. These values well be stripped from the host name and what is left over will be considered the sub domain.
  }
}

```

#### Datasbase
Wiser requires a certain database structure to work, several tables and triggers are required. At the moment, we only support MySQL, but other databases might be added in the future.
The first table you need is called `easy_customers`, this table is needed to lookup the connection string and other information for the customer when using multi tenancy. At the moment this table is always required, even if you don't use multi tenancy (but that will change in the future). This table can be created like this:
```sql
CREATE TABLE `easy_customers`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `customerid` int NULL DEFAULT NULL,
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `db_host` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `db_login` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `db_pass` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `db_passencrypted` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NULL DEFAULT NULL,
  `db_port` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `db_dbname` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `encryption_key` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `encryption_key_test` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `subdomain` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `wiser_title` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE INDEX `subdomain`(`subdomain`) USING BTREE,
  INDEX `customerid`(`customerid`) USING BTREE,
  INDEX `name`(`name`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;
```

For being able to actually use Wiser, you will need a lot more tables. We have several SQL scripts to create these tables and add the minimum amount of data required to be able to login. These scripts can be found in `API\Core\Queries\WiserInstallation`. You should execute these script in the following order:
1. `CreateTables.sql`
2. `CreateTriggers.sql`
3. `InsertInitialData.sql`

The scripts `InsertInitialDataConfigurator.sql` and `InsertInitialDataEcommerce.sql` can be used if you want to run a website that uses the GeeksCoreLibrary that can be managed in Wiser. If you have a website with a webshop, run `InsertInitialDataEcommerce.sql` and if you have a website with a product configurator, run `InsertInitialDataConfigurator.sql` to setup Wiser to work with those kinds of websites.

## Debugging
1. Open PowerShell/CMS Window in the directory that contains the `FrontEnd.csproj` file (__NOT__ the root directory, that contains the `WiserCore.sln` file!).
1. Run the command `node_modules\.bin\webpack --w --mode=development`. This will make webpack watch your javascript and automatically rebuild them when needed, so you don't have to rebuild it manully every time.
1. To make debugging a little easier, you can setup Visual Studio to always start both the API and FrontEnd projects at the same time. You can do this by right clicking the solution and then `Properties`. Then go to `Common Properties --> Startup Project` and choose `Multiple startup projects`. Then set both `Api` and `FrontEnd` to `Start` and click `OK`.
