namespace MigrationHostApp;

[Flags]
public enum SourceDataType
{
    ExternalUsers = 0x0,
    ManagedOwners = 0x1,
    AgentAuthorities = 0x2,
    AgentAuthorityForms = 0x3
    //etc.
}