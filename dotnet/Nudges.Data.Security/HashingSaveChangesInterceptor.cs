using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nudges.Security;

namespace Nudges.Data.Security;

public sealed class HashingSaveChangesInterceptor(HashService hashService) : SaveChangesInterceptor {
    private static HashedAttribute? GetHashedAttribute(PropertyEntry entry) =>
        entry.Metadata.PropertyInfo?.GetCustomAttribute<HashedAttribute>();

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result) {
        Process(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default) {
        Process(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Process(DbContext? context) {
        if (context is null) {
            return;
        }

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entityEntry in entries) {
            foreach (var sourceProp in entityEntry.Properties) {
                var attr = GetHashedAttribute(sourceProp);
                if (attr is null || sourceProp.Metadata.ClrType != typeof(string)) {
                    continue;
                }

                // attr.SourceProperty is the target (where the hash should be stored)
                var targetProp = entityEntry.Properties.FirstOrDefault(p => p.Metadata.Name == attr.SourceProperty);
                var value = sourceProp?.CurrentValue as string;
                if (targetProp is not null && !string.IsNullOrWhiteSpace(value)) {
                    targetProp.CurrentValue = hashService.ComputeHash(value);
                }
            }
        }
    }
}
