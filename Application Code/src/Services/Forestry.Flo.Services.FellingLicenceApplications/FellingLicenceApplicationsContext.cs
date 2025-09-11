using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.FellingLicenceApplications;

public class FellingLicenceApplicationsContext : DbContext, IUnitOfWork
{
    private readonly ILogger<FellingLicenceApplicationsContext>? _logger;
    public const string SchemaName = "FellingLicenceApplications";
    public const string ApplicationReferenceIdsSequenceName = "AppRefIdCounter";

    public FellingLicenceApplicationsContext()
    {
    }

    public FellingLicenceApplicationsContext(DbContextOptions<FellingLicenceApplicationsContext> options) : base(options)
    {
    }

    public FellingLicenceApplicationsContext(DbContextOptions<FellingLicenceApplicationsContext> options, ILogger<FellingLicenceApplicationsContext> logger) : base(options)
    {
        _logger = logger;
    }

    public DbSet<FellingLicenceApplication> FellingLicenceApplications { get; set; } = null!;

    public DbSet<Document> Documents { get; set; } = null!;
    
    public DbSet<ExternalAccessLink> ExternalAccessLinks { get; set; } = null!;

    public DbSet<ConsulteeComment> ConsulteeComments { get; set; } = null!;

    public DbSet<StatusHistory> StatusHistories { get; set; } = null!;

    public DbSet<AssigneeHistory> AssigneeHistories { get; set; } = null!;

    public DbSet<LinkedPropertyProfile> LinkedPropertyProfiles { get; set; } = null!;

    public DbSet<ProposedFellingDetail> ProposedFellingDetails { get; set; } = null!;

    public DbSet<ProposedRestockingDetail> ProposedRestockingDetails { get; set; } = null!;

    public DbSet<FellingSpecies> FellingSpecies { get; set; } = null!;

    public DbSet<FellingOutcome> FellingOutcome { get; set; } = null!;

    public DbSet<FellingSpecies> RestockingSpecies { get; set; } = null!;

    public DbSet<SubmittedFlaPropertyCompartment> SubmittedFlaPropertyCompartments { get; set; } = null!;

    public DbSet<SubmittedFlaPropertyDetail> SubmittedFlaPropertyDetails { get; set; } = null!;

    public DbSet<CaseNote> CaseNote { get; set; } = null!;
    
    public DbSet<WoodlandOfficerReview> WoodlandOfficerReviews { get; set; } = null!;

    public DbSet<SiteVisitEvidence> SiteVisitEvidences { get; set; } = null!;

    public DbSet<ApproverReview> ApproverReviews { get; set; } = null!;

    public DbSet<AdminOfficerReview> AdminOfficerReviews { get; set; } = null!;

    public DbSet<LarchCheckDetails> LarchCheckDetails { get; set; } = null!;

    public DbSet<ConfirmedFellingDetail> ConfirmedFellingDetails { get; set; } = null!;

    public DbSet<ConfirmedRestockingDetail> ConfirmedRestockingDetails { get; set; } = null!;

    public DbSet<ConfirmedFellingSpecies> ConfirmedFellingSpecies { get; set; } = null!;

    public DbSet<ConfirmedRestockingSpecies> ConfirmedRestockingSpecies { get; set; } = null!;

    public DbSet<PublicRegister> PublicRegister { get; set; } = null!;

    public DbSet<FellingLicenceApplicationStepStatus> FellingLicenceApplicationStepStatus { get; set; } = null!;

