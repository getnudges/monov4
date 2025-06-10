using Confluent.Kafka;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Nudges.Data.Products;
using Nudges.Data.Products.Models;
using Nudges.Kafka;

namespace ProductApi.Tests.Unit;

public sealed class MutationTests {

    private static ProductDbContext CreateInMemoryDbContext() {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProductDbContext(options);
    }

    private static ProductDbContext CreateSqliteDbContext(bool connect = true) {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseSqlite("Filename=file::memory:")
            .Options;
        var context = new ProductDbContext(options);
        if (connect) {
            context.Database.OpenConnection();
        }
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task CreatePlanShouldReturnPlanWhenSuccessful() {
        // Arrange
        var mockContext = CreateInMemoryDbContext();
        var mockProductProducer = new Mock<KafkaMessageProducer<PlanKey, PlanEvent>>();
        var mockIdSerializer = new Mock<INodeIdSerializer>();
        var mockLogger = new Mock<ILogger<Mutation>>();
        var mutation = new Mutation(mockLogger.Object);
        var input = new CreatePlanInput("Test Plan", "Description", "IconUrl", null, null, true);
        var cancellationToken = CancellationToken.None;

        var plan = new Plan { Id = 1, Name = "Test Plan" };
        mockIdSerializer.Setup(s => s.Format(nameof(Plan), plan.Id)).Returns("serialized-id");
        mockProductProducer.Setup(p => p.Produce(It.IsAny<PlanKey>(), It.IsAny<PlanEvent>(), cancellationToken))
            .Returns(Task.FromResult(Mock.Of<DeliveryResult<PlanKey, PlanEvent>>()));

        // Act
        var result = await mutation.CreatePlan(mockContext, mockProductProducer.Object, mockIdSerializer.Object, input, cancellationToken);

        // Assert
        Assert.Equal(plan, result);
    }

    [Fact]
    public async Task CreatePlanShouldReturnPlanCreationErrorWhenProduceExceptionOccurs() {
        // Arrange
        var mockContext = CreateInMemoryDbContext();
        var mockProductProducer = new Mock<KafkaMessageProducer<PlanKey, PlanEvent>>();
        var mockIdSerializer = new Mock<INodeIdSerializer>();
        var mockLogger = new Mock<ILogger<Mutation>>();
        var mutation = new Mutation(mockLogger.Object);
        var input = new CreatePlanInput("Test Plan", "Description", "IconUrl", null, null, true);
        var cancellationToken = CancellationToken.None;

        var plan = new Plan { Id = 1, Name = "Test Plan" };
        mockIdSerializer.Setup(s => s.Format(nameof(Plan), plan.Id)).Returns("serialized-id");
        mockProductProducer.Setup(p => p.Produce(It.IsAny<PlanKey>(), It.IsAny<PlanEvent>(), cancellationToken))
            .ThrowsAsync(new Exception("test"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await mutation.CreatePlan(mockContext, mockProductProducer.Object, mockIdSerializer.Object, input, cancellationToken));
    }

    [Fact]
    public async Task CreatePlanShouldReturnKafkaProduceErrorWhenKafkaExceptionOccurs() {
        // Arrange
        var mockContext = CreateInMemoryDbContext();
        var mockProductProducer = new Mock<KafkaMessageProducer<PlanKey, PlanEvent>>();
        var mockIdSerializer = new Mock<INodeIdSerializer>();
        var mockLogger = new Mock<ILogger<Mutation>>();
        var mutation = new Mutation(mockLogger.Object);
        var input = new CreatePlanInput("Test Plan", "Description", "IconUrl", null, null, true);
        var cancellationToken = CancellationToken.None;

        var plan = new Plan { Id = 1, Name = "Test Plan" };
        mockIdSerializer.Setup(s => s.Format(nameof(Plan), plan.Id)).Returns("serialized-id");
        mockProductProducer.Setup(p => p.Produce(It.IsAny<PlanKey>(), It.IsAny<PlanEvent>(), cancellationToken))
            .ThrowsAsync(new KafkaException(new Error(ErrorCode.BrokerNotAvailable)));

        // Act & Assert
        await Assert.ThrowsAsync<KafkaException>(async () => await mutation.CreatePlan(mockContext, mockProductProducer.Object, mockIdSerializer.Object, input, cancellationToken));
    }

    [Fact]
    public async Task CreatePlanShouldRollbackTransactionWhenKafkaExceptionOccurs() {
        // Arrange
        var mockContext = CreateSqliteDbContext();
        var mockProductProducer = new Mock<KafkaMessageProducer<PlanKey, PlanEvent>>();
        var mockIdSerializer = new Mock<INodeIdSerializer>();
        var mockLogger = new Mock<ILogger<Mutation>>();
        var mutation = new Mutation(mockLogger.Object);
        var input = new CreatePlanInput("Test Plan", "Description", "IconUrl", null, null, true);
        var cancellationToken = CancellationToken.None;

        var plan = new Plan { Id = 1, Name = "Test Plan" };
        mockIdSerializer.Setup(s => s.Format(nameof(Plan), plan.Id)).Returns("serialized-id");
        mockProductProducer.Setup(p => p.Produce(It.IsAny<PlanKey>(), It.IsAny<PlanEvent>(), cancellationToken))
            .ThrowsAsync(new KafkaException(new Error(ErrorCode.BrokerNotAvailable)));

        // Act
        var result = await mutation.CreatePlan(mockContext, mockProductProducer.Object, mockIdSerializer.Object, input, cancellationToken);

        // Assert
        Assert.Equal(0, await mockContext.Plans.CountAsync());
    }
}
