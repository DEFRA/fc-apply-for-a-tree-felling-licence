using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public static class GisMappingExtensions
{
    public static InternalCompartmentDetails<Polygon> ToInternalCompartmentDetails(
        this SubmittedFlaPropertyCompartment submittedFlaPropertyCompartment)
    {
        return Create(submittedFlaPropertyCompartment.CompartmentNumber, submittedFlaPropertyCompartment.SubCompartmentName, submittedFlaPropertyCompartment.GISData!);
    }

    public static InternalCompartmentDetails<Polygon> ToInternalCompartmentDetails(
        this Compartment compartment)
    {
        return Create(compartment.CompartmentNumber, compartment.SubCompartmentName, compartment.GISData!);
    }

    public static InternalFullCompartmentDetails ToInternalFullCompartmentDetails(
        this SubmittedFlaPropertyCompartment submittedFlaPropertyCompartment)
    {
        var internalCompartmentDetails = new InternalFullCompartmentDetails
        {
            CompartmentNumber = submittedFlaPropertyCompartment.CompartmentNumber,
            WoodlandName = submittedFlaPropertyCompartment.WoodlandName,
            GISData = submittedFlaPropertyCompartment.GISData
        };

        if (!string.IsNullOrEmpty(submittedFlaPropertyCompartment.SubCompartmentName))
        {
            internalCompartmentDetails.SubCompartmentNo = submittedFlaPropertyCompartment.SubCompartmentName;
        }

        var compartmentPolygon = JsonConvert.DeserializeObject<Polygon>(submittedFlaPropertyCompartment.GISData!);

        internalCompartmentDetails.ShapeGeometry = compartmentPolygon!;

        return internalCompartmentDetails;
    }
    
    private static InternalCompartmentDetails<Polygon> Create(string compartmentNumber, string? subCompartmentName, string gisData)
    {
        var internalCompartmentDetails = new InternalCompartmentDetails<Polygon>
        {
            CompartmentNumber = compartmentNumber
        };

        if (!string.IsNullOrEmpty(subCompartmentName))
        {
            internalCompartmentDetails.SubCompartmentNo = subCompartmentName;
        }

        var compartmentPolygon = JsonConvert.DeserializeObject<Polygon>(gisData!);

        internalCompartmentDetails.ShapeGeometry = compartmentPolygon!;

        return internalCompartmentDetails;
    }
}