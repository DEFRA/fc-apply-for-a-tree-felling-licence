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

        /// <summary>
        /// Gets or sets the ID of the related felling licence application.
        /// </summary>
        public Guid FellingLicenceApplicationId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the completion status of the Application Details step.
        /// true = complete, false = incomplete, null = not started/unknown.
        /// </summary>
        public bool? ApplicationDetailsStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the completion status of the Select Compartments step.
        /// true = complete, false = incomplete, null = not started/unknown.
        /// </summary>
        public bool? SelectCompartmentsStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the completion status of the Operations step.
        /// true = complete, false = incomplete, null = not started/unknown.
        /// </summary>
        public bool? OperationsStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the completion status of the Constraint Check step.
        /// true = complete, false = incomplete, null = not started/unknown.
        /// </summary>
        public bool? ConstraintCheckStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the completion status of the Supporting Documentation step.
        /// true = complete, false = incomplete, null = not started/unknown.
        /// </summary>
        public bool? SupportingDocumentationStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the completion status of the Terms and Conditions step.
        /// true = complete, false = incomplete, null = not started/unknown.
        /// </summary>
        public bool? TermsAndConditionsStatus { get; set; }

        /// <summary>
        /// Gets and sets a value indicating the status of the Environmental Impact Assessment step.
        /// true = complete, false = incomplete, null = not started/unknown.
        /// </summary>
        public bool? EnvironmentalImpactAssessmentStatus { get; set; }

        /// <summary>
        /// Gets and sets a value indicating the status of the Habitat Restoration step.
        /// true = complete, false = incomplete, null = not started/unknown.
        /// </summary>
        public bool? HabitatRestorationStatus { get; set; }

        /// <summary>
        /// Gets and sets a value indicating the status of the Ten Year Licence step.
        /// true = complete, false = incomplete, null = not started/unknown.
        /// </summary>
        public bool? TenYearLicenceStepStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the completion status of the Agent Authority Form (AAF) step.
        /// true = complete, false = incomplete, null = not started/unknown.
        /// </summary>
        public bool? AafStepStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the completion status of the Tree Health Issues step.
        /// </summary>
        public bool? TreeHealthIssuesStatus { get; set; }

        // https://stackoverflow.com/questions/44829824/how-to-store-json-in-an-entity-field-with-ef-core

        // Note: There is no FellingAndRestockingDetailsStatus, instead we use the aggregation represented by CompartmentFellingRestockingStatuses

        /// <summary>
        /// Gets or sets the per-compartment felling and restocking completion statuses used to
        /// determine the overall felling and restocking details progress for the application.
        /// </summary>
        public List<CompartmentFellingRestockingStatus> CompartmentFellingRestockingStatuses { get; set; }

        /// <summary>
        /// Gets and sets the per-compartment designations completion statuses used to determine the
        /// overall designations data entry progression for the application.
        /// </summary>
        public List<CompartmentDesignationStatus> CompartmentDesignationsStatuses { get; } = [];

        /// <summary>
        /// Gets or sets the navigation property to the related felling licence application.
        /// </summary>
        public FellingLicenceApplication FellingLicenceApplication { get; set; }
    }
}
