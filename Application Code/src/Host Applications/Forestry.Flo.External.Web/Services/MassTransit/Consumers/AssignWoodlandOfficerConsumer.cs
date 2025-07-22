using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Exceptions;
using Forestry.Flo.External.Web.Services.MassTransit.Messages;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Services.MassTransit.Consumers;

/// <summary>
/// An <see cref="IConsumer"/> implementation for consuming <see cref="AssignWoodlandOfficerMessage"/>.
/// </summary>
public class AssignWoodlandOfficerConsumer : IConsumer<AssignWoodlandOfficerMessage>
{
    private readonly AssignWoodlandOfficerAsyncUseCase _useCase;

    public AssignWoodlandOfficerConsumer(
        [FromServices] AssignWoodlandOfficerAsyncUseCase useCase)
    {
        _useCase = useCase;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AssignWoodlandOfficerMessage> context) => 
        await _useCase.AssignWoodlandOfficerAsync(
                context.Message, 
                context.CancellationToken)
            .OnFailure(error => throw new MessageConsumptionException(error));
}