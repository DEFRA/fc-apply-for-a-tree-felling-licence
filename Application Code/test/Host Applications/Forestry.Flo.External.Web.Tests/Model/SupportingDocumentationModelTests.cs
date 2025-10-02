using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.External.Web.Tests.Model;

public class SupportingDocumentationModelTests
{
    [Theory, AutoMoqData]
    public void ShouldCalculateCompletedStatus_WhenHaveDocuments(SupportingDocumentationModel model)
    {
        //arrange
        //act
        var result = model.Status;

        //assert
        Assert.Equal(ApplicationStepStatus.Completed, result);
    }

    [Theory, AutoMoqData]
    public void ShouldCalculateNotStartedStatus_WhenNoDocuments(SupportingDocumentationModel model)
    {
        //arrange
        model.Documents = new List<DocumentModel>();
        model.StepComplete = null;

        //act
        var result = model.Status;

        //assert
        Assert.Equal(ApplicationStepStatus.NotStarted, result);
    }
}