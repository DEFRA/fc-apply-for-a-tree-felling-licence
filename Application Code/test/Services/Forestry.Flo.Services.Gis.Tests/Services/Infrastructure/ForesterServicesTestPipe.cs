using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Services;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.Gis.Models.Internal;
using Microsoft.Extensions.Logging;
using Forestry.Flo.Services.Common;

namespace Forestry.Flo.Services.Gis.Tests.Services.Infrastructure;

public class IForesterAccessTestPipe(EsriConfig config, IHttpClientFactory httpClientFactory,
    ILogger<ForesterServices> logger) : ForesterServices(config, httpClientFactory, logger)

{
    public async Task<Result<Stream>> GenerateImage_SingleCompartmentAsync(InternalCompartmentDetails<BaseShape> compartment, CancellationToken cancellationToken, int delay, MapGeneration generationType, string title)
    {
        return await base.GenerateImage_SingleCompartmentAsync(compartment, cancellationToken, delay, generationType, title);
    }

    public async Task<Result<Stream>> GenerateImage_MultipleCompartmentsAsync(
        List<InternalCompartmentDetails<BaseShape>> compartments, CancellationToken cancellationToken, int delay, MapGeneration generationType, string title)
    {
        return await base.GenerateImage_MultipleCompartmentsAsync(compartments, cancellationToken, delay, generationType, title);
    }
}

