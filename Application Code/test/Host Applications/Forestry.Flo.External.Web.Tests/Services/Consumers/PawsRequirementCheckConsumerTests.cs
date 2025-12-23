using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Exceptions;
using Forestry.Flo.External.Web.Services.Interfaces;
using Forestry.Flo.External.Web.Services.MassTransit.Consumers;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.External.Web.Tests.Services.Consumers;

public class PawsRequirementCheckConsumerTests
{
    private readonly Mock<ICheckForPawsRequirementUseCase> _mockUseCase = new();
    private readonly Mock<ConsumeContext<PawsRequirementCheckMessage>> _mockConsumeContext = new();

    [Theory, AutoData]
    public async Task ShouldCompleteIfUseCaseReturnsSuccess(PawsRequirementCheckMessage message)
    {
        var sut = CreateSut();

        _mockUseCase.Setup(x => x.CheckAndUpdateApplicationForPaws(
                It.IsAny<PawsRequirementCheckMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _mockConsumeContext.SetupGet(x => x.Message).Returns(message);
        _mockConsumeContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await sut.Consume(_mockConsumeContext.Object);

        _mockUseCase.Verify(x => x.CheckAndUpdateApplicationForPaws(
            It.Is<PawsRequirementCheckMessage>(m => m == message),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task ShouldThrowIfUseCaseReturnsFailure(PawsRequirementCheckMessage message)
    {
        var sut = CreateSut();
        
        _mockUseCase.Setup(x => x.CheckAndUpdateApplicationForPaws(
                It.IsAny<PawsRequirementCheckMessage>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("error"));
        
        _mockConsumeContext.SetupGet(x => x.Message).Returns(message);
        _mockConsumeContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);
        
        await Assert.ThrowsAsync<MessageConsumptionException>(() => sut.Consume(_mockConsumeContext.Object));

        _mockUseCase.Verify(x => x.CheckAndUpdateApplicationForPaws(
            It.Is<PawsRequirementCheckMessage>(m => m == message),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private PawsRequirementCheckConsumer CreateSut()
    {
        _mockUseCase.Reset();
        _mockConsumeContext.Reset();

        return new PawsRequirementCheckConsumer(
            _mockUseCase.Object,
            new NullLogger<PawsRequirementCheckConsumer>());
    }
}