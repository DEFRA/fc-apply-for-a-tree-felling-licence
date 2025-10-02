using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.Gis.Models.Internal.Request;
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
                        convertedObj = JsonConvert.DeserializeObject<Polygon>(shape.ShapeDetails);
                        if (convertedObj != null && ((Polygon)convertedObj).Rings == null)
                        {
                            convertedObj = null;
                        }
                        break;
                    case "polyline":
                        convertedObj = JsonConvert.DeserializeObject<Line>(shape.ShapeDetails);
                        if (convertedObj != null && ((Line)convertedObj).Path == null)
                        {
                            convertedObj = null;
                        }
                        break;
                    case "point":
                        convertedObj = JsonConvert.DeserializeObject<Point>(shape.ShapeDetails);

                        if (convertedObj != null && (((Point)convertedObj).X == null | ((Point)convertedObj).Y == null))
                        {
                            convertedObj = null;
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
            Polygon? polygon = null;
            try
            {
                polygons.ForEach(polygon1 =>
                {
                    if (polygon == null)
                    {
                        polygon = polygon1;
                    }
                    else
                    {
                        if (polygon.Rings != null && polygon1.Rings != null)
                        {
                            polygon.Rings.Add(polygon1.Rings[0]);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return Result.Failure<Polygon>(ex.Message);
            }
            return polygon == null ? Result.Failure<Polygon>("Unable to set Polygon")
                : Result.Success(polygon);
        }
    }
}
