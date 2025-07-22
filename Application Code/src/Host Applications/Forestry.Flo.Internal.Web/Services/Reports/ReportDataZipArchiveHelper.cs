using CsvHelper;
using Forestry.Flo.Services.FellingLicenceApplications.Models.Reports;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using CsvHelper.TypeConversion;
using Microsoft.IO;

namespace Forestry.Flo.Internal.Web.Services.Reports;

public static class ReportDataZipArchiveHelper
{
    private const string DownloadFileEntryExtension = "csv";
    private const string DownloadFileMimeType = "application/zip";

    public static List<ReportZipEntryInfo> GetAllReportsForArchive(
        FellingLicenceApplicationsReportQueryResultModel reportData, 
        RecyclableMemoryStreamManager streamManager)
    {
        var reportZipEntryInfos = new List<ReportZipEntryInfo>();

        //todo - could do with refactoring - so only hold one report data stream in memory at a time, disposing after report data added to zip.
     
        var flaReportsInfo = CreateReportEntry(reportData.FellingLicenceApplicationReportEntries, streamManager,
            "FellingLicenceApplicationReport");

        var consultationsReportInfo = CreateReportEntry(reportData.ConsultationPublicRegisterExemptCases, streamManager,
            "ConsultationPublicRegisterExemptionReport");

        var submittedPropertyProfilesReportInfo = CreateReportEntry(reportData.SubmittedPropertyProfileReportEntries,
            streamManager, "SubmittedPropertyProfileReport");

        var confirmedCompartmentDetailReportInfo = CreateReportEntry(reportData.ConfirmedCompartmentDetailReportEntries,
            streamManager, "ConfirmedCompartmentDetailReport");

        var proposedCompartmentDetailReportInfo = CreateReportEntry(reportData.ProposedCompartmentDetailReportEntries,
            streamManager, "ProposedCompartmentDetailReport");

        reportZipEntryInfos.Add(flaReportsInfo);
        reportZipEntryInfos.Add(consultationsReportInfo);
        reportZipEntryInfos.Add(submittedPropertyProfilesReportInfo);
        reportZipEntryInfos.Add(confirmedCompartmentDetailReportInfo);
        reportZipEntryInfos.Add(proposedCompartmentDetailReportInfo);

        return reportZipEntryInfos;
    }

    public static async Task<IActionResult> CreateZipArchiveForDataDownloadAsync(
        List<ReportZipEntryInfo> reportArchiveInformation,
        RecyclableMemoryStreamManager streamManager,
        string archiveName = "export.zip")
    {
        var zipStream = streamManager.GetStream();

        using (var zipFile = new ZipArchive(zipStream, ZipArchiveMode.Update, leaveOpen: true))
        {
            foreach (var reportArchiveInfo in reportArchiveInformation)
            {
                await AddEntryToArchive(zipFile, reportArchiveInfo.DataMemoryStream,
                    reportArchiveInfo.ReportName);
            }
        }

        zipStream.Seek(0, SeekOrigin.Begin);
        return new FileStreamResult(zipStream, DownloadFileMimeType) { FileDownloadName = archiveName };
        
        async Task AddEntryToArchive(ZipArchive archive, Stream memoryStream, string fileName)
        {
            var entry = archive.CreateEntry(fileName);
            await using var entryStream = entry.Open();
            await memoryStream.CopyToAsync(entryStream);
        }
    }

    /// <summary>
    /// Creates object required to be added to Zip archive.
    /// </summary>
    /// <param name="reportDataSet">Object containing the report data to be saved to file</param>
    /// <param name="streamManager">Memory stream manager</param>
    /// <param name="fileName">The name of the file entry, to be contained in the zip archive</param>
    /// <returns></returns>
    private static ReportZipEntryInfo CreateReportEntry(object reportDataSet, RecyclableMemoryStreamManager streamManager, string fileName)
    {
        return new ReportZipEntryInfo
        {
            DataMemoryStream = streamManager.GetStream(WriteCsvToMemory(reportDataSet, streamManager)),
            ReportName = $"{fileName}.{DownloadFileEntryExtension}"
        };
    }

    private static byte[] WriteCsvToMemory(dynamic records, RecyclableMemoryStreamManager streamManager)
    {
        using (var memoryStream = streamManager.GetStream())
        using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
        using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
        {
            var options = new TypeConverterOptions { Formats = new[] { "dd/MMM/yyyy" } };
            csvWriter.Context.TypeConverterOptionsCache.AddOptions<DateTime>(options);
            csvWriter.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);

            csvWriter.WriteRecords(records);
            streamWriter.Flush();
            csvWriter.Flush();
            return memoryStream.ToArray();
        }
    }
}
