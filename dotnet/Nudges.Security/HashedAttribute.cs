namespace Nudges.Security;

[AttributeUsage(AttributeTargets.Property)]
public sealed class HashedAttribute(string sourceProperty) : Attribute {
    public string SourceProperty { get; } = sourceProperty;
}
