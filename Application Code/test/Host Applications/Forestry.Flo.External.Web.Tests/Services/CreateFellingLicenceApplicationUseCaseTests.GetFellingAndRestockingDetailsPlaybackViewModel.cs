using AutoFixture;
using Forestry.Flo.External.Web.Controllers;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Tests.Common;
using LinqKit;
using Newtonsoft.Json;

namespace Forestry.Flo.External.Web.Tests.Services;

public partial class CreateFellingLicenceApplicationUseCaseTests
{
    [Theory]
    [InlineData(FellingLicenceStatus.Draft)]
    [InlineData(FellingLicenceStatus.ReturnedToApplicant)]
    [InlineData(FellingLicenceStatus.WithApplicant)]
    public async Task ShouldGetFellingAndRestockingDetailsPlaybackViewModelEditableApplication(FellingLicenceStatus status)
    {
        var statusHistory = _fixture.Build<StatusHistory>()
            .With(x => x.Status, status)
            .Without(x => x.FellingLicenceApplication)
            .Without(x => x.FellingLicenceApplicationId)
            .Create();

        var application = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.StatusHistories, new List<StatusHistory> { statusHistory })
            .Create();

        // arrange
        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);

        var compartments = application.LinkedPropertyProfile!.ProposedFellingDetails!.Select(d =>
            new ModelMappingTests.TestCompartment(d.PropertyProfileCompartmentId)).ToList();

        _compartmentRepository
            .Setup(r => r.ListAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(compartments);

        foreach (var felling in application.LinkedPropertyProfile!.ProposedFellingDetails!)
        {
            foreach (var restocking in felling.ProposedRestockingDetails!)
            {
                restocking.PropertyProfileCompartmentId = felling.PropertyProfileCompartmentId;
            }
        }

        // act
        var result =
            await _sut.GetFellingAndRestockingDetailsPlaybackViewModel(application.Id, _externalApplicant,
                CancellationToken.None);

        // assert
        Assert.True(result.HasValue);
        Assert.Equal(application.Id, result.Value.ApplicationId);
        application.LinkedPropertyProfile!.ProposedFellingDetails!.Select(p =>
            p.PropertyProfileCompartmentId.ToString().ToUpper()).ForEach(x => Assert.True(result.Value.GIS!.ToUpper().Contains(x)));
        Assert.Equal("FellingLicenceApplication", result.Value.FellingCompartmentsChangeLink.Controller);
        Assert.Equal(nameof(FellingLicenceApplicationController.SelectCompartments), result.Value.FellingCompartmentsChangeLink.Action);

        object values = new
        { applicationId = application.Id, isForRestockingCompartmentSelection = false, returnToPlayback = true };
        Assert.Equivalent(values, result.Value.FellingCompartmentsChangeLink.Values);
        Assert.Equal(application.LinkedPropertyProfile!.ProposedFellingDetails.Count, result.Value.FellingCompartmentDetails.Count);

        for (int i = 0; i < application.LinkedPropertyProfile!.ProposedFellingDetails.Count; i++)
        {
            var resultCompartment = result.Value.FellingCompartmentDetails.Find(f =>
                f.CompartmentId == application.LinkedPropertyProfile!.ProposedFellingDetails[i]
                    .PropertyProfileCompartmentId);
            Assert.NotNull(resultCompartment);
            Assert.Equal("FellingLicenceApplication", resultCompartment.FellingOperationsChangeLink.Controller);
            Assert.Equal(nameof(FellingLicenceApplicationController.SelectFellingOperationTypes), resultCompartment.FellingOperationsChangeLink.Action);

            var fellingValues = new
            {
                applicationId = application.Id,
                fellingCompartmentId = application.LinkedPropertyProfile!.ProposedFellingDetails[i]
                    .PropertyProfileCompartmentId,
                returnToPlayback = true
            };
            Assert.Equivalent(fellingValues, resultCompartment.FellingOperationsChangeLink.Values);
            Assert.Single(resultCompartment.FellingDetails);

            var actualFellingDetail = resultCompartment.FellingDetails[0];
            var expectedfellingDetail = application.LinkedPropertyProfile!.ProposedFellingDetails[i];

            Assert.Equivalent(expectedfellingDetail, actualFellingDetail.FellingDetail);
            Assert.Equal(resultCompartment.CompartmentName, actualFellingDetail.FellingCompartmentName);

            Assert.Single(actualFellingDetail.RestockingCompartmentDetails);

            Assert.Equal(expectedfellingDetail.PropertyProfileCompartmentId, actualFellingDetail.RestockingCompartmentDetails[0].CompartmentId);
            Assert.Equal(expectedfellingDetail.ProposedRestockingDetails!.Count, actualFellingDetail.RestockingCompartmentDetails[0].RestockingDetails.Count);

            for (int j = 0; j < expectedfellingDetail.ProposedRestockingDetails.Count; j++)
            {
                var restocking = actualFellingDetail.RestockingCompartmentDetails[0].RestockingDetails.Find(r =>
                    r.RestockingDetail.Id == expectedfellingDetail.ProposedRestockingDetails[j].Id);
                var cpt = compartments.FirstOrDefault(c => c.Id == expectedfellingDetail.ProposedRestockingDetails[j].PropertyProfileCompartmentId);

                Assert.NotNull(restocking);
                Assert.Equivalent(expectedfellingDetail.ProposedRestockingDetails[j], restocking.RestockingDetail);
                Assert.Equal(cpt.CompartmentNumber, restocking.RestockingCompartmentName);
            }
        }

        var gisCpts = compartments.Select(c => new
        {
            c.Id,
            c.GISData,
            DisplayName = c.CompartmentNumber,
            Selected = true
        });
        Assert.Equal(JsonConvert.SerializeObject(gisCpts), result.Value.GIS);
    }

    [Theory]
    [InlineData(FellingLicenceStatus.Received)]
    [InlineData(FellingLicenceStatus.Submitted)]
    [InlineData(FellingLicenceStatus.WoodlandOfficerReview)]
    [InlineData(FellingLicenceStatus.SentForApproval)]
    [InlineData(FellingLicenceStatus.Approved)]
    [InlineData(FellingLicenceStatus.Refused)]
    [InlineData(FellingLicenceStatus.Withdrawn)]
    [InlineData(FellingLicenceStatus.AdminOfficerReview)]
    [InlineData(FellingLicenceStatus.ReferredToLocalAuthority)]
    public async Task ShouldGetFellingAndRestockingDetailsPlaybackViewModelSubmittedApplication(FellingLicenceStatus status)
    {
        var statusHistory = _fixture.Build<StatusHistory>()
            .With(x => x.Status, status)
            .Without(x => x.FellingLicenceApplication)
            .Without(x => x.FellingLicenceApplicationId)
            .Create();

        var application = _fixture.Build<FellingLicenceApplication>()
            .With(x => x.StatusHistories, new List<StatusHistory> { statusHistory })
            .Create();

        var submittedPropertyDetail = _fixture.Build<SubmittedFlaPropertyDetail>()
            .Without(x => x.FellingLicenceApplication)
            .With(x => x.SubmittedFlaPropertyCompartments, [])
            .Create();

        foreach (var fellingCpt in application.LinkedPropertyProfile.ProposedFellingDetails
                     .Select(x => x.PropertyProfileCompartmentId))
        {
            var submittedCpt = _fixture.Build<SubmittedFlaPropertyCompartment>()
                .Without(x => x.SubmittedFlaPropertyDetail)
                .With(x => x.CompartmentId, fellingCpt)
                .With(x => x.GISData, JsonConvert.SerializeObject(_fixture.Create<Polygon>()))
                .Create();
            submittedPropertyDetail.SubmittedFlaPropertyCompartments.Add(submittedCpt);
        }

        // arrange
        _fellingLicenceApplicationRepository.Setup(x => x.GetIsEditable(application.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _fellingLicenceApplicationRepository
            .Setup(r => r.GetAsync(It.Is<Guid>(a => a == application.Id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _fellingLicenceApplicationRepository
            .Setup(x => x.GetExistingSubmittedFlaPropertyDetailAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(submittedPropertyDetail);

        var compartments = application.LinkedPropertyProfile!.ProposedFellingDetails!.Select(d =>
            new ModelMappingTests.TestCompartment(d.PropertyProfileCompartmentId)).ToList();

        _compartmentRepository
            .Setup(r => r.ListAsync(It.IsAny<IList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(compartments);

        foreach (var felling in application.LinkedPropertyProfile!.ProposedFellingDetails!)
        {
            foreach (var restocking in felling.ProposedRestockingDetails!)
            {
                restocking.PropertyProfileCompartmentId = felling.PropertyProfileCompartmentId;
            }
        }

        // act
        var result =
            await _sut.GetFellingAndRestockingDetailsPlaybackViewModel(application.Id, _externalApplicant,
                CancellationToken.None);

        // assert
        Assert.True(result.HasValue);
        Assert.Equal(application.Id, result.Value.ApplicationId);
        application.LinkedPropertyProfile!.ProposedFellingDetails!.Select(p =>
            p.PropertyProfileCompartmentId.ToString().ToUpper()).ForEach(x => Assert.True(result.Value.GIS!.ToUpper().Contains(x)));
        Assert.Equal("FellingLicenceApplication", result.Value.FellingCompartmentsChangeLink.Controller);
        Assert.Equal(nameof(FellingLicenceApplicationController.SelectCompartments), result.Value.FellingCompartmentsChangeLink.Action);

        object values = new
        { applicationId = application.Id, isForRestockingCompartmentSelection = false, returnToPlayback = true };
        Assert.Equivalent(values, result.Value.FellingCompartmentsChangeLink.Values);
        Assert.Equal(application.LinkedPropertyProfile!.ProposedFellingDetails.Count, result.Value.FellingCompartmentDetails.Count);

        for (int i = 0; i < application.LinkedPropertyProfile!.ProposedFellingDetails.Count; i++)
        {
            var resultCompartment = result.Value.FellingCompartmentDetails.Find(f =>
                f.CompartmentId == application.LinkedPropertyProfile!.ProposedFellingDetails[i]
                    .PropertyProfileCompartmentId);
            Assert.NotNull(resultCompartment);
            Assert.Equal("FellingLicenceApplication", resultCompartment.FellingOperationsChangeLink.Controller);
            Assert.Equal(nameof(FellingLicenceApplicationController.SelectFellingOperationTypes), resultCompartment.FellingOperationsChangeLink.Action);

            var fellingValues = new
            {
                applicationId = application.Id,
                fellingCompartmentId = application.LinkedPropertyProfile!.ProposedFellingDetails[i]
                    .PropertyProfileCompartmentId,
                returnToPlayback = true
            };
            Assert.Equivalent(fellingValues, resultCompartment.FellingOperationsChangeLink.Values);
            Assert.Single(resultCompartment.FellingDetails);

            var actualFellingDetail = resultCompartment.FellingDetails[0];
            var expectedfellingDetail = application.LinkedPropertyProfile!.ProposedFellingDetails[i];

            Assert.Equivalent(expectedfellingDetail, actualFellingDetail.FellingDetail);
            Assert.Equal(resultCompartment.CompartmentName, actualFellingDetail.FellingCompartmentName);

            Assert.Single(actualFellingDetail.RestockingCompartmentDetails);

            Assert.Equal(expectedfellingDetail.PropertyProfileCompartmentId, actualFellingDetail.RestockingCompartmentDetails[0].CompartmentId);
            Assert.Equal(expectedfellingDetail.ProposedRestockingDetails!.Count, actualFellingDetail.RestockingCompartmentDetails[0].RestockingDetails.Count);

            for (int j = 0; j < expectedfellingDetail.ProposedRestockingDetails.Count; j++)
            {
                var restocking = actualFellingDetail.RestockingCompartmentDetails[0].RestockingDetails.Find(r =>
                    r.RestockingDetail.Id == expectedfellingDetail.ProposedRestockingDetails[j].Id);
                
                var cpt = submittedPropertyDetail.SubmittedFlaPropertyCompartments.FirstOrDefault(c => c.CompartmentId == expectedfellingDetail.ProposedRestockingDetails[j].PropertyProfileCompartmentId);

                Assert.NotNull(restocking);
                Assert.Equivalent(expectedfellingDetail.ProposedRestockingDetails[j], restocking.RestockingDetail);
                Assert.Equal(cpt.CompartmentNumber, restocking.RestockingCompartmentName);
            }
        }

        var gisCpts = submittedPropertyDetail.SubmittedFlaPropertyCompartments.Select(c => new
        {
            Id = c.CompartmentId,
            c.GISData,
            DisplayName = c.CompartmentNumber,
            Selected = true
        });
        Assert.Equal(JsonConvert.SerializeObject(gisCpts), result.Value.GIS);
    }
}