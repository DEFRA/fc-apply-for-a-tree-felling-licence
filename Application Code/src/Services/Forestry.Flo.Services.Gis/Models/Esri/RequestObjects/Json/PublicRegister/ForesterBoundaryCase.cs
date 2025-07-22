using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.PublicRegister
{
    public class ForesterBoundaryCase<T> : PublicRegisterBase<T>
    {
        /// <summary>
        /// The Property Name
        /// </summary>
        [JsonProperty("property_name", NullValueHandling = NullValueHandling.Ignore)]
        [MaxLength(50)]
        public string? PropertyName { get; set; }

        /// <summary>
        /// The Case type
        /// </summary>
        [JsonProperty("case_type", NullValueHandling = NullValueHandling.Ignore)]
        [MaxLength(20)]
        public string? CaseType { get; set; } = null!;

        /// <summary>
        /// The Status of the case. If adding then it should be "Consultation".
        /// This is defaulted in the constructor.
        /// </summary>
        [JsonProperty("case_status", NullValueHandling = NullValueHandling.Ignore)]
        [MaxLength(20)]
        public string? CaseStatus { get; set; }

        /// <summary>
        /// The National Grid Reference of the boundary.
        /// </summary>
        [JsonProperty("grid_reference", NullValueHandling = NullValueHandling.Ignore)]
        [MaxLength(50)]
        public string? GridReference { get; set; }

        /// <summary>
        /// The Local Authority responsible for the area.
        /// </summary>
        [JsonProperty("local_authority", NullValueHandling = NullValueHandling.Ignore)]
        [MaxLength(50)]
        public string? LocalAuthority { get; set; }

        /// <summary>
        /// The Nearest town for the boundary
        /// </summary>
        [JsonProperty("nearest_town", NullValueHandling = NullValueHandling.Ignore)]
        [MaxLength(50)]
        public string? NearestTown { get; set; }

        /// <summary>
        /// The FC admin area for the boundary
        /// </summary>
        [JsonProperty("administrative_region", NullValueHandling = NullValueHandling.Ignore)]
        [MaxLength(50)]
        public string? AdminLocation { get; set; }

        /// <summary>
        /// The Broadleaf Area (ha)
        /// </summary>
        [JsonProperty("broadleaf_area", NullValueHandling = NullValueHandling.Ignore)]
        public double? BroadLeafArea { get; set; }

        /// <summary>
        /// The Coniferous Area (ha)
        /// </summary>
        [JsonProperty("coniferous_area", NullValueHandling = NullValueHandling.Ignore)]
        public double? ConiferousArea { get; set; }

        /// <summary>
        /// The Open Ground ARea (ha)
        /// </summary>
        [JsonProperty("open_ground_area", NullValueHandling = NullValueHandling.Ignore)]
        public double? OpenGroundArea { get; set; }

        /// <summary>
        /// The Total Area (HA)
        /// </summary>
        [JsonProperty("total_area", NullValueHandling = NullValueHandling.Ignore)]
        public double? TotalArea { get; set; }

        /// <summary>
        /// The Date the item should appear on the Register
        /// </summary>
        [JsonProperty("public_register_start_date", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(MicrosecondEpochConverter))]
        public DateTime? PRStartDate { get; set; }

        /// <summary>
        /// The Length of time in days to be on the register
        /// </summary>
        [JsonProperty("period", NullValueHandling = NullValueHandling.Ignore)]
        public int? TimeOnTheConsultationRegister { get; set; }

        /// <summary>
        /// The date the case should be removed from the consultation register
        /// </summary>
        [JsonProperty("public_register_end_date", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(MicrosecondEpochConverter))]
        public DateTime? PREndDate { get; set; }

        /// <summary>
        /// The Decision on the Public Register. If adding then its should be "true".
        /// </summary>
        [JsonProperty("decision_on_pr", NullValueHandling = NullValueHandling.Ignore)]
        public string? OnThePR { get; set; }

        /// <summary>
        /// The Approval Date of the Case
        /// </summary>
        [JsonProperty("case_approval_date", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(MicrosecondEpochConverter))]
        public DateTime? CaseApprovalDate { get; set; }

        /// <summary>
        /// The Approval Date of the Case
        /// </summary>
        [JsonProperty("organisation", NullValueHandling = NullValueHandling.Ignore)]
        public string Organisation => "Forestry Commission";
    }
}
