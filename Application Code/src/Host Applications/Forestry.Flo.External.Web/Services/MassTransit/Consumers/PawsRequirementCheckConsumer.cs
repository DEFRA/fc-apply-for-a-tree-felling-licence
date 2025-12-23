using Ardalis.GuardClauses;
using Forestry.Flo.External.Web.Exceptions;
using Forestry.Flo.External.Web.Services.Interfaces;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.External.Web.Services.MassTransit.Consumers;

/// <summary>
/// An <see cref="IConsumer{TMessage}"/> implementation for <see cref="PawsRequirementCheckMessage"/> messages.
/// </summary>
public class PawsRequirementCheckConsumer : IConsumer<PawsRequirementCheckMessage>
{
    private readonly ILogger<PawsRequirementCheckConsumer> _logger;
    private readonly ICheckForPawsRequirementUseCase _useCase;

    /// <summary>
    /// Creates an instance of a <see cref="PawsRequirementCheckConsumer"/>.
    /// </summary>
    /// <param name="useCase">A usecase to process consumed messages.</param>
    /// <param name="logger">A logging instance.</param>
    public PawsRequirementCheckConsumer(
        [FromServices] ICheckForPawsRequirementUseCase useCase,
        [FromServices] ILogger<PawsRequirementCheckConsumer> logger)
    {
        _logger = logger ?? new NullLogger<PawsRequirementCheckConsumer>();
        _useCase = Guard.Against.Null(useCase);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<PawsRequirementCheckMessage> context)
    {
        _logger.LogInformation("Consuming PawsRequirementCheckMessage for ApplicationId: {ApplicationId}", context.Message.ApplicationId);

        var result = await _useCase.CheckAndUpdateApplicationForPaws(context.Message, context.CancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to process PawsRequirementCheckMessage for ApplicationId: {ApplicationId}. Error: {Error}", context.Message.ApplicationId, result.Error);
            throw new MessageConsumptionException(result.Error);
        }
    }
}