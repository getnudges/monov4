using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace UnAd.Localization.Analyzers.Tests;

public static class TestHelper {
    public static Task Verify(string source, (string name, string contents)[] resxFiles) {
        // Parse the provided string into a C# syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Create a Roslyn compilation for the syntax tree.
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [syntaxTree],
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]
        );

        // Convert the provided resxFiles into AdditionalText objects
        var additionalTexts = resxFiles.Select((resxContent) =>
            new TestAdditionalText($"{resxContent.name}.resx", resxContent.contents)).ToImmutableArray<AdditionalText>();

        // Create an instance of our ResourceStringGenerator incremental source generator
        var generator = new ResourceStringGenerator();

        // The GeneratorDriver is used to run our generator against a compilation
        var driver = CSharpGeneratorDriver.Create(generator).AddAdditionalTexts(additionalTexts);

        // Run the source generator!
        driver = driver.RunGenerators(compilation);

        var results = driver.GetRunResult();
        var errors = results.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        Assert.False(errors, "Expected no errors");

        // Use verify to snapshot test the source generator output!
        //return Verifier.Verify(driver);
        return Task.CompletedTask;
    }
}

// Helper class to mock AdditionalText for the resx files
internal class TestAdditionalText(string path, string text) : AdditionalText {
    private readonly string _text = text;

    public override string Path { get; } = path;

    public override SourceText GetText(CancellationToken cancellationToken) =>
        SourceText.From(_text, Encoding.UTF8);

}
