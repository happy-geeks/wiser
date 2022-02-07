# Wiser
Wiser v3. This includes the API and the front-end projects.

## Requirements
### Software
1. Install [NodeJs](https://nodejs.org/en/) LTS.
2. To run Wiser in IIS, you need to download the [Windows Hosting Bundle](https://dotnet.microsoft.com/en-us/download/dotnet/5.0) and install it.

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

To setup this database, you can open a PowerShell or CMD window in the directory that contains the `Api.csproj` file and run the following command:
```
npm run setup:mysql -- --host=host --database=database --user=user --password=password
```
You can use the following parameters with this command:
- **host** (required): The hostname or IP address to the MySQL database.
- **database** (required): The name of the database scheme to create.
- **user** (required): The username of the MySQL user.
- **password** (required): The password of the MySQL user. Note that the script does not support the new MySQL 8 password, only `mysql_native_password`.
- **port** (optional): The port for the database. Default value is `3306`.
- **isConfigurator** (optional): Set to `true` if you want to make a configurator with Wiser.
- **isWebshop** (optional): Set to `true` if you want to make a webshop with Wiser.

After installation, you can login with the username `admin` and password `admin`. Please change this password as soon as possible. The password can be changed via the module `Gebruikers - Wiser`. In that module you can also add other users that you want to be able to login to Wiser.

You can also install Wiser manually:
We have several SQL scripts to create these tables and add the minimum amount of data required to be able to login. These scripts can be found in `API\Core\Queries\WiserInstallation`. You should execute these script in the following order:
1. `CreateTables.sql`
2. `CreateTriggers.sql`
3. `InsertInitialData.sql`

The scripts `InsertInitialDataConfigurator.sql` and `InsertInitialDataEcommerce.sql` can be used if you want to run a website that uses the GeeksCoreLibrary that can be managed in Wiser. If you have a website with a webshop, run `InsertInitialDataEcommerce.sql` and if you have a website with a product configurator, run `InsertInitialDataConfigurator.sql` to setup Wiser to work with those kinds of websites.

## Debugging
1. Open PowerShell/CMS Window in the directory that contains the `FrontEnd.csproj` file (__NOT__ the root directory, that contains the `WiserCore.sln` file!).
1. Run the command `node_modules\.bin\webpack --w --mode=development`. This will make webpack watch your javascript and automatically rebuild them when needed, so you don't have to rebuild it manully every time.
1. To make debugging a little easier, you can setup Visual Studio to always start both the API and FrontEnd projects at the same time. You can do this by right clicking the solution and then `Properties`. Then go to `Common Properties --> Startup Project` and choose `Multiple startup projects`. Then set both `Api` and `FrontEnd` to `Start` and click `OK`.

# Multitenancy
Wiser works with multitenancy, but only with different (sub) domains. So for example, if Wiser runs on the domain `wiser.nl`, then you can use different sub domains for multi tenancy (eg. `foobar.wiser.nl`). Or you can just use multiple domains, like `example.com` and `foorbar.com`. When someone opens a (sub) domain of Wiser, that (sub) domain will then be looked up in `easy_customers`, via the column `subdomain`. If a row has been found, a connectionstring will be generated with the data from that row and that will be used for all requests on that sub domain.

If you open Wiser without a sub domain, then the sub domain will be hard-coded to `main` and Wiser will not check the database for the connection string, but just use the connection string from the appsettings.

## Admin accounts
When using multi tenancy, all users in the module `Gebruikers - Wiser` of the main database will be seen as admin users. Admin users are special users that can login in any tenant as another of that tenant. When someone tries to login, Wiser will first see if that is a user from that particular tenant. If Wiser was not able to login the user via the tenant database, then Wiser will check if that user exists in the main database. If it does, the user will be logged in and will then see an extra step for logging in. In that step, the user sees a dropdown with all users of that tenant. They can then select any of those users and will then be logged in as that user.

## Setting up multitenancy
First you need a table with the name `easy_customers`, this table is needed to lookup the connection string and other information for the customer when using multi tenancy. This table can be created like this:
```sql
CREATE TABLE `easy_customers`  (
  `id` int NOT NULL AUTO_INCREMENT,
  `customerid` int NULL DEFAULT NULL,
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `db_host` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `db_login` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
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

Next, you need to tell Wiser the main domain(s) that it will be running on. You can do this in the appsettings of the `FrontEnd` project, by adding `FrontEnd.WiserHostNames`. This should be an array with one or more domains (without `http(s)`). Example:
```json
{
  "FrontEnd": {
    "WiserHostNames": [".wiser.nl", ".wiser3.nl"]
  }
}
```
Wiser will take the entire host, remove the parts that are set in `WiserHostNames` in the appsettings and will use what's left over as the sub domain. So for example, if you use the example above in the appsettings and you open https://foo.wiser.nl/bar, then Wiser will take the host (which is `foo.wiser.nl`), remove the part `.wiser.nl` and then the final sub domain will be `foo`, which you should then add to `easy_customers`.

We call this "sub domain" because that it how it was originally intended, but it doesn't have to be a subdomain. You could, for example, add `example.com` to the `WiserHostNames` and then use the domain `myexample.com`, then the 'sub domain' will be `my`. Or you could even set `WiserHostNames` to an empty array and then the entire hostname will be used as 'sub domain', so multitenancy also works with multiple domains that way, then you just need to add the entire domain to the `subdomain` column of `easy_customers`.

You will also need to enter the credentials for the database in `easy_customers`, so that Wiser knows how to connect to the database of that tenant. The password needs to be encrypted with AES, with ciphermode CBC. You need to create a salt of 8-12 bytes, use that salt in the encryption and then append that same salt to the end of the encrypted value. The `GeeksCoreLibrary` has a method for this, called `StringExtensions.EncryptWithAesWithSalt()`. This value then needs to be saved in the `db_passencrypted` column. The encryption key that you use to encrypt the password, needs to be saved in the appsettings of the API, in the property `API.DatabasePasswordEncryptionKey`, so that Wiser can decrypt the password and use it the connection string.

# Roles and permissions
You can use roles and permissions to manage what users can see and do in Wiser. At the moment we don't have a module for this yet, so they need to be added/changed manually in the database.

All roles can be added in the table `wiser_roles`. In `wiser_permission` you can set the permissions per role per module/item/field. The `permissions` column is a bitwise column that contain one or more of these permissions:
- 0 = No permissions, the user cannot see or change this module/item/field.
- 1 = Read
- 2 = Create
- 4 = Update
- 8 = Delete

So if someone has all permissions, you need to enter the value 1 + 2 + 4 + 8 = 15.

You can link users to one or more roles via `wiser_user_roles`. If a user has multiple roles with different permissions, all permissions of all roles are valid. So if the user has a role that allows them to only see an item and a role that allows them to see and update an item, that user can always see and update that item.

By default, users can see and change everything. If there is no entry in wiser_permission for an item, it will be seen as if you added and entry with the permission value of "15". If you want to block certain users from seeing an item, you need to add a value with "0".
