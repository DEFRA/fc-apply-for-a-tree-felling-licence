using FluentValidation;
using Forestry.Flo.External.Web.Models.UserAccount;

namespace Forestry.Flo.External.Web.Services.Validation
{
    public class UserAccountModelValidator : AbstractValidator<UserAccountModel>
    {
        //public UserAccountModelValidator()
        //{
        //    RuleFor(x => x..NotEmpty();
        //    RuleFor(x => x.ReferenceIdentifier).NotEmpty();
        //    RuleFor(x => x.CreationDate).NotNull();
        //    RuleFor(x => x.Section1).NotNull().SetValidator(new Section1Validator());
        //    RuleFor(x => x.Section2).NotNull().SetValidator(new Section2Validator());
        //    RuleFor(x => x.Section3).NotNull().SetValidator(new Section3Validator());
        //    RuleFor(x => x.Section4).NotNull().SetValidator(new Section4Validator());
        //    RuleFor(x => x.Section5).NotNull().SetValidator(new Section5Validator());
        //    RuleFor(x => x.Section6).NotNull().SetValidator(new Section6Validator());
        //    RuleFor(x => x.Section7).NotNull().SetValidator(new Section7Validator());
        //    RuleFor(x => x.Applicant).NotNull().SetValidator(new ApplicantValidator());
        //    RuleFor(x => x.SupportingDocumentsSection).NotNull()
        //        .SetValidator(new SupportingInformationSectionValidator());

        //    CheckKeyDatesSection3();
        //    CheckKeyDatesSection5();
        //}

        //Default constructor for FluentValidation DI
        public UserAccountModelValidator()
        {
            
        }

        public UserAccountModelValidator(string sectionAction)
        {
            switch (sectionAction)
            {
                case nameof(UserTypeModel):
                    RuleFor(x => x.UserTypeModel).NotNull().SetValidator(new UserTypeModelValidator());
                    break;
                case nameof(AccountPersonNameModel):
                    RuleFor(x => x.PersonName).NotNull().SetValidator(new AccountPersonNameModelValidator());
                    break;
                case nameof(AccountPersonContactModel):
                    RuleFor(x => x.PersonContactsDetails).NotNull().SetValidator(new AccountPersonContactModelValidator());
                    break;

            }
        }
    }
}
