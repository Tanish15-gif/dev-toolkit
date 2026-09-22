# AuthModule — .NET 8 Reusable Auth Module

A self-contained, plug-and-play authentication module for ASP.NET Core 8 projects.
Drop it in, implement two small interfaces, and get a full auth system in one call.

---

## Features

- **Credential auth** — register, login, BCrypt password hashing (work factor 12)
- **Google OAuth** — social sign-in + password setup flow for Google-only accounts
- **JWT** — short-lived access tokens, live per-request session revocation via DB check
- **Refresh tokens** — stored as SHA-256 hash, rotated on every use
- **OTP forgot-password** — HMAC-SHA256 keyed OTP, constant-time comparison
- **Session management** — list active sessions, revoke individual or all sessions
- **RBAC** — `Owner / Admin / Customer` roles, policy-based authorization
- **Rate limiting** — auth-specific policies merged safely with host project policies
- **Health check** — `SessionStoreHealthCheck` probes the `UserSessions` table
- **Fully decoupled** — no dependency on any specific DbContext, brand name, or domain entity

---

## Folder Structure

```
AuthModule/
├── Controllers/          CustomAuthController.cs
├── Authorization/        AppRoleAuthorizationHandler, AppRoleRequirement
├── DTOs/                 Request + response DTOs (you fill these in)
├── Entities/             AppUser, Role, AppUserRole, RefreshToken, UserSession,
│                         OtpCode, PasswordResetSession, ExternalLogin, UserAuthEvent
├── Enums/                AuthEventType, AuthFailureReason, ExternalLoginProvider, OtpPurpose
├── Exceptions/           BadRequestException
├── Extensions/           AuthServiceCollectionExtensions  ← the single entry point
├── Interfaces/
│   ├── IAuthDbContext.cs          ← implement on your DbContext
│   ├── IUserRegisteredHook.cs     ← optional post-registration callback
│   ├── Repositories/              5 repo interfaces + ICustomAuthRepository facade
│   └── Services/                  12 service interfaces
├── Models/               UserAgentDetails (parsed User-Agent record)
├── Repositories/         5 concrete repositories + CustomAuthRepository facade
├── Services/             12 service implementations
│   ├── Context/          AuthContextAccessor, AuthEventLogger
│   └── Helpers/          AuthHelpers
└── Settings/             JwtSettings, GoogleAuthSettings, OtpSettings, AppInfoSettings
```

```
Constants/                AuthSchemes, RoleNames, AppPolicies, RateLimitPolicies
Infrastructure/
├── Email/                IEmailService  ← implement with MailKit / SendGrid
├── HealthChecks/         SessionStoreHealthCheck
└── RateLimiting/         RateLimitKeyHelper
```

---

## Setup

### 1. Implement `IAuthDbContext` on your DbContext

The module never references a concrete DbContext. Your project's EF context
just needs to implement the interface:

```csharp
// YourProject/Data/AppDbContext.cs
public class AppDbContext : DbContext, IAuthDbContext
{
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<AppUserRole> AppUserRoles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<OtpCode> OtpCodes { get; set; }
    public DbSet<PasswordResetSession> PasswordResetSessions { get; set; }
    public DbSet<ExternalLogin> ExternalLogins { get; set; }
    public DbSet<UserAuthEvent> UserAuthEvents { get; set; }
}
```

Then register it in DI:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Let the module resolve IAuthDbContext → AppDbContext
builder.Services.AddScoped<IAuthDbContext>(sp => sp.GetRequiredService<AppDbContext>());
```

### 2. Implement `IEmailService`

```csharp
// YourProject/Infrastructure/SmtpEmailService.cs
public class SmtpEmailService : IEmailService
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody,
        string? plainTextBody = null, CancellationToken ct = default)
    {
        // implement with MailKit, SendGrid, Resend, etc.
    }
}

// DI registration
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
```

### 3. Add `appsettings.json` sections

```json
{
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-at-least-32-chars",
    "Issuer": "https://yourapp.com",
    "Audience": "https://yourapp.com",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 30
  },
  "GoogleAuth": {
    "ClientId": "your-google-client-id"
  },
  "Otp": {
    "HashKey": "your-hmac-secret-key",
    "ExpiryMinutes": 10,
    "ResendCooldownSeconds": 60,
    "MaxAttempts": 5
  },
  "AppInfo": {
    "Name": "Your App Name",
    "SupportEmail": "support@yourapp.com",
    "BaseUrl": "https://yourapp.com"
  }
}
```

### 4. Wire up in `Program.cs`

```csharp
using AuthModule.Extensions;

// --- your DbContext setup ---
builder.Services.AddDbContext<AppDbContext>(...);
builder.Services.AddScoped<IAuthDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// --- one call wires everything ---
builder.Services.AddJwtAuthModule(builder.Configuration);

// --- health checks ---
builder.Services.AddAuthModuleHealthChecks();

var app = builder.Build();

app.UseAuthentication();   // must be before UseAuthorization
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

---

## Optional: Post-Registration Hook

To create domain-specific profiles (Customer, Employee, etc.) right after a user
registers — without coupling the auth module to your domain — implement `IUserRegisteredHook`:

```csharp
public class CustomerProfileHook : IUserRegisteredHook
{
    private readonly AppDbContext _db;
    public CustomerProfileHook(AppDbContext db) => _db = db;

    public async Task OnUserRegisteredAsync(AppUser user, CancellationToken ct = default)
    {
        _db.Customers.Add(new Customer { AppUserId = user.AppUserId, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
    }
}

// DI
builder.Services.AddScoped<IUserRegisteredHook, CustomerProfileHook>();
```

