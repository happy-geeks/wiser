# Wiser
Wiser v3. This includes the API and the front-end projects.

## Requirements
### Software
1. Install [NodeJs](https://nodejs.org/en/) LTS.
2. To run Wiser in IIS, you need to download the [Windows Hosting Bundle](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) and install it.

### Compile front-end
1. Open PowerShell/CMD Window in the directory that contains the `FrontEnd.csproj` file (__NOT__ the root directory, that contains the `WiserCore.sln` file!).
1. Run `npm install`.
1. Run `node_modules\.bin\webpack --mode=development`.

If you get an error for not having enough rights to execute the script please execute the following in PowerShell as administrator:
```Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Unrestricted```

### Setup secrets<a name="setup-secrets"></a>
1. Create 2 files named `appsettings-secrets.json`, one for the API and one for the front-end, somewhere outside of the project directory.
1. Open `appSettings.[Environment].json` in both projects and save the directory to the secrets in the property `GCL.SecretsBaseDirectory`. When running Wiser locally on your PC, you need the file `appSettings.Development.json`. Please note that this directory should always end with a slash. Example: `Z:\AppSettings\Wiser\FrontEnd\`.
1. The `appsettings-secrets.json` files should look like this:
#### API
```json
{
  "GCL": {
    "connectionString": "", // Mandatory: The connection string to the main database for Wiser. See the chapter 'Database' for an example connectiom string.
    "DefaultEncryptionKey": "", // Mandatory: The default encryption key that should be used for encrypting values with AES when no encryption key is given. You can generate a value for this yourself.
    "DefaultEncryptionKeyTripleDes": "",  // Mandatory: The default encryption key that should be used for encrypting values with Tripe DES when no encryption key is given. You can generate a value for this yourself.
    "evoPdfLicenseKey": "" // If you're going to use the PdfService, you need a license key for Evo PDF, or make your own implementation.
  },
  "Api": {
    "AdminUsersEncryptionKey": "", // Mandatory: The encryption key to use for encrypting IDs and other data for admin users. You can generate a value for this yourself.
    "DatabasePasswordEncryptionKey": "", // Mandatory: The encryption key that will be used for encrypting and saving passwords for connection strings to customer databases. You can generate a value for this yourself.
    "ClientSecret": "", // Mandatory: The secret for the default client for OAUTH2 authentication. You can generate a value for this yourself.
    "PusherAppId": "", // Some modules use pusher to send notifications to users. Enter the app ID for pusher here if you want to use that.
    "PusherAppKey": "", // The app key for pusher.
    "PusherAppSecret": "", // The app secret for pusher.
    "PusherSalt": "", // A salt to use when hashing event IDs for pusher.,
    "SigningCredentialCertificate": "", // Mandatory: The fully qualified name of the certificate in the store of the server, of the certificate to use for IdentityServer4 (OAUTH2) authentication. This can be any valid certificate that is not self-signed. Only required on production environment. Development uses a self-signed certificate that will be automatically generated.
    "UseTerserForTemplateScriptMinification": false // Whether terser should be used to handle the minification of JavaScript templates made in the Templates module.
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
    "ApiClientId": "wiser", // Mandatory: The client ID for OAUTH2 authentication with the API, this should (at the moment) always be "wiser".
    "ApiClientSecret": "", // Mandatory: The client secret for OAUTH2 authentication with the API, this should be the same value as "API.ClientSecret" in the appsettings-secrets.json of the API.
    "TrackJsToken": "", // If you want to use Track JS to track all errors, enter a token for that here. TrackJS will not be loaded if this is empty.
    "MarkerIoToken": "", // If you want to use Marker.io for allowing your users to send bug reports, enter the token for that here. Marker.io will not be loaded if this is empty.
    "ApiBaseUrl": "", // Mandatory: The base URL for the API. Example: https://api.wiser3.nl/. You should use https://localhost:44349 when running/debugging Wiser locally on your PC.
    "WiserHostNames": [] // One or more host names for running the Wiser FrontEnd on. This is needed to figure out the sub domain for multi tenancy. These values well be stripped from the host name and what is left over will be considered the sub domain. This should be an empty array when running/debugging Wiser locally on your PC.
  }
}

```

# Database
Wiser requires a certain database structure to work, several tables and triggers are required. 

## Requirements
At the moment, we only support MySQL, but other databases might be added in the future. Wiser requires MySQL 5.7 or higher to work, because it uses JSON functions and those have been added in MySQL 5.7.

Wiser does not work if the SQL mode `ONLY_FULL_GROUP_BY` is enabled, so please make sure you disable this mode in your database when running Wiser.

Please note that the installation script does not work with MySQL 8 users. For this to work you need to set the authentication plugin of your database user to "mysql_native_password". This is because the node package we use for MySQL does not support this yet.

If you do not have SUPER privileges in the database, you might get an error while running `CreateTriggers.sql`. To fix this, you need to either disable `bin_logging` in MySQL, or enable the option `log_bin_trust_function_creators`. For more information see [this article](https://dev.mysql.com/doc/refman/5.7/en/stored-programs-logging.html).

## Connection string
The connection string in the `appsettings.json` or `appsettings-secrets.json` of the API should look like this:
```
server=;port=;uid=;pwd=;database=;pooling=true;Convert Zero Datetime=true;CharSet=utf8;AllowUserVariables=True
```
Note the options that are added at the end of the connection string, Wiser will not work properly without these options.

Keep in mind that if the password has special characters it needs to be escaped in the connection string.

## Installation script
The installation script creates a new database schema and then creates several tables in that database. For it to work, you'll need a database user that has enough permissions to do all this.
To setup this database, you can open a PowerShell or CMD window in the directory that contains the `Api.csproj` file and run the following command:
```
npm run setup:mysql -- --host=host --database=database --user=user --password=password --port=port
```
You can use the following parameters with this command:
- **host** (required): The hostname or IP address to the MySQL database.
- **database** (required): The name of the database scheme to create.
- **user** (required): The username of the MySQL user.
- **password** (required): The password of the MySQL user. Note that the script **does not support** the new MySQL 8 password, only `mysql_native_password`.
- **port** (optional): The port for the database. Default value is `3306`.
- **isConfigurator** (optional): Set to `true` if you want to make a configurator with Wiser.
- **isWebshop** (optional): Set to `true` if you want to make a webshop with Wiser.
- **isMultiLanguage** (optional): Set to `true` if you want to make a multi language application with Wiser.

After installation, you can login with the username `admin` and password `admin`. Please change this password as soon as possible. The password can be changed via the module `Gebruikers - Wiser`. In that module you can also add other users that you want to be able to login to Wiser.

You can also install Wiser manually:
We have several SQL scripts to create these tables and add the minimum amount of data required to be able to login. These scripts can be found in `API\Core\Queries\WiserInstallation`. You should execute these script in the following order:
1. `CreateTables.sql`
2. `CreateTriggers.sql`
3. `StoredProcedures.sql`
4. `InsertInitialData.sql`

The scripts `InsertInitialDataConfigurator.sql` and `InsertInitialDataEcommerce.sql` can be used if you want to run a website that uses the GeeksCoreLibrary that can be managed in Wiser. If you have a website with a webshop, run `InsertInitialDataEcommerce.sql` and if you have a website with a product configurator, run `InsertInitialDataConfigurator.sql` to setup Wiser to work with those kinds of websites.

# Using terser for JavaScript template minification (optional)
The API can be configured to use an npm package called [terser](https://terser.org/) to handle the minification of JavaScript templates that are created in the Templates module instead of [NUglify](https://github.com/trullock/NUglify). To do this, the terser npm package must be installed in the root directory where the API is running on the server:
1. Open PowerShell/CMD window in the directory where the API is running.
1. Run the command `npm install terser`. This will install terser and all its dependencies, and create terser command files in the `node_modules/.bin` folder. After the installation is done, verify that the `node_modules/.bin` directory exists in the root directory of the API and that it contains these files:
    - `terser`
    - `terser.cmd`
    - `terser.ps1`
1. Open the `appsettings-secrets.json` file of the API in an editor.
    - See the [Setup secrets](#setup-secrets) section above to check where this file is located.
1. Set the value of the setting `Api.UseTerserForTemplateScriptMinification` to `true`.
    - See the [Setup secrets](#setup-secrets) section above for an example.
1. (Re)start the API.

The reason the setting is saved in the `appsettings-secrets.json` file instead of the `appSettings.json` file is to avoid the value getting overwritten when deploying a new version.

## How terser is used to minify
Because terser works with input files, the API will create a temporary file where the script will temporarily be stored. The directory where these scripts are temporarily stored is `temp/minify` in the API base directory. Wiser will attempt to create this directory, but it's not a bad idea to create it manually so the right permissions can be set. Wiser will automatically delete the temporary file after the minification has been completed.

# Debugging
1. Open PowerShell/CMS Window in the directory that contains the `FrontEnd.csproj` file (__NOT__ the root directory, that contains the `WiserCore.sln` file!).
1. Run the command `node_modules\.bin\webpack --mode=development -w`. This will make webpack watch your javascript and automatically rebuild them when needed, so you don't have to rebuild it manully every time.
1. To make debugging a little easier, you can setup Visual Studio to always start both the API and FrontEnd projects at the same time. You can do this by right clicking the solution and then `Properties`. Then go to `Common Properties --> Startup Project` and choose `Multiple startup projects`. Then set both `Api` and `FrontEnd` to `Start` and click `OK`.
1. If you use Rider, we already have a configuration saved for this, it's called "Debug Both". If you don't see it, you can also set that up yourself. To do this, click `Edit configuration` in the configurations dropdown (in the toolbar), this will open a new screen. In that screen select the root item `.NET Launch Settings Profile`, then click the plus icon on the top left. Now add a `Compound` and give it any name, such as "Debug both". Lastly, add the configurations `.NET Launch Settings Profile 'Api'` and `.NET Launch Settings Profile 'FrontEnd'` to that compount and click `OK`.
1. When debugging, Rider/Visual Studio will create a self-signed certificate. You need to add this certificate to the trusted sources on your computer, otherwise you won't be able to run/debug Wiser properly. In Rider you can do this by pressing `CTRL+SHIFT+A`, then typing `Certificate` and clicking the result (Set up ASP.NET Core Developer certificate). You will then get a pop-up asking for confirmation to add the certificate to the trusted sources. Click yes here. Now you should be able to run Wiser locally. If you don't do this, you will get the error `System.InvalidOperationException: IDX20803: Unable to obtain configuration` when loading the list of modules and other things.

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

# Publishing
To publish Wiser 3 to your own server, you should use the following publish settings:
- Configuration: `Release`
- Target Framework: `net7.0`
- Deployment Mode: `Self-Contained`
- Target Runtime: The correct runtime for your system, for Windows this is usually `win-x64`
- Under File Publish Option, tick the box for `Enable ReadyToRun compilation`

# Authentication
The API uses `IdentityServer4` for authentication, it uses OAUTH2 with bearer tokens. It uses a global secret key and client id that need to be configured in the appsettings. The actual user credentials come from the database of Wiser, these are entities with the type `wiseruser`. The installation script will create a user with username `Admin` and password `admin` to authenticate via the API to login to Wiser.

This authentication requires an SSL certificate. By default it will generate a self-signed certificate that will be saved as `tempkey.jwk` in the root directory of the API. On all other environments, the API will expect a proper SSL certificate, but you can change that in `Startup.cs` if you search for `AddSigningCredential`. Especially on production servers it's recommended to configure the API to use a proper SSL certificate. If you really want/need to use the self-signed certificate on production, you can either copy the `tempkey.jwk` file from your development environment, or give the application pool (if you run Wiser in IIS) write permissions in the root directory of the API, so that it can create a new certificate there. After the certificate has been created, you can remove the write permissions again.

In the appsettings you should set the property `Api.SigningCredentialCertificate`. That property should contain the full name of the certificate, to be able to find it in the certificate store of the server that the API runs on. To find out what this value should be, you can (on Windows) execute the following command in PowerShell:
```
Get-ChildItem -path Cert:\LocalMachine\My -Recurse
```
The correct value will be shown in the column `Subject`, you can copy that value to the app settings (including `CN=`).

If you run the API on IIS in Windows, you need to give IIS permissions to access the private keys of the specified certificate. You can do this by opening the Windows certificate manager (`Certmgr.exe`), then find your certificate there. Right click the certificate, then `All Tasks -> Manage Private Keys -> Add group "IIS_IUSRS"`. If you don't do this, you will most likely get the error `WindowsCryptographicException: Keyset does not exist`.
