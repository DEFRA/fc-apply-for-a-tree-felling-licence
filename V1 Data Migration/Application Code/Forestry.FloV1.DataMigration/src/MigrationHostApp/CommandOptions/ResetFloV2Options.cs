using CommandLine;

namespace MigrationHostApp.CommandOptions;

[Verb("reset", HelpText = "Resets the target environment, such as clearing FloV2 db tables.")]
public class ResetFloV2Options : ICommandOptions
{
    //todo could have cmd options for db and or azure, or 1 or other etc etc.
}