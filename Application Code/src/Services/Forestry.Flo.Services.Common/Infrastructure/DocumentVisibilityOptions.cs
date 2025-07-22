namespace Forestry.Flo.Services.Common.Infrastructure;

public class DocumentVisibilityOptions
{
    public VisibilityOptions ExternalLisConstraintReport { get; set; } = null!;
    public VisibilityOptions SiteVisitAttachment { get; set; } = null!;
    public VisibilityOptions FcLisConstraintReport { get; set; } = null!;
    public VisibilityOptions ApplicationDocument { get; set; } = null!;

    public class VisibilityOptions
    {
        public bool VisibleToApplicant { get; set; }
        public bool VisibleToConsultees { get; set; }
    }
}