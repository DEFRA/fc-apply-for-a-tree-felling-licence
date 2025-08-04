using CommandLine;

namespace MigrationHostApp.CommandOptions;

[Verb("migrate", HelpText = "Migrates specific entity/entities")]
public class MigrationOptions : BaseVerbOptions, ICommandOptions
{
    [Option(
        't', "testmode",
        Default = false,
        HelpText = "Use test mode, no actual data or files are migrated.")]
    public bool EnableTestMode { get; set; }

    [Option(
        'd', "disableprevalidation",
        Default = false,
        HelpText = "Run the migration without performing pre-validation first.")]
    public bool DisablePreValidation { get; set; }

    [Option(
        'e', "UserEmailAddressToUse",
        Required = false,
        HelpText = "Perform user inserts using the specified email address with +[floV1UserId} suffix before the domain")]
    public string? EmailAddressToUse { get; set; }


}