# DevToolkit Auth

A runnable .NET 10 Web API with authentication source you can copy into an existing API. SQL Server is the default database. All authentication dependencies live in `AuthModule`; there is one application project and no separate library project.

Includes email/password registration and login, JWT access tokens, rotating refresh tokens, device sessions, Google sign-in, OTP password recovery, password changes, request limiting, and authentication health checks.

## Choose how to use it

**Starting a new backend:** clone into an empty folder and build your application here. You do not need to create another Web API.

```powershell
mkdir BeautyParlour
cd BeautyParlour
git clone https://github.com/Tanish15-gif/dev-toolkit.git .
```

The final `.` puts the repository files directly in the current empty folder. Alternatively, run `git clone https://github.com/Tanish15-gif/dev-toolkit.git BeautyParlour` from its parent folder. Git needs a destination directory; it does not merge a repository into an existing application automatically.

**Adding authentication to an existing backend:** clone into a separate temporary folder, then copy only `AuthModule` into your API. Keep your own `Program.cs`, project file, configuration, Git history, and business code. Follow [existing project integration](#existing-project-integration).

```text
README.md
.gitignore
AuthModule.sql
DevToolkit.Api.csproj
Program.cs
appsettings.json
AuthModule/
    Controllers/
    DTOs/
    Entities/
    Data/
        AuthDbContext.cs
        AuthModelConfiguration.cs
        Migrations/
    Services/
        Context/
        Email/
    Repositories/
    Interfaces/
        Services/
        Repositories/
    Authorization/
    Constants/
    Enums/
    Models/
    Helpers/
    Exceptions/
    Middleware/
    HealthChecks/
    Extensions/
    Settings/
```

`bin` and `obj` are generated when you build and are ignored by Git. No folder contains placeholder `.gitkeep` files.

## Run a new project

You need the .NET 10 SDK and a SQL Server instance that your Windows account or configured SQL login can access.

### 1. Configure your application

In `appsettings.json`, change these non-secret values:

| Setting | Example |
| --- | --- |
| `AppInfo:Name` | `Beauty Parlour` |
| `JwtSettings:Issuer` | `BeautyParlour.Api` |
| `JwtSettings:Audience` | `BeautyParlour.Web` |
| `Cors:AllowedOrigins` | Your frontend origin, such as `http://localhost:5173` |
| `Email:FromName` | `Beauty Parlour` |
| `AllowedHosts` | Your deployed API hostname; `localhost` for local use |

You can keep the project filename and `DevToolkit.Auth` namespaces. Neither must match your application's display name. To rename the starter project, rename `DevToolkit.Api.csproj` and use the new filename in commands below. The next step generates a separate User Secrets ID for your application.

### 2. Save local secrets

Run from the folder containing `DevToolkit.Api.csproj`:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:AuthDatabase" "Server=localhost;Database=BeautyParlour;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"

$random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$jwtBytes = New-Object byte[] 48
$otpBytes = New-Object byte[] 32
$random.GetBytes($jwtBytes)
$random.GetBytes($otpBytes)
$random.Dispose()

dotnet user-secrets set "JwtSettings:SecretKey" ([Convert]::ToBase64String($jwtBytes))
dotnet user-secrets set "Otp:HashKey" ([Convert]::ToBase64String($otpBytes))
```

Use the server name you connect to in SQL Server Management Studio. `localhost` means a local default instance, not a named instance or LocalDB. The local `TrustServerCertificate=True` example is for development; use a valid server certificate and certificate validation when deploying.

The JWT secret and OTP key are required. Their committed values are intentionally empty. Google and email can be configured after testing registration/login. Google sign-in will require a client ID, and password-recovery email will require an email provider.

For deployment, supply secrets through your hosting secret store or environment variables, for example `JwtSettings__SecretKey`, `Otp__HashKey`, and `ConnectionStrings__AuthDatabase`. User Secrets are for development, and server secrets must never go into React configuration.

### 3. Restore and create the authentication tables

```powershell
dotnet restore
dotnet build
dotnet tool install --global dotnet-ef --version 10.0.14
```

If `dotnet-ef` is already installed, check `dotnet ef --version` and use a compatible EF Core 10 tool version. The starter includes the initial SQL Server migration, so run:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --context AuthDbContext
```

This creates the nine authentication tables and the `User`, `Admin`, and `Owner` role records. It does not create an administrator account. Public registration always assigns `User`.

The application never runs migrations automatically during startup. Review migrations before applying them to an existing database. Business tables remain your application's responsibility.

#### Read or apply the raw SQL

[AuthModule.sql](AuthModule.sql) contains the SQL Server schema for the nine authentication tables, their keys/indexes/relationships, and the `User`, `Admin`, and `Owner` role records. It includes `__EFMigrationsHistory` so EF tooling recognizes the migration after the SQL is applied. It contains no seeded accounts, credentials, token values, or business data.

You can use this script instead of `dotnet ef database update`. Create your target database first, then open the file in SQL Server Management Studio, select that database, review the script, and execute it. With `sqlcmd`, replace the example server/database as needed:

```powershell
sqlcmd -S localhost -d BeautyParlour -E -C -b -i .\AuthModule.sql
```

`-E` uses Windows authentication, `-C` trusts the local development certificate, and `-b` stops the command on SQL errors. Use certificate validation when deploying. The file does not create or select a database.

The script is [idempotent through EF migration history](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying#idempotent-sql-scripts): migrations already recorded as applied are skipped. Existing tables without matching migration history are not automatically merged or upgraded. Keep the built-in auth context as the owner of these tables; a shared business context needs its own reviewed migration as described below.

After changing the model and adding a migration, refresh the root SQL export:

```powershell
dotnet ef migrations script --idempotent --context AuthDbContext --output AuthModule.sql
```

This replaces the exported file. Preserve its introductory execution notes and `SET XACT_ABORT ON` setting when updating it. The C# migrations remain the source of truth; update the SQL export alongside them.

### 4. Start the API

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --urls "http://localhost:5000"
```

`GET http://localhost:5000/health` checks whether the process is running. `GET /health/ready` also checks that the authentication session table can be read. Readiness returns 503 if the database or table is unavailable. There is no Swagger UI bundled; use the requests below, your API client, or a frontend.

Use HTTPS in deployment. When hosting behind a reverse proxy, configure trusted forwarded headers before the auth pipeline so client IP limits use the correct address. Configure the deployment HTTPS port or terminate HTTPS at a correctly configured proxy.

### 5. Test registration, access, refresh, and logout

In a second PowerShell terminal:

```powershell
$baseUrl = "http://localhost:5000"
$body = @{
    fullName = "Test Developer"
    email = "developer@example.com"
    password = "ExamplePassword123!"
} | ConvertTo-Json

$auth = Invoke-RestMethod "$baseUrl/auth/custom/register" -Method Post -ContentType "application/json" -Body $body
$headers = @{ Authorization = "Bearer $($auth.accessToken)" }

Invoke-WebRequest "$baseUrl/auth/custom/sessions/validate" -Headers $headers
Invoke-RestMethod "$baseUrl/auth/custom/sessions" -Headers $headers

$refreshBody = @{ refreshToken = $auth.refreshToken } | ConvertTo-Json
$auth = Invoke-RestMethod "$baseUrl/auth/custom/refresh" -Method Post -ContentType "application/json" -Body $refreshBody

$logoutBody = @{ refreshToken = $auth.refreshToken } | ConvertTo-Json
Invoke-RestMethod "$baseUrl/auth/custom/logout" -Method Post -ContentType "application/json" -Body $logoutBody
```

Session validation returns HTTP 204 while authenticated. After logout, using an access token from that session must return 401. To log in again, send `email` and `password` to `POST /auth/custom/login`.

Always replace both stored tokens with the refresh response. Reusing a consumed refresh token revokes that device session. Clients should coordinate simultaneous refresh requests, including requests from multiple browser tabs.

## Existing project integration

This source targets .NET 10. An application with an existing identity system needs an explicit account/schema integration; copying this module does not merge existing users automatically.

1. Copy the complete `AuthModule` folder into your API project, outside any other module of the same name. Do not copy the toolkit's `Program.cs` or `.csproj` over yours.
2. Add the package references from `DevToolkit.Api.csproj` to your own project, aligning EF Core package versions with your application.
3. Merge the configuration sections from the starter into your configuration and set your own secrets. In an existing project, run `dotnet user-secrets init` if it has no User Secrets configuration.
4. Register the module in your existing `Program.cs` before `builder.Build()`:

```csharp
using DevToolkit.Auth.Constants;
using DevToolkit.Auth.Extensions;
using Microsoft.EntityFrameworkCore;

builder.Services.AddDevToolkitAuth(
    builder.Configuration,
    database => database.UseSqlServer(
        builder.Configuration.GetConnectionString("AuthDatabase")
    )
);
```

5. Merge these pipeline calls in order. Keep your existing exception handling, HTTPS, trusted-proxy handling, and CORS setup:

```csharp
app.UseRouting();
// Your named CORS policy goes here, if needed.
app.UseDevToolkitAuth();
app.MapControllers();
app.MapDevToolkitHealthChecks();
```

`UseDevToolkitAuth()` includes authentication, authorization, rate limiting, and the auth abuse limiter. If your application already calls `UseAuthentication()`, `UseAuthorization()`, or `UseRateLimiter()`, merge these stages rather than running them twice. A host with a more complex pipeline can call the individual stages explicitly; see `AuthApplicationBuilderExtensions.cs`.

The module adds its named JWT scheme without replacing an existing authentication default. For an API that uses this JWT scheme as its main sign-in method, add `builder.Services.AddAuthentication(AuthSchemes.CustomJwt)`. In applications that keep another default, protect JWT endpoints with `[Authorize(AuthenticationSchemes = AuthSchemes.CustomJwt)]` or an explicit policy. Login and registration remain anonymous even if your host has a fallback authorization policy.

The module's controller filter handles known authentication errors. The starter also uses `GlobalExceptionMiddleware` for unexpected errors; existing applications can keep their own global exception handler instead.

6. With the built-in auth context, review and apply the included migration using `dotnet ef database update --context AuthDbContext`. Your existing business DbContext can coexist in the same SQL Server database, but each context must own its tables and migrations. Do not map another incompatible user model onto the same table names.

To keep authentication and business data in one DbContext instead, implement `IAuthDbContext` in your existing context, expose the nine auth DbSets, and call `modelBuilder.AddDevToolkitAuthModel()` from `OnModelCreating`. Register that existing context normally, then call:

```csharp
builder.Services.AddDevToolkitAuth<BeautyParlourDbContext>(builder.Configuration);
```

`AddDevToolkitAuthModel()` is in `DevToolkit.Auth.Data`. Generate and review a migration for your own context; the provided `AuthDbContext` migration is not a migration for your business context. An alternative for a new shared context is to inherit from `AuthDbContext`, call its base `OnModelCreating`, and add business mappings afterwards.

Keep the `DevToolkit.Auth` namespaces when copying. This lets the module compile without renaming every file. Source copying means you own and review later updates; it does not provide automatic upstream package updates.

## Add your application's features

The module creates an authentication account, not a BeautyParlour client, employee, appointment, or repair record. Store those models in your own feature folders and link them using `AppUserId`.

If registration needs to create business data, implement `IUserRegisteredHook` and register it before `AddDevToolkitAuth`. The hook runs on password and Google account creation, within the auth transaction. Only operations using the same database context participate in that transaction; remote calls, email delivery, and a separate business DbContext do not roll back with it. Keep external side effects outside this hook or use your application's reliable event/outbox handling.

Account self-service requires authentication, independently of your domain role names. The default role records are `User`, `Admin`, and `Owner`; add your own roles and authorization policies for application features. No public endpoint accepts a role to grant administrative access.

## Email and Google configuration

Set `Email:Host`, `Email:Port`, `Email:FromEmail`, `Email:FromName`, and any provider credentials. Store `Email:Password` in secrets. Port 587 uses required STARTTLS by default; use `Email:UseSsl=true` for implicit TLS, normally port 465. You can register your own `IEmailService` before the module to replace MailKit.

Forgot-password requests return the same message whether an account exists or not. An email delivery failure is logged without the reset code and invalidates the generated OTP; it does not disclose account existence in the HTTP response. Check the configured provider's delivery when testing recovery.

Set `GoogleAuth:ClientId` to your application's Google web client ID. The frontend sends the Google ID token as `{ "idToken": "..." }` to `POST /auth/custom/google`. The backend validates it. Existing-account linking by email is restricted to addresses for which Google is authoritative; third-party email accounts need additional account verification rather than automatic linking.

The backend is frontend-independent. React or another client calls these endpoints; the shop's branded React pages are not bundled. Choose and review the browser token-storage strategy for your application. The current API returns bearer tokens in JSON and does not implement an HttpOnly-cookie flow or BFF.

## API routes

All routes below start with `/auth/custom`.

| Method | Route | Authentication |
| --- | --- | --- |
| POST | `/register` | Anonymous |
| POST | `/login` | Anonymous |
| POST | `/refresh` | Refresh token in body |
| POST | `/logout` | Refresh token in body |
| POST | `/google` | Google ID token in body |
| POST | `/forgot-password/request` | Anonymous |
| POST | `/forgot-password/verify` | Email and OTP |
| POST | `/forgot-password/reset` | Single-use reset token |
| GET | `/security-summary` | JWT |
| POST | `/change-password` | JWT and current password |
| POST | `/set-password/google-reauth` | JWT and fresh linked Google ID token |
| POST | `/set-password/complete` | JWT and single-use setup token |
| GET | `/sessions` | JWT |
| GET | `/sessions/validate` | JWT; returns 204 |
| POST | `/sessions/logout-others` | JWT and `currentRefreshToken` |
| POST | `/sessions/logout-all` | JWT |
| POST | `/sessions/{userSessionId}/revoke` | JWT; another device belonging to this user |

Registration returns `accessToken`, `refreshToken`, `accessTokenExpiresAt`, and a `user` object with `appUserId`, name, email, optional phone/avatar, confirmation state, and roles. Invalid input returns 400; duplicate email returns 409; invalid or revoked credentials return 401; request limits return 429.

## Authentication support

| Folder | Responsibility |
| --- | --- |
| Controllers / DTOs | Endpoints and validated request/response formats |
| Entities / Data | Nine auth tables, mappings, and migrations |
| Services / Repositories | Auth operations and database access |
| Interfaces | Replaceable services, database contract, registration hook |
| Services/Context | Current authenticated user/session and auth event logging |
| Services/Email | Default MailKit email delivery |
| Authorization / Constants / Enums | Role checks, named policies, routes, event types |
| Models / Helpers | Device information, validation, request and response helpers |
| Exceptions | Expected errors and the module controller's exception filter |
| Middleware | Auth request abuse protection and optional global error handling |
| HealthChecks | Read-only session-table availability and latency check |
| Extensions / Settings | Registration, pipeline, endpoint setup, and configuration |

Passwords use BCrypt with work factor 12 and an eight-character minimum. Password input is limited to 72 UTF-8 bytes. Refresh/reset tokens are hashed in the database; OTPs use a keyed HMAC. Password reset and Google password setup use different token purposes. JWT requests validate the database account and device session so revocation and account disablement take effect on subsequent requests. Soft-deleted email addresses remain reserved.

Named limits are 5 auth attempts per five minutes, 3 OTP requests per ten minutes, 10 OTP verifications per five minutes, 120 authenticated reads per minute, and 10 other auth submissions per minute. The additional pre-auth limiter defaults to 60 requests per IP and 15 per supplied email per five minutes. Its values are configurable under `AuthModule`. Limits are held in one application process; multi-instance deployments need a shared limiting strategy.

`/health` is public liveness; `/health/ready` is public auth readiness; `/health/details` requires the module's Admin/Owner policy. A successful read check does not establish database write access. No admin account is seeded, and detailed health output does not include exception messages.

This module does not include Microsoft login, login OTP/MFA, a complete email-confirmation flow, refresh-token cookies, shop business features, or generic submission idempotency. Add features deliberately after verifying the existing flows.

Before deploying, verify the real SQL Server migration, email delivery, Google configuration, frontend session behavior, and hosting configuration. `dotnet build` is a compile check, not proof that external integrations work. If you publish a new application based on this repository, connect its Git remote to your own application repository before pushing application-specific changes.
