# DevToolkit Auth and Backend Utilities

Reusable authentication and backend utilities for ASP.NET Core 10. Reference the
library from your own application, configure it, create the database schema,
and use its authentication endpoints.

Keep the library namespace as `DevToolkit.Auth`. Your application can have any
name. In this guide, `YourProject.Api` is an application you create yourself.
The integration guide is contained entirely in this README.

## Features

- Email/password registration and login with BCrypt.
- JWT access tokens and hashed, rotating refresh tokens.
- Google sign-in and Google reauthentication for password setup.
- Email OTP password recovery with single-use reset sessions.
- Per-device sessions and immediate account/session revocation checks.
- User, Admin, and Owner roles with database-backed authorization.
- Input validation, rate limits, abuse checks, and safe error responses.
- Configurable SMTP email delivery and application branding.
- Optional registration hooks and submission retry protection.
- Public liveness/readiness endpoints and admin-only health diagnostics.

## Repository layout

```text
dev-toolkit/
├── .gitignore
├── DevToolkit.slnx
├── README.md
└── dotnet/
    ├── DevToolkit.Auth.csproj
    ├── GlobalUsings.cs
    ├── AuthModule/
    └── Infrastructure/
```

### Why each folder exists

| Folder or file | Purpose |
| --- | --- |
| AuthModule/Authorization | Database-backed role authorization |
| AuthModule/Constants | Authentication schemes, role names, and auth policy names |
| AuthModule/Controllers | Authentication HTTP endpoints |
| AuthModule/Data | The reusable auth DbContext |
| AuthModule/DTOs | Request and response contracts used by the current auth flows |
| AuthModule/DTOs/Google | Google sign-in and password reauthentication contracts |
| AuthModule/DTOs/LogInOut | Password login and logout contracts |
| AuthModule/DTOs/OTP | OTP verification and reset-token responses |
| AuthModule/DTOs/Passwords | Password change, reset, setup, and session sign-out contracts |
| AuthModule/DTOs/Session | Device/session response contracts |
| AuthModule/Entities | Persisted users, roles, tokens, OTPs, sessions, and events |
| AuthModule/Enums | Auth event types, failure reasons, providers, and OTP purposes |
| AuthModule/Exceptions | Explicit application errors used by auth and backend helpers |
| AuthModule/Extensions | Registration, auth middleware integration, and EF mappings |
| AuthModule/Filters | Expected error responses for authentication controllers |
| AuthModule/Interfaces | Contracts for data access and registration hooks |
| AuthModule/Interfaces/Repositories | Database operation contracts |
| AuthModule/Interfaces/Services | Authentication service contracts |
| AuthModule/Models | Parsed user-agent details for session information |
| AuthModule/Repositories | EF operations, atomic token consumption, and session persistence |
| AuthModule/Services | Credential, Google, token, password, and session behavior |
| AuthModule/Services/Context | Current user/session access and authentication event logging |
| AuthModule/Services/Helpers | Email normalization and active-account validation |
| AuthModule/Settings | JWT, OTP, Google, and application branding options |
| Infrastructure/Email | SMTP settings and the replaceable email adapter |
| Infrastructure/Extensions | Backend registration, endpoint metadata, and health endpoint mapping |
| Infrastructure/HealthChecks | The session-store readiness probe |
| Infrastructure/Helpers | Shared validation, body limits, problem responses, and health JSON |
| Infrastructure/Metadata | Attributes selecting optional authentication and retry protection |
| Infrastructure/Middleware | Error handling, abuse limits, optional auth, and retry protection |
| Infrastructure/RateLimiting | Rate-limit policy names and trusted client/user keys |
| Infrastructure/Settings | Backend limits, route selection, retention, and health timeouts |
| GlobalUsings.cs | Framework imports used by both AuthModule and Infrastructure |
| DevToolkit.Auth.csproj | Builds both folders as one reusable library |

