using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.Publish
{
    public class InternalFellingLicenceApplication<T> : ObjectIdResponse<T>
    {
        [JsonProperty("property_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? PropertyName { get; set; }

        [JsonProperty("application_ref", NullValueHandling = NullValueHandling.Ignore)]
        public string? ApplicationRef { get; set; }

        [JsonProperty("grid_reference", NullValueHandling = NullValueHandling.Ignore)]
        public string? GridReference { get; set; }

        [JsonProperty("nearest_town", NullValueHandling = NullValueHandling.Ignore)]
        public string? NearestTown { get; set; }

        [JsonProperty("admin_hub_area_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? AdminHubAreaName { get; set; }

        [JsonProperty("consultation_public_register_start_date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ConsultationPublicRegisterStartDate { get; set; }

        [JsonProperty("consultation_public_register_period", NullValueHandling = NullValueHandling.Ignore)]
        public int? ConsultationPublicRegisterPeriod { get; set; }

        [JsonProperty("consultation_public_register_end_date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ConsultationPublicRegisterEndDate { get; set; }
        
        [JsonProperty("decision_public_register_start_date", NullValueHandling = NullValueHandling.Ignore)]      
        public DateTime? DecisionPublicRegisterStartDate { get; set; }
        
        [JsonProperty("decision_public_register_period", NullValueHandling = NullValueHandling.Ignore)]
        public int? DecisionPublicRegisterPeriod { get; set; }

        [JsonProperty("decision_public_register_end_date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? DecisionPublicRegisterEndDate { get; set; }

        [JsonProperty("compartment_label", NullValueHandling = NullValueHandling.Ignore)]
        public string? CompartmentLabel { get; set; }

        [JsonProperty("Confirmed TotalAreaHa", NullValueHandling = NullValueHandling.Ignore)]
        public float? ConfirmedTotalAreaHa { get; set; }

        [JsonProperty("fs_area_name", NullValueHandling = NullValueHandling.Ignore)]
        public string? FsAreaName { get; set; }
        
        [JsonProperty("current_application_status", NullValueHandling = NullValueHandling.Ignore)]
        public string? CurrentApplicationStatus { get; set; }

        [JsonProperty("applicant", NullValueHandling = NullValueHandling.Ignore)]
        public string? Applicant { get; set; }

        [JsonProperty("conditions", NullValueHandling = NullValueHandling.Ignore)]
        public string? Conditions { get; set; }

        [JsonProperty("submitted_date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? SubmittedDate { get; set; }

        [JsonProperty("decision_date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? DecisionDate { get; set; }

        [JsonProperty("withdrawal_date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? WithdrawalDate { get; set; }
        
        [JsonProperty("admin_officer", NullValueHandling = NullValueHandling.Ignore)]
        public string? AdminOfficer { get; set; }

        [JsonProperty("woodland_officer", NullValueHandling = NullValueHandling.Ignore)]
        public string? WoodLandOfficer { get; set; }

        [JsonProperty("approving_officer", NullValueHandling = NullValueHandling.Ignore)]
        public string? ApprovingOfficer { get; set; }

        [JsonProperty("expired_date", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? ExpiredDate { get; set; }

        [JsonProperty("expiry_category", NullValueHandling = NullValueHandling.Ignore)]
        public string? ExpiryCategory { get; set; }
    }
}
