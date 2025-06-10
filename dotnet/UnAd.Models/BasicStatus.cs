using Ardalis.SmartEnum;

namespace UnAd.Models;

public class BasicStatus : SmartEnum<BasicStatus> {
    public static readonly BasicStatus Active = new(nameof(Active).ToUpperInvariant(), 1);
    public static readonly BasicStatus Inactive = new(nameof(Inactive).ToUpperInvariant(), 2);
    public static readonly BasicStatus Archived = new(nameof(Archived).ToUpperInvariant(), 3);
    public static readonly BasicStatus Deleted = new(nameof(Deleted).ToUpperInvariant(), 4);

    private BasicStatus(string name, int value) : base(name, value) { }
}

public static class PriceTierStatusExtensions {

    public static BasicStatus ToPriceTierStatus(this string status) {
        return status switch {
            "ACTIVE" => BasicStatus.Active,
            "INACTIVE" => BasicStatus.Inactive,
            "ARCHIVED" => BasicStatus.Archived,
            "DELETED" => BasicStatus.Deleted,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Invalid status"),
        };
        ;
    }

    public static string ToStatusString(this BasicStatus status) => status.ToString().ToUpperInvariant();
}

