using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nudges.Configuration.Analyzers;

[Generator]
public class AppConfigurationGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(static postInitializationContext =>
            postInitializationContext.AddSource("ConfigurationKeyAttribute.g.cs", SourceText.From("""
                namespace Nudges.Configuration {
                    [global::System.CodeDom.Compiler.GeneratedCode("Nudges.Configuration.Analyzers", "0.0.1")]
                    [global::System.AttributeUsage(AttributeTargets.Field)]
                    public sealed class ConfigurationKeyAttribute : global::System.Attribute { }
                
                    [global::System.CodeDom.Compiler.GeneratedCode("Nudges.Configuration.Analyzers", "0.0.1")]
                    public class MissingConfigException(string key) : global::System.Exception($"Config key {key} has no value") { }
                }
                """, Encoding.UTF8)));

        var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "Nudges.Configuration.ConfigurationKeyAttribute",
            predicate: (node, _) => IsSyntaxTargetForGeneration(node),
            transform: GenerateSyntaxModels
        )
        .Where(static model => model is not null)
        .Collect();

        context.RegisterSourceOutput(pipeline, static (context, keys) =>
            context.AddSource($"ConfigurationExtensions.g.cs", SourceText.From(GenerateClass(keys), Encoding.UTF8)));
    }

    private static string GenerateClass(IEnumerable<Model> strings) => $$"""
        namespace Nudges.Configuration.Extensions {
            [global::System.CodeDom.Compiler.GeneratedCode("Nudges.Configuration.Analyzers", "0.0.1")]
            public static partial class ConfigurationExtensions {
                {{string.Join("\r\n", strings.Select(GenerateExtensionMethod))}}
            }
        }
        """;

    private static string GenerateExtensionMethod(Model model) => $$"""
        /// <summary>
        /// Gets the value of the configuration key {{model.Value}}
        /// </summary>
        public static string Get{{model.KeyName}}(this global::Microsoft.Extensions.Configuration.IConfiguration configuration) =>
            configuration["{{model.Value}}"] ?? throw new MissingConfigException("{{model.Value}}");
        """;

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) {
        if (node is VariableDeclaratorSyntax field) {
            // TODO: make sure they are publicly accessible (or do they need to be?)
            if (field.Initializer?.Value is LiteralExpressionSyntax literal && !string.IsNullOrEmpty(literal?.Token.Text)) {
                return true;
            }
            ;
        }
        return false;
    }

    private static Model GenerateSyntaxModels(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken = default) {
        if (context.TargetSymbol is IFieldSymbol fieldSymbol) {
            // Check if the field is of type string
            if (fieldSymbol.Type.SpecialType == SpecialType.System_String) {
                // Verify if the field is a constant field
                if (fieldSymbol.IsConst) {
                    // Extract the constant value
                    if (fieldSymbol.ConstantValue is string constantValue) {
                        // Create and return your model using the field name and the constant value
                        return new Model(fieldSymbol.Name, constantValue);
                    } else {
                        var syntaxReference = fieldSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                        if (syntaxReference != null) {
                            // Obtain the field syntax node
                            var fieldSyntax = syntaxReference.GetSyntax(cancellationToken) as VariableDeclaratorSyntax;
                            if (fieldSyntax?.Initializer?.Value is LiteralExpressionSyntax literal && literal.Token.Value is string literalValue) {
                                return new Model(fieldSymbol.Name, literalValue);
                            }
                        }
                    }
                }
            }
        }
        // Return a default model if no match found
        return default!;
    }


    private class Model(string keyName, string value) {
        public string KeyName { get; } = keyName;
        public string Value { get; } = value;
    }
}
