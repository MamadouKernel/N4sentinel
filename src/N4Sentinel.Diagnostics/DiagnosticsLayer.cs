using System.Reflection;

namespace N4Sentinel.Diagnostics;

/// <summary>
/// §3.15 — couche Diagnostic : normalisation, corrélation, règles, score de confiance
/// et explication. Le score n'est jamais publié sans l'explication qui le justifie.
/// </summary>
public static class DiagnosticsLayer
{
    public static Assembly Assembly => typeof(DiagnosticsLayer).Assembly;
}
