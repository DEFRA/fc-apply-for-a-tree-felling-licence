namespace Forestry.Flo.Services.Common.Infrastructure;

/// <summary>
/// Attribute to mark options within an enum that are allowed to be selected by an applicant
/// when other options in the enum are intended for system use only (e.g. for Withdrawal reasons,
/// some options are intended for selection by the applicant when withdrawing an application, but
/// others are intended to be set by the system when an application is withdrawn automatically as
/// a result of certain events such as expiry of the 21 day resubmission deadline. This attribute
/// allows us to distinguish between these two types of options and ensure that only the appropriate
/// options are presented to the applicant in the UI). 
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ApplicantOptionAttribute : Attribute
{
    
}