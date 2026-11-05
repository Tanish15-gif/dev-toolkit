<p align="center">
  <img src="https://github.com/user-attachments/assets/1efbef4b-0335-4ab0-a753-ecf4a197f55e" alt="DevToolkit" width="560">
</p>

A reusable authentication starter for **ASP.NET Core 10**, built around a single Web API project and a dedicated `AuthModule`.

DevToolkit brings together account authentication, password recovery, device sessions, role authorization, authentication request protection, and health monitoring. Its HTTP API is independent of the frontend, so applications can provide their own interface and business features.

For installation and integration, follow the [Setup Guide](SetupGuide.md).

## Reference Project Preview

![Reference Project Preview: sign-in interface with email/password and Google sign-in](https://github.com/user-attachments/assets/79dd35d2-f211-4b40-bd7c-937e5120a976)

A reference sign-in interface showing how an application can present email/password and Google authentication. The frontend shown is a visual reference; DevToolkit supplies the backend API, and applications provide their own UI.
API testing is covered in the [BeautyParlour Project Reference](SetupGuide.md#beautyparlour-project-reference).

## Features

| Area                      | Included functionality                                                                                                 |
| ------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Account authentication    | Email/password registration and login, normalized email addresses, password hashing, and failed-login lockout          |
| Access and refresh tokens | Signed JWT access tokens, hashed refresh-token storage, token rotation, and refresh-token replay handling              |
| Google sign-in            | Server-side Google ID-token validation, linked external accounts, and reauthentication before password creation        |
| Password recovery         | Email OTPs, resend cooldown, expiry, attempt limits, and single-use password-reset tokens                              |
| Password management       | Password changes and password creation for Google accounts                                                             |
| Device sessions           | Device/browser information, active-session listing, individual device revocation, and sign-out of other or all devices |
| Authorization             | Database-backed role checks and default `User`, `Admin`, and `Owner` roles                                             |
| Authentication events     | Records for registration, login, refresh, logout, and account-lockout events                                           |
| Request protection        | Named authentication/OTP rate limits, additional IP/account limits, and bounded pre-authentication request inspection  |
| Error handling            | Expected authentication errors, structured problem responses, and trace identifiers                                    |
| Health monitoring         | Public liveness/readiness endpoints and protected detailed diagnostics                                                 |

## Technology

| Component                  | Technology                   |
| -------------------------- | ---------------------------- |
| Runtime and HTTP API       | .NET 10 / ASP.NET Core       |
| Database access            | Entity Framework Core 10     |
| Included database provider | SQL Server                   |
| Access tokens              | JWT with HMAC-SHA256 signing |
| Password hashing           | BCrypt.Net-Next              |
| Google token validation    | Google.Apis.Auth             |
| Email delivery             | MailKit / SMTP               |

## Repository structure

```text
dev-toolkit/
├── README.md
├── SetupGuide.md
├── .gitignore
├── LICENSE
├── DevToolkit.Api.csproj
├── Program.cs
├── appsettings.json
├── AuthModule.sql
└── AuthModule/
    ├── Authorization/
    ├── Constants/
    ├── Controllers/
    ├── Data/
    │   ├── AuthDbContext.cs
    │   ├── AuthModelConfiguration.cs
    │   └── Migrations/
    ├── DTOs/
    ├── Entities/
    ├── Enums/
    ├── Exceptions/
    ├── Extensions/
    ├── HealthChecks/
    ├── Helpers/
    ├── Interfaces/
    │   ├── Repositories/
    │   └── Services/
    ├── Middleware/
    ├── Models/
    ├── Repositories/
    ├── Services/
    │   ├── Context/
    │   └── Email/
    └── Settings/
```

### Root files

| File                                           | Responsibility                                                                                                       |
| ---------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| [DevToolkit.Api.csproj](DevToolkit.Api.csproj) | Web API project definition and package dependencies                                                                  |
| [Program.cs](Program.cs)                       | Application entry point, SQL Server registration, CORS, authentication pipeline, and endpoint mapping                |
| [appsettings.json](appsettings.json)           | Configuration sections for the application, authentication, email, request limits, and hosting                       |
| [AuthModule.sql](AuthModule.sql)               | SQL Server schema export aligned with the authentication migrations, including role records and EF migration history |
| [.gitignore](.gitignore)                       | Exclusions for generated build output, local configuration, and development artifacts                                |

### Inside AuthModule

| Folder             | Responsibility                                                                                                  |
| ------------------ | --------------------------------------------------------------------------------------------------------------- |
| `Authorization`    | Role requirements and the authorization handler                                                                 |
| `Constants`        | Authentication scheme, route, role, authorization-policy, and rate-limit names                                  |
| `Controllers`      | Authentication and account-security HTTP endpoints                                                              |
| `Data`             | Auth database context, entity mappings, indexes, relationships, and migrations                                  |
| `DTOs`             | Validated request objects and API response contracts                                                            |
| `Entities`         | Authentication database models                                                                                  |
| `Enums`            | Authentication event types, failure reasons, OTP purposes, and external providers                               |
| `Exceptions`       | Expected error types and the auth controller's exception filter                                                 |
| `Extensions`       | Service registration, request-pipeline composition, and health-endpoint mapping                                 |
| `HealthChecks`     | Session-store availability and read-latency checks                                                              |
| `Helpers`          | Input validation, password byte-length validation, request inspection, rate-limit keys, and response formatting |
| `Interfaces`       | Contracts for services, repositories, database access, and registration hooks                                   |
| `Middleware`       | Authentication abuse limiting and global exception handling                                                     |
| `Models`           | Internal models such as parsed device and browser information                                                   |
| `Repositories`     | Database operations for accounts, tokens, external logins, recovery, and sessions                               |
| `Services`         | Authentication workflows and security operations                                                                |
| `Services/Context` | Current user/session access and authentication-event logging                                                    |
| `Services/Email`   | Default SMTP email delivery implementation                                                                      |
| `Settings`         | Configuration models for JWT, OTP, Google, email, application branding, limits, and health-check timeouts       |

## Authentication data

The module contains nine authentication tables:

| Table                   | Stored information                                                                                  |
| ----------------------- | --------------------------------------------------------------------------------------------------- |
| `AppUsers`              | Account identity, password hash, confirmation state, activity status, and login/lockout information |
| `Roles`                 | Role definitions                                                                                    |
| `AppUserRoles`          | User-to-role assignments                                                                            |
| `ExternalLogins`        | External provider identities linked to an account                                                   |
| `UserSessions`          | Device sessions, browser information, timestamps, and revocation state                              |
| `RefreshTokens`         | Refresh-token hashes, expiry, rotation information, and associated device sessions                  |
| `OtpCodes`              | OTP hashes, purpose, expiry, attempt counts, and consumption state                                  |
| `PasswordResetSessions` | Single-use password-reset or password-setup token hashes, purposes, and expiry                      |
| `UserAuthEvents`        | Authentication event records, success/failure state, and request context                            |

`__EFMigrationsHistory` is EF Core's migration metadata table, separate from the nine auth tables. The initial migration contains the `User`, `Admin`, and `Owner` role records; it does not contain application accounts or administrator credentials. Public registration assigns the `User` role.

The included migrations and SQL export target **SQL Server**. PostgreSQL and MySQL are not validated database targets in this version.

## Token and session behaviour

An authentication response contains `accessToken`, `refreshToken`, `accessTokenExpiresAt`, and a `user` object with the account identifier, profile information, confirmation state, and roles.

- **Access tokens** are returned to the client and are not stored as token strings in the database.
- **Refresh tokens** are returned to the client while their SHA-256 hashes are stored in `RefreshTokens`.
- **Device sessions** are stored separately in `UserSessions` and are associated with access and refresh tokens.
- **JWT validation** checks the signature, issuer, audience, expiry, account status, and database session state.
- **Session revocation** causes associated access tokens to be rejected on subsequent requests, even before their JWT expiry.
- **Refresh-token rotation** replaces a consumed refresh token; reuse of a revoked token revokes its associated device session.

Browser token persistence belongs to the client application. The backend returns bearer tokens in JSON and does not automatically write browser `localStorage` or issue refresh-token cookies.

## Password and account protection

Passwords use BCrypt with work factor 12, an eight-character minimum, and a maximum of 72 UTF-8 bytes. OTPs use a keyed HMAC-SHA256 hash and constant-time hash comparison. Password-reset tokens and Google password-setup tokens have separate purposes and single-use consumption checks.

Disabled or soft-deleted accounts are rejected during authentication. Soft-deleted email addresses remain reserved. Password changes and password resets revoke existing device sessions and refresh tokens.

Forgot-password requests use a neutral response regardless of whether an eligible account exists. Email-delivery failures invalidate the generated OTP and are logged without the reset code. Google account linking by email is restricted to addresses for which Google is authoritative.

The pre-authentication limiter and named rate-limit policies protect account and OTP endpoints. Limiter state is held in the running application process.

## API reference

All authentication routes use the `/auth/custom` prefix.

| Method | Route                              | Purpose                                                      |
| ------ | ---------------------------------- | ------------------------------------------------------------ |
| POST   | `/register`                        | Create an account and authentication session                 |
| POST   | `/login`                           | Authenticate with email and password                         |
| POST   | `/refresh`                         | Rotate a refresh token and issue a new access token          |
| POST   | `/logout`                          | End the session associated with a refresh token              |
| POST   | `/google`                          | Authenticate using a Google ID token                         |
| POST   | `/forgot-password/request`         | Request a password-recovery OTP                              |
| POST   | `/forgot-password/verify`          | Verify an OTP and issue a reset token                        |
| POST   | `/forgot-password/reset`           | Consume a reset token and update the password                |
| GET    | `/security-summary`                | Read password/provider state and active-session count        |
| POST   | `/change-password`                 | Change the authenticated account's password                  |
| POST   | `/set-password/google-reauth`      | Reauthenticate a linked Google account for password creation |
| POST   | `/set-password/complete`           | Consume a setup token and create a password                  |
| GET    | `/sessions`                        | List the account's active device sessions                    |
| GET    | `/sessions/validate`               | Validate the current authenticated session; returns HTTP 204 |
| POST   | `/sessions/logout-others`          | End other device sessions while retaining the current one    |
| POST   | `/sessions/logout-all`             | End all device sessions for the account                      |
| POST   | `/sessions/{userSessionId}/revoke` | Revoke another device session belonging to the account       |

Account-security and session-management endpoints require JWT authentication. The recovery, refresh, logout, and Google flows validate their corresponding OTPs or tokens. Self-service account endpoints do not depend on application-specific domain roles.

## Health monitoring

| Endpoint          | Access        | Responsibility                                              |
| ----------------- | ------------- | ----------------------------------------------------------- |
| `/health`         | Public        | Process liveness, independent of database availability      |
| `/health/ready`   | Public        | Readiness checks, including auth session-store availability |
| `/health/details` | Admin / Owner | Structured health status, timings, and diagnostic data      |

The session-store check measures whether the session table can be read and how long that read takes. It does not claim to verify database write permissions. Unhealthy readiness returns HTTP 503; the detailed response excludes exception messages and disables caching.

## Extension points

| Contract                          | Responsibility                                                                               |
| --------------------------------- | -------------------------------------------------------------------------------------------- |
| `IAuthDbContext`                  | Database operations needed by authentication, independently of business tables               |
| `IUserRegisteredHook`             | Application-specific work following account creation through password or Google registration |
| `IEmailService`                   | Replaceable authentication-email delivery                                                    |
| `IGoogleIdTokenValidator`         | Google ID-token validation boundary                                                          |
| Service and repository interfaces | Boundaries between authentication workflows and persistence                                  |

Registration hooks execute inside the authentication transaction. Only database operations sharing that context participate in the same transaction. Application-specific customer, employee, appointment, product, and repair models remain outside the auth module.

## License

DevToolkit is free to use, modify, and include in personal or commercial projects under the [MIT License](LICENSE). Keep the copyright and license notice when redistributing the code.

## Current scope

DevToolkit contains backend authentication source, a runnable API host, authentication request protection, and health endpoints. It does not include a frontend UI, Microsoft sign-in, login OTP/MFA, a complete signup email-verification flow, refresh-token cookies, or general-purpose submission idempotency.
