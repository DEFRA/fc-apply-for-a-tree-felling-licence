using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Common.User;

public enum AccountTypeInternal
{
    [Display(Name = "FC Staff Member")]
    FcStaffMember,

    [Display(Name = "Operations Admin Officer")]
    AdminOfficer,

    [Display(Name = "Woodland Officer")]
    WoodlandOfficer,

    [Display(Name = "Operations Admin Manager")]
    AdminHubManager,

    [Display(Name = "Field Manager")]
    FieldManager,

    [Display(Name = "Other")]
    Other,

    [Display(Name = "Account Administrator")]
    AccountAdministrator
}

public enum AccountTypeInternalOther
{
    [Display(Name="Area Business Manager")]
    AreaBusinessManager,

    [Display(Name = "Area Director")]
    AreaDirector,

    [Display(Name = "Area Operations Manager")]
    AreaOperationsManager,

    [Display(Name = "Development Woodland Officer")]
    DevelopmentWoodlandOfficer,

    [Display(Name = "Enforcement Officer")]
    EnforcementOfficer,

    [Display(Name = "Evidence And Analysis Officer")]
    EvidenceAndAnalysisOfficer,

    [Display(Name = "Operations Team Manager")]
    OperationsTeamManager,

    [Display(Name = "Partnership And Expertise Manager")]
    PartnershipAndExpertiseManager,

    [Display(Name = "Regulations And Incentive Support Officer")]
    RegulationsAndIncentiveSupportOfficer,

    [Display(Name = "Regulations Manager")]
    RegulationsManager,

    [Display(Name = "Regulations Project Officer")]
    RegulationsProjectOfficer,

    [Display(Name = "Technical Support Officer")]
    TechnicalSupportOfficer
}