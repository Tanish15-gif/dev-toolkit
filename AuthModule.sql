/* AuthModule.sql
SQL Server schema for DevToolkit authentication.
Generated from AuthDbContext migration 20261104111807_InitialAuth (EF Core 10.0.14).

Authentication tables:
   AppUsers, Roles, AppUserRoles, ExternalLogins, UserSessions,
   RefreshTokens, OtpCodes, PasswordResetSessions, UserAuthEvents.
Includes their keys, indexes, relationships, and the User/Admin/Owner role records.
 __EFMigrationsHistory records the applied migration so EF tooling recognizes it.
No application users, credentials, token values, or business records are seeded.

Select an existing target database in SSMS before executing this whole file.
Command-line alternative (replace the server and database for your application):
  sqlcmd -S localhost -d YourDatabase -E -C -b -i .\AuthModule.sql
-C trusts the local development certificate; use certificate validation in deployment.
The script does not create or select a database and does not merge conflicting tables.
Already-applied migrations are skipped according to __EFMigrationsHistory.

Keep this export aligned with AuthModule/Data/Migrations. Regenerate with:
  dotnet ef migrations script --idempotent --context AuthDbContext --output AuthModule.sql
Preserve these execution notes and SET XACT_ABORT ON when regenerating the file.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE TABLE [AppUsers] (
        [AppUserId] int NOT NULL IDENTITY,
        [FullName] nvarchar(120) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NULL,
        [UpdatedAt] datetime2 NULL,
        [DeletedAt] datetime2 NULL,
        [PasswordHash] nvarchar(200) NULL,
        [NormalizedEmail] nvarchar(150) NOT NULL,
        [EmailConfirmed] bit NOT NULL,
        [LastLoginAt] datetime2 NULL,
        [LastSeenAt] datetime2 NULL,
        [LastLogoutAt] datetime2 NULL,
        [AccessFailedCount] int NOT NULL,
        [LockoutEndAt] datetime2 NULL,
        [PasswordUpdatedAt] datetime2 NULL,
        [ProfileImageUrl] nvarchar(2048) NULL,
        [ProfileImageSource] nvarchar(50) NULL,
        [ProfileImageUpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_AppUsers] PRIMARY KEY ([AppUserId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE TABLE [Roles] (
        [RoleId] int NOT NULL IDENTITY,
        [RoleName] nvarchar(50) NOT NULL,
        [Description] nvarchar(200) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([RoleId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE TABLE [ExternalLogins] (
        [ExternalLoginId] int NOT NULL IDENTITY,
        [AppUserId] int NOT NULL,
        [Provider] nvarchar(50) NOT NULL,
        [ProviderUserId] nvarchar(200) NOT NULL,
        [Email] nvarchar(150) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ExternalLogins] PRIMARY KEY ([ExternalLoginId]),
        CONSTRAINT [FK_ExternalLogins_AppUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AppUsers] ([AppUserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE TABLE [OtpCodes] (
        [OtpCodeId] int NOT NULL IDENTITY,
        [AppUserId] int NULL,
        [Email] nvarchar(150) NOT NULL,
        [NormalizedEmail] nvarchar(150) NOT NULL,
        [Purpose] nvarchar(50) NOT NULL,
        [CodeHash] nvarchar(256) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [ConsumedAt] datetime2 NULL,
        [AttemptCount] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByIp] nvarchar(64) NULL,
        CONSTRAINT [PK_OtpCodes] PRIMARY KEY ([OtpCodeId]),
        CONSTRAINT [FK_OtpCodes_AppUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AppUsers] ([AppUserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE TABLE [PasswordResetSessions] (
        [PasswordResetSessionId] int NOT NULL IDENTITY,
        [AppUserId] int NOT NULL,
        [Purpose] nvarchar(50) NOT NULL,
        [TokenHash] nvarchar(256) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [ConsumedAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByIp] nvarchar(64) NULL,
        CONSTRAINT [PK_PasswordResetSessions] PRIMARY KEY ([PasswordResetSessionId]),
        CONSTRAINT [FK_PasswordResetSessions_AppUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AppUsers] ([AppUserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE TABLE [UserAuthEvents] (
        [UserAuthEventId] int NOT NULL IDENTITY,
        [AppUserId] int NULL,
        [Email] nvarchar(150) NULL,
        [EventType] nvarchar(100) NOT NULL,
        [Success] bit NOT NULL,
        [IpAddress] nvarchar(64) NULL,
        [UserAgent] nvarchar(500) NULL,
        [Description] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserAuthEvents] PRIMARY KEY ([UserAuthEventId]),
        CONSTRAINT [FK_UserAuthEvents_AppUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AppUsers] ([AppUserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE TABLE [UserSessions] (
        [UserSessionId] uniqueidentifier NOT NULL,
        [AppUserId] int NOT NULL,
        [DeviceType] nvarchar(50) NOT NULL,
        [DeviceName] nvarchar(100) NOT NULL,
        [BrowserName] nvarchar(100) NOT NULL,
        [BrowserVersion] nvarchar(100) NULL,
        [OperatingSystem] nvarchar(100) NOT NULL,
        [UserAgent] nvarchar(500) NULL,
        [IpAddress] nvarchar(64) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [LastSeenAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [RevokedByIp] nvarchar(64) NULL,
        CONSTRAINT [PK_UserSessions] PRIMARY KEY ([UserSessionId]),
        CONSTRAINT [FK_UserSessions_AppUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AppUsers] ([AppUserId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE TABLE [AppUserRoles] (
        [AppUserId] int NOT NULL,
        [RoleId] int NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_AppUserRoles] PRIMARY KEY ([AppUserId], [RoleId]),
        CONSTRAINT [FK_AppUserRoles_AppUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AppUsers] ([AppUserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AppUserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([RoleId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [RefreshTokenId] int NOT NULL IDENTITY,
        [AppUserId] int NOT NULL,
        [TokenHash] nvarchar(256) NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedByIp] nvarchar(64) NULL,
        [UserAgent] nvarchar(500) NULL,
        [RevokedAt] datetime2 NULL,
        [RevokedByIp] nvarchar(64) NULL,
        [ReplacedByTokenHash] nvarchar(256) NULL,
        [UserSessionId] uniqueidentifier NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([RefreshTokenId]),
        CONSTRAINT [FK_RefreshTokens_AppUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AppUsers] ([AppUserId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RefreshTokens_UserSessions_UserSessionId] FOREIGN KEY ([UserSessionId]) REFERENCES [UserSessions] ([UserSessionId]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'CreatedAt', N'Description', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] ON;
    EXEC(N'INSERT INTO [Roles] ([RoleId], [CreatedAt], [Description], [RoleName])
    VALUES (1, ''2026-01-01T00:00:00.0000000Z'', NULL, N''User''),
    (2, ''2026-01-01T00:00:00.0000000Z'', NULL, N''Admin''),
    (3, ''2026-01-01T00:00:00.0000000Z'', NULL, N''Owner'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'CreatedAt', N'Description', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE INDEX [IX_AppUserRoles_RoleId] ON [AppUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AppUsers_NormalizedEmail] ON [AppUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE INDEX [IX_ExternalLogins_AppUserId] ON [ExternalLogins] ([AppUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ExternalLogins_Provider_ProviderUserId] ON [ExternalLogins] ([Provider], [ProviderUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE INDEX [IX_OtpCodes_AppUserId] ON [OtpCodes] ([AppUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE INDEX [IX_OtpCodes_ExpiresAt] ON [OtpCodes] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE INDEX [IX_OtpCodes_NormalizedEmail_Purpose_CreatedAt] ON [OtpCodes] ([NormalizedEmail], [Purpose], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE INDEX [IX_PasswordResetSessions_AppUserId] ON [PasswordResetSessions] ([AppUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE INDEX [IX_PasswordResetSessions_ExpiresAt] ON [PasswordResetSessions] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PasswordResetSessions_TokenHash] ON [PasswordResetSessions] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_AppUserId] ON [RefreshTokens] ([AppUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_ExpiresAt] ON [RefreshTokens] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserSessionId] ON [RefreshTokens] ([UserSessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_RoleName] ON [Roles] ([RoleName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE INDEX [IX_UserAuthEvents_AppUserId_CreatedAt] ON [UserAuthEvents] ([AppUserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    CREATE INDEX [IX_UserSessions_AppUserId_RevokedAt] ON [UserSessions] ([AppUserId], [RevokedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104111807_InitialAuth'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104111807_InitialAuth', N'10.0.14');
END;

COMMIT;
GO
