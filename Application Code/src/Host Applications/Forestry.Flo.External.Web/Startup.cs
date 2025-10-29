using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Infrastructure;
using Forestry.Flo.Services.FileStorage.Configuration;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Forestry.Flo.External.Web;

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

        var userFileUploadOptions = new UserFileUploadOptions();
        Configuration.Bind("userFileUpload",userFileUploadOptions);

        services.Configure<FormOptions>(o =>
        {
            o.MultipartBodyLengthLimit = userFileUploadOptions.ServerMaxUploadSizeBytes;
        });

        services.Configure<KestrelServerOptions>(o =>
        {
            o.Limits.MaxRequestBodySize = userFileUploadOptions.ServerMaxUploadSizeBytes;
        });
        
        services.Configure<IISServerOptions>(o =>
        {
            o.MaxRequestBodySize = userFileUploadOptions.ServerMaxUploadSizeBytes;
        });

        services.Configure<CookiePolicyOptions>(x =>
        {
            x.CheckConsentNeeded = _ => true;
            x.Secure = CookieSecurePolicy.Always;
        });

        services.AddOptions<GovUkOneLoginOptions>().BindConfiguration(GovUkOneLoginOptions.ConfigurationKey);
        services.AddOptions<AuthenticationOptions>().BindConfiguration(AuthenticationOptions.ConfigurationKey);

        var builder = services.AddControllersWithViews(options =>
        {
            var formValueProviderFactoryIndex =
                options.ValueProviderFactories.IndexOf(options.ValueProviderFactories.OfType<FormValueProviderFactory>()
                    .Single());
            options.ValueProviderFactories[formValueProviderFactoryIndex] = new TrimmedFormValueProviderFactory();

            options.Filters.Add(new VerifyUserAccountActiveActionFilter());
        });

        //todo remove this an the related nuget package once GDS styling work is complete.
        if (HostEnvironment.IsDevelopment())
        {
            builder.AddRazorRuntimeCompilation();
        }

        var authenticationOptions = new AuthenticationOptions();
        Configuration.Bind("authenticationOptions", authenticationOptions);

        services.AddHttpContextAccessor();
        services.AddRequestContext();
        services.AddDistributedMemoryCache();
        services.AddSession();

        switch (authenticationOptions.Provider)
        {
            case AuthenticationProvider.OneLogin:
                services.AddOneLoginServices(Configuration);
                break;
            case AuthenticationProvider.Azure:
                services.AddAzureAdB2CServices(Configuration);
                break;
        }

        services.AddFloServices(Configuration);
        services.AddDomainServices(ConfigureDatabaseConnection, Configuration, HostEnvironment);
        services.RegisterMassTransit(Configuration);

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
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.Use((context, next) =>
        {
            context.Request.Scheme = "https";

            var userFileUploadOptions = Configuration.Get<UserFileUploadOptions>();

            if (context.Features.Get<IHttpMaxRequestBodySizeFeature>() != null)
            {
                context.Features.Get<IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize =
                    userFileUploadOptions.MaxFileSizeBytes * userFileUploadOptions.MaxNumberDocuments;
            }

            return next();
        });

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
        app.UseSession();
        app.UseCookiePolicy();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

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