Authentication-specific constants live inside AuthModule. Infrastructure contains
shared backend functionality used by authentication and host application endpoints.
Reference the complete project file when integrating the toolkit.

Generated `bin` and `obj` folders are ignored by Git. They reappear when you build;
they do not belong in a commit. Empty future-feature folders are omitted.


## Build the library

Requires the .NET 10 SDK. From the toolkit repository root:

```powershell
dotnet restore DevToolkit.slnx
dotnet build DevToolkit.slnx
```

## Integrate into a new application

The steps below start with a new API project beside the toolkit checkout. SQLite
is used for the initial setup so a separate database server is not required.

### 1. Create your application and reference the toolkit

Run from the directory containing your projects:

```powershell
git clone https://github.com/Tanish15-gif/dev-toolkit.git dev-toolkit
dotnet new webapi --use-controllers --framework net10.0 -n YourProject.Api
dotnet add YourProject.Api/YourProject.Api.csproj reference dev-toolkit/dotnet/DevToolkit.Auth.csproj
dotnet add YourProject.Api/YourProject.Api.csproj package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.14
dotnet add YourProject.Api/YourProject.Api.csproj package Microsoft.EntityFrameworkCore.Design --version 10.0.14
```

Use your existing checkout if you already cloned the repository. Adjust the
application name and reference path to match your folders. You can also copy the
complete `dotnet` directory into your application repository and reference its
project file.

### 2. Configure your application

Merge these sections into your application's `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "AuthDatabase": "Data Source=your-project-auth.db"
  },
  "JwtSettings": {
    "SecretKey": "",
    "Issuer": "YourProject.Api",
    "Audience": "YourProject.Client",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 14,
    "AbsoluteSessionDays": 30
  },
  "Otp": {
    "HashKey": "",
    "ExpiryMinutes": 10,
    "ResendCooldownSeconds": 60,
    "MaxAttempts": 5,
    "ResetTokenExpiryMinutes": 10
  },
  "AppInfo": {
    "Name": "Your Project",
    "SupportEmail": "support@example.com",
    "BaseUrl": "http://localhost:5000"
  },
  "GoogleAuth": {
    "ClientId": ""
  },
  "Smtp": {
    "Host": "",
    "Port": 587,
    "Username": "",
    "Password": "",
    "FromEmail": "",
    "FromName": "Your Project",
    "UseSsl": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "localhost;127.0.0.1",
  "Backend": {
    "EnableExceptionHandling": true,
    "EnableAuthAbuseProtection": true,
    "AuthPathPrefixes": [
      "/auth/custom"
    ],
    "OptionalAuthenticationPaths": [],
    "IdempotencyPaths": [],
    "AuthIpPermitLimit": 60,
    "AuthAccountPermitLimit": 15,
    "AuthWindowSeconds": 300,
    "SessionStoreSlowMilliseconds": 1000,
    "HealthCheckTimeoutSeconds": 5
  }
}
```

Change the issuer, audience, application name, connection string, and provider
settings for your project. The empty secret values must be configured separately.

Generate separate JWT and OTP secrets and store them in your application's user-secrets:

```powershell
dotnet user-secrets init --project YourProject.Api

$jwtSecret = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$otpSecret = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))

