using System.Collections.ObjectModel;
using AutoMapper;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.Compartment;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Models.PropertyProfile;
using Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FileStorage.Model;
using Forestry.Flo.Services.PropertyProfiles.Entities;

using ServiceTenantType = Forestry.Flo.Services.Applicants.Entities.WoodlandOwner.TenantType;
namespace Forestry.Flo.External.Web.Services;

public static class ModelMapping
{
    private static readonly IMapper Mapper;

    static ModelMapping()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Address, Flo.Services.Applicants.Entities.Address>();
            cfg.CreateMap<Flo.Services.Applicants.Entities.Address, Address>();
            cfg.CreateMap<PropertyProfileModel, PropertyProfile>();
            cfg.CreateMap<PropertyProfile, PropertyProfileModel>();
            cfg.CreateMap<PropertyProfile, Models.FellingLicenceApplication.PropertyProfileDetails>();
            cfg.CreateMap<PropertyProfile, Models.PropertyProfile.PropertyProfileDetails>();
            cfg.CreateMap<CompartmentModel, Compartment>()
                .ForMember(dest => dest.PropertyProfile,
                    opt => opt.Ignore());
            cfg.CreateMap<Compartment, CompartmentModel>()
                .ForMember(dest => dest.PropertyProfileName,
                    opt =>
                        opt.MapFrom(c => c.PropertyProfile.Name));
            cfg.CreateMap<IEnumerable<PropertyProfile>, PropertyProfileDetailsListViewModel>()
                .ForMember(dest => dest.PropertyProfileDetailsList, 
                    opt => opt.MapFrom(c => c));
            cfg.CreateMap<ProposedFellingDetail, ProposedFellingDetailModel>()
                .ForMember(dest => dest.CompartmentTotalHectares, opt => opt.Ignore())
                .ForMember(dest => dest.Species, opt =>
                    opt.MapFrom(s =>  MapFellingSpecies(s)))
                .ForMember(dest => dest.Status, opt => opt.Ignore());
            cfg.CreateMap<ProposedFellingDetailModel,ProposedFellingDetail>()
                .ForMember(dest => dest.FellingSpecies, opt => 
                    opt.MapFrom(s => s.Species.Values.ToList()));
            cfg.CreateMap<FellingSpecies, SpeciesModel>()
                .ForMember(dest => dest.SpeciesName, opt =>
                    opt.MapFrom(s => TreeSpeciesFactory.SpeciesDictionary[s.Species].Name));
            cfg.CreateMap<ProposedRestockingDetail, ProposedRestockingDetailModel>()
                .ForMember(dest => dest.CompartmentTotalHectares, opt => opt.Ignore())
                .ForMember(dest => dest.RestockingCompartmentId, opt => opt.MapFrom(s => s.PropertyProfileCompartmentId))
                .ForMember(dest => dest.Species, opt =>
                    opt.MapFrom(s =>  MapRestockingSpecies(s)))
                .ForMember(dest => dest.Status, opt => opt.Ignore());
            cfg.CreateMap<ProposedRestockingDetailModel, ProposedRestockingDetail>()
                .ForMember(dest => dest.RestockingSpecies, opt => 
                    opt.MapFrom(s => s.Species.Values.ToList()));
            cfg.CreateMap<RestockingSpecies, SpeciesModel>()
                .ForMember(dest => dest.SpeciesName, opt =>
                    opt.MapFrom(s => TreeSpeciesFactory.SpeciesDictionary[s.Species].Name));
            cfg.CreateMap<Document, DocumentModel>()
                .ForMember(dest => dest.DocumentPurpose, opt => 
                    opt.MapFrom(s => s.Purpose));

            cfg.CreateMap<PropertyProfile, SubmittedFlaPropertyDetail>()
                .ForMember(dest => dest.PropertyProfileId, opt => opt.MapFrom(p => p.Id))
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            cfg.CreateMap<Compartment, SubmittedFlaPropertyCompartment>()
                .ForMember(dest => dest.CompartmentId, opt => opt.MapFrom(p => p.Id))
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            cfg.CreateMap<TenantType?, ServiceTenantType>()
                .ConvertUsing((
                    value, _) =>
                {
                    return value switch
                    {
                        TenantType.CrownLand => ServiceTenantType.CrownLand,
                        TenantType.NonCrownLand => ServiceTenantType.NonCrownLand,
                        _ => ServiceTenantType.None
                    };
                });

