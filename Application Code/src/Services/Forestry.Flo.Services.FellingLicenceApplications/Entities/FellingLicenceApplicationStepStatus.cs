using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.FellingLicenceApplications.Entities
{
    public class FellingLicenceApplicationStepStatus
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [Key]
        public Guid Id { get; protected set; }

        public Guid FellingLicenceApplicationId { get; set; }

        public bool? ApplicationDetailsStatus { get; set; }

        public bool? SelectCompartmentsStatus { get; set; }

        public bool? OperationsStatus { get; set; }

        public bool? ConstraintCheckStatus { get; set; }

        public bool? SupportingDocumentationStatus { get; set; }

        public bool? TermsAndConditionsStatus { get; set; }

        /// <summary>
        /// Gets and sets a value indicating the status of the Environmental Impact Assessment step.
        /// </summary>
        public bool? EnvironmentalImpactAssessmentStatus { get; set; }

        /// <summary>
        /// Gets and sets a value indicating the status of the Ten Year Licence step.
        /// </summary>
        public bool? TenYearLicenceStepStatus { get; set; }

        // https://stackoverflow.com/questions/44829824/how-to-store-json-in-an-entity-field-with-ef-core

        // Note: There is no FellingAndRestockingDetailsStatus, instead we use the aggregation represented by CompartmentFellingRestockingStatuses

        public List<CompartmentFellingRestockingStatus> CompartmentFellingRestockingStatuses { get; set; }

        public FellingLicenceApplication FellingLicenceApplication { get; set; }
    }
}
