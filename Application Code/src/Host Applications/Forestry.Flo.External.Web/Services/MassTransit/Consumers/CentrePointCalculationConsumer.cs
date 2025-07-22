using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Exceptions;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Services.MassTransit.Consumers;

/// <summary>
/// An <see cref="IConsumer"/> implementation for consuming <see cref="CentrePointCalculationMessage"/>.
/// </summary>
public class CentrePointCalculationConsumer : IConsumer<CentrePointCalculationMessage>
{
    private readonly CalculateCentrePointUseCase _useCase;

    public CentrePointCalculationConsumer(
        [FromServices] CalculateCentrePointUseCase useCase)
    {
        _useCase = useCase;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<CentrePointCalculationMessage> context) => 
        await _useCase.CalculateCentrePointAsync(
                context.Message, 
                context.CancellationToken)
            .OnFailure(error => throw new MessageConsumptionException(error));
}