dotnet user-secrets set "JwtSettings:SecretKey" "$jwtSecret" --project YourProject.Api
dotnet user-secrets set "Otp:HashKey" "$otpSecret" --project YourProject.Api
```

The JWT secret must contain at least 32 UTF-8 bytes. The OTP key must be Base64
encoding of at least 32 random bytes. Token lifetime keys are `AccessTokenMinutes`,
`RefreshTokenDays`, and `AbsoluteSessionDays`. Invalid JWT/OTP settings fail startup.

Registration and password login can be verified before configuring Google or SMTP.
Set GoogleAuth:ClientId for Google sign-in. Set SMTP Host, Port, FromEmail, FromName,
and your provider's Username/Password to deliver password recovery emails.
Store the SMTP password in user-secrets or your deployment secret store.

SMTP uses required STARTTLS by default, typically on port 587. Set `UseSsl: true`
for implicit TLS, typically on port 465. A custom `IEmailService` can be registered
before `AddDevToolkitAuth` to use a different email provider.

In production, use your secret store or environment variables such as
`JwtSettings__SecretKey`, `Otp__HashKey`, and `Smtp__Password`.

### 3. Register the toolkit in your Program.cs

```csharp
using System.Security.Claims;
using DevToolkit.Auth.Extensions;
using DevToolkit.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDevToolkitAuth(builder.Configuration, database =>
    database.UseSqlite(
        builder.Configuration.GetConnectionString("AuthDatabase"),
        sqlite => sqlite.MigrationsAssembly(typeof(Program).Assembly.FullName)
    )
);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseDevToolkitAuth();
app.MapControllers();
app.MapDevToolkitHealthChecks();

app.MapGet("/me", (ClaimsPrincipal user) => new
{
    UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
    Email = user.FindFirstValue(ClaimTypes.Email)
}).RequireAuthorization();

app.Run();
```

The module registers its controllers, services, repositories, auth context,
rate-limit policies, backend middleware dependencies, and session-store health
check. The migrations assembly is your application, so generated migrations belong
to your application project.

For browser clients, configure the host's allowed origins and run CORS middleware
before `UseDevToolkitAuth()`. Behind a reverse proxy, configure trusted proxies
and run `UseForwardedHeaders()` first.

### 4. Create the database schema

Install the matching EF command-line tool if needed, then generate and apply
migrations in your application:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.14
dotnet ef migrations add InitialAuth --context AuthDbContext --project YourProject.Api --startup-project YourProject.Api
dotnet ef database update --context AuthDbContext --project YourProject.Api --startup-project YourProject.Api
```

The mappings create users, roles, user-role relationships, tokens, OTPs, sessions,
external logins, and authentication events. User/Admin/Owner roles are initialized;
registration assigns User. The host controls assignment of administrative roles.
Normalized email addresses remain reserved after soft deletion.

### 5. Start your API and verify login

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project YourProject.Api --urls http://localhost:5000
```

In another terminal, register a development account:

```powershell
$registration = @{
    fullName = "Example Developer"
    email = "developer@example.com"
    password = "ExamplePassword123!"
} | ConvertTo-Json

$auth = Invoke-RestMethod -Uri "http://localhost:5000/auth/custom/register" -Method Post -ContentType "application/json" -Body $registration
Invoke-RestMethod -Uri "http://localhost:5000/me" -Headers @{ Authorization = "Bearer $($auth.accessToken)" }
Invoke-RestMethod -Uri "http://localhost:5000/health/ready"
```

The registration response contains an access token, refresh token, expiry, and user.
Send the access token as `Authorization: Bearer <token>`. Use an HTTP client to test
the remaining endpoints below.

### SQL Server instead of SQLite

Install `Microsoft.EntityFrameworkCore.SqlServer` version 10.0.14 in your host,
provide a SQL Server connection string, and replace the database provider setup:

```csharp
builder.Services.AddDevToolkitAuth(builder.Configuration, database =>
    database.UseSqlServer(
        builder.Configuration.GetConnectionString("AuthDatabase"),
        sql => sql.MigrationsAssembly(typeof(Program).Assembly.FullName)
    )
);
```

Generate migrations for the provider you actually deploy. The toolkit uses EF Core
relational APIs and receives its provider configuration from the host.

## Share your application's DbContext

If you want auth and domain data in the same context, inherit from AuthDbContext:

```csharp
using DevToolkit.Auth.Data;
using Microsoft.EntityFrameworkCore;

namespace YourProject.Api.Data;

public class YourProjectDbContext : AuthDbContext
{
    public YourProjectDbContext(DbContextOptions<YourProjectDbContext> options) : base(options)
    {
    }

