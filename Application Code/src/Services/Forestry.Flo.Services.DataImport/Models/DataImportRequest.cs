using Forestry.Flo.Services.Common.Models;
using Microsoft.AspNetCore.Http;

namespace Forestry.Flo.Services.DataImport.Models;

public record DataImportRequest
{
    public UserAccessModel UserAccessModel { get; set; }

    public Guid WoodlandOwnerId { get; set; }

    public FormFileCollection ImportFileSet { get; set; }
}