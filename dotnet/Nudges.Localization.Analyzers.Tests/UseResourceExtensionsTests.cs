namespace Nudges.Localization.Analyzers.Tests;

public class UseResourceExtensionsTests {
    [Fact]
    public async Task GeneratesProducersWithoutError() {
        var sourceCode = """
            using Nudges.Localization;
            using Nudges.Localization.Extensions;

            namespace TestProgram;

            [UseResourceExtensions]
            public class Program {
                public static void Main(IStringLocalizer<Program> localizer) {
                    x
                    Console.WriteLine(localizer.GetTest());
                }
            }

            internal static class Handlers {
                public static string Login(IStringLocalizer<Program> localizer) {
                    return localizer.GetTest();
                }
            }
        """;

        await TestHelper.Verify(sourceCode, [
            ("Program.resx", File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Program.resx")))
        ]);

    }
}
