using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nudges.Security;

namespace Nudges.Data.Security;

public sealed class EncryptionSaveChangesInterceptor(IEncryptionService encryption)
    : SaveChangesInterceptor {

    private static EncryptedAttribute? GetEncryptedAttribute(PropertyEntry entry) =>
        entry.Metadata.PropertyInfo?.GetCustomAttribute<EncryptedAttribute>();

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
                var attr = GetEncryptedAttribute(sourceProp);
                if (attr is null || sourceProp.Metadata.ClrType != typeof(string)) {
                    continue;
                }

                var targetProp = entityEntry.Properties.FirstOrDefault(p => p.Metadata.Name == attr.SourceProperty);
                var value = sourceProp?.CurrentValue as string;
                if (targetProp is not null && !string.IsNullOrWhiteSpace(value)) {
                    targetProp.CurrentValue = encryption.Encrypt(value);
                }
            }
        }
    }
}
