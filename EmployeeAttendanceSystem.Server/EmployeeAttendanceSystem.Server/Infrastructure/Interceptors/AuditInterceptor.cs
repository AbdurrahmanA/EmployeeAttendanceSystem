using EmployeeAttendanceSystem.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace EmployeeAttendanceSystem.Server.Infrastructure.Interceptors
{
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = false
        };

        private readonly HashSet<string> _ignoredProperties = new HashSet<string>
        {
            "PasswordHash",
            "SecurityStamp",
            "ConcurrencyStamp",
            "NormalizedUserName",
            "NormalizedEmail",
            "AccessFailedCount",
            "LockoutEnabled",
            "LockoutEnd",
            "PhoneNumberConfirmed",
            "TwoFactorEnabled",
            "EmailConfirmed"
        };

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var dbContext = eventData.Context;
            if (dbContext == null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

            var httpContext = _httpContextAccessor.HttpContext;
            string? userId = null;
            string? userName = null;
            string? ipAddress = null;
            string? userAgent = null;

            if (httpContext != null)
            {
                userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                userName = httpContext.User.Identity?.Name;
                ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            }

            var entries = dbContext.ChangeTracker.Entries()
                .Where(e => e.Entity is not AuditLog &&
                             (e.State == EntityState.Added ||
                              e.State == EntityState.Modified ||
                              e.State == EntityState.Deleted))
                .ToList();

            var auditLogs = new List<AuditLog>();

            foreach (var entry in entries)
            {
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    EntityType = entry.Entity.GetType().Name,
                    Timestamp = DateTime.UtcNow.AddHours(3),
                    Success = true
                };

                var primaryKey = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                if (primaryKey != null)
                {
                    auditLog.EntityId = entry.State == EntityState.Added
                        ? primaryKey.CurrentValue?.ToString()
                        : primaryKey.OriginalValue?.ToString();
                }

                var oldValues = new Dictionary<string, object?>();
                var newValues = new Dictionary<string, object?>();


                foreach (var prop in entry.Properties)
                {
                    string propName = prop.Metadata.Name;


                    if (propName == "PasswordHash" && entry.State == EntityState.Modified && prop.IsModified)
                    {
                        var originalHash = prop.OriginalValue?.ToString();
                        var currentHash = prop.CurrentValue?.ToString();

                        if (originalHash != currentHash)
                        {
                            oldValues["Password"] = "********";
                            newValues["Password"] = "********";
                        }

                        continue;
                    }

                    if (_ignoredProperties.Contains(propName)) continue;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            newValues[propName] = prop.CurrentValue;
                            break;

                        case EntityState.Deleted:
                            oldValues[propName] = prop.OriginalValue;
                            break;

                        case EntityState.Modified:
                            if (prop.IsModified)
                            {
                                var original = prop.OriginalValue?.ToString();
                                var current = prop.CurrentValue?.ToString();

                                if (original != current)
                                {
                                    oldValues[propName] = prop.OriginalValue;
                                    newValues[propName] = prop.CurrentValue;
                                }
                            }
                            break;
                    }
                }


                bool hasChanges = newValues.Count > 0 || oldValues.Count > 0;

                if (hasChanges)
                {
                    auditLog.Action = entry.State switch
                    {
                        EntityState.Added => "Insert",
                        EntityState.Modified => "Update",
                        EntityState.Deleted => "Delete",
                        _ => entry.State.ToString()
                    };

                    if (oldValues.Count > 0)
                        auditLog.OldValues = JsonSerializer.Serialize(oldValues, _jsonOptions);

                    if (newValues.Count > 0)
                        auditLog.NewValues = JsonSerializer.Serialize(newValues, _jsonOptions);

                    auditLogs.Add(auditLog);
                }
            }

            if (auditLogs.Any())
            {
                await dbContext.Set<AuditLog>().AddRangeAsync(auditLogs, cancellationToken);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}