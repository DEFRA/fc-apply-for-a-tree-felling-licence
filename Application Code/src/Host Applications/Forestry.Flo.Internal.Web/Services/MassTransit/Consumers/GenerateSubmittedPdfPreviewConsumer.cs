using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Internal.Web.Services.MassTransit.Messages;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Services.MassTransit.Consumers;

/// <summary>
/// An <see cref="IConsumer"/> implementation for consuming <see cref="GenerateSubmittedPdfPreviewMessage"/>.
/// </summary>
public class GenerateSubmittedPdfPreviewConsumer(
    [FromServices] IGeneratePdfApplicationUseCase useCase)
    : IConsumer<GenerateSubmittedPdfPreviewMessage>
{
    public const string QueueName = "generate-submitted-pdf-preview";

    /// <inheritdoc />
    /// <remarks>
    /// This should not be used for approved applications.
    /// </remarks>
    public async Task Consume(ConsumeContext<GenerateSubmittedPdfPreviewMessage> context) =>
        await useCase.GeneratePdfApplicationAsync(
            context.Message.InternalUserId,
            context.Message.ApplicationId,
            context.CancellationToken);
}