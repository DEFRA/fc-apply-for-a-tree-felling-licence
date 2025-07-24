using AutoMapper;
using Forestry.Flo.Services.AdminHubs.Entities;
using Forestry.Flo.Services.AdminHubs.Model;

namespace Forestry.Flo.Services.AdminHubs.Services;

public static class ModelMapping
{
    private static readonly IMapper Mapper;

    static ModelMapping()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AdminHub, AdminHubModel>()
                .ForMember(d => d.AdminManagerUserAccountId, opts => opts.MapFrom(s => s.AdminManagerId));
            cfg.CreateMap<AdminHubOfficer, AdminHubOfficerModel>();
            cfg.CreateMap<Area, AreaModel>();
        });

        Mapper = config.CreateMapper();
    }

    public static AdminHubModel ToAdminHubModel(AdminHub adminHub)
    {
        return Mapper.Map<AdminHubModel>(adminHub);
    }

    public static IReadOnlyCollection<AdminHubModel> ToAdminHubModels(IReadOnlyCollection<AdminHub> adminHubs)
    {
        return Mapper.Map<IReadOnlyCollection<AdminHubModel>>(adminHubs);
    }
}