namespace Forestry.Flo.Services.Gis.Models.Esri.Configuration
{
    public class EsriConfig
    {

        /// <summary>
        /// Settings that are only needed to access Forestry AGOL 
        /// </summary>
        public ForestryConfig Forestry { get; set; } = null!;

        /// <summary>
        /// Settings that are only needed to access Forester AGOL 
        /// </summary>
        public ForesterConfig Forester { get; set; } = null!;

        /// <summary>
        /// The Spatial reference code to use on the map
        /// </summary>
        public int SpatialReference { get; set; }

        public string LayoutTemplate { get; set; }

        public PublicRegistryConfig PublicRegister { get; set; } = null!;
    }
}