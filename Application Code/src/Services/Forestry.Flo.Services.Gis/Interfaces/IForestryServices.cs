using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

namespace Forestry.Flo.Services.Gis.Interfaces
{
    /// <summary>
    /// The Access class for handling calls to the Arch GIS Online Services 
    /// </summary>
    public interface IForestryServices
    {
        /// <summary>
        /// Calls the generate services, to create shapes from the file uploaded
        /// </summary>
        /// <param name="name">The name of the file</param>
        /// <param name="ext">The file extension of the file</param>
        /// <param name="generalize"></param>
        /// <param name="offset">The Maximum off set for the features</param>
        /// <param name="reduce">If the shapes should be simplified</param>
        /// <param name="roundTo">The number of decimal points to handle</param>
        /// <param name="file">The Byte array of the file to upload</param>
        /// <param name="uploadType">File type. currently only <value>shapefile</value> is teste</param>
        /// <param name="cancellationToken">Cancellation  Token for the call</param>
        /// <returns>The result of Generate is a JSON feature collection></returns>
        /// <see cref="https://developers.arcgis.com/rest/users-groups-and-items/generate.htm"/>
        Task<Result<string>> GetFeaturesFromFileAsync(string name, string ext, bool generalize, int offset, bool reduce,
            int roundTo, byte[] file, string uploadType, CancellationToken cancellationToken);

        /// <summary>
        /// Calls the generate services, to create shapes from the file uploaded
        /// </summary>
        /// <param name="name">The name of the file</param>
        /// <param name="ext">The file extension of the file</param>
        /// <param name="generalize"></param>
        /// <param name="offset">The Maximum off set for the features</param>
        /// <param name="reduce">If the shapes should be simplified</param>
        /// <param name="roundTo">The number of decimal points to handle</param>
        /// <param name="conversionString">the string value to convert</param>
        /// <param name="uploadType">File type. currently only <value>geojson</value> is tested</param>
        /// <param name="cancellationToken">Cancellation  Token for the call</param>
        /// <returns>The result of Generate is a JSON feature collection></returns>
        /// <see cref="https://developers.arcgis.com/rest/users-groups-and-items/generate.htm"/>
        Task<Result<string>> GetFeaturesFromStringAsync(string name, string ext, bool generalize, int offset,
            bool reduce,
            int roundTo, string conversionString, string uploadType, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the Centre point of a collections of Polygons
        /// </summary>
        /// <param name="compartments">The Compartments to use</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The centre point of the map</returns>
        Task<Result<Point>> CalculateCentrePointAsync(List<string> compartments, CancellationToken cancellationToken);

        /// <summary>
        /// The method combines all the polygons into a giant union then gets the "Label point" the centre of that polygon,
        /// Which then gets sent to ESRI for them to convert to a Lat Long. To which then gets converted into OS Grid
        /// </summary>
        /// <param name="point">All the compartments to use.</param>
        /// <param name="cancellationToken">The cancellation Token</param>
        /// <returns>The centre point</returns>
        Task<Result<string>> GetOSGridReferenceAsync(Point point, CancellationToken cancellationToken);
    }
}