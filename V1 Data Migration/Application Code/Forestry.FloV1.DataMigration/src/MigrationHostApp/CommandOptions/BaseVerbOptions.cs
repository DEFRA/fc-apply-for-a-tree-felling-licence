using CommandLine;

namespace MigrationHostApp.CommandOptions;

public class BaseVerbOptions
{
    [Option(
        'm', "migration",
        Required = true,
        HelpText = "The migration to be invoked.")]
    public SourceDataType SourceDataType { get; set; }
}