    public DbSet<EnvironmentalImpactAssessment> EnvironmentalImpactAssessments { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
        {
            var config = new ConfigurationBuilder()
            .AddUserSecrets<FellingLicenceApplicationsContext>()
            .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");

            optionsBuilder.UseNpgsql(
                connectionString,
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", SchemaName));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.AddAppRefIdCountersForYears(ApplicationReferenceIdsSequenceName, 2022, 2050);

        modelBuilder.Entity<FellingLicenceApplication>().ToTable("FellingLicenceApplication");
        modelBuilder.Entity<Document>().ToTable("Document");
        modelBuilder.Entity<StatusHistory>().ToTable("StatusHistory");
        modelBuilder.Entity<AssigneeHistory>().ToTable("AssigneeHistory");
        modelBuilder.Entity<LinkedPropertyProfile>().ToTable("LinkedPropertyProfile");
        modelBuilder.Entity<ProposedFellingDetail>().ToTable("ProposedFellingDetail");
        modelBuilder.Entity<ProposedRestockingDetail>().ToTable("ProposedRestockingDetail");
        modelBuilder.Entity<FellingSpecies>().ToTable("FellingSpecies");
        modelBuilder.Entity<FellingOutcome>().ToTable("FellingOutcome");
        modelBuilder.Entity<RestockingSpecies>().ToTable("RestockingSpecies");
        modelBuilder.Entity<RestockingOutcome>().ToTable("RestockingOutcome");
        modelBuilder.Entity<SubmittedFlaPropertyCompartment>().ToTable("SubmittedFlaPropertyCompartment");
        modelBuilder.Entity<SubmittedFlaPropertyDetail>().ToTable("SubmittedFlaPropertyDetail");
        modelBuilder.Entity<FellingLicenceApplicationStepStatus>().ToTable("FellingLicenceApplicationStepStatus");
        modelBuilder.Entity<CaseNote>().ToTable("CaseNote");
        modelBuilder.Entity<ExternalAccessLink>().ToTable("ExternalAccessLink");
        modelBuilder.Entity<ConsulteeComment>().ToTable("ConsulteeComment");
        modelBuilder.Entity<WoodlandOfficerReview>().ToTable("WoodlandOfficerReview");
        modelBuilder.Entity<ApproverReview>().ToTable("ApproverReview");
        modelBuilder.Entity<AdminOfficerReview>().ToTable("AdminOfficerReview");
        modelBuilder.Entity<LarchCheckDetails>().ToTable("LarchCheckDetails");
        modelBuilder.Entity<PublicRegister>().ToTable("PublicRegister");
        modelBuilder.Entity<ConfirmedFellingDetail>().ToTable("ConfirmedFellingDetail");
        modelBuilder.Entity<ConfirmedFellingSpecies>().ToTable("ConfirmedFellingSpecies");
        modelBuilder.Entity<ConfirmedRestockingDetail>().ToTable("ConfirmedRestockingDetail");
        modelBuilder.Entity<ConfirmedRestockingSpecies>().ToTable("ConfirmedRestockingSpecies");
        modelBuilder.Entity<SiteVisitEvidence>().ToTable("SiteVisitEvidence");

        modelBuilder.Entity<WoodlandOwner>().ToTable("WoodlandOwner","Applicants", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<UserAccount>().ToTable("UserAccount","Applicants", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Agency>().ToTable("Agency", "Applicants", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<PropertyProfile>().ToTable("PropertyProfile", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Compartment>().ToTable("Compartment", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<InternalUsers.Entities.UserAccount.UserAccount>()
            .ToTable("UserAccount", "InternalUsers", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<EnvironmentalImpactAssessment>().ToTable("EnvironmentalImpactAssessment");
        modelBuilder.Entity<EnvironmentalImpactAssessmentRequestHistory>().ToTable("EnvironmentalImpactAssessmentRequestHistory");

        modelBuilder.HasDefaultSchema(SchemaName);

        // Note: No foreign keys added to tables in schemas outside of FellingLicenceApplications
        modelBuilder
            .Entity<FellingLicenceApplication>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder.Entity<FellingLicenceApplication>()
            .HasIndex(p => p.ApplicationReference)
            .IsUnique();

        modelBuilder
            .Entity<FellingLicenceApplication>()
            .Property(e => e.Source)
            .HasConversion<string>();

        modelBuilder
            .Entity<StatusHistory>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<StatusHistory>()
            .Property(e => e.Status)
            .HasConversion<string>();

        modelBuilder
            .Entity<FellingSpecies>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<FellingSpecies>()
            .Property(e => e.Species);

        modelBuilder
            .Entity<RestockingSpecies>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<RestockingSpecies>()
            .Property(e => e.Species);

        modelBuilder
            .Entity<ExternalAccessLink>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<ExternalAccessLink>()
            .HasOne<FellingLicenceApplication>()
            .WithMany(x => x.ExternalAccessLinks)
            .HasForeignKey(p => p.FellingLicenceApplicationId)
            .IsRequired();

        modelBuilder
            .Entity<ExternalAccessLink>()
            .Property(l => l.IsMultipleUseAllowed)
            .HasDefaultValue(false);

        modelBuilder
            .Entity<ExternalAccessLink>()
            .Property(p => p.LinkType)
            .HasConversion<string>();

        modelBuilder
            .Entity<ExternalAccessLink>()
            .Property(p => p.SharedSupportingDocuments)
            .HasJsonConversion();

        modelBuilder
            .Entity<ConsulteeComment>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<ConsulteeComment>()
            .Property(x => x.DocumentIds)
            .HasJsonConversion();

        modelBuilder
            .Entity<ConsulteeComment>()
            .HasOne<FellingLicenceApplication>()
            .WithMany(x => x.ConsulteeComments)
            .HasForeignKey(p => p.FellingLicenceApplicationId)
            .IsRequired();

        modelBuilder
            .Entity<Document>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();
        modelBuilder
            .Entity<Document>()
            .Property(e => e.Purpose)
            .HasConversion<string>();

        modelBuilder
            .Entity<Document>()
            .Property(e => e.AttachedByType)
            .HasConversion<string>();

        modelBuilder
            .Entity<EnvironmentalImpactAssessmentRequestHistory>()
            .Property(e => e.RequestType)
            .HasConversion<string>();

        modelBuilder
            .Entity<SiteVisitEvidence>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<SiteVisitEvidence>(e =>
            {
                e.HasOne(p => p.WoodlandOfficerReview)
                    .WithMany(p => p.SiteVisitEvidences)
                    .HasForeignKey(p => p.WoodlandOfficerReviewId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder
            .Entity<EnvironmentalImpactAssessmentRequestHistory>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<Document>(e =>
            {
                e.HasOne(p => p.FellingLicenceApplication)
                    .WithMany(p => p.Documents)
                    .HasForeignKey(p => p.FellingLicenceApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder
            .Entity<StatusHistory>(e =>
            {
                e.HasOne(p => p.FellingLicenceApplication)
                    .WithMany(p => p.StatusHistories)
                    .HasForeignKey(p => p.FellingLicenceApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder
            .Entity<AssigneeHistory>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<AssigneeHistory>(e =>
            {
                e.HasOne(p => p.FellingLicenceApplication)
                    .WithMany(p => p.AssigneeHistories)
                    .HasForeignKey(p => p.FellingLicenceApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder
            .Entity<LinkedPropertyProfile>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        // Configuring one to one mapping for LinkedPropertyProfile -> FellingLicenceApplication
        // Reference property on FellingLicenceApplication is nullable to allow FellingLicenceApplication to
        // exist without LinkedPropertyProfile
        modelBuilder
            .Entity<LinkedPropertyProfile>(e =>
            {
                e.HasOne(p => p.FellingLicenceApplication)
                    .WithOne(p => p.LinkedPropertyProfile)
                    .HasForeignKey<LinkedPropertyProfile>(p => p.FellingLicenceApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder
            .Entity<ProposedFellingDetail>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<ProposedFellingDetail>(e =>
            {
                e.HasOne(p => p.LinkedPropertyProfile)
                    .WithMany(p => p.ProposedFellingDetails)
                    .HasForeignKey(p => p.LinkedPropertyProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.Property(p => p.OperationType)
                    .HasConversion<string>();
            });

        modelBuilder
            .Entity<ProposedFellingDetail>()
            .HasIndex(i => new { i.LinkedPropertyProfileId, i.PropertyProfileCompartmentId, i.OperationType })
            .IsUnique();

        modelBuilder.Entity<ProposedFellingDetail>()
            .Property(e => e.AreaToBeFelled)
            .HasPrecision(18, 2);

        // > A LinkedPropertyProfile will have zero to many Proposed Felling Details, and a ProposedFellingDetails will have zero to many Proposed Restocking
        // Details - each felling/restocking entry would be for a particular compartment. The Property Profile Compartment
        // Id would refer back to the compartment entity we already have in the application. [Users may edit property details
        // multiple times] and those edits be reflected in the FLA only up tothe point it is submitted, at which point the current state of the property should be "snapshotted"
        // on the FLA (the submitted FLA property details and submitted fla property compartment entities).

        modelBuilder
            .Entity<ProposedRestockingDetail>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<ProposedRestockingDetail>(e =>
            {
                e.HasOne(p => p.ProposedFellingDetail)
                    .WithMany(p => p.ProposedRestockingDetails)
                    .HasForeignKey(p => p.ProposedFellingDetailsId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.Property(p => p.RestockingProposal)
                    .HasConversion<string>();

            });

        modelBuilder
            .Entity<ProposedRestockingDetail>()
            .HasIndex(i => new { i.ProposedFellingDetailsId, i.PropertyProfileCompartmentId, i.RestockingProposal })
            .IsUnique();

        modelBuilder.Entity<ProposedRestockingDetail>()
            .Property(e => e.Area)
            .HasPrecision(18, 2);

        modelBuilder.Entity<ProposedRestockingDetail>()
            .Property(e => e.PercentageOfRestockArea)
            .HasPrecision(5, 2);

        modelBuilder
            .Entity<FellingSpecies>(e =>
            {
                e.HasOne(p => p.ProposedFellingDetail)
                    .WithMany(p => p.FellingSpecies)
                    .HasForeignKey(p => p.ProposedFellingDetailsId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        // Configuring one to one mapping for FellingOutcome -> ProposedFellingDetail
        // Reference property on ProposedFellingDetail is nullable to allow ProposedFellingDetail to
        // exist without FellingOutcome

        modelBuilder
            .Entity<FellingOutcome>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<FellingOutcome>(e =>
            {
                e.HasOne(p => p.ProposedFellingDetail)
                    .WithMany(p => p.FellingOutcomes)
                    .HasForeignKey(p => p.ProposedFellingDetailsId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder
            .Entity<RestockingSpecies>(e =>
            {
                e.HasOne(p => p.ProposedRestockingDetail)
                    .WithMany(p => p.RestockingSpecies)
                    .HasForeignKey(p => p.ProposedRestockingDetailsId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        // Configuring one to one mapping for FellingOutcome -> ProposedFellingDetail
        // Reference property on ProposedFellingDetail is nullable to allow ProposedFellingDetail to
        // exist without FellingOutcome

        modelBuilder
            .Entity<RestockingOutcome>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<RestockingOutcome>(e =>
            {
                e.HasOne(p => p.ProposedRestockingDetail)
                    .WithMany(p => p.RestockingOutcomes)
                    .HasForeignKey(p => p.ProposedRestockingDetailsId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder
            .Entity<SubmittedFlaPropertyDetail>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<SubmittedFlaPropertyCompartment>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        // Configure 1 - 1 mapping and foreign key from SubmittedFlaPropertyDetail -> FellingLicenceApplication
        // Reference property on FellingLicenceApplication is nullable to allow FellingLicenceApplication to
        // exist without SubmittedFlaPropertyDetail

        modelBuilder
            .Entity<SubmittedFlaPropertyDetail>(e =>
            {
                e.HasOne(p => p.FellingLicenceApplication).WithOne(x => x.SubmittedFlaPropertyDetail)
                 .HasForeignKey<SubmittedFlaPropertyDetail>(p => p.FellingLicenceApplicationId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

        // Configure Many - 1 mapping and foreign key from SubmittedFlaPropertyCompartment -> SubmittedFlaPropertyDetail
        // Reference property on SubmittedFlaPropertyDetail is nullable to allow SubmittedFlaPropertyDetail to
        // exist without SubmittedFlaPropertyCompartments

        modelBuilder
            .Entity<SubmittedFlaPropertyCompartment>(e =>
            {
                e.HasOne(p => p.SubmittedFlaPropertyDetail).WithMany(x => x.SubmittedFlaPropertyCompartments)
                 .HasForeignKey(p => p.SubmittedFlaPropertyDetailId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder.Entity<SubmittedFlaPropertyCompartment>()
            .Property(p => p.ConfirmedTotalHectares)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SubmittedFlaPropertyCompartment>()
            .Property(p => p.TotalHectares)
            .HasPrecision(18, 2);

        modelBuilder
            .Entity<WoodlandOfficerReview>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<WoodlandOfficerReview>()
            .Property(x => x.RecommendedLicenceDuration)
            .HasConversion<string>();

        // Configure 1 - 1 mapping and foreign key from WoodlandOfficerReview -> FellingLicenceApplication
        // Reference property on FellingLicenceApplication is nullable to allow FellingLicenceApplication to
        // exist without WoodlandOfficerReview

        modelBuilder
            .Entity<WoodlandOfficerReview>(e =>
            {
                e.HasOne(p => p.FellingLicenceApplication).WithOne(x => x.WoodlandOfficerReview)
                    .HasForeignKey<WoodlandOfficerReview>(p => p.FellingLicenceApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder
            .Entity<ApproverReview>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<ApproverReview>()
            .Property(x => x.ApprovedLicenceDuration)
            .HasConversion<string>();

        // Configure 1 - 1 mapping and foreign key from ApproverReview -> FellingLicenceApplication
        // Reference property on FellingLicenceApplication is nullable to allow FellingLicenceApplication to
        // exist without ApproverReview

        modelBuilder
            .Entity<ApproverReview>(e =>
            {
                e.HasOne(p => p.FellingLicenceApplication).WithOne(x => x.ApproverReview)
                    .HasForeignKey<ApproverReview>(p => p.FellingLicenceApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder
            .Entity<PublicRegister>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        // Configure 1 - 1 mapping and foreign key from PublicRegister -> FellingLicenceApplication
        // Reference property on FellingLicenceApplication is nullable to allow FellingLicenceApplication to
        // exist without PublicRegister

        modelBuilder
            .Entity<PublicRegister>(e =>
            {
                e.HasOne(p => p.FellingLicenceApplication).WithOne(x => x.PublicRegister)
                    .HasForeignKey<PublicRegister>(p => p.FellingLicenceApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder
            .Entity<ConfirmedFellingDetail>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        // Configure 1 - many mapping and foreign key from ConfirmedFellingDetails -> SubmittedFlaPropertyCompartment
        // Reference property on SubmittedFlaPropertyCompartment is nullable to allow SubmittedFlaPropertyCompartment to
        // exist without ConfirmedFellingDetails

        modelBuilder
            .Entity<ConfirmedFellingDetail>(e =>
            {
                e.HasOne(p => p.SubmittedFlaPropertyCompartment)
                    .WithMany(x => x.ConfirmedFellingDetails)
                    .HasForeignKey(p => p.SubmittedFlaPropertyCompartmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder.Entity<ConfirmedFellingDetail>()
            .Property(e => e.AreaToBeFelled)
            .HasPrecision(18, 2);

        modelBuilder
            .Entity<ConfirmedFellingSpecies>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<ConfirmedFellingSpecies>(e =>
            {
                e.HasOne(p => p.ConfirmedFellingDetail)
                    .WithMany(p => p.ConfirmedFellingSpecies)
                    .HasForeignKey(p => p.ConfirmedFellingDetailId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        // > A SubmittedFlaPropertyCompartment will have zero to 1 Confirmed Felling Details and zero to 1 Confirmed Restocking Details.

        modelBuilder
            .Entity<ConfirmedRestockingDetail>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<ConfirmedRestockingDetail>(e =>
            {
                e.HasOne(p => p.ConfirmedFellingDetail)
                    .WithMany(p => p.ConfirmedRestockingDetails)
                    .HasForeignKey(p => p.ConfirmedFellingDetailId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder.Entity<ConfirmedRestockingDetail>()
            .Property(e => e.Area)
            .HasPrecision(18, 2);

        modelBuilder.Entity<ConfirmedRestockingDetail>()
            .Property(e => e.PercentageOfRestockArea)
            .HasPrecision(5, 2);

        modelBuilder
            .Entity<ConfirmedRestockingSpecies>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<ConfirmedRestockingSpecies>(e =>
            {
                e.HasOne(p => p.ConfirmedRestockingDetail)
                    .WithMany(p => p.ConfirmedRestockingSpecies)
                    .HasForeignKey(p => p.ConfirmedRestockingDetailsId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder
            .Entity<FellingLicenceApplicationStepStatus>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<FellingLicenceApplicationStepStatus>()
            .Property(e => e.CompartmentFellingRestockingStatuses)
            .HasJsonConversion();

        // Configure 1 - 1 mapping and foreign key from FellingLicenceApplicationStepStatus -> FellingLicenceApplication

        modelBuilder
            .Entity<FellingLicenceApplicationStepStatus>(e =>
            {
                e.HasOne(p => p.FellingLicenceApplication).WithOne(x => x.FellingLicenceApplicationStepStatus)
                    .HasForeignKey<FellingLicenceApplicationStepStatus>(p => p.FellingLicenceApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder
            .Entity<AdminOfficerReview>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        // Configure 1 - 1 mapping and foreign key from AdminOfficerReview -> FellingLicenceApplication
        // Reference property on FellingLicenceApplication is nullable to allow FellingLicenceApplication to
        // exist without AdminOfficerReview

        modelBuilder
            .Entity<AdminOfficerReview>(e =>
            {
                e.HasOne(p => p.FellingLicenceApplication).WithOne(x => x.AdminOfficerReview)
                    .HasForeignKey<AdminOfficerReview>(p => p.FellingLicenceApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        // Configure 1 - 1 mapping and foreign key from LarchCheckDetails -> AdminOfficerReview
        modelBuilder
            .Entity<LarchCheckDetails>(e =>
            {
                e.Property(x => x.Id)
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("uuid_generate_v4()")
                    .IsRequired();

                e.HasOne(p => p.FellingLicenceApplication)
                    .WithOne(x => x.LarchCheckDetails)
                    .HasForeignKey<LarchCheckDetails>(p => p.FellingLicenceApplicationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


        modelBuilder
            .Entity<EnvironmentalImpactAssessment>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<EnvironmentalImpactAssessment>(e =>
            {
                e.HasOne(p => p.FellingLicenceApplication)
                    .WithOne(p => p.EnvironmentalImpactAssessment)
                    .OnDelete(DeleteBehavior.Cascade);
            });

        modelBuilder
            .Entity<EnvironmentalImpactAssessment>(e =>
            {
                e.HasMany(x => x.EiaRequests)
                    .WithOne(x => x.EnvironmentalImpactAssessment)
                    .HasForeignKey(x => x.EnvironmentalImpactAssessmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
    }

    public async Task<UnitResult<UserDbErrorReason>> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await base.SaveChangesAsync(cancellationToken);
            return UnitResult.Success<UserDbErrorReason>();
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            _logger?.LogError(
                "Error occured during the entity update, the entity is not unique, exception: {Exception}",
                ex.ToString());
            return UnitResult.Failure(UserDbErrorReason.NotUnique);
        }
    }
}