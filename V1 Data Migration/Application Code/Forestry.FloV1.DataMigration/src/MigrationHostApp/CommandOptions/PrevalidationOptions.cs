using CommandLine;

namespace MigrationHostApp.CommandOptions;

[Verb("prevalidate", HelpText = "Pre-validate a specific migration's source data, such as FloV1 user data-set")]
public class PrevalidationOptions : BaseVerbOptions, ICommandOptions { }