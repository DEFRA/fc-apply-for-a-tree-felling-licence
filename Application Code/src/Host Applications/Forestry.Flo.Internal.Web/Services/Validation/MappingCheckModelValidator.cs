﻿using FluentValidation;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation;

/// <summary>
/// <see cref="AbstractValidator{T}"/> class for validating <see cref="MappingCheckModel"/> instances.
/// </summary>
public class MappingCheckModelValidator : AbstractValidator<MappingCheckModel>
{
    /// <summary>
    /// Creates a new instance of <see cref="MappingCheckModelValidator"/> and sets up
    /// the validation rules.
    /// </summary>
    public MappingCheckModelValidator()
    {
        RuleFor(x => x.CheckFailedReason)
            .NotEmpty()
            .When(x => x.CheckPassed == false)
            .WithMessage("Reason for failure must be provided");
    }
}