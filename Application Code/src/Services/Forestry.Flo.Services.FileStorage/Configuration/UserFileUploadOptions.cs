namespace Forestry.Flo.Services.FileStorage.Configuration
{
    /// <summary>
    /// Configuration used to represent the restrictions on user uploads within an application/service.
    /// </summary>
    public class UserFileUploadOptions
    {
        /// <summary>
        /// The maximum upload size that the server will accept.
        /// </summary>
        public int ServerMaxUploadSizeBytes { get; set; } = 33554432;  // 32MB max upload size for IIS and Kestrel servers.

        /// <summary>
        /// The maximum file size that the application will accept for a single uploaded supporting document.
        /// </summary>
        /// <remarks>
        /// Only applies to Supporting Documents, EIA documents and Agent Authority forms, not other file types that can be
        /// uploaded in the External app such as WMP documents.
        /// </remarks>
        public int MaxFileSizeBytes { get; set; } = 4194304;  // 4MB max file size for a single uploaded supporting document.

        /// <summary>
        /// The maximum number of supporting documents that can be uploaded to one application.
        /// </summary>
        /// <remarks>
        /// Only applies to Supporting Documents, EIA documents and Agent Authority forms, not other file types that can be
        /// uploaded in the External app such as WMP documents.
        /// </remarks>
        public int MaxNumberDocuments { get; set; } = 10;

        /// <summary>
        /// The allowed file types that can be uploaded by users, for supporting documents, EIA documents and WMP documents.
        /// </summary>
        public AllowedFileType[] AllowedFileTypes { get; set; } = {
            new()
            {
                FileUploadReasons = [FileUploadReason.AgentAuthorityForm, FileUploadReason.SupportingDocument, FileUploadReason.EiaDocument, FileUploadReason.WmpDocument],
                Extensions = ["JPG", "JPEG", "PNG"],
                Description = "Image"
            },
            new()
            {
                FileUploadReasons = [FileUploadReason.AgentAuthorityForm, FileUploadReason.SupportingDocument, FileUploadReason.EiaDocument, FileUploadReason.WmpDocument],
                Extensions = ["DOC", "DOCX"],
                Description = "Word document"
            },
            new()
            {
                FileUploadReasons = [FileUploadReason.SupportingDocument, FileUploadReason.WmpDocument],
                Extensions = ["TXT", "RTF"],
                Description = "Text file"
            },
            new()
            {
                FileUploadReasons =[FileUploadReason.SupportingDocument, FileUploadReason.WmpDocument],
                Extensions =["XLS", "XLSX"],
                Description = "Excel spreadsheet"
            },
            new()
            {
                FileUploadReasons = [FileUploadReason.SupportingDocument, FileUploadReason.WmpDocument],
                Extensions = ["CSV"],
                Description = "CSV file"
            },
            new()
            {
                FileUploadReasons = [FileUploadReason.AgentAuthorityForm, FileUploadReason.SupportingDocument, FileUploadReason.EiaDocument, FileUploadReason.WmpDocument],
                Extensions = ["PDF"],
                Description = "PDF document"
            },
            new()
            {
                FileUploadReasons =[FileUploadReason.SupportingDocument, FileUploadReason.WmpDocument],
                Extensions = ["MSG", "EML"],
                Description = "Outlook email"
            }
        };
    }

    /// <summary>
    /// Model class representing details of an allowed file type for upload.
    /// </summary>
    public class AllowedFileType
    {
        /// <summary>
        /// The reasons for which this file type is allowed to be uploaded.
        /// </summary>
        public FileUploadReason[] FileUploadReasons { get; set; }

        /// <summary>
        /// The allowed file extensions for this file type, without the leading dot, e.g. "PDF", "DOCX".
        /// </summary>
        public string[] Extensions { get; set; }

        /// <summary>
        /// A short description of the file type, e.g. "PDF document".
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// The reason for a file upload, used to determine which file types are allowed.
    /// </summary>
    public enum FileUploadReason
    {
        AgentAuthorityForm,
        SupportingDocument,
        EiaDocument,
        WmpDocument
    }
}
