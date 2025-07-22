using AutoMapper;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Services.Applicants.Entities;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FileStorage.Model;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using System.Collections.ObjectModel;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.ConditionsBuilder.Models;
using FellingOperationType = Forestry.Flo.Services.FellingLicenceApplications.Entities.FellingOperationType;
using RestockingSpecies = Forestry.Flo.Services.FellingLicenceApplications.Entities.RestockingSpecies;
using UserAccountModel = Forestry.Flo.Internal.Web.Models.UserAccount.UserAccountModel;
using Forestry.Flo.Internal.Web.Models.Reports;
using Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;
using StatusHistoryModel = Forestry.Flo.Internal.Web.Models.FellingLicenceApplication.StatusHistoryModel;
using ServiceWoodlandOwnerModel = Forestry.Flo.Services.Applicants.Models.WoodlandOwnerModel;
using WoodlandOwnerModel = Forestry.Flo.Internal.Web.Models.UserAccount.WoodlandOwnerModel;

namespace Forestry.Flo.Internal.Web.Services;

public static class ModelMapping
{
    private static readonly IMapper Mapper;

    static ModelMapping()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<UserAccount, UserRegistrationDetailsModel>()
               .ForMember(destination => destination.RequestedAccountType, opts => opts.MapFrom(source => source.AccountType))
               .ForMember(destination => destination.RequestedAccountTypeOther, opts => opts.MapFrom(source => source.AccountTypeOther));
            cfg.CreateMap<UserAccount, UpdateUserRegistrationDetailsModel>()
                .ForMember(destination => destination.RequestedAccountType, opts => 
                    opts.MapFrom(source => source.AccountType))
                .ForMember(destination => destination.RequestedAccountTypeOther, opts =>
                    opts.MapFrom(source => source.AccountTypeOther));

            cfg.CreateMap<UserAccount, UserAccountModel>();
            
            cfg.CreateMap<Address, Models.Address>();
            
            cfg.CreateMap<RestockingSpecies, RestockingSpeciesModel>()
                .ForMember(dest => dest.SpeciesName, opt =>
                    opt.MapFrom(s => TreeSpeciesFactory.SpeciesDictionary[s.Species].Name));

            cfg.CreateMap<FellingSpecies, FellingSpeciesModel>()
                .ForMember(dest => dest.SpeciesName, opt =>
                    opt.MapFrom(s => TreeSpeciesFactory.SpeciesDictionary[s.Species].Name));
            
            cfg.CreateMap<ProposedFellingDetail, ProposedFellingDetailModel>()
                .ForMember(dest => dest.Species, opt =>
                    opt.MapFrom(s =>  MapFellingSpecies(s)))
                .ForMember(dest => dest.StepComplete, opt => opt.Ignore());

            cfg.CreateMap<ProposedRestockingDetail, ProposedRestockingDetailModel>()
                .ForMember(dest => dest.Species, opt =>
                    opt.MapFrom(s =>  MapRestockingSpecies(s)))
                .ForMember(dest => dest.StepComplete, opt => opt.Ignore());

            cfg.CreateMap<Document, DocumentModel>();
            cfg.CreateMap<ExternalAccessLink, ExternalInviteLink>()
                .ForMember(dest => dest.AreCommentsProvided, opt => opt.Ignore());
            cfg.CreateMap<StatusHistory, StatusHistoryModel>();
            cfg.CreateMap<Document, SupportingDocument>();
            
            cfg.CreateMap<CaseNote, CaseNoteModel>();

            cfg.CreateMap<WoodlandOwner, WoodlandOwnerModel>()
                .ForMember(dest => dest.ContactAddressMatchesOrganisationAddress, opt => 
                    opt.MapFrom(s => s.ContactAddress != null && s.ContactAddress.Equals(s.OrganisationAddress)));

            cfg.CreateMap<ServiceWoodlandOwnerModel, WoodlandOwnerModel>();

            cfg.CreateMap<Flo.Internal.Web.Models.Address, Address>();

            cfg.CreateMap<Agency, AgencyModel>()
                .ForMember(dest => dest.AgencyId, opt => opt.MapFrom(p => p.Id));

