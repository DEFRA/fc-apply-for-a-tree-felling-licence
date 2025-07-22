using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using NodaTime.Extensions;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services.AdminOfficerReview;

public abstract class UpdateAdminOfficerReviewServiceTestsBase
{
    protected readonly IUpdateAdminOfficerReviewService Sut;
    protected readonly FellingLicenceApplicationsContext FellingLicenceApplicationsContext;
    protected readonly IFixture Fixture;
    protected readonly Mock<IClock> MockClock = new();

    protected UpdateAdminOfficerReviewServiceTestsBase()
    {
        Fixture = new Fixture().Customize(new CompositeCustomization(
            new AutoMoqCustomization(),
            new SupportMutableValueTypesCustomization()));

        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        Fixture.Customize<DateOnly>(composer => composer.FromFactory<DateTime>(DateOnly.FromDateTime));

        FellingLicenceApplicationsContext = TestFellingLicenceApplicationsDatabaseFactory.CreateDefaultTestContext();

        MockClock.Setup(x => x.GetCurrentInstant()).Returns(DateTime.UtcNow.ToInstant());

        Sut = new UpdateAdminOfficerReviewService(new InternalUserContextFlaRepository(FellingLicenceApplicationsContext), new NullLogger<UpdateAdminOfficerReviewService>(), MockClock.Object);
    }

    protected async Task<FellingLicenceApplication> CreateAndSaveAdminOfficerReviewApplicationAsync(
        FellingLicenceStatus currentStatus, 
        Entities.AdminOfficerReview? adminOfficerReview = null, 
        Guid? performingUserId = null, 
        Guid? woodlandOfficerId = null,
        bool? agentAuthorityCheckPassed = null,
        bool? mappingCheckPassed = null)
    {
        var application = Fixture.Create<FellingLicenceApplication>();
        
        application.AdminOfficerReview = adminOfficerReview ?? new Entities.AdminOfficerReview 
        { 
            AgentAuthorityFormChecked = false,
            MappingChecked = false,
            ConstraintsChecked = false,
            AdminOfficerReviewComplete = false,
            AgentAuthorityCheckPassed = agentAuthorityCheckPassed,
            MappingCheckPassed = mappingCheckPassed
        };

        application.StatusHistories = new List<StatusHistory>
        {
            new()
            {
                Created = DateTime.UtcNow,
                CreatedById = Guid.NewGuid(),
                Status = currentStatus
            }
        };

        application.AssigneeHistories = new List<AssigneeHistory>();

        if (performingUserId is not null)
        {
            application.AssigneeHistories.Add(new AssigneeHistory
            {
                AssignedUserId = performingUserId.Value,
                TimestampAssigned = DateTime.UtcNow,
                TimestampUnassigned = null,
                Role = AssignedUserRole.AdminOfficer
            });
        }

        if (woodlandOfficerId is not null)
        {
            application.AssigneeHistories.Add(new AssigneeHistory
            {
                TimestampAssigned = DateTime.UtcNow,
                AssignedUserId = woodlandOfficerId.Value,
                Role = AssignedUserRole.WoodlandOfficer
            });
        }

        FellingLicenceApplicationsContext.FellingLicenceApplications.Add(application);
        await FellingLicenceApplicationsContext.SaveEntitiesAsync().ConfigureAwait(false);

        return application;
    }


}