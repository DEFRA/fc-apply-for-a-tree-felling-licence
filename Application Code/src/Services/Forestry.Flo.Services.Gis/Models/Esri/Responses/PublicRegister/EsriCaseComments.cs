using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Gis.Models.Esri.Responses.PublicRegister
{
    public class EsriCaseComments
    {
        /// <summary>
        /// The Record ID in the Comments table
        /// </summary>
        [JsonProperty("objectid")]
        public int RecordID { get; set; }


        /// <summary>
        /// The case Refence identifier
        /// </summary>
        [JsonProperty("case_reference")]
        [MaxLength(50)]
        public string CaseReference { get; set; } = null!;

        /// <summary>
        /// The First name of the person making the comments
        /// </summary>
        [JsonProperty("firstname")]
        [MaxLength(50)]
        public string Firstname { get; set; } = null!;

        /// <summary>
        /// The surname of the person making the comment
        /// </summary>
        [JsonProperty("surname")]
        [MaxLength(50)]
        public string Surname { get; set; } = null!;

        /// <summary>
        /// The email adress of the person making the comment
        /// </summary>
        [JsonProperty("email")]
        [MaxLength(50)]
        public string EmailAddress { get; set; } = null!;

        /// <summary>
        /// The comments on the visit
        /// </summary>
        [JsonProperty("comment")]
        [MaxLength(530000)]
        public string CaseNote { get; set; } = null!;

        /// <summary>
        /// Decision on Public Register (Boolean) 
        /// </summary>
        [JsonProperty("public")]
        [MaxLength(50)]
        public string IsPublic { get; set; } = "0";

        /// <summary>
        /// The global ID in the FC System
        /// </summary>
        [JsonProperty("globalid")]
        public Guid? GlobalID { get; set; }

        /// <summary>
        /// The user that created the record
        /// </summary>
        [JsonProperty("created_user")]
        [MaxLength(225)]
        public string CreatedUser { get; set; } = null!;

        /// <summary>
        /// The date that the record was updated on
        /// </summary>
        [JsonProperty("created_date")]
        [JsonConverter(typeof(MicrosecondEpochConverter))]
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The user that updated the record
        /// </summary>
        [JsonProperty("last_edited_user")]
        [MaxLength(225)]
        public string LastEditUser { get; set; } = null!;

        /// <summary>
        /// The date that the record was updated on
        /// </summary>
        [JsonProperty("last_edited_date")]
        [JsonConverter(typeof(MicrosecondEpochConverter))]
        public DateTime LastEditDate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("case_globalid ")]
        public Guid? CaseGlobalID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("notify_status")]
        public int? NotifyStatus { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("globalid_1")]
        public Guid? GlobalIDOne { get; set; }
    }
}
