using System.Reflection;

namespace N4Sentinel.Application;

/// <summary>
/// §3.15 — couche API / Domaine : règles métier, rôles, validation, exposition contrôlée des données.
/// Repère d'assemblage servant à l'enregistrement des services de cette couche.
/// </summary>
public static class ApplicationLayer
{
    public static Assembly Assembly => typeof(ApplicationLayer).Assembly;
}
