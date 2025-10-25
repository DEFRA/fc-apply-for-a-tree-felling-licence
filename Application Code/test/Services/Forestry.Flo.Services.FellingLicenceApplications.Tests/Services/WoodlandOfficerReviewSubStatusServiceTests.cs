using System;
using System.Collections.Generic;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services.WoodlandOfficerReviewSubstatuses;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class WoodlandOfficerReviewSubStatusServiceTests
{
    private static FellingLicenceApplication CreateApplication(
        FellingLicenceStatus status = FellingLicenceStatus.WoodlandOfficerReview,
        WoodlandOfficerReview? woReview = null,
        PublicRegister? publicRegister = null)
    {
        return new FellingLicenceApplication
        {
            StatusHistories = [
                new StatusHistory
                {
                    Status = status
                }
            ],
            WoodlandOfficerReview = woReview,
            PublicRegister = publicRegister
        };
    }

    private static FellingLicenceApplication CreateApplicationWithStatus(FellingLicenceStatus status)
    {
        return new FellingLicenceApplication
        {
            StatusHistories =
            [
                new StatusHistory
                {
                    Status = status
                }
            ]
        };
    }

    [Fact]
    public void GetCurrentSubStatuses_ReturnsEmptySet_WhenNotInWoodlandOfficerReview()
    {
        // Arrange
        var specs = new ISubStatusSpecification[]
        {
            new AmendmentsWithApplicantSpecification(),
            new OnPublicRegisterSpecification()
        };
        var service = new WoodlandOfficerReviewSubStatusService(specs);
        var application = CreateApplicationWithStatus(FellingLicenceStatus.Submitted);

        // Act
        var result = service.GetCurrentSubStatuses(application);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetCurrentSubStatuses_ReturnsAmendmentsWithApplicant_WhenAmendmentReviewNotCompleted()
    {
        // Arrange
        var woReview = new WoodlandOfficerReview
        {
            FellingAndRestockingAmendmentReviews =
                [
                    new FellingAndRestockingAmendmentReview
                    {
                        AmendmentReviewCompleted = false
                    }
                ]
        };
        var application = CreateApplication(woReview: woReview);
        var specs = new ISubStatusSpecification[]
        {
            new AmendmentsWithApplicantSpecification(),
            new OnPublicRegisterSpecification()
        };
        var service = new WoodlandOfficerReviewSubStatusService(specs);

        // Act
        var result = service.GetCurrentSubStatuses(application);

        // Assert
        Assert.Contains(WoodlandOfficerReviewSubStatus.AmendmentsWithApplicant, result);
        Assert.DoesNotContain(WoodlandOfficerReviewSubStatus.OnPublicRegister, result);
    }

    [Fact]
    public void GetCurrentSubStatuses_ReturnsOnPublicRegister_WhenConsultationPublicationTimestampSetAndNotRemoved()
    {
        // Arrange
        var publicRegister = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow,
            ConsultationPublicRegisterRemovedTimestamp = null
        };
        var application = CreateApplication(publicRegister: publicRegister);
        var specs = new ISubStatusSpecification[]
        {
            new AmendmentsWithApplicantSpecification(),
            new OnPublicRegisterSpecification()
        };
        var service = new WoodlandOfficerReviewSubStatusService(specs);

        // Act
        var result = service.GetCurrentSubStatuses(application);

        // Assert
        Assert.Contains(WoodlandOfficerReviewSubStatus.OnPublicRegister, result);
    }

    [Fact]
    public void GetCurrentSubStatuses_ReturnsBothSubStatuses_WhenBothConditionsMet()
    {
        // Arrange
        var woReview = new WoodlandOfficerReview
        {
            FellingAndRestockingAmendmentReviews =
            [
                new FellingAndRestockingAmendmentReview(amendmentReviewCompleted: false)
            ]
        };
        var publicRegister = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = DateTime.UtcNow,
            ConsultationPublicRegisterRemovedTimestamp = null
        };
        var application = CreateApplication(woReview: woReview, publicRegister: publicRegister);
        var specs = new ISubStatusSpecification[]
        {
            new AmendmentsWithApplicantSpecification(),
            new OnPublicRegisterSpecification()
        };
        var service = new WoodlandOfficerReviewSubStatusService(specs);

        // Act
        var result = service.GetCurrentSubStatuses(application);

        // Assert
        Assert.Contains(WoodlandOfficerReviewSubStatus.AmendmentsWithApplicant, result);
        Assert.Contains(WoodlandOfficerReviewSubStatus.OnPublicRegister, result);
    }

    [Fact]
    public void GetCurrentSubStatuses_ReturnsEmptySet_WhenNoSpecsSatisfied()
    {
        // Arrange
        var woReview = new WoodlandOfficerReview
        {
            FellingAndRestockingAmendmentReviews =
                [
                    new FellingAndRestockingAmendmentReview { AmendmentReviewCompleted = true }
                ]
        };
        var publicRegister = new PublicRegister
        {
            ConsultationPublicRegisterPublicationTimestamp = null,
            ConsultationPublicRegisterRemovedTimestamp = null,
            DecisionPublicRegisterPublicationTimestamp = null,
            DecisionPublicRegisterRemovedTimestamp = null
        };
        var application = CreateApplication(woReview: woReview, publicRegister: publicRegister);
        var specs = new ISubStatusSpecification[]
        {
            new AmendmentsWithApplicantSpecification(),
            new OnPublicRegisterSpecification()
        };
        var service = new WoodlandOfficerReviewSubStatusService(specs);

        // Act
        var result = service.GetCurrentSubStatuses(application);

        // Assert
        Assert.Empty(result);
    }
}