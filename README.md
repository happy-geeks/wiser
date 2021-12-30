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

## Debugging
1. Open PowerShell/CMS Window in the directory that contains the `FrontEnd.csproj` file (__NOT__ the root directory, that contains the `WiserCore.sln` file!).
1. Run the command `node_modules\.bin\webpack --w --mode=development`. This will make webpack watch your javascript and automatically rebuild them when needed, so you don't have to rebuild it manully every time.