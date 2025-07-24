namespace Forestry.Flo.Services.Gis.Models.Esri.Configuration
{
    /// <summary>
    /// Contains all the Layers that the system uses.
    /// If we had more time the service would allow for mixing of tokens and layers.
    /// </summary>
    public class FeatureLayerConfig
    {
        /// <summary>
        /// The Name of the layer
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The URI for the services
        /// </summary>
        public string ServiceURI { get; set; }

        /// <summary>
        /// The Fields to select 
        /// </summary>
        public List<string> Fields { get; set; }

        /// <summary>
        /// If the Service needs an active account.
        /// </summary>
        public Boolean NeedsToken { get; set; }
    }
}
