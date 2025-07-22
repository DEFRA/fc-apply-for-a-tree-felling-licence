using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Interfaces;


namespace Forestry.Flo.External.Web.Services
{

    public class UploadShapeFileUseCase
    {
        private readonly ISupportedConfig _gisConfig;
        private readonly IForestryServices _agolAccess;
        private readonly ILogger<UploadShapeFileUseCase> _logger;

        public UploadShapeFileUseCase(ISupportedConfig config, IForestryServices agolAccess, ILogger<UploadShapeFileUseCase> logger)
        {
            _gisConfig = config;
            _agolAccess = agolAccess;
            _logger = logger;
        }

        public Maybe<string[]> GetSupportedFileTypes()
        {
            try
            {
                return Maybe<string[]>.From(_gisConfig.GetSupportedFileTypes());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to access supported file types");
            }

            return Maybe<string[]>.None;
        }

        public async Task<Result<string>> GetShapesFromFileAsync(string name, string ext, bool generalize, int offset,
            bool reduce, int round, IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                byte[] data = new byte[file.Length];
                await using (var reader = file.OpenReadStream())
                {
                    await reader.ReadAsync(data, 0, (int)file.Length);
                }

                var uploadType = "shapefile";
                switch (ext.ToLower())
                {
                    case "csv":
                        uploadType = "csv";
                        break;
                    case "gpx":
                        uploadType = "gpx";
                        break;
                    case "geojson":
                        uploadType = "geojson";
                        break;  

                }

                return await _agolAccess.GetFeaturesFromFileAsync(name, ext, generalize, offset, reduce, round, data, uploadType, cancellationToken);
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Unable Call service");
                return Result.Failure<string>("Unable to call service");
            }
        }

        public async Task<Result<string>> GetShapesFromStringAsync(string name, string ext, bool generalize, int offset,
            bool reduce, int round, string valueString, CancellationToken cancellationToken)
        {
            try
            {
                var uploadType = "shapefile";
                switch (ext.ToLower())
                {
                    case "csv":
                        uploadType = "csv";
                        break;
                    case "gpx":
                        uploadType = "gpx";
                        break;
                    case "geojson":
                        uploadType = "geojson";
                        break;

                }

                return await _agolAccess.GetFeaturesFromStringAsync(name, ext, generalize, offset, reduce, round, valueString, uploadType, cancellationToken);
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "Unable Call service");
                return Result.Failure<string>("Unable to call service");
            }
        }

    }
}
