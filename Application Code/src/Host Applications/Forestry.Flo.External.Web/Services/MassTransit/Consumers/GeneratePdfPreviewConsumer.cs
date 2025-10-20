using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Exceptions;
using Forestry.Flo.External.Web.Services.MassTransit.Messages;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Services.MassTransit.Consumers;

/// <summary>
/// An <see cref="IConsumer"/> implementation for consuming <see cref="GeneratePdfPreviewMessage"/>.
/// </summary>
public class GeneratePdfPreviewConsumer : IConsumer<GeneratePdfPreviewMessage>
{
    private readonly GeneratePdfApplicationUseCase _useCase;

    public GeneratePdfPreviewConsumer(
        [FromServices] GeneratePdfApplicationUseCase useCase)
    {
        _useCase = useCase;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GeneratePdfPreviewMessage> context) => 
        await _useCase.GeneratePreviewDocumentAsync(
                context.Message.ExternalApplicantId,
                context.Message.ApplicationId,
                context.CancellationToken)
            .OnFailure(error => throw new MessageConsumptionException(error));
}