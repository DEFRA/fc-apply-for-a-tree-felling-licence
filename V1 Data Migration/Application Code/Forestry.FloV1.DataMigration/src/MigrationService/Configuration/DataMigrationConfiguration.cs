namespace MigrationService.Configuration;

public sealed class DataMigrationConfiguration
{
    public DatabaseStorage DatabaseStorage { get; set; }
    public AzureBlobStorage AzureBlobStorage { get; set; }
    public ParallelismSettings ParallelismSettings { get; set; }
}

public sealed class DatabaseStorage
{
    /// <summary>
    /// The source database connection for FLOv1
    /// </summary>
    public string V1DefaultConnection { get; set; } = null!;

    /// <summary>
    /// The target database connection for FLOv2
    /// </summary>
    public string V2DefaultConnection { get; set; } = null!;
}

public sealed class AzureBlobStorage
{
    public string ConnectionString { get; set; } = null!;
    public string ContainerName { get; set; } = null!;
}

public sealed class ParallelismSettings
{
    public int MaxDegreeOfParallelism { get; set; } = 16;
}