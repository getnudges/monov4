using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace UnAd.Localization.Analyzers;

[Generator]
public class ResourceStringGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(static postInitializationContext => {
            postInitializationContext.AddSource("MissingResourceException.g.cs", SourceText.From("""
                namespace UnAd.Localization {
                    [global::System.CodeDom.Compiler.GeneratedCode("UnAd.Localization.Analyzers", "0.0.1")]
                    [global::System.Serializable]
                    public class MissingResourceException(string key) : global::System.Exception($"Resource key {key} value not found.") { }
                }
                """, Encoding.UTF8));
            postInitializationContext.AddSource("UseResourceExtensionsAttribute.g.cs", SourceText.From("""
                namespace UnAd.Localization {
                    [global::System.CodeDom.Compiler.GeneratedCode("UnAd.Localization.Analyzers", "0.0.1")]
                    [global::System.AttributeUsage(AttributeTargets.Class)]
                    public sealed class UseResourceExtensionsAttribute : global::System.Attribute { }
                }
                """, Encoding.UTF8));
        });

        var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "UnAd.Localization.UseResourceExtensionsAttribute",
            predicate: static (node, _) => IsSyntaxTargetForGeneration(node),
            transform: GenerateSyntaxModels
        )
        .Where(static model => model is not null)
        .Collect();

        var resourceFiles = context.AdditionalTextsProvider.Where(static file => file.Path.EndsWith(".resx"));
        var namesAndContents = resourceFiles
            .Select(static (text, cancellationToken) =>
                new FileModel(Path.GetFileNameWithoutExtension(text.Path), text.GetText(cancellationToken)!.ToString()))
            .Collect();

        context.RegisterSourceOutput(pipeline.Combine(namesAndContents), static (context, combined) => {
            var (models, files) = combined;
            var generatedCode = GenerateLocalizationClassesForAttributes(models, files);
            context.AddSource("LocalizationExtensions.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
        });
    }

    private static string GenerateLocalizationClassesForAttributes(IEnumerable<ClassModel> models, IEnumerable<FileModel> namesAndContents) {
        var classes = new StringBuilder();

        foreach (var model in models) {
            var resourceFile = namesAndContents.FirstOrDefault(file => file.FileName.Split('.').First() == model.ClassName);
            if (resourceFile is not null) {
                var document = XDocument.Parse(resourceFile.Contents);

                classes.AppendLine($$"""
                    namespace UnAd.Localization.Extensions {
                        [global::System.CodeDom.Compiler.GeneratedCode("UnAd.Localization.Analyzers", "0.0.1")]
                        public static partial class LocalizationExtensions {
                            {{string.Join(System.Environment.NewLine, document.Descendants("data")
                                .Select(element => element.Attribute("name")?.Value)
                                .Where(key => !string.IsNullOrEmpty(key))
                                .Select(key => GenerateExtensionMethod(model, key!)))}}
                    }
                """);
            }
        }

        return classes.ToString();
    }

    private static string GenerateExtensionMethod(ClassModel model, string key) => $$"""
        /// <summary>
        /// Gets the value of the Resource key {{key}}
        /// </summary>
        public static string Get{{key}}(this global::Microsoft.Extensions.Localization.IStringLocalizer<{{model.ClassNamespace}}.{{model.ClassName}}> localizer) {
            if (localizer.GetString("{{key}}") is { } localized && !localized.ResourceNotFound) {
                return localized.Value;
            }
            throw new MissingResourceException("{{key}}");
        }
    """;

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) {
        if (node is ClassDeclarationSyntax classSyntax) {
            var result = classSyntax.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => attr.Name.ToString().StartsWith("UseResourceExtensions", System.StringComparison.Ordinal));
            return result;
        }
        return false;
    }

    private static ClassModel GenerateSyntaxModels(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken = default) {
        if (context.TargetSymbol is INamedTypeSymbol typeSymbol) {
            var className = typeSymbol.Name;
            var classNamespace = typeSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return new ClassModel(className, classNamespace);
        }
        // Return a default model if no match found
        return default!;
    }

    private class ClassModel(string containerName, string containerNamespace) {
        public string ClassName { get; } = containerName;
        public string ClassNamespace { get; } = containerNamespace;
    }

    private class FileModel(string fileName, string contents) {
        public string FileName { get; } = fileName;
        public string Contents { get; } = contents;
    }
}
