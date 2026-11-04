# DevToolkit Setup Guide

A walkthrough for **VS Code, PowerShell, .NET 10, SQL Server, and Thunder Client**. `YourApp` is the example application name; replace it with yours. C# project files end in `.csproj`.

## Before you start

You need Git, the .NET 10 SDK, VS Code with C# support, SQL Server, and Thunder Client or another API client.

Choose a new-project or existing-project path below. After that, open the terminal **inside your API folder**, beside `Program.cs` and your single `.csproj` file. Run the commands there. The project file tells .NET what to build, so you do not need a project-name variable or extra project arguments for this layout.

The order is: **project files → configuration → build → secrets → database → run → test**. Complete each step before moving on.

## BeautyParlour Project Reference

**BeautyParlour** is the reference application used for the setup screenshots and Thunder Client examples. The written commands use `YourApp` so you can substitute your own application name.

![BeautyParlour project structure with AuthModule, Program.cs, configuration, and one application project file](https://github.com/user-attachments/assets/52833d08-043a-4790-8f1f-e9a25d0045c6)

*BeautyParlour Project Reference — AuthModule integrated into the application.*

| In this guide | BeautyParlour reference |
| --- | --- |
| Application folder | `BeautyParlour/` |
| `YourApp.Api.csproj` | `BeautyParlour.csproj` |
| Optional `YourApp` code prefix | `BeautyParlour` |
| `Database=YourApp` | `Database=BeautyParlour` |
| API base URL | The screenshots use `http://localhost:5148`; use the URL printed by your own app |

The connection-string lookup key can remain `YourAppDataBase`; it is separate from the SQL database name. If the reference application uses `AuthDatabase`, keep that name consistent in its settings, startup lookup, and User Secrets.

See the [Reference Project Preview](README.md#reference-project-preview) for an example sign-in interface. The Thunder Client screenshots below show requests sent to specific backend routes and their responses.

## Choose a path

| Starting point                 | Path                                                    |
| ------------------------------ | ------------------------------------------------------- |
| New backend in an empty folder | [A: use the starter](#path-a-use-the-starter)           |
| Existing ASP.NET Core API      | [B: integrate the module](#path-b-integrate-the-module) |

Complete one path, then continue with [application configuration](#application-configuration).

This guide uses SQL Server. The included migrations and SQL file are for SQL Server; another database requires its own compatible provider and migrations.

## Path A: use the starter

### A1. Clone without an extra dev-toolkit directory

From your projects directory:

```powershell
mkdir YourApp.Api
cd YourApp.Api
git clone https://github.com/Tanish15-gif/dev-toolkit.git .
```

The final `.` means the current directory, which must be empty. Files go directly inside `YourApp.Api`, without another `dev-toolkit` folder. Git also creates its hidden `.git` metadata directory. [Git clone documentation](https://git-scm.com/docs/git-clone)

An equivalent alternative from the parent directory is:

```powershell
git clone https://github.com/Tanish15-gif/dev-toolkit.git YourApp.Api
cd YourApp.Api
```

Use one approach, not both. A repository that is still private requires GitHub access.

### A2. Name the project file

In Explorer, rename `DevToolkit.Api.csproj` to `YourApp.Api.csproj`. Keep the existing package references. This starter already is a Web API; creating another Web API inside it is unnecessary.

```text
YourApp.Api/
├── AuthModule/
├── AuthModule.sql
├── YourApp.Api.csproj
├── Program.cs
├── appsettings.json
├── README.md
└── SetupGuide.md
```

`.gitignore` and Git metadata are also present. Generated `bin` and `obj` directories are ignored by Git. There should be one application `.csproj` file in this folder.

Keep the terminal in this API folder for the remaining commands.

Continue with [application configuration](#application-configuration).

### Already have a nested clone?

If you cloned into `YourApp/dev-toolkit` and are starting from the toolkit, open that clone as your application folder, or rename the clone folder to your application's name.

If the parent already contains your own API, follow **Path B**. Moving all the clone's files over an existing application can overwrite startup/configuration and leave two project files. Cloning directly with `.` avoids the manual move for a new, empty destination.

## Path B: integrate the module

This path preserves the application's own project file, startup logic, configuration, dependencies, and Git history. The package versions below target .NET 10.

### B1. Keep the reference clone outside the API

From the directory containing your API folder:

```powershell
git clone https://github.com/Tanish15-gif/dev-toolkit.git dev-toolkit-reference
```

```text
Projects/
├── YourApp.Api/
│   ├── YourApp.Api.csproj
│   └── Program.cs
└── dev-toolkit-reference/
    ├── AuthModule/
    └── DevToolkit.Api.csproj
```

Copy the complete `AuthModule` directory into your API. Keep its `Data/Migrations` files too. `AuthModule.sql` is an optional schema reference and alternative SQL deployment script. Keep the clone's `.git`, `.csproj`, and `Program.cs` in the reference directory. Retain the toolkit's [MIT license notice](LICENSE) with the copied code.

If the clone is already inside your API, move that complete clone outside the API first, then copy the module from it. Nested C# files can otherwise be included in your outer project's build.

See the [BeautyParlour project structure](#beautyparlour-project-reference) above for the integrated layout.

### B2. Add packages to your own project

Open the terminal in your existing API folder, beside its single `.csproj` file. Run these commands to add the required packages:

```powershell
dotnet add package BCrypt.Net-Next --version 4.2.0 --no-restore
dotnet add package Google.Apis.Auth --version 1.75.0 --no-restore
dotnet add package MailKit --version 4.18.0 --no-restore
dotnet add package System.Security.Cryptography.Pkcs --version 10.0.14 --no-restore
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.14 --no-restore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.14 --no-restore
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.14 --no-restore
```

Restore is performed at the build checkpoint. Align any EF packages already in your application with a compatible EF Core 10 version. Projects with central package management should retain that convention. The toolkit's `.csproj` is the dependency/version reference. [Package command documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-package-add)

For manual XML merging, copy only the relevant `PackageReference` entries into an `ItemGroup` in your own project. Retain the EF Design entry's `PrivateAssets`/`IncludeAssets` metadata. Preserve other packages, project settings, and an existing `UserSecretsId`; do not replace the whole file or introduce a second `.csproj`.

![BeautyParlour.csproj showing the authentication package references alongside the application's OpenAPI package](https://github.com/user-attachments/assets/7a43baf7-e7f2-4309-8ef4-9c7883b2caaf)

*BeautyParlour Project Reference — authentication dependencies in the application's own project file. Each application initializes its own User Secrets store; retain your own UserSecretsId.*

### B3. Merge Program.cs registration

Add these imports:

```csharp
using DevToolkit.Auth.Constants;
using DevToolkit.Auth.Extensions;
using Microsoft.EntityFrameworkCore;
```

After your `builder` is created and before `builder.Build()`, add:

```csharp
var authConnectionString = builder.Configuration.GetConnectionString("YourAppDataBase");

if (string.IsNullOrWhiteSpace(authConnectionString))
{
    throw new InvalidOperationException("Configure ConnectionStrings:YourAppDataBase.");
}

builder.Services.AddDevToolkitAuth(
    builder.Configuration,
    database => database.UseSqlServer(authConnectionString)
);
```

If this JWT scheme is the API's main authentication method, also add:

```csharp
builder.Services.AddAuthentication(AuthSchemes.CustomJwt);
```

An application already using cookies or another default can retain its own default. Module account endpoints select the module JWT scheme explicitly. Your own JWT-protected endpoints need an explicit scheme/policy when another scheme remains the default.

Merge these stages after `builder.Build()`:

```csharp
app.UseRouting();
// Your named CORS policy belongs here, if present.
app.UseDevToolkitAuth();
app.MapControllers();
app.MapDevToolkitHealthChecks();
```

`UseDevToolkitAuth()` contains authentication, authorization, rate limiting, and auth abuse limiting. Merge those stages once if your application already has them. Keep your existing HTTPS/trusted-proxy handling and global exception handler. The starter's [Program.cs](Program.cs) shows the optional global exception middleware and CORS registration.

Copying the module does not register CORS. An existing API can keep its CORS policy; a separate frontend origin needs permission for that origin and headers such as `Authorization` and `Content-Type`. The starter reads `Cors:AllowedOrigins` for its named policy.

The basic integration uses `AuthDbContext`, which can coexist with a business context when each owns its tables. It does not automatically merge an existing identity schema. A [shared business context](#optional-share-a-database-context-with-business-features) is covered below.

## Application configuration

For Path A, edit the existing values. For Path B, merge missing sections into your own settings and retain your other configuration.

This annotated example explains the application-specific values:

```jsonc
{
  "ConnectionStrings": {
    // This is the lookup key used in Program.cs, not the SQL database name.
    // Save the real connection string in User Secrets below.
    "YourAppDataBase": ""
  },
  "AppInfo": {
    "Name": "YourApp" // Public name used in authentication email text.
  },
  "JwtSettings": {
    "SecretKey": "", // Generated and stored in User Secrets.
    "Issuer": "YourApp.Api",
    "Audience": "YourApp.Web",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 14,
    "AbsoluteSessionDays": 30
  },
  "Otp": {
    "HashKey": "", // Generated and stored in User Secrets.
    "ExpiryMinutes": 10,
    "ResendCooldownSeconds": 60,
    "MaxAttempts": 5,
    "ResetTokenExpiryMinutes": 10
  },
  "GoogleAuth": {
    "ClientId": "" // Optional until Google sign-in is tested.
  },
  "Email": {
    "Host": "",
    "Port": 587,
    "Username": "",
    "Password": "", // Real SMTP credentials belong in User Secrets.
    "FromEmail": "",
    "FromName": "YourApp",
    "UseSsl": false
  },
  "AuthModule": {
    "EnableAbuseProtection": true,
    "AuthIpPermitLimit": 60,
    "AuthAccountPermitLimit": 15,
    "AuthWindowSeconds": 300,
    "AuthInspectionMaxBytes": 16384,
    "SessionStoreSlowMilliseconds": 1000,
    "HealthCheckTimeoutSeconds": 5
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"] // Your frontend origin.
  },
  "AllowedHosts": "localhost" // Your deployed API hostname when applicable.
}
```

ASP.NET Core configuration supports comments. The committed starter settings remain plain JSON for compatibility with other JSON tools. This example omits logging for clarity; retain your logging settings when merging.

### Connection key versus database name

These locations must use the same **lookup key**:

| Location                                       | This guide's value                       |
| ---------------------------------------------- | ---------------------------------------- |
| Property under `ConnectionStrings` in settings | `YourAppDataBase`                        |
| `Program.cs` lookup                            | `GetConnectionString("YourAppDataBase")` |
| User Secrets key                               | `ConnectionStrings:YourAppDataBase`      |

The key can stay `YourAppDataBase` even if the actual database is named `BeautyParlour`. The database name is the `Database=...` part of the connection string. A lookup-key rename must be reflected in all three locations.

An existing application may retain `AuthDatabase` or another key if its three locations already agree.

## Optional: use your own code prefix

The original namespaces work as supplied. For your own prefix, complete the startup integration above first so replacement also covers those imports and calls.

1. Save your work and make a backup or local commit.
2. Open VS Code workspace Replace with **Ctrl+Shift+H**.
3. Search for `DevToolkit` and replace with `YourApp`, or your own valid C# identifier.
4. Enable **Match Case** and disable regular expressions.
5. Set **files to include** to `AuthModule/**/*.cs,Program.cs,*.csproj`.
6. Review the changes, including migration designer/snapshot C# files, then apply replacement.

Use one prefix, such as `YourApp`. Do not enter a path, spaces, a hyphenated name, or the literal text `YourApp.API/YourApp`.

| Before                        | After                      |
| ----------------------------- | -------------------------- |
| `DevToolkit.Auth.Extensions`  | `YourApp.Auth.Extensions`  |
| `AddDevToolkitAuth(...)`      | `AddYourAppAuth(...)`      |
| `UseDevToolkitAuth()`         | `UseYourAppAuth()`         |
| `MapDevToolkitHealthChecks()` | `MapYourAppHealthChecks()` |

Text replacement does not rename filenames. `YourApp.Auth` code can live in `YourApp.Api.csproj`; namespace and project filenames need not be identical. Keep replacement scoped to application code, excluding this guide and repository links. Lowercase rate-policy names remain internal identifiers. Later snippets with the original prefix need its corresponding replacement if you chose this step. [VS Code search and replace](https://code.visualstudio.com/docs/editing/codebasics#_search-across-files)

![VS Code Search panel showing DevToolkit replaced with BeautyParlour](https://github.com/user-attachments/assets/8f39843f-9710-4baf-8163-0dae8d61cea2)

*BeautyParlour Project Reference — renaming the code prefix in VS Code.*

The screenshot shows the search and replacement fields. Apply the **Files to include** filter in step 5 before replacing: `AuthModule/**/*.cs,Program.cs,*.csproj`. Leave README text, repository URLs, and SQL text outside this replacement.

## Build checkpoint

From the folder containing your application project:

```powershell
dotnet restore
dotnet build
```

Expected: **Build succeeded**. Fix any errors before continuing. This checks the code; the database tables are created later. `dotnet run` also restores and builds automatically, but this checkpoint helps catch integration errors early.

## Store development secrets

### 1. Enable User Secrets

```powershell
dotnet user-secrets init
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

The first command enables a local secret store for your project. The second sets this terminal to **Development**, so the app can read those secrets. Keep using this terminal for migration and startup. If you open another terminal later, set Development there too.

Initialization adds a `UserSecretsId` to your project file. Keep an existing ID; a new independent application should initialize its own store.

User Secrets stores values outside the repository in your user profile. It is local development storage, not an encrypted production vault. `UserSecretsId` identifies the store and is not a password. [User Secrets documentation](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0)

### 2. Save the SQL Server connection string

For a local default SQL Server instance with Windows authentication:

```powershell
dotnet user-secrets set "ConnectionStrings:YourAppDataBase" "Server=localhost;Database=YourApp;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"
```

Replace `Database=YourApp` with your database name and use the server name that works in SQL Server Management Studio:

| Installed instance type | Example server name      |
| ----------------------- | ------------------------ |
| Local default instance  | `localhost`              |
| Named instance          | `.\SQLEXPRESS`           |
| LocalDB                 | `(localdb)\MSSQLLocalDB` |

These are alternatives, not instances installed by the toolkit. The example trusts a local development certificate; deployment needs an appropriate server certificate and validation. For SQL authentication, save your provider-approved connection string under the same secret key, including its credentials outside the committed settings file.

### 3. Generate JWT and OTP keys

Copy and run this whole block. It creates and saves two random keys for your application. Leave the variable names and byte counts as shown:

```powershell
$random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$jwtBytes = New-Object byte[] 48
$otpBytes = New-Object byte[] 32

$random.GetBytes($jwtBytes)
$random.GetBytes($otpBytes)
$random.Dispose()

dotnet user-secrets set "JwtSettings:SecretKey" ([Convert]::ToBase64String($jwtBytes))
dotnet user-secrets set "Otp:HashKey" ([Convert]::ToBase64String($otpBytes))
```

Each `set` should report **Successfully saved**. This block works with Windows PowerShell without requiring the newer static random-byte overload. Re-running it replaces the keys: a JWT-key change invalidates existing access tokens, and an OTP-key change invalidates outstanding codes.

| Value                         | Responsibility                                           |
| ----------------------------- | -------------------------------------------------------- |
| `JwtSettings:SecretKey`       | Server signing/validation key; not a Bearer token        |
| `Otp:HashKey`                 | Server key protecting stored OTP hashes                  |
| Login response `accessToken`  | JWT sent by a client in Authorization                    |
| Login response `refreshToken` | Refresh/logout token; its hash is stored in the database |

Keep committed secret fields blank. Google and SMTP can remain unconfigured for the first registration/login test.

## Create the database

### 1. Prepare the EF tool

```powershell
dotnet ef --version
```

If missing, install it:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.14
```

If already installed, update it to match this starter:

```powershell
dotnet tool update --global dotnet-ef --version 10.0.14
```

### 2. Apply the migration

The initial SQL Server migration already exists; no second initial migration is needed for the built-in auth context.

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --context AuthDbContext
```

This creates the database if your account has permission, applies pending migrations, and finishes with **Done**. The API does not run migrations automatically during startup.

![BeautyParlour terminal showing a successful build, application of the InitialAuth migration, and Done](https://github.com/user-attachments/assets/aa7e3857-996e-490c-86d9-3f11e1f86697)

*BeautyParlour Project Reference — the initial authentication migration applied successfully.*

If you already applied the migrations, run the same update command again. EF applies only pending migrations; when none remain, it reports that the database is already up to date. You do not need to drop the database, create another initial migration, or register the same test account again. Continue API testing with the existing account.

Expected tables are `AppUsers`, `Roles`, `AppUserRoles`, `ExternalLogins`, `UserSessions`, `RefreshTokens`, `OtpCodes`, `PasswordResetSessions`, and `UserAuthEvents`, plus `__EFMigrationsHistory`. Role records are `User`, `Admin`, and `Owner`; no admin account is created.

![SQL Server query results showing the nine BeautyParlour authentication tables and the EF migrations history table](https://github.com/user-attachments/assets/645370bf-2828-44b6-b395-1c0dada9e868)

*BeautyParlour Project Reference — nine authentication tables plus __EFMigrationsHistory in SQL Server.*

### Alternative SQL export

[AuthModule.sql](AuthModule.sql) is the SQL Server migration export. It can be inspected or applied to an existing target database using SSMS or `sqlcmd` instead of the EF update command. It includes migration-history records. Select the right database, review the script, and use a runner that stops on errors.

It does not create/select a database or merge unrelated existing tables. Its guards use migration history, not automatic repair of manually created schemas. C# migrations remain the schema source of truth. [EF migration deployment](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)

## Start the API

From the same API folder and Development terminal used above, run:

```powershell
dotnet run
```

That is the startup command. It builds and starts your application. [Microsoft dotnet run documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run)

![BeautyParlour terminal showing dotnet run and the API listening on localhost port 5148 in Development](https://github.com/user-attachments/assets/795dd197-1b2c-4dfb-9915-a1b7b389a51c)

*BeautyParlour Project Reference — `dotnet run` starts this app using its existing launch profile.*

Keep the terminal running and find **Now listening on:**. Copy that address for your API requests. BeautyParlour uses `http://localhost:5148`; your port may be different. A starter without a launch profile normally uses `http://localhost:5000` unless another URL is configured.

If you opened a new terminal and its launch profile does not already select Development, run this once **before** `dotnet run` so User Secrets are loaded:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

The project folder must contain one application `.csproj`. If you see a multiple-project error, resolve the extra project file before continuing. Stop the API with **Ctrl+C** when finished.

## Test with Thunder Client

The following screenshots show **BeautyParlour Project Reference** testing specific API routes in Thunder Client. The **method and URL in the request bar** identify the endpoint being tested. The credentials and tokens shown belong to the disposable test example.

The examples use `http://localhost:5148` to match the screenshots. Replace that address with your own **Now listening on:** address. For example, if your app listens on port 5000, use `http://localhost:5000/auth/custom/login` for login.

For each request, select its method, enter the URL, choose **Body → JSON** when a body is shown, and click **Send**. JSON requests use `Content-Type: application/json`. Use **Auth → None** for readiness, register, login, refresh, and logout; Bearer authentication is used for the protected session routes.

The images demonstrate backend requests and responses. You can call these same routes from **React, Angular, a TypeScript or JavaScript app, a mobile app, or another frontend**. Thunder Client helps you test the backend before connecting your interface.

### 1. Readiness

```http
GET http://localhost:5148/health/ready
```

Expected: **200 OK**, body `Healthy`. A 503 means a readiness check failed, commonly because the database or auth tables are unavailable.

![Thunder Client GET health readiness request returning 200 OK and Healthy](https://github.com/user-attachments/assets/a85ea550-ff0b-40a3-b103-a72e8efc7f0a)

*BeautyParlour Project Reference — `GET /health/ready` returns `200 OK` and `Healthy`.*

### 2. Register

```http
POST http://localhost:5148/auth/custom/register
```

Use **Auth → None** and this body:

```json
{
  "fullName": "Beauty Parlour Test",
  "email": "beautytest2@example.com",
  "password": "ExamplePass123!"
}
```

Expected: **200 OK**, containing `accessToken`, `refreshToken`, `accessTokenExpiresAt`, and a `user` with the `User` role. An already-registered email returns 409; log in with that account or use a different test address.

![Thunder Client registration request returning 200 OK with its authentication response](https://github.com/user-attachments/assets/78ed66aa-8560-4897-b0a5-7bead1bf2803)

*BeautyParlour Project Reference — `POST /auth/custom/register` creates a test account. The request body above matches this example. Use a fresh test email for registration, then use that same email and password for login.*

### 3. Log in

```http
POST http://localhost:5148/auth/custom/login
```

Use **Auth → None** and the registered credentials:

```json
{
  "email": "beautytest2@example.com",
  "password": "ExamplePass123!"
}
```

Expected: **200 OK** with fresh tokens. Keep the actual response in your local API client; use its tokens for the next requests. Do not copy token values from screenshots.

![Thunder Client login request returning 200 OK with its authentication response](https://github.com/user-attachments/assets/ae62a57e-0be8-4645-b150-9c8815bd157d)

*BeautyParlour Project Reference — `POST /auth/custom/login` authenticates the registered test account.*

### 4. Validate a token

```http
GET http://localhost:5148/auth/custom/sessions/validate
```

In **Auth → Bearer**, paste the login response's `accessToken` without JSON quotes or another `Bearer` prefix. This module's JWT has three dot-separated parts. Signing keys, refresh tokens, and database hashes are not Bearer access tokens.

Expected: **204 No Content**, empty body. With **Auth → None**, the request should return **401 Unauthorized**.

![Thunder Client Bearer session validation returning 204 No Content for the test account](https://github.com/user-attachments/assets/4c218db8-5505-43cb-ace3-54445da7b7fc)

*BeautyParlour Project Reference — `GET /auth/custom/sessions/validate` accepts an active authenticated session.*

### 5. List sessions

```http
GET http://localhost:5148/auth/custom/sessions
```

Use the same access token. Expected: **200 OK** with device-session records and a current-session marker. Registration and a subsequent login create separate sessions.

### 6. Refresh

```http
POST http://localhost:5148/auth/custom/refresh
```

Use **Auth → None** and the latest login refresh token:

```json
{
  "refreshToken": "PASTE_THE_LATEST_REFRESH_TOKEN_HERE"
}
```

Expected: **200 OK** with a replacement access/refresh pair. Use both new values from now on. Reusing a consumed refresh token revokes its associated device session, so do not repeatedly send an old value during a normal refresh test.

### 7. Logout and revocation

```http
POST http://localhost:5148/auth/custom/logout
```

Use **Auth → None** and the replacement refresh token:

```json
{
  "refreshToken": "PASTE_THE_REPLACEMENT_REFRESH_TOKEN_HERE"
}
```

Expected: **200 OK** with a logout message.

![Thunder Client logout request returning 200 OK and Logged out successfully for the test session](https://github.com/user-attachments/assets/d72bd6fd-46cf-4056-a371-0c5162f978a9)

*BeautyParlour Project Reference — `POST /auth/custom/logout` returns a successful logout response.*

Now send `GET /auth/custom/sessions/validate` again using **that same session's access token**. Expected: **401 Unauthorized**. The logout screenshot shows the logout response; this follow-up request checks that session revocation took effect. Another login creates a new session.

The [API reference](README.md#api-reference) lists other account/security operations. Repeated requests may return 429 when rate limits are reached; wait for the window to reset rather than treating it as a password failure.

## Connect your frontend

After the API tests pass, connect your React, Angular, TypeScript, JavaScript, or other frontend to the same routes. Your frontend provides the forms and sends HTTP requests to this API.

| Frontend action | Backend request |
| --- | --- |
| Create an account | `POST /auth/custom/register` with full name, email, and password |
| Sign in | `POST /auth/custom/login` with email and password |
| Validate the current session | `GET /auth/custom/sessions/validate` with `Authorization: Bearer <accessToken>` |
| Refresh tokens | `POST /auth/custom/refresh` with the latest refresh token |
| Sign out | `POST /auth/custom/logout` with the latest refresh token |

Set the frontend's API base URL to your backend's listening address. A React app might use `http://localhost:5173` for its own page while calling the API at `http://localhost:5148`; those are different applications and ports.

In the backend, add the frontend's exact origin to `Cors:AllowedOrigins`, or keep the equivalent CORS policy in your existing API. Allow the methods and headers your frontend sends, including `Authorization` and `Content-Type`. Restart the backend after changing configuration. Thunder Client does not enforce browser CORS rules, so also test from the browser after connecting your UI.

Use the `accessToken` returned by your own login response for protected requests. JWT signing keys stay on the backend. Frontend token storage and the interface belong to your application; the toolkit does not automatically write browser local storage.

You can stop the basic setup here once register, login, token validation, refresh, and logout work. The remaining sections cover optional integrations.

## Optional email recovery and Google sign-in

Complete the basic API setup first. Add either integration below when your application needs it.

| You want to | Configure |
| --- | --- |
| Let users sign in with their Google account | Google consent settings, a web OAuth Client ID, and a frontend Google sign-in button |
| Send password-reset OTP emails using Gmail | A Gmail App Password and the backend's email settings |

### A. Set up Google sign-in

#### 1. Create a Google Cloud project and consent configuration

1. Open [Google Cloud Console](https://console.cloud.google.com/) and create or select a project, such as `BeautyParlour`.
2. Open **Google Auth Platform → Branding**. If prompted, choose **Get started**.
3. Enter your application's name, support email, and developer contact email.
4. Choose **External** for a public application serving Google accounts outside your organization. **Internal** is for an eligible Google Workspace organization.
5. Finish the consent setup. Under **Audience → Test users**, add your own Google email and the accounts you will use while testing an External app.
6. Review **Data Access**. For basic sign-in, use the standard `openid`, `email`, and `profile` identity scopes; Gmail or Drive access is unnecessary.

Before launching your application, review its Branding and Audience settings and any verification requirements shown by Google. Making the DevToolkit GitHub repository public does not publish your application's Google consent configuration. [Google's consent setup guide](https://developers.google.com/workspace/guides/configure-oauth-consent)

#### 2. Create the web OAuth Client ID

1. Open **Google Auth Platform → Clients → Create client**.
2. Select **Web application** and give it a name, such as `BeautyParlour Web`.
3. Under **Authorized JavaScript origins**, add your frontend's origin. For local React/Vite testing, add both `http://localhost` and `http://localhost:5173`. An Angular app might use `http://localhost:4200` instead; add the port your frontend actually uses.
4. Origins contain the scheme, host, and optional port. Do not add a page path such as `/login` here.
5. This guide uses a JavaScript sign-in callback. **Authorized redirect URIs** are needed if you choose Google's redirect mode instead.
6. Create the client and copy its **Client ID**, ending in `.apps.googleusercontent.com`.

Use your frontend's origin here; its port can differ from the backend's. Add your deployed frontend's HTTPS origin when you host the application. [Google's web client setup](https://developers.google.com/identity/gsi/web/guides/get-google-api-clientid)

#### 3. Configure the backend and frontend

From your API folder, replace the placeholder and run:

```powershell
dotnet user-secrets set "GoogleAuth:ClientId" "YOUR_WEB_CLIENT_ID.apps.googleusercontent.com"
```

Use that **same Client ID** in your frontend's Google Identity Services configuration. The Client ID identifies the app and can appear in frontend configuration. This toolkit's ID-token flow does not require a Client Secret.

Add the frontend origin to the backend's `Cors:AllowedOrigins` as described in [Connect your frontend](#connect-your-frontend). Keep the Development setting from the main guide, then restart the API with `dotnet run`.

#### 4. Connect the sign-in button and test

Configure a Google Identity Services button in your React, Angular, TypeScript, or other frontend. In its JavaScript callback, Google's `response.credential` is the **Google ID token**. Send that value to the backend as JSON. [Google's credential-response documentation](https://developers.google.com/identity/gsi/web/guides/display-google-one-tap#credential_response)

```http
POST http://localhost:5148/auth/custom/google
Content-Type: application/json
```

```json
{
  "idToken": "PASTE_THE_GOOGLE_ID_TOKEN_FROM_THE_FRONTEND_CALLBACK"
}
```

Use your API's actual port. For a manual Thunder Client check, use **Auth → None** and paste a fresh ID token from your configured frontend. Thunder Client does not generate Google ID tokens for you. The repository supplies the backend endpoint; you add the frontend button.

Expected: **200 OK** with DevToolkit's `accessToken`, `refreshToken`, and account details. Use the returned **DevToolkit access token** for `/auth/custom/sessions/validate`. The Google ID token is the input to Google sign-in, not the Bearer token for this API's protected routes.

### B. Set up Gmail App Passwords for OTP email

This configures the backend to send recovery emails. Users can still sign in with email/password without Google sign-in being configured.

#### 1. Generate the App Password

1. Sign in to the Google account whose Gmail address will send the emails.
2. Open [Google Account security settings](https://myaccount.google.com/security) and enable **2-Step Verification** if it is not already enabled.
3. Open [App Passwords](https://myaccount.google.com/apppasswords). Google may ask you to sign in again.
4. If prompted for an app name, use a recognizable name such as `BeautyParlour Mail`, then create the password.
5. Copy the generated App Password. Use this value for `Email:Password`, rather than your normal Google account password.

If App Passwords is unavailable, check Google's eligibility notes: organization policies, Advanced Protection, or security-key-only 2-Step Verification can prevent the option from appearing. Changing your Google account password also revokes existing App Passwords. [Google's App Password guide](https://support.google.com/accounts/answer/185833?hl=en)

#### 2. Save Gmail settings in User Secrets

Replace `YOUR_GMAIL_ADDRESS` and `YOUR_GOOGLE_APP_PASSWORD`, then run from your API folder:

```powershell
dotnet user-secrets set "Email:Host" "smtp.gmail.com"
dotnet user-secrets set "Email:Port" "587"
dotnet user-secrets set "Email:Username" "YOUR_GMAIL_ADDRESS"
dotnet user-secrets set "Email:Password" "YOUR_GOOGLE_APP_PASSWORD"
dotnet user-secrets set "Email:FromEmail" "YOUR_GMAIL_ADDRESS"
dotnet user-secrets set "Email:FromName" "YourApp"
dotnet user-secrets set "Email:UseSsl" "false"
```

Use the same Gmail address for `Username` and `FromEmail` in this basic example. Port **587** uses **STARTTLS** in this toolkit, so `UseSsl=false` still uses an encrypted connection. `UseSsl=true` selects implicit TLS, normally on port 465. [Google's Gmail SMTP settings](https://support.google.com/mail/answer/7104828?hl=en)

Restart the API with `dotnet run` after saving the settings. Other email providers can use their own supported SMTP configuration or replace `IEmailService`.

#### 3. Test delivery and password recovery

Use a registered email/password account whose inbox you can open. The Gmail sender account and the recipient account do not have to be the same.

1. Send `POST /auth/custom/forgot-password/request` with `{ "email": "YOUR_REGISTERED_EMAIL" }`.
2. Open the recipient's inbox and check Spam/Junk too. A neutral **200** response by itself does not prove an email was delivered.
3. Send `POST /auth/custom/forgot-password/verify` with the same `email` and the received six-digit `code`.
4. Send `POST /auth/custom/forgot-password/reset` with the returned `resetToken` and a `newPassword`.
5. Confirm the old password fails, the new password works, and old sessions are rejected.

### Common Google setup problems

| Problem | Check |
| --- | --- |
| Google reports an origin mismatch | Match the actual frontend scheme, hostname, and port in Authorized JavaScript origins; `localhost` and `127.0.0.1` are different hosts |
| A test account cannot access the OAuth app | Check Audience, the app's testing status, and its test-user list |
| Backend rejects the Google token | Use a fresh Google ID token and the same web Client ID in frontend and `GoogleAuth:ClientId` |
| Gmail authentication fails | Check the full Gmail username, App Password, 2-Step Verification eligibility, and port/TLS settings |
| Gmail stopped working after a Google password change | Generate a new App Password and update `Email:Password` |

## Optional: share a database context with business features

The basic guide uses the module context. For one shared context, your application can inherit from `AuthDbContext`, or implement `IAuthDbContext` and apply the mappings from `AuthModelConfiguration`.

Register that context and use the generic overload, such as `AddDevToolkitAuth<YourAppDbContext>(...)` before optional prefix renaming. The shared context needs its own reviewed migrations; the included `AuthDbContext` migration is not a migration for another context. Existing account tables and roles need explicit integration rather than an overwrite.

Application profiles can link through `AppUserId`. `IUserRegisteredHook` supports account-creation work; only operations sharing the auth context participate in the same registration transaction.

## Troubleshooting

| Result                                           | Check                                                                                                                                      |
| ------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------ |
| `MSB1011`: more than one project/solution        | Open the folder containing your app's own `.csproj`; keep reference clones and accidentally copied extra project files outside that folder |
| EF, BCrypt, Google, or MailKit namespace missing | Check dependencies are in the actual application `.csproj`, then restore it                                                                |
| Duplicate top-level statements                   | Look for a nested clone or second startup source included by the project                                                                   |
| Registration method missing after renaming       | Use renamed imports/methods consistently; include migration C# files in replacement and rebuild                                            |
| Connection string missing                        | Match `YourAppDataBase` across configuration, Program.cs, and User Secrets                                                                 |
| JWT/OTP settings missing                         | Check the application's `UserSecretsId`, exact key names, and Development environment                                                      |
| SQL connection/login failure                     | Check SSMS server name, authentication, permissions, and certificate settings                                                              |
| EF tool unavailable                              | Install/update it and reopen the terminal if its tool path was just added                                                                  |
| Readiness 503                                    | Check connectivity and that migration was applied to this application's database                                                           |
| Token validation 401                             | Use a fresh access token; check expiry, revocation, and the API URL                                                                        |
| Token validation 204 with empty body             | Expected success                                                                                                                           |
| Browser CORS error                               | Check actual frontend origin and allowed headers; Thunder Client does not enforce browser CORS rules                                       |

## Development and deployment

These commands use local Development configuration. Deployments need HTTPS, production secret storage, appropriate trusted-proxy handling, and deployed frontend origins/hostnames. User Secrets is not deployed with the API. Limiter state is process-local; multiple instances need a shared limiting strategy.

After adopting the starter, application-specific changes belong in your own repository. Existing APIs retain their own Git metadata/history throughout this integration.