    // Add your application's DbSet properties here.
    // If overriding OnModelCreating, call base.OnModelCreating(modelBuilder).
}
```

Register that context and use the generic overload:

```csharp
builder.Services.AddDbContext<YourProjectDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthDatabase"))
);

builder.Services.AddDevToolkitAuth<YourProjectDbContext>(builder.Configuration);
```

An existing DbContext can instead implement IAuthDbContext and call
`modelBuilder.AddDevToolkitAuthModel()` from OnModelCreating. The generic overload
registers the interface mapping to that same scoped context.

## Add your own profile after registration

Implement `IUserRegisteredHook` in your host and register it as scoped. The module
calls all registered hooks inside the registration transaction. Use the same host
context when writing domain data. An exception rolls back the auth registration.
Run external side effects such as welcome emails after a committed transaction,
or through your application's transactional outbox.

## Endpoints

All auth endpoints are under `/auth/custom`.

| Method | Suffix | Authentication |
| --- | --- | --- |
| POST | /register | Public |
| POST | /login | Public |
| POST | /refresh | Refresh token in body |
| POST | /logout | Refresh token in body |
| POST | /google | Google ID token in body |
| POST | /forgot-password/request | Public |
| POST | /forgot-password/verify | Public |
| POST | /forgot-password/reset | Reset token in body |
| GET | /security-summary | Bearer token |
| POST | /change-password | Bearer token |
| POST | /set-password/google-reauth | Bearer token + recent Google ID token |
| POST | /set-password/complete | Bearer token + setup token |
| GET | /sessions | Bearer token |
| POST | /sessions/logout-others | Bearer token + current refresh token |
| POST | /sessions/logout-all | Bearer token |
| POST | /sessions/{userSessionId}/revoke | Bearer token |
| GET | /sessions/validate | Bearer token; returns 204 |

Expected input failures return 400, invalid credentials/sessions return 401,
duplicate registration returns 409, and rate limits return 429. Unexpected
infrastructure failures return a safe 500/503 problem response with a trace ID.
The host can disable the included global exception handler when it supplies its own.

## Backend middleware and health checks

The single `UseDevToolkitAuth()` call now runs the following pipeline, in order:

1. Global exception handling (configurable).
2. IP/account abuse checks before JWT authentication (configurable).
3. JWT authentication.
4. Optional-authentication checks for selected public endpoints.
5. Authorization and named endpoint rate limits.
6. Submission retry protection for selected endpoints.

Call `UseRouting()` before it when assembling your own pipeline. Run trusted-proxy
and CORS middleware before the toolkit. It should appear before endpoint execution.
Configure response compression before the toolkit when using retry protection.
Call the combined toolkit pipeline once; it already includes the backend middleware.

The `Backend` section shown above contains the settings. Set
`EnableExceptionHandling: false` if the host already owns global error handling.
The pre-auth limiter defaults to 60 requests per IP and 15 requests per normalized
account per five minutes, with a 16 KiB JSON inspection limit. Account keys are
hashed in memory. Malformed JSON still goes through MVC's normal input validation.
These limits are additional to endpoint policies; tune both for your workload.

For public forms, explicitly select the desired behavior:

```csharp
using DevToolkit.Infrastructure.RateLimiting;
using DevToolkit.Infrastructure.Extensions;

app.MapPost("/intake", (SubmissionDto request) => Results.Ok(request))
    .WithOptionalAuthentication()
    .WithSubmissionIdempotency()
    .RequireRateLimiting(RateLimitPolicies.FormSubmit);
