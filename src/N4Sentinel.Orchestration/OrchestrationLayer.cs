using System.Reflection;

namespace N4Sentinel.Orchestration;

/// <summary>
/// §3.15 — couche Orchestrateur : dépendances, état persistant, timeout, retry, pause,
/// reprise et compensation. L'état vit en base, jamais en mémoire seule : le moteur doit
/// reprendre une exécution après un redémarrage de la solution.
/// </summary>
public static class OrchestrationLayer
{
    public static Assembly Assembly => typeof(OrchestrationLayer).Assembly;
}
