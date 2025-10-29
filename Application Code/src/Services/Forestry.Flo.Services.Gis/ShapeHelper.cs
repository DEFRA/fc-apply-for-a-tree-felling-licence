using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.Gis.Models.Internal.Request;
using Forestry.Flo.Services.Gis.Models.MapObjects;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis
{
    public static class ShapeHelper
    {
        public static Maybe<FlowShape<BaseShape>> ConvertShape(FlowShape<string> shape)
        {
            Guard.Against.Null(shape);

            if (string.IsNullOrEmpty(shape.ShapeDetails) || string.IsNullOrEmpty(shape.ShapeType))
            {
                return Maybe<FlowShape<BaseShape>>.None;
            }
            BaseShape? convertedObj = null;
            var shapeName = shape.ShapeType.ToLower().Trim();
            try
            {
                switch (shapeName)
                {
                    case "polygon":

                        var convertedObjAsPoly = JsonConvert.DeserializeObject<Polygon>(shape.ShapeDetails);
                        if (convertedObjAsPoly?.Rings != null)
                        {
                            convertedObj = convertedObjAsPoly;
                        }

                        break;
                    case "polyline":

                        var convertedObjAsLine = JsonConvert.DeserializeObject<Line>(shape.ShapeDetails);
                        if (convertedObjAsLine?.Path != null)
                        {
                            convertedObj = convertedObjAsLine;
                        }

                        break;
                    case "point":

                        var convertedObjAsPoint = JsonConvert.DeserializeObject<Point>(shape.ShapeDetails);
                        if (convertedObjAsPoint is { X: not null, Y: not null })
                        {
                            convertedObj = convertedObjAsPoint;
                        }

                        break;
                }
            }
            catch (Exception)
            {
                convertedObj = null;
            }

            return convertedObj == null ? Maybe<FlowShape<BaseShape>>.None : Maybe.From(new FlowShape<BaseShape>
            {
                ShapeDetails = convertedObj,
                ShapeType = shapeName,
                Name = shape.Name
            });
        }

        /// <summary>
        /// Adds all polygons together to make a multipart one.
        /// We don't use the Geo service to do this as it can simplify the geometry.
        /// </summary>
        /// <param name="polygons">The Polygons to merge</param>
        /// <returns>A Polygon</returns>
        public static Result<Polygon> MakeMultiPart(List<Polygon> polygons)
        {
            Guard.Against.NullOrEmpty(polygons);

            // Collect all rings from all polygons
            var allRings = new List<List<List<float>>>();
            SpatialReference? spatialRef = null;

            foreach (var poly in polygons)
            {
                if (poly?.Rings is not { Count: > 0 })
                {
                    continue;
                }
                allRings.AddRange(poly.Rings);
                spatialRef ??= poly.SpatialSettings;
            }

            if (allRings.Count == 0)
            {
                return Result.Failure<Polygon>("No valid rings found in input polygons.");
            }


            var multiPartPolygon = new Polygon
            {
                Rings = allRings,
                SpatialSettings = spatialRef
            };

            return Result.Success(multiPartPolygon);
        }
    }
}