            cfg.CreateMap<ServiceTenantType, TenantType?>()
                .ConvertUsing((
                    value, _) =>
                {
                    return value switch
                    {
                        ServiceTenantType.CrownLand => TenantType.CrownLand,
                        ServiceTenantType.NonCrownLand => TenantType.NonCrownLand,
                        _ => null
                    };
                });
        });

        Mapper = config.CreateMapper();
    }

    private static Dictionary<string, SpeciesModel> MapFellingSpecies(ProposedFellingDetail s) =>
        (s.FellingSpecies ?? new List<FellingSpecies>()).ToDictionary(d => d.Species, d => Mapper.Map<SpeciesModel>(d));
    private static Dictionary<string, SpeciesModel> MapRestockingSpecies(ProposedRestockingDetail s) =>
        (s.RestockingSpecies ?? new List<RestockingSpecies>()).ToDictionary(d => d.Species, d => Mapper.Map<SpeciesModel>(d));

    /// <summary>
    /// Maps a Property profile UI model to a Property Profile entity
    /// </summary>
    /// <param name="propertyProfileModel">A Property Profile UI model to map</param>
    /// <returns>A result Property Profile entity</returns>
    public static PropertyProfile ToPropertyProfile(PropertyProfileModel propertyProfileModel)
    {
        return Mapper.Map<PropertyProfile>(propertyProfileModel);
    }

    /// <summary>
    /// Maps a Property profile entity to a Property Profile UI model
    /// </summary>
    /// <param name="propertyProfile">A Property Profile entity to map</param>
    /// <returns>A result Property Profile UI model</returns>
    public static PropertyProfileModel? ToPropertyProfileModel(PropertyProfile? propertyProfile)
    {
        if (propertyProfile == null)
        {
            return null;
        }

        var model = Mapper.Map<PropertyProfileModel>(propertyProfile);

        model.Compartments = model.Compartments.OrderByNameNumericOrAlpha();

        return model;
    }
    
    /// <summary>
    /// Maps an address model to an address entity
    /// </summary>
    /// <param name="address"></param>
    /// <returns>An address entity created from an address model</returns>
    public static Flo.Services.Applicants.Entities.Address ToAddressEntity(Address address) =>
        Mapper.Map<Flo.Services.Applicants.Entities.Address>(address);

    public static Address ToAddressModel(Flo.Services.Applicants.Entities.Address address) =>
        Mapper.Map<Address>(address);

    /// <summary>
    /// Maps a Property profile entity list to a Property Profile UI model list
    /// </summary>
    /// <param name="propertyProfiles">A Property Profile entity to map</param>
    /// <returns>A result Property Profile UI model</returns>
    public static IEnumerable<PropertyProfileModel> ToPropertyProfileModel(IEnumerable<PropertyProfile> propertyProfiles)
    {
        return Mapper.Map<IEnumerable<PropertyProfileModel>>(propertyProfiles);
    }

    /// <summary>
    /// Maps a Compartment UI model to a Compartment entity
    /// </summary>
    /// <param name="compartmentModel">A Compartment model to map</param>
    /// <returns>A result Compartment entity</returns>
    public static Compartment ToCompartment(CompartmentModel compartmentModel)
    {
        return Mapper.Map<Compartment>(compartmentModel);
    }

    /// <summary>
    /// Maps a Compartment entity to a Compartment UI model
    /// </summary>
    /// <param name="compartment">A Compartment entity to map</param>
    /// <returns>A result Compartment entity</returns>
    public static CompartmentModel ToCompartmentModel(Compartment compartment)
    {
        return Mapper.Map<CompartmentModel>(compartment);
    }

    /// <summary>
    /// Maps a list of Compartment entities to a list of Compartment UI models
    /// </summary>
    /// <param name="compartments">A list of Compartment entities to map</param>
    /// <returns>A result Compartment entity</returns>
    public static IEnumerable<CompartmentModel> ToCompartmentModelList(IEnumerable<Compartment> compartments)
    {
        return Mapper.Map<IEnumerable<CompartmentModel>>(compartments);
    }
    
    public static IEnumerable<Models.FellingLicenceApplication.PropertyProfileDetails> ToPropertyProfileDetailsModelList(IEnumerable<PropertyProfile> propertyProfiles)
    {
        return Mapper.Map<IEnumerable<Models.FellingLicenceApplication.PropertyProfileDetails>>(propertyProfiles);
    }

    public static PropertyProfileDetailsListViewModel ToPropertyProfileDetailsListViewModel(IEnumerable<PropertyProfile> propertyProfiles)
    {
        return Mapper.Map<PropertyProfileDetailsListViewModel>(propertyProfiles);
    }

    public static IEnumerable<DocumentModel> ToDocumentsModelForApplicantView(IList<Document>? documentEntities)
    {
        if (documentEntities == null || !documentEntities.Any()) return Enumerable.Empty<DocumentModel>();

        var forApplicantView = documentEntities.Where(x => x.VisibleToApplicant).ToList();
        return Mapper.Map<IEnumerable<DocumentModel>>(forApplicantView);
    }

    public static DocumentModel ToDocumentModel(Document? document) =>
        Mapper.Map<DocumentModel>(document);

    /// <summary>
    /// Maps the proposed felling details entity to the view model
    /// </summary>
    /// <param name="proposedFellingDetail">The proposed felling details entity</param>
    /// <param name="totalHectares"></param>
    /// <returns>The proposed felling details model object</returns>
    public static ProposedFellingDetailModel ToProposedFellingDetailModel(ProposedFellingDetail proposedFellingDetail,
        double? totalHectares)
    {
        var result = Mapper.Map<ProposedFellingDetailModel>(proposedFellingDetail);
        result.CompartmentTotalHectares = totalHectares;
        result.FellingLicenceStatus = proposedFellingDetail.LinkedPropertyProfile?.FellingLicenceApplication?.StatusHistories?
            .OrderByDescending(x => x.Created)
            .FirstOrDefault()?.Status ?? FellingLicenceStatus.Draft;
        result.IsTreeMarkingUsed = !string.IsNullOrWhiteSpace(proposedFellingDetail.TreeMarking);
        return result;
    }

    /// <summary>
    /// Maps the proposed restocking details entity to the view model object
    /// </summary>
    /// <param name="proposedRestockingDetail">The proposed restocking details entity</param>
    /// <param name="totalHectares"></param>
    /// <returns>The proposed restocking details model object</returns>
    public static ProposedRestockingDetailModel ToProposedRestockingDetailModel(
        ProposedRestockingDetail proposedRestockingDetail, double? totalHectares)
    {
        var result = Mapper.Map<ProposedRestockingDetailModel>(proposedRestockingDetail);
        result.CompartmentTotalHectares = totalHectares;
        result.FellingLicenceStatus = proposedRestockingDetail.ProposedFellingDetail?.LinkedPropertyProfile?.FellingLicenceApplication?.StatusHistories?
            .OrderByDescending(x => x.Created)
            .FirstOrDefault()?.Status ?? FellingLicenceStatus.Draft;
        return result;
    }

    /// <summary>
    /// Maps a PropertyProfile entity to a SubmittedFlaPropertyDetail entity
    /// </summary>
    public static SubmittedFlaPropertyDetail ToSubmittedFlaPropertyDetail(PropertyProfile propertyProfile)
    {
        return Mapper.Map<SubmittedFlaPropertyDetail>(propertyProfile);
    }

    /// <summary>
    /// Maps an enumerable of Compartment entity to an enumerable of SubmittedFlaPropertyCompartment
    /// </summary>
    public static IEnumerable<SubmittedFlaPropertyCompartment> ToSubmittedFlaPropertyCompartmentList(IEnumerable<Compartment> compartments)
    {
        return Mapper.Map<IEnumerable<SubmittedFlaPropertyCompartment>>(compartments);
    }

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

    /// <summary>
    /// Maps a service tenant type to a tenant type.
    /// </summary>
    public static TenantType? ToTenantType(ServiceTenantType tenantType)
    {
        return Mapper.Map<TenantType?>(tenantType);
    }

    /// <summary>
    /// Maps a tenant type to a service tenant type.
    /// </summary>
    public static ServiceTenantType ToTenantType(TenantType? tenantType)
    {
        return Mapper.Map<ServiceTenantType>(tenantType);
    }
}