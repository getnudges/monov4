using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nudges.Kafka.Analyzers;

[Generator]
public class ModelProducerGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(static postInitializationContext =>
            postInitializationContext.AddSource("EventModelAttribute.g.cs", SourceText.From("""
                namespace Nudges.Kafka {
                    [global::System.CodeDom.Compiler.GeneratedCode("Nudges.Kafka.Analyzers", "0.0.1")]
                    [global::System.AttributeUsage(AttributeTargets.Class)]
                    public sealed class EventModelAttribute(global::System.Type EventKeyType) : global::System.Attribute { }
                }
                """, Encoding.UTF8)));

        var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "Nudges.Kafka.EventModelAttribute",
            predicate: (node, _) => IsSyntaxTargetForGeneration(node),
            transform: GenerateSyntaxModels
        )
        .Where(static model => model?.ModelTypeName is not null)
        .Collect();


        //context.RegisterSourceOutput(pipeline, static (context, models) => {
        //    foreach (var model in models) {
        //        context.AddSource($"{model.ModelTypeName}Seralizers.g.cs", SourceText.From(GenerateSerializers(model), Encoding.UTF8));
        //    }
        //});

        context.RegisterSourceOutput(pipeline, static (context, models) => {
            foreach (var model in models) {
                context.AddSource($"{model.ModelTypeName}Producer.g.cs", SourceText.From(GenerateProducer(model), Encoding.UTF8));
                context.AddSource($"{model.ModelTypeName}Consumer.g.cs", SourceText.From(GenerateConsumer(model), Encoding.UTF8));
            }
        });
    }

    private static string GenerateProducer(Model model) => $$"""
        using System.Diagnostics;
        namespace Nudges.Kafka {
            [global::System.CodeDom.Compiler.GeneratedCode("Nudges.Kafka", "0.0.1")]
            public sealed partial class {{model.ModelTypeName}}Producer(string topic, global::Confluent.Kafka.ProducerConfig producerConfig): global::Nudges.Kafka.KafkaMessageProducer<{{model.KeyNamespace}}.{{model.KeyTypeName}}, {{model.ModelNamespace}}.{{model.ModelTypeName}}> {
        
                internal sealed class {{model.ModelTypeName}}Serializer : global::Confluent.Kafka.ISerializer<{{model.ModelNamespace}}.{{model.ModelTypeName}}> {
                    public byte[] Serialize({{model.ModelNamespace}}.{{model.ModelTypeName}} data, global::Confluent.Kafka.SerializationContext context) =>
                        global::System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data/*, global::Nudges.Kafka.{{model.ModelTypeName}}SerializerContext.Default.{{model.ModelTypeName}}*/);
                }
        
                internal sealed class {{model.KeyTypeName}}Serializer : global::Confluent.Kafka.ISerializer<{{model.KeyNamespace}}.{{model.KeyTypeName}}> {
                    public byte[] Serialize({{model.KeyNamespace}}.{{model.KeyTypeName}} data, global::Confluent.Kafka.SerializationContext context) => 
                        global::System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data/*, global::Nudges.Kafka.{{model.KeyTypeName}}SerializerContext.Default.{{model.KeyTypeName}}*/);
                }
        
                private readonly global::Confluent.Kafka.IProducer<{{model.KeyNamespace}}.{{model.KeyTypeName}}, {{model.ModelNamespace}}.{{model.ModelTypeName}}> _producer =
                    new global::Confluent.Kafka.ProducerBuilder<{{model.KeyNamespace}}.{{model.KeyTypeName}}, {{model.ModelNamespace}}.{{model.ModelTypeName}}>(producerConfig)
                    .SetValueSerializer(new {{model.ModelTypeName}}Serializer())
                    .SetKeySerializer(new {{model.KeyTypeName}}Serializer())
                    .Build();
        
                public override Task<global::Confluent.Kafka.DeliveryResult<{{model.KeyNamespace}}.{{model.KeyTypeName}}, {{model.ModelNamespace}}.{{model.ModelTypeName}}>> Produce(
                    {{model.KeyNamespace}}.{{model.KeyTypeName}} key, {{model.ModelNamespace}}.{{model.ModelTypeName}} data, global::System.Threading.CancellationToken cancellationToken = default) {
                    using var activity = KafkaMessageProducer<{{model.KeyNamespace}}.{{model.KeyTypeName}}, {{model.ModelNamespace}}.{{model.ModelTypeName}}>.ActivitySource.StartActivity(
                        "ProduceKafkaMessage_{{model.KeyTypeName}}", global::System.Diagnostics.ActivityKind.Producer, global::System.Diagnostics.Activity.Current?.Context ?? default);
                    try {
                        activity?.SetTag("kafka.topic", topic);
                        activity?.SetTag("message.key", key?.ToString());
                        activity?.Start();
                        var resultTask = _producer.ProduceAsync(topic, new global::Confluent.Kafka.Message<{{model.KeyNamespace}}.{{model.KeyTypeName}}, {{model.ModelNamespace}}.{{model.ModelTypeName}}> {
                            Key = key,
                            Value = data,
                            Headers = [
                                new global::Confluent.Kafka.Header("traceparent", global::System.Text.Encoding.UTF8.GetBytes(activity?.Id ?? string.Empty)),
                                new global::Confluent.Kafka.Header("tracestate", global::System.Text.Encoding.UTF8.GetBytes(activity?.Context.TraceState ?? string.Empty))
                            ]
                        }, cancellationToken);
                        _ = resultTask.ContinueWith(t => {
                            if (t.IsFaulted) {
                                activity?.SetStatus(ActivityStatusCode.Error);
                                activity?.SetTag("exception.message", t.Exception?.GetBaseException().Message);
                            } else {
                                activity?.SetStatus(ActivityStatusCode.Ok);
                            }
                        }, TaskScheduler.Default);
                        return resultTask;
                    } catch (global::System.Exception ex) {
                        activity?.SetStatus(ActivityStatusCode.Error);
                        activity?.SetTag("exception.message", ex.Message);
                        activity?.SetTag("exception.stacktrace", ex.StackTrace);
                        throw;
                    }
                }
        
                public void Dispose() =>
                    _producer.Dispose();
            }
        }
        """;

    private static string GenerateConsumer(Model model) => $$"""
        namespace Nudges.Kafka {
            [global::System.CodeDom.Compiler.GeneratedCode("Nudges.Kafka", "0.0.1")]
            public sealed partial class {{model.ModelTypeName}}Consumer(string topic, global::Confluent.Kafka.ConsumerConfig consumerConfig) {
        
            private sealed class {{model.ModelTypeName}}Deserializer : global::Confluent.Kafka.IDeserializer<{{model.ModelNamespace}}.{{model.ModelTypeName}}> {
                public {{model.ModelNamespace}}.{{model.ModelTypeName}} Deserialize(global::System.ReadOnlySpan<byte> data, bool isNull, global::Confluent.Kafka.SerializationContext context) =>
                    isNull
                    || global::System.Text.Encoding.UTF8.GetString(data).Equals("null", global::System.StringComparison.OrdinalIgnoreCase)
                        ? default! // TODO: what do I do here?
                        : global::System.Text.Json.JsonSerializer.Deserialize<{{model.ModelNamespace}}.{{model.ModelTypeName}}>(data/*, {{model.ModelTypeName}}SerializerContext.Default.{{model.ModelTypeName}}*/)
                            ?? throw new InvalidOperationException("Failed to deserialize {{model.ModelTypeName}}");
            }
        
            private sealed class {{model.KeyTypeName}}Deserializer : global::Confluent.Kafka.IDeserializer<{{model.KeyNamespace}}.{{model.KeyTypeName}}> {
                public {{model.KeyNamespace}}.{{model.KeyTypeName}} Deserialize(global::System.ReadOnlySpan<byte> data, bool isNull, global::Confluent.Kafka.SerializationContext context) =>
                    isNull
                        || global::System.Text.Encoding.UTF8.GetString(data).Equals("null", global::System.StringComparison.OrdinalIgnoreCase)
                            ? default! // TODO: what do I do here?
                            : global::System.Text.Json.JsonSerializer.Deserialize<{{model.KeyNamespace}}.{{model.KeyTypeName}}>(data/*, {{model.KeyTypeName}}SerializerContext.Default.{{model.KeyTypeName}}*/)
                                ?? throw new InvalidOperationException("Failed to deserialize {{model.KeyTypeName}}");
            }
        
            private readonly global::Confluent.Kafka.IConsumer<{{model.KeyNamespace}}.{{model.KeyTypeName}}, {{model.ModelNamespace}}.{{model.ModelTypeName}}> _consumer =
                new global::Confluent.Kafka.ConsumerBuilder<{{model.KeyNamespace}}.{{model.KeyTypeName}}, {{model.ModelNamespace}}.{{model.ModelTypeName}}>(consumerConfig)
                .SetValueDeserializer(new {{model.ModelTypeName}}Deserializer())
                .SetKeyDeserializer(new {{model.KeyTypeName}}Deserializer())
                .Build();
        
            public void Subscribe() =>
                _consumer.Subscribe(topic);
        
            public global::Confluent.Kafka.ConsumeResult<{{model.KeyNamespace}}.{{model.KeyTypeName}}, {{model.ModelNamespace}}.{{model.ModelTypeName}}> Consume(
                global::System.Threading.CancellationToken cancellationToken = default) =>
                _consumer.Consume(cancellationToken);
        
            public void Commit(global::Confluent.Kafka.ConsumeResult<{{model.KeyNamespace}}.{{model.KeyTypeName}}, {{model.ModelNamespace}}.{{model.ModelTypeName}}> consumeResult) =>
                _consumer.Commit(consumeResult);
        
            public void Close() =>
                _consumer.Close();
        
            public void Dispose() =>
                _consumer.Dispose();
            }
        }
        """;

    private static string GenerateSerializers(Model model) => $$"""
        namespace Nudges.Kafka {

            [global::System.Text.Json.Serialization.JsonSourceGenerationOptions(PropertyNamingPolicy = global::System.Text.Json.Serialization.JsonKnownNamingPolicy.CamelCase)]
            [global::System.Text.Json.Serialization.JsonSerializable(typeof({{model.ModelNamespace}}.{{model.ModelTypeName}}))]
            internal sealed partial class {{model.ModelTypeName}}SerializerContext : global::System.Text.Json.Serialization.JsonSerializerContext { }

            [global::System.Text.Json.Serialization.JsonSourceGenerationOptions(PropertyNamingPolicy = global::System.Text.Json.Serialization.JsonKnownNamingPolicy.CamelCase)]
            [global::System.Text.Json.Serialization.JsonSerializable(typeof({{model.KeyNamespace}}.{{model.KeyTypeName}}))]
            internal sealed partial class {{model.KeyTypeName}}SerializerContext : global::System.Text.Json.Serialization.JsonSerializerContext { }
        }
        """;

    private static bool IsSyntaxTargetForGeneration(SyntaxNode node) {
        if (node is ClassDeclarationSyntax classDeclaration) {
            return classDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => attr.Name.ToString().Contains("EventModelAttribute")
                || attr.Name.ToString().Contains("EventModel"));
        }
        if (node is RecordDeclarationSyntax recordDeclaration) {
            return recordDeclaration.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => attr.Name.ToString().Contains("EventModelAttribute")
                || attr.Name.ToString().Contains("EventModel"));
        }
        return false;
    }

    private static Model GenerateSyntaxModels(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken = default) {
        var attributeData = context.Attributes.First();
        var keyType = (INamedTypeSymbol)attributeData.ConstructorArguments[0].Value;

        return new Model(
            modelNamespace: context.TargetSymbol.ContainingNamespace?.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(
                    SymbolDisplayGlobalNamespaceStyle.Included)),
            keyNamespace: keyType.ContainingNamespace?.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(
                    SymbolDisplayGlobalNamespaceStyle.Included)),
            modelTypeName: context.TargetSymbol.Name,
            keyTypeName: keyType.Name);
    }

    private class Model(string modelNamespace, string keyNamespace, string modelTypeName, string keyTypeName) {
        public string ModelNamespace { get; } = modelNamespace;
        public string KeyNamespace { get; } = keyNamespace;
        public string ModelTypeName { get; } = modelTypeName;
        public string KeyTypeName { get; } = keyTypeName;
    }
}

