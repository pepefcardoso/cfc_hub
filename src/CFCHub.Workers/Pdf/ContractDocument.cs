using System;
using CFCHub.Workers.Outbox.Handlers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CFCHub.Workers.Pdf;

public class ContractDocument : IDocument
{
    private readonly ContractGenerationRequested _payload;

    public ContractDocument(ContractGenerationRequested payload)
    {
        _payload = payload;
    }

    public DocumentSettings GetSettings()
    {
        var settings = DocumentSettings.Default;
        settings.PdfA = true;
        return settings;
    }

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("CFCHub").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text("Contrato de Prestação de Serviços").FontSize(14).SemiBold();
            });
            // Logo placeholder loaded from S3 or config as per requirements
            row.ConstantItem(100).Height(50).Placeholder();
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(10);

            column.Item().Text("Dados do Aluno").FontSize(14).SemiBold();
            column.Item().Text($"Nome: {_payload.StudentName}");
            column.Item().Text($"Email: {_payload.StudentEmail}");
            column.Item().Text($"Categoria Pretendida: {_payload.CnhCategory}");
            column.Item().Text($"Data de Matrícula: {_payload.EnrollmentDate:dd/MM/yyyy}");
            
            column.Item().PaddingTop(10).Text("Objeto do Contrato").FontSize(14).SemiBold();
            column.Item().Text($"O presente contrato tem por objeto a prestação de serviços de formação de condutores na categoria {_payload.CnhCategory}.");
            column.Item().Text($"Valor total acordado: {_payload.TotalAmount:C}");

            column.Item().PaddingTop(10).Text("Cláusula LGPD").FontSize(14).SemiBold();
            column.Item().Text("O aluno consente com o tratamento de seus dados pessoais para as finalidades descritas na Política de Privacidade, de acordo com a Lei Geral de Proteção de Dados (Lei 13.709/2018).");
            column.Item().Text($"Versão da Política: {_payload.PolicyVersion ?? "v1.0"} | Hash de Aceite: {_payload.PolicyContentHash ?? "N/A"}");

            column.Item().PaddingTop(30).Text("Assinaturas").FontSize(14).SemiBold();
            column.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().LineHorizontal(1);
                    c.Item().Text("Assinatura do Aluno").AlignCenter();
                });
                row.ConstantItem(50);
                row.RelativeItem().Column(c =>
                {
                    c.Item().LineHorizontal(1);
                    c.Item().Text("CFC").AlignCenter();
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Página ");
            x.CurrentPageNumber();
            x.Span(" de ");
            x.TotalPages();
        });
    }
}
