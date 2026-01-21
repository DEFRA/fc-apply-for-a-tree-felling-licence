using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.TreeHealth;

namespace Forestry.Flo.External.Web.Services.Validation;

public class TreeHealthIssuesViewModelValidator : AbstractValidator<TreeHealthIssuesViewModel>
{
    public TreeHealthIssuesViewModelValidator()
    {
        // None and Other not checked, so at least one other option must be selected
        RuleFor(x => x.TreeHealthIssues.TreeHealthIssueSelections)
            .Must(selections => selections != null && selections.Values.Any(selected => selected))
            .When(x => x.TreeHealthIssues is { NoTreeHealthIssues: false, OtherTreeHealthIssue: false })
            .WithMessage("Select the tree health or public safety reasons for the application, or select \"No tree health or public safety reasons\"");

        // When None is checked, then Other and all other options must be unchecked
        RuleFor(x => x.TreeHealthIssues)
            .Must(s => s.OtherTreeHealthIssue is false && s.TreeHealthIssueSelections.All(z => z.Value is false))
            .When(x => x.TreeHealthIssues.NoTreeHealthIssues)
            .WithMessage("You cannot select \"No tree health or public safety reasons\" with another option")
            .OverridePropertyName("TreeHealthIssues.TreeHealthIssueSelections");

        RuleFor(x => x.TreeHealthIssues.OtherTreeHealthIssueDetails)
            .NotEmpty()
            .When(x => x.TreeHealthIssues.OtherTreeHealthIssue)
            .WithMessage("Enter a description of the other tree disease or public safety issue");                                         
    }
}