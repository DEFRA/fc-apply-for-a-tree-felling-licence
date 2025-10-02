using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.External.Web.Tests.Model
{
    public class ApplicationStepBaseTests
    {
        [Theory, AutoMoqData]
        public void ApplicationStepStatusShouldBeNotStarted_WhenApplicationStepBaseStatus_Null(ApplicationStepBase model)
        {
            //arrange

            model.StepComplete = null;

            //act

            var result = model.Status;

            //assert
            Assert.Equal(ApplicationStepStatus.NotStarted, result);
        }

        [Theory, AutoMoqData]
        public void ApplicationStepStatusShouldBeInProgress_WhenApplicationStepBaseStatus_False(ApplicationStepBase model)
        {
            //arrange

            model.StepComplete = false;

            //act

            var result = model.Status;

            //assert
            Assert.Equal(ApplicationStepStatus.InProgress, result);
        }

        [Theory, AutoMoqData]
        public void ApplicationStepStatusShouldBeCompleted_WhenApplicationStepBaseStatus_True(ApplicationStepBase model)
        {
            //arrange

            model.StepComplete = true;

            //act

            var result = model.Status;

            //assert
            Assert.Equal(ApplicationStepStatus.Completed, result);
        }
    }
}
