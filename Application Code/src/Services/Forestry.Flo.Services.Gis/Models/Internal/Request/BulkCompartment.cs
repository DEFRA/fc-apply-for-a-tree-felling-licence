using Newtonsoft.Json;


namespace Forestry.Flo.Services.Gis.Models.Internal.Request
{
    public class ImportCompartmentModel
    {
        public string CompartmentNumber { get; set; } = null!;

        public string GISData { get; set; }

        public string? SubCompartmentName { get; set; }

        public string? Designation { get; set; }

        public string? WoodlandName { get; set; }

        public double? TotalHectares {get;set;}

        public int ShapeID { get; set; }
    }

    public class BulkImportCompartment
    {
        public Guid PropertyProfile { get; set; }

        public List<ImportCompartmentModel> Compartments { get; set; } = new List<ImportCompartmentModel>();
    }
}
