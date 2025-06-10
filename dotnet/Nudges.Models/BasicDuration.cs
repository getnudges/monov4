using Ardalis.SmartEnum;

namespace Nudges.Models;

public class BasicDuration : SmartEnum<BasicDuration> {
    public static readonly BasicDuration Week = new($"P7D", 7, TimeSpan.FromDays(7));
    public static readonly BasicDuration Month = new("P30D", 30, TimeSpan.FromDays(30));
    public static readonly BasicDuration Year = new("P365D", 365, TimeSpan.FromDays(365));

    public int Days { get; }
    public TimeSpan Duration { get; }

    private BasicDuration(string name, int days, TimeSpan duration) : base(name, days) {
        Days = days;
        Duration = duration;
    }
}

public static class BasicDurationExtensions {
    public static TimeSpan ToTimeSpan(this BasicDuration duration) => TimeSpan.FromDays(duration.Days);

    public static BasicDuration ToBasicDuration(this TimeSpan duration) =>
        duration switch {
            { Days: 7 } => BasicDuration.Week,
            { Days: 30 } => BasicDuration.Month,
            { Days: 365 } => BasicDuration.Year,
            _ => throw new ArgumentOutOfRangeException(nameof(duration), duration, "Invalid duration"),
        };

    public static string ToDurationString(this BasicDuration duration) => duration.Name.ToLowerInvariant();
}
