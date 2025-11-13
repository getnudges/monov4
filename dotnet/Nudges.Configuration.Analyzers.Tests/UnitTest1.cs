namespace Nudges.Configuration.Analyzers.Tests;

public class UnitTest1 {
    [Fact]
    public Task GeneratesProducersWithoutError() {
        var sourceCode = """
            using Nudges.Configuration;
            namespace TestNamespace;

            public static class AppConfig {
                [ConfigurationKey]
                public const string RedisUrl = "REDIS_URL";
                [ConfigurationKey(true)]
                public const string OtherUrl = "OTHER_URL";
                [ConfigurationKey(Optional = true)]
                public const string OtherOtherUrl = "OTHER_OTHER_URL_FU";
            }
        """;
        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(sourceCode);
    }
}
