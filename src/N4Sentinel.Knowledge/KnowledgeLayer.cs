using System.Reflection;

namespace N4Sentinel.Knowledge;

/// <summary>
/// §3.15 — couche Connaissance : indexation, recherche, réponses sourcées et gestion
/// des versions. Une réponse sans source citable n'est pas une réponse recevable.
/// </summary>
public static class KnowledgeLayer
{
    public static Assembly Assembly => typeof(KnowledgeLayer).Assembly;
}
