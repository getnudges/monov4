namespace UnAd.Configuration.Analyzers.Tests;

public class UnitTest1 {
    [Fact]
    public Task GeneratesProducersWithoutError() {
        var sourceCode = """
            using UnAd.Configuration;
            namespace TestNamespace;

            public static class AppConfig {
                [ConfigurationKey]
                public const string RedisUrl = "REDIS_URL";
            }
        """;
        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(sourceCode);
    }
}
