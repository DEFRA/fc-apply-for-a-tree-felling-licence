namespace Forestry.Flo.Services.Common.Infrastructure;
public class PDFGeneratorAPIOptions
{
    /// <summary>
    /// The URL path for requesting the pdf from the pdf generator api for producing the application
    /// </summary>
    public string BaseUrl { get; set; }

    /// <summary>
    /// The name of the template used for the pdf generator use
    /// </summary>
    public string TemplateName { get; set; } = "FellingLicence.html";

    /// <summary>
    /// The version listed at the bottom of the lincence generated - three levels of production
    /// 0.0 = private beta
    /// 0 = public beta
    /// 1 = live
    /// </summary>
    public string Version { get; set; }
}