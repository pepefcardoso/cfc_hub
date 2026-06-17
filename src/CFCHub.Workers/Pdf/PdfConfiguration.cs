using QuestPDF.Infrastructure;

namespace CFCHub.Workers.Pdf;

public static class PdfConfiguration
{
    public static void Configure()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }
}
