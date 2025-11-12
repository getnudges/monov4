using Confluent.Kafka;
using KafkaConsumer;
using KafkaConsumer.Middleware;
using KafkaConsumer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using StrawberryShake;
using Nudges.Kafka.Middleware;
using Nudges.Kafka.Events;

namespace Nudges.Kafka.Tests.Integration;

public class PlanSubscriptionMiddlewareTests : KafkaTestBase {
    private readonly IServiceProvider _serviceProvider;
    private readonly Mock<INudgesClient> _mockNudgesClient;
    private readonly Mock<IForeignProductService> _mockForeignProductService;
    private readonly Mock<KafkaMessageProducer<NotificationKey, NotificationEvent>> _mockNotificationProducer;
    private readonly Mock<KafkaMessageProducer<ClientKey, ClientEvent>> _mockClientEventProducer;

    public PlanSubscriptionMiddlewareTests() {
        var services = new ServiceCollection();
        _mockNudgesClient = new Mock<INudgesClient>();
        _mockForeignProductService = new Mock<IForeignProductService>();
        _mockNotificationProducer = new Mock<KafkaMessageProducer<NotificationKey, NotificationEvent>>();
        _mockClientEventProducer = new Mock<KafkaMessageProducer<ClientKey, ClientEvent>>();

        services.AddSingleton(_mockNudgesClient.Object);
        services.AddSingleton(_mockForeignProductService.Object);
        services.AddSingleton(_mockNotificationProducer.Object);
        services.AddSingleton(_mockClientEventProducer.Object);
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact(Skip = "Docker mess")]
    public async Task PlanSubscriptionMiddlewareHandlesPlanSubscriptionCreatedEvent() {
        // Arrange
        var middleware = new PlanSubscriptionEventMiddleware(
            _serviceProvider.GetRequiredService<ILogger<PlanSubscriptionEventMiddleware>>(),
            () => _mockNudgesClient.Object,
            _mockForeignProductService.Object,
            _mockNotificationProducer.Object,
            _mockClientEventProducer.Object
        );

        var producer = new PlanSubscriptionEventProducer(
            Topics.PlanSubscriptions,
            new ProducerConfig {
                BootstrapServers = BootstrapServers,
                AllowAutoCreateTopics = true
            });
        var processor = KafkaMessageProcessorBuilder
            .For<PlanSubscriptionKey, PlanSubscriptionEvent>(Topics.PlanSubscriptions, BootstrapServers)
            .Use(middleware)
            .Build();

        var planSubscriptionId = Guid.NewGuid();
        var key = PlanSubscriptionKey.PlanSubscriptionCreated(planSubscriptionId);
        var @event = new PlanSubscriptionEvent();
        var mockPlanSubscriptionOpResult = new Mock<IOperationResult<IGetPlanSubscriptionByIdResult>>();
        var mockPlanSubscriptionResult = new Mock<IGetPlanSubscriptionByIdResult>();
        mockPlanSubscriptionResult.Setup(x => x.PlanSubscriptionById).Returns(Mock.Of<IGetPlanSubscriptionById_PlanSubscriptionById>());
        mockPlanSubscriptionOpResult.Setup(x => x.Errors).Returns([]);
        mockPlanSubscriptionOpResult.Setup(x => x.Data).Returns(mockPlanSubscriptionResult.Object);
        var mockUpdateClientOpResult = new Mock<IOperationResult<IUpdateClientResult>>();
        var mockUpdateClientResult = new Mock<IUpdateClientResult>();
        mockUpdateClientResult.Setup(x => x.UpdateClient).Returns(Mock.Of<IUpdateClient_UpdateClient>());
        mockUpdateClientOpResult.Setup(x => x.Errors).Returns([]);
        mockUpdateClientOpResult.Setup(x => x.Data).Returns(mockUpdateClientResult.Object);

        _mockNudgesClient
            .Setup(x => x.GetPlanSubscriptionById.ExecuteAsync(planSubscriptionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPlanSubscriptionOpResult.Object);

        _mockNudgesClient
            .Setup(x => x.UpdateClient.ExecuteAsync(It.IsAny<UpdateClientInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUpdateClientOpResult.Object);

        // Act
        await producer.Produce(key, @event, CancellationToken.None);
        var cts = new CancellationTokenSource();
        await await Task.WhenAny(
            Task.Delay(500),
            Task.Delay(550));
        await Task.WhenAny(
            Task.Delay(2000),
            Task.Delay(2500));
        await Task.WhenAny(
            Task.Delay(500),
            processor.ProcessMessages(cts.Token));
        //await Assert.ThrowsAsync<OperationCanceledException>(async () => await Task.WhenAny(
        //    processor.ProcessMessages(cts.Token),
        //    Task.Delay(500).ContinueWith((t) => cts.Cancel())));

        // Assert
        _mockNudgesClient.Verify(x => x.GetPlanSubscriptionById.ExecuteAsync(planSubscriptionId, It.IsAny<CancellationToken>()), Times.Once);
        _mockNudgesClient.Verify(x => x.UpdateClient.ExecuteAsync(It.IsAny<UpdateClientInput>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockNotificationProducer.Verify(x => x.Produce(It.IsAny<NotificationKey>(), It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
