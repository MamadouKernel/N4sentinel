namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Niveau de confiance d'une hypothèse (FR-062 : "calculé selon les règles validées et les preuves
/// disponibles"). La méthode de calcul reste la description libre portée par la <see cref="DiagnosticRule"/>
/// appliquée (FR-065) — il n'existe pas de moteur de scoring automatique en V1 ; c'est l'utilisateur habilité
/// qui qualifie le niveau en s'appuyant sur cette méthode et les preuves qu'il a rassemblées.
/// </summary>
public enum DiagnosticConfidenceLevel
{
    Low,
    Medium,
    High,
}
