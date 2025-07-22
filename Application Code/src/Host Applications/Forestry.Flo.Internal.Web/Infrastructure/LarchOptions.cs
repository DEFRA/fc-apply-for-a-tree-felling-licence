namespace Forestry.Flo.Internal.Web.Infrastructure;

public class LarchOptions
{
    public int EarlyFadDay { get; set; } = 30;
    public int EarlyFadMonth { get; set; } = 6;
    public int LateFadDay { get; set; } = 31;
    public int LateFadMonth { get; set; } = 10;
    public int FlyoverPeriodStartDay { get; set; } = 1;
    public int FlyoverPeriodStartMonth { get; set; } = 4;
    public int FlyoverPeriodEndDay { get; set; } = 31;
    public int FlyoverPeriodEndMonth { get; set; } = 8;
}
