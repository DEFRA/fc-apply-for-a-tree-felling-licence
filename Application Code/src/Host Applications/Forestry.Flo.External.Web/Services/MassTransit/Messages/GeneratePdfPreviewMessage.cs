using Forestry.Flo.External.Web.Services.MassTransit.Consumers;

namespace Forestry.Flo.External.Web.Services.MassTransit.Messages;

/// <summary>
/// A message class containing the relevant properties for generating a PDF preview in a <see cref="GeneratePdfPreviewConsumer"/>
/// </summary>
public class GeneratePdfPreviewMessage
{
    /// <summary>
    /// Creates a new instance of a <see cref="GeneratePdfPreviewMessage"/>.
    /// </summary>
    /// <param name="externalApplicantId">The identifier for the external applicant generating the preview.</param>
    /// <param name="applicationId">The identifier for the application to generate a PDF preview for.</param>
    public GeneratePdfPreviewMessage(Guid externalApplicantId, Guid applicationId)
    {
        ExternalApplicantId = externalApplicantId;
        ApplicationId = applicationId;
    }

    /// <summary>
    /// Gets and inits an identifier for the external applicant creating generating the preview.
    /// </summary>
    public Guid ExternalApplicantId { get; init; }

    /// <summary>
    /// Gets and inits an identifier for the application used for generating the preview.
    /// </summary>
    public Guid ApplicationId { get; init; }
}