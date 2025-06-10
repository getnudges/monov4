using Testcontainers.Kafka;

namespace UnAd.Kafka.Tests.Integration;

public class KafkaTestBase : IAsyncLifetime {
    public KafkaContainer Kafka { get; private set; }

    public string BootstrapServers => Kafka.GetBootstrapAddress();

    public KafkaTestBase() {
        Kafka = new KafkaBuilder()
            .Build();
    }

    public async Task InitializeAsync() {
        await Kafka.StartAsync();
    }

    public async Task DisposeAsync() {
        await Kafka.StopAsync();
    }
}
