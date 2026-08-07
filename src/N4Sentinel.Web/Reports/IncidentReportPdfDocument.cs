using N4Sentinel.Application.Sops.Dtos;
using N4Sentinel.Web.Formatting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace N4Sentinel.Web.Reports;

/// <summary>FR-090 : export PDF réel d'un rapport d'incident (alternative au JSON structuré du Sprint 15).</summary>
public sealed class IncidentReportPdfDocument(IncidentReportDto report) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Text("Rapport d'incident — N4 Sentinel").SemiBold().FontSize(16);

            page.Content().Column(column =>
            {
                column.Spacing(8);

                column.Item().Text(report.Symptom).SemiBold().FontSize(13);
                column.Item().Text($"Détecté le {report.DetectedAtUtc:g}");
                column.Item().Text($"Période analysée : {report.PeriodStartUtc:g} → {report.PeriodEndUtc:g}");

                if (report.ConcludedAtUtc is not null)
                {
                    column.Item().Text($"Conclu le {report.ConcludedAtUtc:g}");
                }

                if (report.Duration is not null)
                {
                    column.Item().Text($"Durée : {report.Duration:hh\\:mm\\:ss}");
                }

                column.Item().Text($"Demandé par {report.RequestedByUserId}");

                if (report.ConclusionLevel is not null)
                {
                    column.Item().Text($"Conclusion : {report.ConclusionLevel.Value.ToLabel()}");
                }

                if (!string.IsNullOrEmpty(report.ConclusionSummary))
                {
                    column.Item().Text($"Synthèse : {report.ConclusionSummary}");
                }

                column.Item().PaddingTop(10).Text("Hypothèses évaluées").SemiBold().FontSize(12);
                foreach (var h in report.Hypotheses)
                {
                    column.Item().Column(hc =>
                    {
                        hc.Item().Text($"[{h.Domain.ToLabel()}] {h.CauseDescription} — confiance {h.ConfidenceLevel.ToLabel()}").SemiBold();
                        if (!string.IsNullOrEmpty(h.SafeActionsOrEscalation))
                        {
                            hc.Item().Text($"Action recommandée : {h.SafeActionsOrEscalation}").FontSize(9);
                        }
                    });
                }

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
}