If not registered, the module silently skips it — no errors.

---

## API Endpoints

All routes are under `POST /auth/custom` unless noted.

| Method | Route                                     | Auth | Rate Limit              | Description                                          |
| ------ | ----------------------------------------- | ---- | ----------------------- | ---------------------------------------------------- |
| `POST` | `/auth/custom/register`                   | —    | `auth-attempt` (5/5min) | Create a new account                                 |
| `POST` | `/auth/custom/login`                      | —    | `auth-attempt`          | Login with email + password                          |
| `POST` | `/auth/custom/refresh`                    | —    | —                       | Rotate a refresh token                               |
| `POST` | `/auth/custom/logout`                     | —    | —                       | Revoke refresh token + session                       |
| `POST` | `/auth/custom/google`                     | —    | `auth-attempt`          | Google ID token → auth tokens                        |
| `POST` | `/auth/custom/forgot-password/request`    | —    | `otp-request` (3/10min) | Send OTP to email                                    |
| `POST` | `/auth/custom/forgot-password/verify`     | —    | `otp-verify` (10/5min)  | Verify OTP → reset token                             |
| `POST` | `/auth/custom/forgot-password/reset`      | —    | `auth-attempt`          | Set new password with reset token                    |
| `GET`  | `/auth/custom/security-summary`           | ✅   | `public-read`           | Active sessions + account info                       |
| `POST` | `/auth/custom/change-password`            | ✅   | `auth-attempt`          | Change password (requires current password)          |
| `POST` | `/auth/custom/set-password/google-reauth` | ✅   | `auth-attempt`          | Google re-auth → setup session for password creation |
| `POST` | `/auth/custom/set-password/complete`      | ✅   | `auth-attempt`          | Set a password on a Google-only account              |
| `GET`  | `/auth/custom/sessions`                   | ✅   | `public-read`           | List all active sessions                             |
| `POST` | `/auth/custom/sessions/logout-others`     | ✅   | `auth-attempt`          | Sign out all other devices                           |
| `POST` | `/auth/custom/sessions/logout-all`        | ✅   | `auth-attempt`          | Sign out all devices                                 |
| `POST` | `/auth/custom/sessions/{id}/revoke`       | ✅   | `auth-attempt`          | Sign out one specific device                         |
| `GET`  | `/auth/custom/sessions/validate`          | ✅   | —                       | 204 if token is valid                                |
| `GET`  | `/health`                                 | —    | —                       | Session store health check                           |

> ✅ = requires `Authorization: Bearer <access_token>`

---

## Security Model

| Concern            | Approach                                                                                    |
| ------------------ | ------------------------------------------------------------------------------------------- |
| Passwords          | BCrypt, work factor 12                                                                      |
| Refresh tokens     | Stored as SHA-256 hash; rotated on every use; linked to `UserSessionId`                     |
| Session revocation | JWT `OnTokenValidated` hits DB on **every request** via `ISessionRepository`                |
| OTP                | HMAC-SHA256 with keyed hash; `CryptographicOperations.FixedTimeEquals` (constant-time)      |
| JWT clock skew     | `ClockSkew = TimeSpan.Zero` — no tolerance window                                           |
| Account lockout    | 5 failed login attempts → 15-minute lockout stored in `LockoutEndAt`                        |
| IP spoofing        | Rate limit keys read `X-Forwarded-For[0]` (leftmost only) → `X-Real-IP` → `RemoteIpAddress` |

---

## Authorization Policies

| Policy constant               | Allows                       |
| ----------------------------- | ---------------------------- |
| `AppPolicies.AdminOnly`       | `Owner`, `Admin`             |
| `AppPolicies.CustomerOnly`    | `Customer`                   |
| `AppPolicies.AdminOrCustomer` | `Owner`, `Admin`, `Customer` |

```csharp
[Authorize(Policy = AppPolicies.AdminOnly)]
public IActionResult AdminDashboard() { ... }
```

Role names live in `RoleNames` constants — keep them in sync with your seeded `Roles` table.

---

## Rate Limit Policies

Auth-specific policies are registered automatically by `AddJwtAuthModule`. General-purpose policies in `RateLimitPolicies` are just constants — register them yourself:

```csharp
services.AddRateLimiter(options =>
{
    options.AddPolicy(RateLimitPolicies.PublicRead, ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            RateLimitKeyHelper.GetIpKey(ctx),
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) }));
});
```

| Policy         | Default limit | Use case                        |
| -------------- | ------------- | ------------------------------- |
| `auth-attempt` | 5 / 5 min     | Login, register, Google sign-in |
| `otp-request`  | 3 / 10 min    | OTP send/resend                 |
| `otp-verify`   | 10 / 5 min    | OTP verification                |
| `public-read`  | 120 / 1 min   | General read endpoints          |
| `form-submit`  | 10 / 1 min    | Contact forms                   |
| `image-upload` | 8 / 5 min     | File uploads                    |
| `admin-action` | 80 / 1 min    | Admin write operations          |
| `chat-message` | 15 / 1 min    | Real-time chat                  |

---

## DI Lifetimes Reference

| Lifetime      | Services                                                                        |
| ------------- | ------------------------------------------------------------------------------- |
| **Scoped**    | All repositories, all auth services, `IAuthContextAccessor`, `IAuthEventLogger` |
| **Singleton** | `IUserAgentParser`, `IOtpSecurityService`, `IPasswordResetTokenService`         |
