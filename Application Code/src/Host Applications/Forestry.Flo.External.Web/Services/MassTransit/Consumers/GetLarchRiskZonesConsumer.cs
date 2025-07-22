using Forestry.Flo.External.Web.Services.MassTransit.Messages;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Services.MassTransit.Consumers;

/// <summary>
/// An <see cref="IConsumer"/> implementation for consuming <see cref="GetLarchRiskZonesMessage"/>.
/// </summary>
public class GetLarchRiskZonesConsumer : IConsumer<GetLarchRiskZonesMessage>
{
    private readonly CreateFellingLicenceApplicationUseCase _useCase;

    public GetLarchRiskZonesConsumer(
        [FromServices] CreateFellingLicenceApplicationUseCase useCase)
    {
        _useCase = useCase;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetLarchRiskZonesMessage> context)
    {
        await _useCase.UpdateSubmittedFlaPropertyCompartmentZonesAsync(
            context.Message.CompartmentIds,
            context.Message.UserId,
            context.Message.ApplicationId,
            context.CancellationToken);
    }
}