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
                    public sealed class ConfigurationKeyAttribute(bool optional = false) : global::System.Attribute {
                        public bool Optional { get; } = optional;
                    }
                
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
        public static string{{(model.Optional ? "?" : "")}} Get{{model.KeyName}}(this global::Microsoft.Extensions.Configuration.IConfiguration configuration) =>
            configuration["{{model.Value}}"]{{(model.Optional ? ";" : $" ?? throw new global::Nudges.Configuration.MissingConfigException(\"{model.Value}\");")}}
        """;

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) {
        if (node is VariableDeclaratorSyntax field) {
            // TODO: make sure they are publicly accessible (or do they need to be?)
            if (field.Initializer?.Value is LiteralExpressionSyntax literal && !string.IsNullOrEmpty(literal?.Token.Text)) {
                return true;
            }
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
                    string? constantValue = null;
                    if (fieldSymbol.ConstantValue is string cv) {
                        constantValue = cv;
                    } else {
                        var syntaxReference = fieldSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                        if (syntaxReference != null) {
                            // Obtain the field syntax node
                            var fieldSyntax = syntaxReference.GetSyntax(cancellationToken) as VariableDeclaratorSyntax;
                            if (fieldSyntax?.Initializer?.Value is LiteralExpressionSyntax literal && literal.Token.Value is string literalValue) {
                                constantValue = literalValue;
                            }
                        }
                    }

                    if (constantValue is not null) {
                        var optional = false;

                        // Get the actual attribute syntax from the target node
                        if (context.TargetNode is VariableDeclaratorSyntax variableDeclarator) {
                            var fieldDeclaration = variableDeclarator.Parent?.Parent as FieldDeclarationSyntax;

                            var attributeSyntax = fieldDeclaration?.AttributeLists
                                .SelectMany(al => al.Attributes)
                                .FirstOrDefault(a => a.Name.ToString().Contains("ConfigurationKey"));

                            if (attributeSyntax?.ArgumentList?.Arguments.Count > 0) {
                                var firstArg = attributeSyntax.ArgumentList.Arguments[0];

                                // Check if it's a positional argument (true/false literal)
                                if (firstArg.NameEquals?.Name.Identifier.Text == "Optional") {
                                    var cv1 = context.SemanticModel.GetConstantValue(firstArg.Expression, cancellationToken);
                                    if (cv1.HasValue && cv1.Value is bool boolValue) {
                                        optional = boolValue;
                                    }
                                }
                                // Check for named argument (optional: true)
                                else if (firstArg.Expression is LiteralExpressionSyntax) {
                                    var cv2 = context.SemanticModel.GetConstantValue(firstArg.Expression, cancellationToken);
                                    if (cv2.HasValue && cv2.Value is bool boolValue) {
                                        optional = boolValue;
                                    }
                                }
                            }
                        }
                        return new Model(fieldSymbol.Name, constantValue, optional);
                    }
                }
            }
        }

        // Return a default model if no match found
        return default!;
    }


    private class Model(string keyName, string value, bool optional) {
        public string KeyName { get; } = keyName;
        public string Value { get; } = value;
        public bool Optional { get; } = optional;
    }
}
