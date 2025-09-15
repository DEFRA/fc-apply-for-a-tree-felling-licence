namespace Forestry.Flo.Services.FileStorage.Configuration
{
    /// <summary>
    /// Configuration used to represent the restrictions on user uploads within an application/service.
    /// </summary>
    public class UserFileUploadOptions
    {
        public int MaxFileSizeBytes { get; set; } = 4194304;

        public int MaxNumberDocuments { get; set; } = 10;

        public AllowedFileType[] AllowedFileTypes { get; set; } = {
            new()
            {
                FileUploadReasons = [FileUploadReason.AgentAuthorityForm, FileUploadReason.SupportingDocument, FileUploadReason.EiaDocument], 
                Extensions = ["JPG", "JPEG", "PNG"], 
                Description = "Image"
            },
            new()
            {
                FileUploadReasons = [FileUploadReason.AgentAuthorityForm, FileUploadReason.SupportingDocument, FileUploadReason.EiaDocument],
                Extensions = ["DOC", "DOCX"], 
                Description = "Word document"
            },
            new()
            {
                FileUploadReasons = [FileUploadReason.SupportingDocument],
                Extensions = ["TXT", "RTF"], 
                Description = "Text file"
            },
            new()
            {
                FileUploadReasons =[FileUploadReason.SupportingDocument], 
                Extensions =["XLS", "XLSX"], 
                Description = "Excel spreadsheet"
            },
            new()
            {
                FileUploadReasons = [FileUploadReason.SupportingDocument],
                Extensions = ["CSV"], 
                Description = "CSV file"
            },
            new()
            {
                FileUploadReasons = [FileUploadReason.AgentAuthorityForm, FileUploadReason.SupportingDocument, FileUploadReason.EiaDocument],
                Extensions = ["PDF"], 
                Description = "PDF document"
            },
            new()
            {
                FileUploadReasons =[FileUploadReason.SupportingDocument],
                Extensions = ["MSG", "EML"], 
                Description = "Outlook email"
            }
        };
    }
    
    public class AllowedFileType
    {
        public FileUploadReason[] FileUploadReasons { get; set; }
        public string[] Extensions { get; set; }
        public string Description { get; set; }
    }

    public enum FileUploadReason
    {
        AgentAuthorityForm,
        SupportingDocument,
        EiaDocument
    }
}
