using System.Reflection;

namespace N4Sentinel.Architecture.Tests;

/// <summary>
/// §3.15 — le découpage en couches n'est pas qu'un dossier : ces tests échouent dès qu'une
/// couche référence une couche qu'elle n'a pas le droit de connaître.
///
/// Limite assumée : le compilateur élague les références de projet non utilisées, donc ce test
/// détecte les violations effectives (du code d'une couche utilise une autre couche), pas les
/// références déclarées mais inertes. C'est le comportement voulu : c'est l'usage qui crée le couplage.
/// </summary>
public class LayeringTests
{
    private const string Prefixe = "N4Sentinel.";

    private static readonly Assembly Domain = typeof(Domain.Common.Entity).Assembly;
    private static readonly Assembly Application = typeof(Application.ApplicationLayer).Assembly;
    private static readonly Assembly Orchestration = typeof(Orchestration.OrchestrationLayer).Assembly;
    private static readonly Assembly Connectors = typeof(Connectors.ConnectorsLayer).Assembly;
    private static readonly Assembly Diagnostics = typeof(Diagnostics.DiagnosticsLayer).Assembly;
    private static readonly Assembly Knowledge = typeof(Knowledge.KnowledgeLayer).Assembly;
    private static readonly Assembly Data = typeof(Data.DataLayer).Assembly;

    [Fact]
    public void Le_domaine_ne_depend_d_aucune_autre_couche()
    {
        Assert.Empty(CouchesReferenceesPar(Domain));
    }

    [Fact]
    public void La_couche_application_ne_depend_que_du_domaine()
    {
        var interdites = CouchesReferenceesPar(Application)
            .Except(["N4Sentinel.Domain"])
            .ToList();

        Assert.Empty(interdites);
    }

    [Theory]
    [InlineData(nameof(Orchestration))]
    [InlineData(nameof(Connectors))]
    [InlineData(nameof(Diagnostics))]
    [InlineData(nameof(Knowledge))]
    [InlineData(nameof(Data))]
    public void Les_couches_techniques_ne_se_connaissent_pas_entre_elles(string nomDeCouche)
    {
        var couche = nomDeCouche switch
        {
            nameof(Orchestration) => Orchestration,
            nameof(Connectors) => Connectors,
            nameof(Diagnostics) => Diagnostics,
            nameof(Knowledge) => Knowledge,
            _ => Data
        };

        var autorisees = new[] { "N4Sentinel.Application", "N4Sentinel.Domain" };
        var interdites = CouchesReferenceesPar(couche).Except(autorisees).ToList();

        Assert.Empty(interdites);
    }

    private static List<string> CouchesReferenceesPar(Assembly assembly) =>
        assembly.GetReferencedAssemblies()
            .Select(a => a.Name!)
            .Where(name => name.StartsWith(Prefixe, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();
}
