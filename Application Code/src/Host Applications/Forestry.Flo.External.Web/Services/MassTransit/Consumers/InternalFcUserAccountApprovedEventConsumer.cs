using Forestry.Flo.Services.Common.MassTransit.Messages;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Services.MassTransit.Consumers;

/// <summary>
/// An <see cref="IConsumer"/> implementation for consuming <see cref="InternalFcUserAccountApprovedEvent"/>.
/// </summary>
public class InternalFcUserAccountApprovedEventConsumer : IConsumer<InternalFcUserAccountApprovedEvent>
{
    private readonly CreateExternalUserProfileForInternalFcUserUseCase _useCase;

    public InternalFcUserAccountApprovedEventConsumer(
        [FromServices] CreateExternalUserProfileForInternalFcUserUseCase useCase)
    {
        _useCase = useCase;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<InternalFcUserAccountApprovedEvent> context)
        => await _useCase
            .ProcessAsync(context.Message, context.CancellationToken);
}
