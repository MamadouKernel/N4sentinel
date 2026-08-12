using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Operations;

/// <summary>Verdict de complétude d'une demande d'opération.</summary>
public sealed record VerdictDeDemande(bool Complete, IReadOnlyList<string> ChampsManquants);

/// <summary>
/// FR-014 — motif, fenêtre d'intervention, périmètre, impact et référence d'incident,
/// obligatoires en Production. Le motif seul est exigé partout ailleurs : aucune opération ne
/// se lance sans justification tracée, où qu'elle s'exécute.
/// </summary>
public static class ValidateurDeDemande
{
    public static VerdictDeDemande Evaluer(
        EnvironmentType typeDEnvironnement,
        string? motif,
        string? referenceIncident,
        DateTimeOffset? fenetreDebut,
        DateTimeOffset? fenetreFin,
        string? perimetre,
        string? impactAttendu)
    {
        var manquants = new List<string>();

        if (string.IsNullOrWhiteSpace(motif))
        {
            manquants.Add("Motif");
        }

        if (typeDEnvironnement == EnvironmentType.Production)
        {
            if (string.IsNullOrWhiteSpace(referenceIncident))
            {
                manquants.Add("Référence d'incident");
            }

            if (fenetreDebut is null || fenetreFin is null)
            {
                manquants.Add("Fenêtre d'intervention");
            }
            else if (fenetreFin <= fenetreDebut)
            {
                manquants.Add("Fenêtre d'intervention (fin antérieure ou égale au début)");
            }

            if (string.IsNullOrWhiteSpace(perimetre))
            {
                manquants.Add("Périmètre");
            }

            if (string.IsNullOrWhiteSpace(impactAttendu))
            {
                manquants.Add("Impact attendu");
            }
        }

        return new VerdictDeDemande(manquants.Count == 0, manquants);
    }
}
