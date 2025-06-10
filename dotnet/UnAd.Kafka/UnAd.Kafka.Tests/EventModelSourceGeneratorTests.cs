using System.Diagnostics;
using System.Reflection;
//using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
//using Microsoft.CodeAnalysis.Text;
using UnAd.Kafka.Generators;
//using VerifyCS = UnAd.Kafka.Tests.CSharpSourceGeneratorVerifier<UnAd.Kafka.Generators.EventModelSourceGenerator>;

namespace UnAd.Kafka.Tests;

public class EventModelSourceGeneratorTests {
    [Fact]
    public void EventModelSourceGenerator1() {
        // Create the 'input' compilation that the generator will act on
        var inputCompilation = CreateCompilation("""
            namespace MyApp;
            using UnAd.Kafka.Abstractions;

            [EventModel(typeof(ClientKey))]
            public class ClientEvent { }

            public class ClientKey { }
            """);

        // directly create an instance of the generator
        // (Note: in the compiler this is loaded from an assembly, and created via reflection at runtime)
        var generator = new EventModelSourceGenerator();

        // Create the driver that will control the generation, passing in our generator
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the generation pass
        // (Note: the generator driver itself is immutable, and all calls return an updated version of the driver that you should use for subsequent calls)
        driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

        // We can now assert things about the resulting compilation:
        Debug.Assert(diagnostics.IsEmpty); // there were no diagnostics created by the generators
        Debug.Assert(outputCompilation.GetDiagnostics().IsEmpty); // verify the compilation with the added source has no diagnostics
    }

    private static CSharpCompilation CreateCompilation(string source)
        => CSharpCompilation.Create("compilation",
            [CSharpSyntaxTree.ParseText(source)],
            [MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

//    [Fact]
//    public async Task EventModelSourceGenerator2() {
//        var code = @"
//namespace UnAd.Kafka;

//[EventModel(typeof(ClientKey))]
//public class ClientEvent { }

//public class ClientKey { }

//";
//        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("UnAd.Kafka.Tests.Matches.ClientEventSerializer.txt");
//        using var reader = new StreamReader(stream);
//        var expected = reader.ReadToEnd();
//        await new VerifyCS.Test {
//            TestState =
//            {
//                Sources = { code },
//                GeneratedSources =
//                {
//                    (typeof(EventModelSourceGenerator), $"ClientEventSerializer.g.cs", SourceText.From(expected, Encoding.UTF8, SourceHashAlgorithm.Sha256)),
//                },
//            },
//        }.RunAsync();
//    }
}

