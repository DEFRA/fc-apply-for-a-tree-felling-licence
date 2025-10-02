using AutoFixture.Xunit2;
using Forestry.Flo.Services.Gis.Infrastructure;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.Gis.Tests.Infrastructure;

public class DevelopmentPublicRegisterTests
{
    [Theory, AutoData]
    public async Task ReturnsExpectedTestCaseCommentsByCaseReference(string caseReference)
    {
        var sut = CreateSut();

        var result = await sut.GetCaseCommentsByCaseReferenceAsync(caseReference, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(caseReference, result.Value[0].CaseReference);
        Assert.Equal("New comment", result.Value[0].CaseNote);
        Assert.Equal("John", result.Value[0].Firstname);
        Assert.Equal("Smith", result.Value[0].Surname);
        Assert.Equal(Guid.Parse("9d029ff6-2a4e-4d25-92bc-976d385441b6"), result.Value[0].GlobalID);
    }

    [Theory, AutoData]
    public async Task ReturnsExpectedTestCaseCommentsByCaseReferenceAndDate(string caseReference, DateTime date)
    {
        var sut = CreateSut();
     
        var result = await sut.GetCaseCommentsByCaseReferenceAndDateAsync(caseReference, DateOnly.FromDateTime(date), CancellationToken.None);
        
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal(caseReference, result.Value[0].CaseReference);
        Assert.Equal("New comment", result.Value[0].CaseNote);
        Assert.Equal("John", result.Value[0].Firstname);
        Assert.Equal("Smith", result.Value[0].Surname);
        Assert.Equal(Guid.Parse("9d029ff6-2a4e-4d25-92bc-976d385441b6"), result.Value[0].GlobalID);
    }

    [Theory, AutoData]
    public async Task CanFakeAddingToPublicRegisterWithValidRef(string propertyName, string caseType, string gridRef, string nearestTown,
        string localAdminArea, string adminRegion, DateTime publicRegisterStart, int period, double? broadLeafArea,
        double? coniferousArea, double? openGroundArea, double? totalArea)
    {
        var caseRef = "012/1234/2345/Test";
        var sut = CreateSut();
        var result = await sut.AddCaseToConsultationRegisterAsync(caseRef, propertyName, caseType, gridRef, nearestTown,
            localAdminArea, adminRegion, publicRegisterStart, period, broadLeafArea, coniferousArea,
            openGroundArea, totalArea, new List<InternalCompartmentDetails<Polygon>>(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Value);
    }

    [Theory, AutoData]
    public async Task CanFakeAddingToPublicRegisterWithInvalidRef(string caseRef, string propertyName, string caseType, string gridRef, string nearestTown,
        string localAdminArea, string adminRegion, DateTime publicRegisterStart, int period, double? broadLeafArea,
        double? coniferousArea, double? openGroundArea, double? totalArea)
    {
        var sut = CreateSut();
        var result = await sut.AddCaseToConsultationRegisterAsync(caseRef, propertyName, caseType, gridRef, nearestTown,
            localAdminArea, adminRegion, publicRegisterStart, period, broadLeafArea, coniferousArea,
            openGroundArea, totalArea, new List<InternalCompartmentDetails<Polygon>>(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value);
    }

    [Theory, AutoData]
    public async Task CanFakeRemovingFromPublicRegister(int objectId, string caseReference, DateTime endDateOnPr)
    {
        var sut = CreateSut();

        var result = await sut.RemoveCaseFromConsultationRegisterAsync(objectId, caseReference, endDateOnPr, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Theory, AutoData]
    public async Task CanFakeAddingToDecisionPublicRegister(int objectId, string caseReference,
        string fellingLicenceOutcome, DateTime caseApprovalDateTime)
    {
        var sut = CreateSut();

        var result = await sut.AddCaseToDecisionRegisterAsync(objectId, caseReference, fellingLicenceOutcome, caseApprovalDateTime, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Theory, AutoData]
    public async Task CanFakeRemovingFromDecisionPublicRegister(int objectId, string caseReference)
    {
        var sut = CreateSut();
       
        var result = await sut.RemoveCaseFromDecisionRegisterAsync(objectId, caseReference, CancellationToken.None);
        
        Assert.True(result.IsSuccess);
    }

    private IPublicRegister CreateSut()
    {
        return new DevelopmentPublicRegister(new NullLogger<DevelopmentPublicRegister>());
    }
}