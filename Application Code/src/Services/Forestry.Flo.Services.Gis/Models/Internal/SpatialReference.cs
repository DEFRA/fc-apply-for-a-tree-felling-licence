using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.MapObjects
{
    public class SpatialReference
    {
        /// <summary>
        ///  Gets the latest well-known ID for this instance.
        /// </summary>
        [JsonProperty("latestWkid", NullValueHandling = NullValueHandling.Ignore)]
        public int? LastGoodID { get; set; }
        /// <summary>
        /// The spatial reference wkid to be used to select the SpatialReference in the CoordinateSystemsControl
        /// </summary>
        [JsonProperty("wkid")]
        public int ID { get; set; }

        public SpatialReference()
        {
            ID = 0;
        }

        public SpatialReference(int wkID)
        {
            ID = wkID;
        }

        public SpatialReference(int wkID, int? latest_WKID)
        {
            ID = wkID;
            LastGoodID = latest_WKID;
        }
    }
}
