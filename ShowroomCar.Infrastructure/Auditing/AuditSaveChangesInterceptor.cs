using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using ShowroomCar.Infrastructure.Persistence;
using ShowroomCar.Infrastructure.Persistence.Entities;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace ShowroomCar.Infrastructure.Auditing
{
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _http;
        private readonly AuditScope _scope;
        private readonly ILogger<AuditSaveChangesInterceptor> _logger;

        // Tạm giữ audit entries giữa SavingChanges → SavedChanges
        private readonly List<AuditLog> _buffer = new();

        public AuditSaveChangesInterceptor(
            IHttpContextAccessor http,
            AuditScope scope,
            ILogger<AuditSaveChangesInterceptor> logger)
        {
            _http = http;
            _scope = scope;
            _logger = logger;
        }

        private long? GetCurrentUserId()
        {
            var user = _http.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true) return null;

            // JWT mình đã set 'sub' = userId
            var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;
            if (long.TryParse(sub, out var uid)) return uid;
            return null;
        }

        private static object? GetPk(EntityEntry e)
        {
            var key = e.Metadata.FindPrimaryKey();
            if (key == null) return null;
            if (key.Properties.Count == 1)
            {
                var prop = key.Properties[0].Name;
                return e.Property(prop).CurrentValue ?? e.Property(prop).OriginalValue;
            }
            // PK phức hợp → trả về dictionary
            var dict = new Dictionary<string, object?>();
            foreach (var p in key.Properties)
            {
                var v = e.Property(p.Name);
                dict[p.Name] = v.CurrentValue ?? v.OriginalValue;
            }
            return dict;
        }

        private static string? BuildChanges(EntityEntry e)
        {
            var data = new Dictionary<string, object?>();

            switch (e.State)
            {
                case EntityState.Added:
                    foreach (var p in e.Properties)
                        data[p.Metadata.Name] = new { current = p.CurrentValue };
                    break;

                case EntityState.Deleted:
                    foreach (var p in e.Properties)
                        data[p.Metadata.Name] = new { original = p.OriginalValue };
                    break;

                case EntityState.Modified:
                    foreach (var p in e.Properties.Where(p => p.IsModified))
                        data[p.Metadata.Name] = new { original = p.OriginalValue, current = p.CurrentValue };
                    break;

                default:
                    return null;
            }

            return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
        }

        private static string? MapAction(EntityState state) =>
            state switch
            {
                EntityState.Added => "CREATE",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted => "DELETE",
                _ => null
            };

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (_scope.IsAuditing) return base.SavingChanges(eventData, result);

            var ctx = eventData.Context as ShowroomDbContext;
            if (ctx == null) return base.SavingChanges(eventData, result);

            _buffer.Clear();
            var actor = GetCurrentUserId();

            foreach (var e in ctx.ChangeTracker.Entries().Where(x =>
                         x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                // Bỏ qua bảng audit chính nó
                if (e.Entity is AuditLog) continue;

                var action = MapAction(e.State);
                if (action == null) continue;

                var entityName = e.Metadata.GetTableName() ?? e.Entity.GetType().Name;
                var entityId = GetPk(e);
                var changes = BuildChanges(e);

                _buffer.Add(new AuditLog
                {
                    Entity = entityName,
                    EntityId = entityId?.ToString() ?? "",
                    Action = action,
                    Changes = changes,
                    ActorUserId = actor,
                    CreatedAt = DateTime.Now
                });
            }

            return base.SavingChanges(eventData, result);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            TryFlushAudit(eventData);
            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            await TryFlushAuditAsync(eventData, cancellationToken);
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        private void TryFlushAudit(DbContextEventData eventData)
        {
            if (_scope.IsAuditing) return;
            if (_buffer.Count == 0) return;

            var ctx = eventData.Context as ShowroomDbContext;
            if (ctx == null) return;

            try
            {
                _scope.IsAuditing = true; // chặn đệ quy
                ctx.AuditLogs.AddRange(_buffer);
                ctx.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit flush failed");
            }
            finally
            {
                _buffer.Clear();
                _scope.IsAuditing = false;
            }
        }

        private async Task TryFlushAuditAsync(DbContextEventData eventData, CancellationToken ct)
        {
            if (_scope.IsAuditing) return;
            if (_buffer.Count == 0) return;

            var ctx = eventData.Context as ShowroomDbContext;
            if (ctx == null) return;

            try
            {
                _scope.IsAuditing = true; // chặn đệ quy
                await ctx.AuditLogs.AddRangeAsync(_buffer, ct);
                await ctx.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit flush failed");
            }
            finally
            {
                _buffer.Clear();
                _scope.IsAuditing = false;
            }
        }
    }
}
