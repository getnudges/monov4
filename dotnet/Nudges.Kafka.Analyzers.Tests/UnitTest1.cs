using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Nudges.Kafka.Analyzers.Tests;

public class UnitTest1 {
    public class EnumGeneratorSnapshotTests {
        [Fact]
        public Task GeneratesProducersWithoutError() {
            var sourceCode = @"
            using Nudges.Kafka;
            namespace TestNamespace
            {
                [EventModel(typeof(TestClassKey))]
                public class TestClass
                {
                    public void TestMethod() {}
                }

                public class TestClassKey
                {
                    public void TestMethod() {}
                }
            }";
            // Pass the source code to our helper and snapshot test the output
            return TestHelper.Verify(sourceCode);
        }

        [Fact]
        public void Test() {
            var sourceCode = @"
            using Nudges.Kafka;
            namespace TestNamespace
            {
                [EventModel(typeof(TestClassKey))]
                public class TestClass
                {
                    public void TestMethod() {}
                }

                public class TestClassKey
                {
                    public void TestMethod() {}
                }
            }";
            var compilation = CSharpCompilation.Create("TestProject",
                [CSharpSyntaxTree.ParseText(sourceCode)],
                [],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new ModelProducerGenerator();
            var sourceGenerator = generator.AsSourceGenerator();

            // trackIncrementalGeneratorSteps allows to report info about each step of the generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                generators: [sourceGenerator],
                driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));

            // Run the generator
            driver = driver.RunGenerators(compilation);

            // Update the compilation and rerun the generator
            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("// dummy"));
            driver = driver.RunGenerators(compilation);

            // Assert the driver doesn't recompute the output
            var result = driver.GetRunResult().Results.Single();
            var allOutputs = result.TrackedOutputSteps.SelectMany(outputStep => outputStep.Value).SelectMany(output => output.Outputs);
            Assert.Collection(allOutputs, output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));

            // Assert the driver use the cached result from AssemblyName and Syntax
            var assemblyNameOutputs = result.TrackedSteps["AssemblyName"].Single().Outputs;
            Assert.Collection(assemblyNameOutputs, output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));

            var syntaxOutputs = result.TrackedSteps["Syntax"].Single().Outputs;
            Assert.Collection(syntaxOutputs, output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
        }

        [Fact]
        public void TestMyIncrementalGenerator() {
            // Arrange: Create a sample source code to test the generator
            var sourceCode = @"
            namespace TestNamespace {
                public record DemoKey(string EventType, string EventKey) {
                    public override string ToString() => $""{EventType}:{EventKey}"";
                }

                [EventModel(typeof(DemoKey))]
                public record DemoEvent(params int[] PriceTierIds) { }
            }";

            // Create a Roslyn Compilation
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees: [syntaxTree],
                references: [
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Confluent.Kafka.IProducer<,>).Assembly.Location),
                ]);

            // Instantiate the generator to test
            var generator = new ModelProducerGenerator();

            // Create the generator driver
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

            // Act: Run the source generator on the compilation
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var diagnostics);

            // Assert: Check the results (diagnostics or generated code)
            var generatedTrees = updatedCompilation.SyntaxTrees;
            var generatedCode = string.Join("\n\r", generatedTrees.Select(s => s.ToString()));

            // Verify that generated code contains expected content
            Assert.Contains("ExpectedGeneratedCodePart", generatedCode);
        }

    }
}
