using System.Reflection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Nudges.Security;

namespace Nudges.Data.Security;

public sealed class EncryptionMaterializationInterceptor(IEncryptionService encryption) : IMaterializationInterceptor {
    public object InitializedInstance(MaterializationInterceptionData materializationData, object entity) {
        DecryptEntity(entity);
        return entity;
    }
    private static EncryptedAttribute? GetEncryptedAttribute(PropertyEntry entry) =>
        entry.Metadata.PropertyInfo?.GetCustomAttribute<EncryptedAttribute>();

    private void DecryptEntity(object entity) {
        var type = entity.GetType();

        var encryptedProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p =>
                p.GetCustomAttribute<EncryptedAttribute>() is not null &&
                p.PropertyType == typeof(string) &&
                p.CanRead && p.CanWrite)
                .Select(p => (type.GetProperty(p.GetCustomAttribute<EncryptedAttribute>()!.SourceProperty)!, p));

        foreach (var (sourceProp, targetProp) in encryptedProps) {
            var sourceValue = sourceProp.GetValue(entity) as string;
            if (string.IsNullOrEmpty(sourceValue)) {
                continue;
            }
            try {
                var decrypted = encryption.Decrypt(sourceValue);
                targetProp.SetValue(entity, decrypted);
            } catch {
                // idk....
            }
        }
    }
}
