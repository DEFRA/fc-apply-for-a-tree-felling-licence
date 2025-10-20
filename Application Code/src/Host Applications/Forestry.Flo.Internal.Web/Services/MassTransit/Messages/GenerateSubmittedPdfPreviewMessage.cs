using Forestry.Flo.Internal.Web.Services.MassTransit.Consumers;

namespace Forestry.Flo.Internal.Web.Services.MassTransit.Messages;

/// <summary>
/// A message class containing the relevant properties for generating a PDF preview in a <see cref="GenerateSubmittedPdfPreviewConsumer"/>
/// </summary>
public class GenerateSubmittedPdfPreviewMessage
{
    /// <summary>
    /// Creates a new instance of a <see cref="GenerateSubmittedPdfPreviewMessage"/>.
    /// </summary>
    /// <param name="internalUserId">The identifier for the internal user generating the preview.</param>
    /// <param name="applicationId">The identifier for the application to generate a PDF preview for.</param>
    public GenerateSubmittedPdfPreviewMessage(Guid internalUserId, Guid applicationId)
    {
        InternalUserId = internalUserId;
        ApplicationId = applicationId;
    }

    /// <summary>
    /// Gets and inits an identifier for the internal user generating the preview.
    /// </summary>
    public Guid InternalUserId { get; init; }

    /// <summary>
    /// Gets and inits an identifier for the application used for generating the preview.
    /// </summary>
    public Guid ApplicationId { get; init; }
}