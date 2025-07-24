using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.FellingLicenceApplications.Configuration;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TinyCsvParser.Model;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class UpdateWoodlandOfficerReviewHandleConfirmedFellingAndRestockingChangesTests : UpdateWoodlandOfficerReviewServiceTestsBase
{
    private readonly FellingLicenceApplicationsContext _context;
    private readonly IFellingLicenceApplicationInternalRepository _internalRepository;

    public UpdateWoodlandOfficerReviewHandleConfirmedFellingAndRestockingChangesTests()
    {
        _context = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();
        _internalRepository = new InternalUserContextFlaRepository(_context);
    }

    [Theory, AutoMoqData]
    public async Task ShouldUpdateWoodlandOfficerReviewEntity(
        FellingLicenceApplication application,
        Guid performingUserId)
    {
        //arrange

        var sut = CreateSut();

        application.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete = false;

        _context.FellingLicenceApplications.Add(application);
        await _internalRepository.UnitOfWork.SaveEntitiesAsync();

        //act

        var result =
            await sut.HandleConfirmedFellingAndRestockingChangesAsync(application.Id, performingUserId, true,
                CancellationToken.None);

        //assert result is success

        result.IsSuccess.Should().BeTrue();

        var updatedFlaMaybe = await _internalRepository.GetAsync(application.Id, CancellationToken.None);

        var updatedFla = updatedFlaMaybe.Value;

        //assert database values have been updated

        updatedFla.WoodlandOfficerReview!.LastUpdatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
        updatedFla.WoodlandOfficerReview.LastUpdatedById.Should().Be(performingUserId);
        updatedFla.WoodlandOfficerReview.ConfirmedFellingAndRestockingComplete.Should().BeTrue();
    }

    private new UpdateWoodlandOfficerReviewService CreateSut()
    {
        FellingLicenceApplicationRepository.Reset();
        UnitOfWork.Reset();
        Clock.Reset();
        MockCaseNotesService.Reset();
        MockAddDocumentService.Reset();
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
            _internalRepository,
            Clock.Object,
            new OptionsWrapper<WoodlandOfficerReviewOptions>(configuration),
            MockCaseNotesService.Object,
            MockAddDocumentService.Object,
            new NullLogger<UpdateWoodlandOfficerReviewService>(),
            VisibilityOptions.Object);
    }
}