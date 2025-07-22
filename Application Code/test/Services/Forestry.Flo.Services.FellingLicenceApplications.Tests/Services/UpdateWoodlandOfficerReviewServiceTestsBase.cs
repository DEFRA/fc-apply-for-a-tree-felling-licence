using System;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public abstract class UpdateWoodlandOfficerReviewServiceTestsBase
{
    protected readonly Mock<IFellingLicenceApplicationInternalRepository> FellingLicenceApplicationRepository = new();
    protected readonly Mock<IUnitOfWork> UnitOfWork = new();
    protected readonly Mock<IClock> Clock = new();
    protected readonly Mock<IViewCaseNotesService> MockCaseNotesService = new();
    protected readonly Mock<IAddDocumentService> MockAddDocumentService = new();
    protected readonly Mock<IOptions<DocumentVisibilityOptions>> VisibilityOptions = new();
    protected readonly Instant Now = Instant.FromDateTimeUtc(DateTime.UtcNow);

    protected UpdateWoodlandOfficerReviewService CreateSut()
    {
        FellingLicenceApplicationRepository.Reset();
        UnitOfWork.Reset();
        Clock.Reset();
        MockCaseNotesService.Reset();
        MockAddDocumentService.Reset();

        FellingLicenceApplicationRepository
            .SetupGet(x => x.UnitOfWork)
            .Returns(UnitOfWork.Object);
        Clock.Setup(x => x.GetCurrentInstant()).Returns(Now);

        var configuration = new WoodlandOfficerReviewOptions
        {
            PublicRegisterPeriod = TimeSpan.FromDays(30)
        };

        VisibilityOptions.Setup(x => x.Value).Returns(new DocumentVisibilityOptions
        {
            ApplicationDocument = new DocumentVisibilityOptions.VisibilityOptions
            {
                VisibleToApplicant = true,
                VisibleToConsultees = true
            },
            ExternalLisConstraintReport = new DocumentVisibilityOptions.VisibilityOptions
            {
                VisibleToApplicant = true,
                VisibleToConsultees = true
            },
            FcLisConstraintReport = new DocumentVisibilityOptions.VisibilityOptions
            {
                VisibleToApplicant = false,
                VisibleToConsultees = false
            },
            SiteVisitAttachment = new DocumentVisibilityOptions.VisibilityOptions
            {
                VisibleToApplicant = true,
                VisibleToConsultees = true
            }
        });

        return new UpdateWoodlandOfficerReviewService(
            FellingLicenceApplicationRepository.Object,
            Clock.Object,
            new OptionsWrapper<WoodlandOfficerReviewOptions>(configuration),
            MockCaseNotesService.Object,
            MockAddDocumentService.Object,
            new NullLogger<UpdateWoodlandOfficerReviewService>(),
            VisibilityOptions.Object);
    }
}