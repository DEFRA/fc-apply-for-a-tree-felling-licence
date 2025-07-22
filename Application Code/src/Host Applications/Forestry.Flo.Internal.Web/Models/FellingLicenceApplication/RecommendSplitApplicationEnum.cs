using System.ComponentModel;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public enum RecommendSplitApplicationEnum
{
    [Description("Recommendation to split due to larch in zone 1 and other species")]
    MixLarchZone1,
    [Description("Recommendation to split due larch in mixed zones including zone 1")]
    LarchOnlyMixZone,
    [Description("Recommendation to split due larch in mixed zones including zone 1 and other species")]
    MixLarchMixZone,
    [Description("Don't return application")]
    DontReturnApplication,
}
