using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using CFCHub.Workers.Outbox.Handlers;

namespace CFCHub.Workers.Pdf;

public class PaymentReceiptDocument : IDocument
{
    private readonly PaymentReceiptRequested _payload;

    public PaymentReceiptDocument(PaymentReceiptRequested payload)
    {
        _payload = payload;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(12));

            page.Header().Text("Payment Receipt").SemiBold().FontSize(20).FontColor(Colors.Blue.Darken2);

            page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
            {
                x.Spacing(10);
                x.Item().Text($"Receipt ID: {_payload.PaymentId}");
                x.Item().Text($"Date: {_payload.PaymentDate:d}");
                x.Item().Text($"Student: {_payload.StudentName}");
                x.Item().Text($"Payment Method: {_payload.PaymentMethod}");
                x.Item().Text($"Amount Paid: R$ {_payload.Amount:F2}").Bold();
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
                x.Span(" of ");
                x.TotalPages();
            });
        });
    }
}
