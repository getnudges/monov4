using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Nudges.Configuration.Analyzers.Tests;
public static class TestHelper {
    public static Task Verify(string source) {
        // Parse the provided string into a C# syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Create a Roslyn compilation for the syntax tree.
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [syntaxTree]);


        // Create an instance of our EnumGenerator incremental source generator
        var generator = new AppConfigurationGenerator();

        // The GeneratorDriver is used to run our generator against a compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the source generator!
        driver = driver.RunGenerators(compilation);

        var results = driver.GetRunResult();
        var errors = results.Diagnostics.Any(driver => driver.Severity == DiagnosticSeverity.Error);

        Assert.False(errors, "Expected no errors");
        // Use verify to snapshot test the source generator output!
        //return Verifier.Verify(driver);
        return Task.CompletedTask;
    }
}
