using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;

namespace UnAd.Kafka.Tests;

public static class CSharpSourceGeneratorVerifier<TSourceGenerator>
    where TSourceGenerator : IIncrementalGenerator, new() {
    public class Test : CSharpSourceGeneratorTest<TSourceGenerator, Microsoft.CodeAnalysis.Testing.DefaultVerifier> {
        public Test() { }

        protected override CompilationOptions CreateCompilationOptions() {
            var compilationOptions = base.CreateCompilationOptions();
            return compilationOptions.WithSpecificDiagnosticOptions(
                 compilationOptions.SpecificDiagnosticOptions.SetItems(GetNullableWarningsFromCompiler()));
        }

        public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

        private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler() {
            string[] args = ["/warnaserror:nullable"];
            var commandLineArguments = CSharpCommandLineParser.Default.Parse(args, baseDirectory: Environment.CurrentDirectory, sdkDirectory: Environment.CurrentDirectory);
            var nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

            return nullableWarnings;
        }

        protected override ParseOptions CreateParseOptions() =>
            ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
    }
}