```

`SubmissionDto` is your host's request type. Guests are accepted, but an invalid
supplied authorization header returns 401. Controller actions can use
`[OptionalAuthentication]` and `[SubmissionIdempotency]` from
`DevToolkit.Infrastructure.Metadata`. Existing applications can instead configure
`Backend:OptionalAuthenticationPaths` (prefixes) and `Backend:IdempotencyPaths`
(exact paths). There are no business-specific form or chat routes built into the module.

Retry protection applies to JSON POST requests carrying a UUID `Idempotency-Key`.
It fingerprints the body, query, and content type and scopes the key to the endpoint
and authenticated user (or guest IP). Identical retries replay the original response;
different details return 409. Authentication endpoints cannot use it.

The default store has 1,000 entries, a ten-minute retention period, a 32 KiB request
limit, and a 64 KiB response limit. In-flight entries remain pinned. Full storage
returns 503. If execution fails ambiguously, or a response contains cookies or cannot
be safely buffered/replayed, the same key returns 409 instead of rerunning the handler.
Keep selected handlers short, JSON-based, and free of streaming responses.

This is bounded, process-local retry protection. Restarting the application loses
its entries, and multiple instances do not share them. It does not provide a durable
exactly-once guarantee. Important business writes need a database-backed idempotency
record committed in the same transaction as the write. Neither this store nor the
rate limiter substitutes for deployment-level traffic controls.

`MapDevToolkitHealthChecks()` follows the same endpoint split as the reference system:

| Endpoint | Access | Behavior |
| --- | --- | --- |
| /health | Public | Liveness; does not query the database |
| /health/ready | Public | Core readiness; returns 503 when a dependency is unhealthy |
| /health/details | Admin/Owner | JSON diagnostics, durations, descriptions, and check data |

The session-store probe checks that the table is readable, reports latency, and
degrades when it is slow. It does not claim to verify writes. Its timeout and slow
threshold are configurable. Health responses are not cached; public probes do not
include exception messages or diagnostic data. The detailed endpoint is authorization
protected and should stay private to operations staff.

Register host-specific checks with `builder.Services.AddHealthChecks()`. Add the
`ready` tag to core checks. Add `external` for optional dependencies that should not
take core readiness offline. This version includes the session-store probe;
application-specific storage, AI providers, disk, and memory thresholds remain host
decisions. The original project's AI provider and upload paths are not dependencies.

## Security and behavior

- Passwords use BCrypt with work factor 12 and an explicit 72 UTF-8 byte limit.
- Refresh tokens are random, stored only as SHA-256 hashes, and consumed atomically.
  Reuse revokes that device session. Clients must coordinate concurrent refreshes
  and must not blindly retry a refresh request after an ambiguous network failure.
- Every authenticated request checks the session, account status, and absolute
  session lifetime in the database. This intentionally adds a database read.
- Five failed password attempts lock the account for 15 minutes.
- OTPs use HMAC-SHA256, attempt limits, constant-time comparison, and expiry.
  Reset/setup tokens are hashed, single use, and restricted to their purpose.
- Forgot-password requests use the same success response for unknown, inactive,
  and Google-only accounts. Email delivery failures are logged without message contents.
- Google tokens are validated by Google's library for the configured client.
  Existing accounts are linked by email only for authoritative Gmail/Workspace
  claims. Other third-party email identities require a separate linking flow in
  the host. An unverified preregistration loses its old password and sessions
  when the authoritative Google owner first signs in.
- Email/password registration does not require verified mailbox ownership.
  Add an email-verification workflow before granting email-dependent privileges.
- Auth self-service endpoints require authentication, independent of domain roles.
  Use AppPolicies.AdminOnly for admin endpoints and define extra host policies
  for your own roles. Never accept an admin role from a registration request.
- The included rate limiter is process-local. Multiple server instances need an
  additional shared limiter or a gateway policy.

## Verify in your consuming project

Create and maintain your separate test application or test repository against a
fresh clone of this toolkit. Verify registration/login, invalid credentials,
refresh rotation and reuse, logout/revocation, password recovery, Google sign-in
when configured, rate limits, and health endpoint permissions.

Run database migrations and test your actual database provider, SMTP provider,
Google client configuration, and deployment settings. Readiness should fail when
the session store is unavailable while liveness remains available.

The toolkit repository contains the library and this guide. Verification projects
can live in your own workspace or separate repository.

No open-source license has been selected for this repository yet.
