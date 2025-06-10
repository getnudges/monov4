
namespace UnAd.Kafka.Analyzers.Tests;

public static class ModuleInitializer {
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void Init() => VerifySourceGenerators.Initialize();
}
