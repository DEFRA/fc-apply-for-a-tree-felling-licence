using Forestry.Flo.Internal.Web.Extensions;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Middleware;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.InternalUsers;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Forestry.Flo.Services.FileStorage.Configuration;
using Serilog;

namespace Forestry.Flo.Internal.Web;

public class Startup
{
    public IConfiguration Configuration { get; }
    public IWebHostEnvironment HostEnvironment { get; }

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = configuration;
        HostEnvironment = env;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog());

        services.Configure<CookiePolicyOptions>(x =>
        {
            x.CheckConsentNeeded = _ => true;
            x.Secure = CookieSecurePolicy.Always;
        });

        var userFileUploadOptions = new UserFileUploadOptions();
        Configuration.Bind("userFileUpload",userFileUploadOptions);

        services.Configure<KestrelServerOptions>(o =>
        {
            o.Limits.MaxRequestBodySize = userFileUploadOptions.MaxFileSizeBytes;
        });
        services.Configure<IISServerOptions>(o =>
        {
            o.MaxRequestBodySize = userFileUploadOptions.MaxFileSizeBytes;
        });

        var builder = services.AddControllersWithViews();

        //todo remove this an the related nuget package once GDS styling work is complete.
        if (HostEnvironment.IsDevelopment())
        {
            builder.AddRazorRuntimeCompilation();
        }

        services.AddHttpContextAccessor();
        services.AddRequestContext();
        services.AddValidationMiddlewareOptions(Configuration);
        services.AddAzureAdB2CServices(Configuration);
        services.AddAzureAdServices(Configuration);
        services.AddFloServices(Configuration);
        services.AddDomainServices(ConfigureDatabaseConnection, Configuration, HostEnvironment);
        services.RegisterMassTransit(Configuration);

        services.AddScoped<UserAccountValidationMiddleware>();
        services.AddAuthorization<IDbContextFactory<InternalUsersContext>>((options, dbContextFactory) =>
        {
            options.AddPolicy(AuthorizationPolicyNameConstants.HasFcUserRole, policy 
                => policy.RequireAssertion(x => AuthorizationPolicyService.AssertHasAnyOfRoles(x, dbContextFactory, new List<Roles> { Roles.FcUser })));
          
            options.AddPolicy(AuthorizationPolicyNameConstants.HasFcAdministratorRole, policy 
                => policy.RequireAssertion(x => AuthorizationPolicyService.AssertHasAnyOfRoles(x, dbContextFactory, new List<Roles> { Roles.FcAdministrator })));
        });

        // configure data protection to be not in-memory if the provided configuration setting is available
        var dataProtectionKeysDirectory = Configuration["DataProtectionKeysDirectory"];
        if (!string.IsNullOrWhiteSpace(dataProtectionKeysDirectory))
        {
            services
                .AddDataProtection()
                .SetApplicationName("Felling License Online")
                .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysDirectory));
        }

        // The TempData provider cookie is not essential. Make it essential
        // so TempData is functional when tracking is disabled.
        services.Configure<CookieTempDataProviderOptions>(options => {
            options.Cookie.IsEssential = true;
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.Use((context, next) =>
            {
                context.Request.Scheme = "https";
                return next();
            });
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseCors("CorsPolicy");
        app.UseStatusCodePagesWithRedirects("/Home/Error");
        app.UseHttpsRedirection();

        // TODO - security headers middleware - see EAPC

        // Set up additional MIME types Esri Maps 
        var provider = new FileExtensionContentTypeProvider
        {
            Mappings =
            {
                [".ttf"] = "application/octet-stream",
                [".wasm"] = "application/wasm",
                [".woff"] = "application/font-woff",
                [".woff2"] = "application/font-woff2",
                [".wsv"] = "application/octet-stream"
            }
        };
        app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = provider });

        app.UseCookiePolicy();

        app.UseRouting();

        app.Use(next => context => {
            context.Request.EnableBuffering();
            return next(context);
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseUserAccountValidation();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });
    }

    protected virtual void ConfigureDatabaseConnection(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = Configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseNpgsql(connectionString)
            //  .EnableSensitiveDataLogging()
            .UseLoggerFactory(LoggerFactory.Create(b => b.AddSerilog()));
    }
}