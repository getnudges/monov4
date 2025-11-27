namespace Nudges.Security;

[AttributeUsage(AttributeTargets.Property)]
public sealed class EncryptedAttribute(string sourceProperty) : Attribute {
    public string SourceProperty { get; } = sourceProperty;
}