            cfg.CreateMap<Flo.Services.Applicants.Entities.UserAccount.UserAccount,
                ExternalUserAccountModel>();
        });

        Mapper = config.CreateMapper();
    }
    
    private static Dictionary<string, FellingSpeciesModel> MapFellingSpecies(ProposedFellingDetail s) =>
        (s.FellingSpecies ?? new List<FellingSpecies>()).ToDictionary(d => d.Species, d => Mapper.Map<FellingSpeciesModel>(d));
    private static Dictionary<string, RestockingSpeciesModel> MapRestockingSpecies(ProposedRestockingDetail s) =>
        (s.RestockingSpecies ?? new List<RestockingSpecies>()).ToDictionary(d => d.Species, d => Mapper.Map<RestockingSpeciesModel>(d));


    public static UserRegistrationDetailsModel ToUserRegistrationDetailsModel(UserAccount userAccount)
    {
        return Mapper.Map<UserRegistrationDetailsModel>(userAccount);
    }

    public static UpdateUserRegistrationDetailsModel ToUpdateUserRegistrationDetailsModel(UserAccount userAccount)
    {
        return Mapper.Map<UpdateUserRegistrationDetailsModel>(userAccount);
    }

    public static IEnumerable<UserAccountModel> ToUserAccountModels(IEnumerable<UserAccount> userAccounts)
    {
        return Mapper.Map<IEnumerable<UserAccountModel>>(userAccounts);
    }

    public static UserAccountModel ToUserAccountModel(UserAccount userAccount)
    {
        return Mapper.Map<UserAccountModel>(userAccount);
    }

    public static ProposedFellingDetailModel ToProposedFellingDetailModel(ProposedFellingDetail proposedFellingDetail) =>
        Mapper.Map<ProposedFellingDetailModel>(proposedFellingDetail);

    public static ProposedRestockingDetailModel ToProposedRestockingDetailModel(ProposedRestockingDetail proposedRestockingDetail) =>
        Mapper.Map<ProposedRestockingDetailModel>(proposedRestockingDetail);

    public static IList<DocumentModel> ToDocumentModelList(IList<Document>? documents) =>
        Mapper.Map<IList<DocumentModel>>(documents);

    public static DocumentModel ToDocumentModel(Document? document) =>
        Mapper.Map<DocumentModel>(document);

    public static AgencyModel ToAgencyModel(Agency? agency) => Mapper.Map<AgencyModel>(agency);

    public static ExternalUserAccountModel ToExternalUserAccountModel(
        Flo.Services.Applicants.Entities.UserAccount.UserAccount user) => Mapper.Map<ExternalUserAccountModel>(user);

    public static IEnumerable<StatusHistoryModel> ToStatusHistoryModelList(IList<StatusHistory> statusHistoryItems) =>
        Mapper.Map<IList<StatusHistoryModel>>(statusHistoryItems);
    
    public static IList<SupportingDocument> ToSupportingDocumentList(IList<Document>? documents) =>
        Mapper.Map<IList<SupportingDocument>>(documents);
    
    public static IList<ExternalInviteLink> ToExternalInviteLinkList(IList<ExternalAccessLink>? documents) =>
        Mapper.Map<IList<ExternalInviteLink>>(documents);

    public static IList<CaseNoteModel> ToCaseNoteModelList(IList<CaseNote> caseNotes) =>
        Mapper.Map<IList<CaseNoteModel>>(caseNotes);

    public static WoodlandOwnerModel ToWoodlandOwnerModel(WoodlandOwner woodlandOwnerValue) =>
        Mapper.Map<WoodlandOwnerModel>(woodlandOwnerValue);

    public static WoodlandOwnerModel ToWoodlandOwnerModel(ServiceWoodlandOwnerModel woodlandOwnerValue) =>
        Mapper.Map<WoodlandOwnerModel>(woodlandOwnerValue);

    /// <summary>
    /// Map a FormFileCollection to a list of simplified <see cref="FileToStoreModel"/> models for the file storage service to work with.
    /// </summary>
    /// <param name="formCollection"></param>
    /// <returns></returns>
    public static ReadOnlyCollection<FileToStoreModel> ToFileToStoreModel(FormFileCollection formCollection)
    {
        List<FileToStoreModel> models = formCollection
            .Select(formFile => new FileToStoreModel
            {
                FileName = formFile.FileName,
                ContentType = formFile.ContentType,
                FileBytes = formFile.ReadFormFileBytes()
            }).ToList();

        return new ReadOnlyCollection<FileToStoreModel>(models);
    }

    public static Flo.Services.ConditionsBuilder.Models.FellingOperationType ToConditionsFellingType(
        this FellingOperationType value)
    {
        return value switch
        {
            FellingOperationType.None => Flo.Services.ConditionsBuilder.Models.FellingOperationType.None,
            FellingOperationType.ClearFelling => Flo.Services.ConditionsBuilder.Models.FellingOperationType.ClearFelling,
            FellingOperationType.FellingOfCoppice => Flo.Services.ConditionsBuilder.Models.FellingOperationType.FellingOfCoppice,
            FellingOperationType.FellingIndividualTrees => Flo.Services.ConditionsBuilder.Models.FellingOperationType.FellingIndividualTrees,
            FellingOperationType.RegenerationFelling => Flo.Services.ConditionsBuilder.Models.FellingOperationType.RegenerationFelling,
            FellingOperationType.Thinning => Flo.Services.ConditionsBuilder.Models.FellingOperationType.Thinning,
            _ => throw new ArgumentOutOfRangeException(nameof(value), "Could not map FellingOperationType value")
        };
    }

    public static RestockingProposalType ToConditionsRestockingType(this TypeOfProposal value)
    {
        return value switch
        {
            TypeOfProposal.None => RestockingProposalType.None,
            TypeOfProposal.CreateDesignedOpenGround => RestockingProposalType.CreateDesignedOpenGround,
            TypeOfProposal.DoNotIntendToRestock => RestockingProposalType.DoNotIntendToRestock,
            TypeOfProposal.PlantAnAlternativeArea => RestockingProposalType.PlantAnAlternativeArea,
            TypeOfProposal.NaturalColonisation => RestockingProposalType.NaturalColonisation,
            TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees => RestockingProposalType.PlantAnAlternativeAreaWithIndividualTrees,
            TypeOfProposal.ReplantTheFelledArea => RestockingProposalType.ReplantTheFelledArea,
            TypeOfProposal.RestockByNaturalRegeneration => RestockingProposalType.RestockByNaturalRegeneration,
            TypeOfProposal.RestockWithCoppiceRegrowth => RestockingProposalType.RestockWithCoppiceRegrowth,
            TypeOfProposal.RestockWithIndividualTrees => RestockingProposalType.RestockWithIndividualTrees,
            _ => throw new ArgumentOutOfRangeException(nameof(value), "Could not map TypeOfProposal value")
        };
    }

    /// <summary>
    /// Maps an address model to an address entity
    /// </summary>
    /// <param name="address"></param>
    /// <returns>An address entity created from an address model</returns>
    public static Address ToAddressEntity(Models.Address address) =>
        Mapper.Map<Address>(address);

    /// <summary>
    /// Maps an address entity to an address model
    /// </summary>
    /// <param name="address"></param>
    /// <returns>An address model created from an address entity</returns>
    public static Models.Address ToAddressModel(Address address) =>
        Mapper.Map<Models.Address>(address);

    public static FellingOperationType ToFellingOperationType(this FellingOperationTypeForReporting value)
    {
        return value switch
        {
            FellingOperationTypeForReporting.FellingIndividualTrees => FellingOperationType.FellingIndividualTrees,
            FellingOperationTypeForReporting.ClearFelling => FellingOperationType.ClearFelling,
            FellingOperationTypeForReporting.FellingOfCoppice => FellingOperationType.FellingOfCoppice,
            FellingOperationTypeForReporting.Thinning => FellingOperationType.Thinning,
            FellingOperationTypeForReporting.RegenerationFelling => FellingOperationType.RegenerationFelling,
            FellingOperationTypeForReporting.None => FellingOperationType.None,

            _ => throw new ArgumentOutOfRangeException(nameof(value), "Could not map FellingOperationTypeForReporting value")
        };
    }

    public static ReportDateRangeType ToReportDateRangeType(this DateRangeTypeForReporting value)
    {
        return value switch
        {
            DateRangeTypeForReporting.SubmittedDate => ReportDateRangeType.Submitted,
            DateRangeTypeForReporting.ApprovedDate => ReportDateRangeType.Approved,
            DateRangeTypeForReporting.CitizensCharter=> ReportDateRangeType.CitizensCharter,
            DateRangeTypeForReporting.FinalAction => ReportDateRangeType.FinalAction,
            DateRangeTypeForReporting.OffPublicRegister => ReportDateRangeType.OffPublicRegister,
            DateRangeTypeForReporting.OnPublicRegister => ReportDateRangeType.OnPublicRegister,
            DateRangeTypeForReporting.PublicRegisterExpiry => ReportDateRangeType.PublicRegisterExpiry,
            DateRangeTypeForReporting.ReferredToLocalAuthority => ReportDateRangeType.ReferredToLocalAuthority,
            //DateRangeTypeForReporting.CompletedFelling => ReportDateRangeType.CompletedFelling,
            //DateRangeTypeForReporting.CompletedRestocking => ReportDateRangeType.CompletedRestocking,

            _ => throw new ArgumentOutOfRangeException(nameof(value), "Could not map DateRangeTypeForReporting value")
        };
    }

    public static FellingLicenceStatus ToEntityFellingLicenceStatus(this FellingLicenceApplicationStatusesForReporting value)
    {
        return value switch
        {
            FellingLicenceApplicationStatusesForReporting.Draft => FellingLicenceStatus.Draft,
            FellingLicenceApplicationStatusesForReporting.Approved => FellingLicenceStatus.Approved,
            FellingLicenceApplicationStatusesForReporting.Received => FellingLicenceStatus.Received,
            FellingLicenceApplicationStatusesForReporting.Refused => FellingLicenceStatus.Refused,
            FellingLicenceApplicationStatusesForReporting.SentForApproval => FellingLicenceStatus.SentForApproval,
            FellingLicenceApplicationStatusesForReporting.Submitted => FellingLicenceStatus.Submitted,
            FellingLicenceApplicationStatusesForReporting.WithApplicant => FellingLicenceStatus.WithApplicant,
            FellingLicenceApplicationStatusesForReporting.Withdrawn => FellingLicenceStatus.Withdrawn,
            FellingLicenceApplicationStatusesForReporting.WoodlandOfficerReview => FellingLicenceStatus
                .WoodlandOfficerReview,
            FellingLicenceApplicationStatusesForReporting.ReturnedToApplicant => FellingLicenceStatus.ReturnedToApplicant,
            FellingLicenceApplicationStatusesForReporting.AdminOfficerReview => FellingLicenceStatus.AdminOfficerReview,
            FellingLicenceApplicationStatusesForReporting.ReferredToLocalAuthority => FellingLicenceStatus.ReferredToLocalAuthority,

            _ => throw new ArgumentOutOfRangeException(nameof(value), "Could not map FellingLicenceApplicationStatusesForReporting value")
        };
    }

    /// <summary>
    /// Retrieves a collection of submitted felling and restocking details for a given application.
    /// </summary>
    /// <param name="application">The application to retrieve the felling and restocking details for.</param>
    /// <returns>A collection of felling and restocking details.</returns>
    public static IList<FellingAndRestockingDetail> RetrieveFellingAndRestockingDetails(Flo.Services.FellingLicenceApplications.Entities.FellingLicenceApplication application)
    {
        if (application.SubmittedFlaPropertyDetail?.SubmittedFlaPropertyCompartments is null)
        {
            return new List<FellingAndRestockingDetail>();
        }

        var compartmentsDictionary = application.SubmittedFlaPropertyDetail.SubmittedFlaPropertyCompartments.ToDictionary(c => c.CompartmentId, c => c);

        var fellingAndRestockingDetails =
            (application.LinkedPropertyProfile?.ProposedFellingDetails ?? new List<ProposedFellingDetail>())
            .Select(proposedFellingDetail =>
                new FellingAndRestockingDetail
                {
                    ApplicationId = application.Id,
                    CompartmentId = proposedFellingDetail.PropertyProfileCompartmentId,
                    CompartmentName = compartmentsDictionary[proposedFellingDetail.PropertyProfileCompartmentId].DisplayName,
                    GISData = compartmentsDictionary[proposedFellingDetail.PropertyProfileCompartmentId].GISData,
                    WoodlandId = proposedFellingDetail.LinkedPropertyProfileId,
                    FellingDetail = ToProposedFellingDetailModel(proposedFellingDetail),
                }).ToList();
        var fellingAndRestockingDetailsDictionary =
            fellingAndRestockingDetails.ToDictionary(d => d.FellingDetail.Id, d => d);

        var proposedRestockingDetails = application.LinkedPropertyProfile?.ProposedFellingDetails?.SelectMany(p => p.ProposedRestockingDetails!);

        proposedRestockingDetails?
            .ToList().ForEach(r =>
            {
                fellingAndRestockingDetailsDictionary[r.ProposedFellingDetailsId].RestockingDetail =
                    ToProposedRestockingDetailModel(r);
            });

        return fellingAndRestockingDetails;
    }

}