using DevToolkit.Auth.Constants;
using DevToolkit.Auth.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevToolkit.Auth.Extensions;

public static class AuthModelBuilderExtensions
{
    /// <summary>Applies the module's keys, relationships, indexes, and initial roles.</summary>
    public static ModelBuilder AddDevToolkitAuthModel(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUsers");
            entity.HasKey(user => user.AppUserId);
            entity.Property(user => user.FullName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(150).IsRequired();
            entity.Property(user => user.NormalizedEmail).HasMaxLength(150).IsRequired();
            entity.Property(user => user.PhoneNumber).HasMaxLength(20);
            entity.Property(user => user.PasswordHash).HasMaxLength(200);
            entity.Property(user => user.ProfileImageUrl).HasMaxLength(2048);
            entity.Property(user => user.ProfileImageSource).HasMaxLength(50);
            entity.HasIndex(user => user.NormalizedEmail).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(role => role.RoleId);
            entity.Property(role => role.RoleName).HasMaxLength(50).IsRequired();
            entity.Property(role => role.Description).HasMaxLength(200);
            entity.HasIndex(role => role.RoleName).IsUnique();

            var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            entity.HasData(
                new Role { RoleId = 1, RoleName = RoleNames.User, CreatedAt = createdAt },
                new Role { RoleId = 2, RoleName = RoleNames.Admin, CreatedAt = createdAt },
                new Role { RoleId = 3, RoleName = RoleNames.Owner, CreatedAt = createdAt }
            );
        });

        modelBuilder.Entity<AppUserRole>(entity =>
        {
            entity.ToTable("AppUserRoles");
            entity.HasKey(userRole => new { userRole.AppUserId, userRole.RoleId });
            entity.HasOne(userRole => userRole.AppUser)
                .WithMany(user => user.AppUserRoles).HasForeignKey(userRole => userRole.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(userRole => userRole.Role)
                .WithMany(role => role.AppUserRoles).HasForeignKey(userRole => userRole.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.ToTable("UserSessions");
            entity.HasKey(session => session.UserSessionId);
            entity.Property(session => session.DeviceType).HasMaxLength(50);
            entity.Property(session => session.DeviceName).HasMaxLength(100);
            entity.Property(session => session.BrowserName).HasMaxLength(100);
            entity.Property(session => session.BrowserVersion).HasMaxLength(100);
            entity.Property(session => session.OperatingSystem).HasMaxLength(100);
            entity.Property(session => session.UserAgent).HasMaxLength(500);
            entity.Property(session => session.IpAddress).HasMaxLength(64);
            entity.Property(session => session.RevokedByIp).HasMaxLength(64);
            entity.HasIndex(session => new { session.AppUserId, session.RevokedAt });
            entity.HasOne(session => session.AppUser).WithMany(user => user.UserSessions)
                .HasForeignKey(session => session.AppUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(token => token.RefreshTokenId);
            entity.Property(token => token.TokenHash).HasMaxLength(256).IsRequired();
            entity.Property(token => token.ReplacedByTokenHash).HasMaxLength(256);
            entity.Property(token => token.UserAgent).HasMaxLength(500);
            entity.Property(token => token.CreatedByIp).HasMaxLength(64);
            entity.Property(token => token.RevokedByIp).HasMaxLength(64);
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => token.ExpiresAt);
            entity.HasOne(token => token.AppUser).WithMany(user => user.RefreshTokens)
                .HasForeignKey(token => token.AppUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(token => token.UserSession).WithMany(session => session.RefreshTokens)
                .HasForeignKey(token => token.UserSessionId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.ToTable("ExternalLogins");
            entity.HasKey(login => login.ExternalLoginId);
            entity.Property(login => login.Provider).HasMaxLength(50).IsRequired();
            entity.Property(login => login.ProviderUserId).HasMaxLength(200).IsRequired();
            entity.Property(login => login.Email).HasMaxLength(150);
            entity.HasIndex(login => new { login.Provider, login.ProviderUserId }).IsUnique();
            entity.HasOne(login => login.AppUser).WithMany(user => user.ExternalLogins)
                .HasForeignKey(login => login.AppUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OtpCode>(entity =>
        {
            entity.ToTable("OtpCodes");
            entity.HasKey(otp => otp.OtpCodeId);
            entity.Property(otp => otp.Email).HasMaxLength(150).IsRequired();
            entity.Property(otp => otp.NormalizedEmail).HasMaxLength(150).IsRequired();
            entity.Property(otp => otp.Purpose).HasMaxLength(50).IsRequired();
            entity.Property(otp => otp.CodeHash).HasMaxLength(256).IsRequired();
            entity.Property(otp => otp.CreatedByIp).HasMaxLength(64);
            entity.HasIndex(otp => new { otp.NormalizedEmail, otp.Purpose, otp.CreatedAt });
            entity.HasIndex(otp => otp.ExpiresAt);
            entity.HasOne(otp => otp.AppUser).WithMany(user => user.OtpCodes)
                .HasForeignKey(otp => otp.AppUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PasswordResetSession>(entity =>
        {
            entity.ToTable("PasswordResetSessions");
            entity.HasKey(session => session.PasswordResetSessionId);
            entity.Property(session => session.TokenHash).HasMaxLength(256).IsRequired();
            entity.Property(session => session.Purpose).HasMaxLength(50).IsRequired();
            entity.Property(session => session.CreatedByIp).HasMaxLength(64);
            entity.HasIndex(session => session.TokenHash).IsUnique();
            entity.HasIndex(session => session.ExpiresAt);
            entity.HasOne(session => session.AppUser).WithMany(user => user.PasswordResetSessions)
                .HasForeignKey(session => session.AppUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserAuthEvent>(entity =>
        {
            entity.ToTable("UserAuthEvents");
            entity.HasKey(authEvent => authEvent.UserAuthEventId);
            entity.Property(authEvent => authEvent.Email).HasMaxLength(150);
            entity.Property(authEvent => authEvent.EventType).HasMaxLength(100).IsRequired();
            entity.Property(authEvent => authEvent.IpAddress).HasMaxLength(64);
            entity.Property(authEvent => authEvent.UserAgent).HasMaxLength(500);
            entity.Property(authEvent => authEvent.Description).HasMaxLength(500);
            entity.HasIndex(authEvent => new { authEvent.AppUserId, authEvent.CreatedAt });
            entity.HasOne(authEvent => authEvent.AppUser).WithMany(user => user.UserAuthEvents)
                .HasForeignKey(authEvent => authEvent.AppUserId).OnDelete(DeleteBehavior.Restrict);
        });

        return modelBuilder;
    }
}
