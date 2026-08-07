namespace N4Sentinel.Web.Configuration;

/// <summary>
/// Image de marque affichée dans la navbar, configurable sans recompilation via la section
/// <c>Branding</c> de appsettings.json.
/// </summary>
public sealed class BrandingOptions
{
    public const string SectionName = "Branding";

    public string ApplicationName { get; set; } = "N4 Sentinel";

    public string OrganizationName { get; set; } = "Côte d'Ivoire Terminal";
}
