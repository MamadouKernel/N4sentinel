namespace N4Sentinel.Web.Configuration;

/// <summary>
/// Activation de modules dans la navigation, configurable sans recompilation via la section
/// <c>Features</c> de appsettings.json — utile pour un déploiement restreint (ex. un environnement
/// pilote sans module Environnements encore validé par la DSI).
/// </summary>
public sealed class FeatureOptions
{
    public const string SectionName = "Features";

    public bool Environments { get; set; } = true;
}
