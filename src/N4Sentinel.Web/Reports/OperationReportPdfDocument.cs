using N4Sentinel.Application.Sops.Dtos;
using N4Sentinel.Web.Formatting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace N4Sentinel.Web.Reports;

/// <summary>FR-090 : export PDF réel d'un rapport d'opération (alternative au JSON structuré du Sprint 15).</summary>
public sealed class OperationReportPdfDocument(OperationReportDto report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Text("Rapport d'opération — N4 Sentinel").SemiBold().FontSize(16);

            page.Content().Column(column =>
            {
                column.Spacing(8);

                column.Item().Text($"Opération : {report.OperationRunId}");
                column.Item().Text($"Statut : {report.Status.ToLabel()}");
                column.Item().Text($"Version de workflow : v{report.WorkflowVersionNumber}");
                column.Item().Text($"Demandée par {report.RequestedByUserId} le {report.RequestedAtUtc:g}");

                if (!string.IsNullOrEmpty(report.Motif))
                {
                    column.Item().Text($"Motif : {report.Motif}");
                }

                if (!string.IsNullOrEmpty(report.IncidentOrChangeReference))
                {
                    column.Item().Text($"Référence incident/changement : {report.IncidentOrChangeReference}");
                }

                if (report.ApprovedByUserId is not null)
                {
                    column.Item().Text($"Approuvée par {report.ApprovedByUserId} le {report.ApprovedAtUtc:g}");
                }

                if (report.Duration is not null)
                {
                    column.Item().Text($"Durée : {report.Duration:hh\\:mm\\:ss}");
                }

                column.Item().PaddingTop(10).Text("Chronologie des étapes").SemiBold().FontSize(12);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(20);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("#").SemiBold();
                        header.Cell().Text("Étape").SemiBold();
                        header.Cell().Text("Composant").SemiBold();
                        header.Cell().Text("Statut").SemiBold();
                        header.Cell().Text("Résultat").SemiBold();
                    });

                    foreach (var step in report.Steps)
                    {
                        table.Cell().Text(step.Position.ToString());
                        table.Cell().Text(step.Name);
                        table.Cell().Text(step.ComponentName ?? "—");
                        table.Cell().Text(step.Status.ToString());
                        table.Cell().Text(BuildStepResultText(step));
                    }
                });

                if (report.AssociatedSops.Count > 0)
                {
                    column.Item().PaddingTop(10).Text("SOP associées").SemiBold().FontSize(12);
                    foreach (var sop in report.AssociatedSops)
                    {
                        column.Item().Text($"{sop.SopTitle} (v{sop.SopVersionNumber}) — {sop.Result ?? "—"}");
                    }
                }
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Généré le ");
                x.Span(DateTime.UtcNow.ToLocalTime().ToString("g"));
                x.Span(" — N4 Sentinel");
            });
        });
    }

    private static string BuildStepResultText(OperationReportStepDto step)
    {
        var text = step.ResultMessage ?? "—";
        if (step.OverrideReason is not null)
        {
            text += $" [Contourné par {step.OverriddenByUserId} — motif : {step.OverrideReason}]";
        }

        return text;
    }
}
