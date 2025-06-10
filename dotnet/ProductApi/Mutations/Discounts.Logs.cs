namespace ProductApi;

public static partial class DiscountsLogs {


    [LoggerMessage(Level = LogLevel.Information, Message = "Discount Code {Name} created with ID {Id}")]
    public static partial void LogDiscountCodeCreated(this ILogger<Mutation> logger, int Id, string Name);


    [LoggerMessage(Level = LogLevel.Information, Message = "Discount Code {Id} deleted")]
    public static partial void LogDiscountCodeDeleted(this ILogger<Mutation> logger, int Id);
}
