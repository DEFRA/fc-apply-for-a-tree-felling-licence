using System;
using System.Net;
using System.Text;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Extensions;

public static class ConditionsExtensions
{
    public static string ToHtml(this CalculatedCondition condition, int conditionIndex)
    {
        var lines = new StringBuilder();
        foreach (var line in condition.ConditionsText)
        {
            lines.Append(WebUtility.HtmlEncode(line)).Append("<br/>");
        }

        var result = lines.ToString();

        for(int i = 0; i < condition.Parameters.Count; i++)
        {
            result = result.Replace("{" + condition.Parameters[i].Index + "}", GetInput(conditionIndex, i, condition.Parameters[i].Value, condition.Parameters[i].Description));
        }

        return result;
    }

    /// <summary>
    /// Generates a <see cref="CalculateConditionsRequest"/> from a list of confirmed felling and restocking details and an application ID.
    /// </summary>
    /// <param name="compartments">The list of confirmed felling and restocking detail models.</param>
    /// <param name="applicationId">The ID of the felling licence application.</param>
    /// <returns>A <see cref="CalculateConditionsRequest"/> populated with restocking operations.</returns>
    public static CalculateConditionsRequest GenerateCalculateConditionsRequest(this List<FellingAndRestockingDetailModel> compartments, Guid applicationId)
    {
        var treeSpeciesValues = TreeSpeciesFactory.SpeciesDictionary.Values;
        var restockingOperations = new List<RestockingOperationDetails>();

        foreach (var compartment in compartments)
        {
            foreach (var felling in compartment.ConfirmedFellingDetailModels)
            {
                restockingOperations.AddRange(felling.ConfirmedRestockingDetailModels.Select(restocking => new RestockingOperationDetails
                {
                    FellingCompartmentNumber = compartment.CompartmentNumber,
                    FellingSubcompartmentName = compartment.SubCompartmentName,
                    FellingOperationType = felling.OperationType.ToConditionsFellingType(),
                    RestockingSubmittedFlaPropertyCompartmentId = restocking.CompartmentId,
                    RestockingCompartmentNumber = restocking.CompartmentNumber,
                    RestockingSubcompartmentName = restocking.SubCompartmentName,
                    RestockingProposalType = restocking.RestockingProposal.ToConditionsRestockingType(),
                    PercentNaturalRegeneration = restocking.PercentNaturalRegeneration ?? 0,
                    PercentOpenSpace = restocking.PercentOpenSpace ?? 0,
                    RestockingDensity = restocking.RestockingDensity,
                    NumberOfTrees = restocking.NumberOfTrees,
                    TotalRestockingArea = restocking.Area ?? 0,
                    RestockingSpecies = restocking.ConfirmedRestockingSpecies?.Select(x => new RestockingSpecies
                        {
                            SpeciesCode = x.Species,
                            SpeciesName = treeSpeciesValues.SingleOrDefault(y => y.Code == x.Species)?.Name ?? x.Species,
                            Percentage = x.Percentage ?? 0
                        })
                        .ToList() ?? []
                }));
            }
        }

        return new CalculateConditionsRequest
        {
            IsDraft = false,
            FellingLicenceApplicationId = applicationId,
            RestockingOperations = restockingOperations
        };
    }

    private static string GetInput(int conditionIndex, int parameterIndex, string value, string hint)
    {
        var inputClass = "govuk-input inline";
        if (!string.IsNullOrWhiteSpace(value) && value.Length > 45)
        {
            inputClass = "govuk-input inline2";
        }

        var tagStart = "<input class=\"" + inputClass + "\" type=\"text\" value=\"" + value + "\"";
        var tagEnd = "</input>";
        var hintHtml = string.IsNullOrWhiteSpace(hint) 
            ? string.Empty 
            : "title=\"" + hint + "\" ";
        
        if (hint.Contains("Additional criteria"))
        {
            tagStart = "<textarea class=\"govuk-textarea\" rows=\"3\" ";
            tagEnd = value + "</textarea>";
        }

        return tagStart
               + "id=\"Conditions_" + conditionIndex + "__Parameters_" + parameterIndex  + "__Value\" "
               + "name=\"Conditions[" + conditionIndex + "].Parameters[" + parameterIndex + "].Value\" "
               + hintHtml
               + ">"
               + tagEnd;
    }
}