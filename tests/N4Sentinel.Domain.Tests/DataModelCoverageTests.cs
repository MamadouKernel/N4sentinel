using N4Sentinel.Domain.Common;
using N4Sentinel.Domain.Entities;

namespace N4Sentinel.Domain.Tests;

/// <summary>
/// Traçabilité du §3.18 : le modèle de données doit couvrir au minimum les quinze lignes
/// d'entités listées par le cahier des charges. Deux d'entre elles — « Workflow / Version »
/// et « SOP / Version » — se traduisent par deux types chacune, d'où dix-sept types attendus.
///
/// La présence des types est vérifiée par le compilateur : supprimer une entité du domaine
/// empêche ce projet de test de compiler. Les tests ci-dessous vérifient ce que la compilation
/// ne dit pas — la cohérence de la racine commune et le compte attendu.
/// </summary>
public class DataModelCoverageTests
{
    private static readonly (string LigneDuCdc, Type Type)[] EntitesDu318 =
    [
        ("Environnement", typeof(N4Environment)),
        ("Composant", typeof(N4Component)),
        ("Workflow / Version", typeof(Workflow)),
        ("Workflow / Version", typeof(WorkflowVersion)),
        ("Exécution", typeof(OperationExecution)),
        ("Étape d'exécution", typeof(ExecutionStep)),
        ("Contrôle / Signal", typeof(ControlSignal)),
        ("Incident / Diagnostic", typeof(DiagnosticCase)),
        ("Log importé", typeof(ImportedLogFile)),
        ("Règle de diagnostic", typeof(DiagnosticRule)),
        ("Document", typeof(KnowledgeDocument)),
        ("Shared Folder", typeof(SharedFolder)),
        ("Fichier d'interface / EDI", typeof(EdiFile)),
        ("SOP / Version", typeof(Sop)),
        ("SOP / Version", typeof(SopVersion)),
        ("Association SOP", typeof(SopAssociation)),
        ("Audit", typeof(AuditEntry))
    ];

    [Fact]
    public void Les_quinze_lignes_du_paragraphe_3_18_sont_couvertes()
    {
        var lignesCouvertes = EntitesDu318
            .Select(e => e.LigneDuCdc)
            .Distinct()
            .ToList();

        Assert.Equal(15, lignesCouvertes.Count);
    }

    [Theory]
    [MemberData(nameof(TypesDuModele))]
    public void Chaque_entite_derive_de_la_racine_commune(Type type)
    {
        Assert.True(
            typeof(Entity).IsAssignableFrom(type),
            $"{type.Name} doit dériver de {nameof(Entity)} pour disposer d'un identifiant persistable.");
    }

    [Fact]
    public void Chaque_entite_recoit_un_identifiant_a_la_construction()
    {
        var execution = new OperationExecution
        {
            Reference = "OP-0001",
            DemandePar = "operateur.test",
            Motif = "Vérification du socle",
            ReferenceDeCorrelation = "COR-0001"
        };

        Assert.NotEqual(Guid.Empty, execution.Id);
    }

    [Fact]
    public void Les_identifiants_generes_sont_des_guid_v7()
    {
        // Le préfixe horodaté d'un GUID v7 limite la fragmentation des index en base.
        // Une régression vers Guid.NewGuid() (v4) casse ce test.
        // Note : v7 n'ordonne pas deux identifiants créés dans la même milliseconde —
        // seule la version est vérifiée ici, pas un ordre total.
        var entree = new AuditEntry { Acteur = "a", Action = "b", TypeDObjet = "c" };

        var octets = entree.Id.ToByteArray(bigEndian: true);
        var version = octets[6] >> 4;

        Assert.Equal(7, version);
    }

    public static TheoryData<Type> TypesDuModele()
    {
        var data = new TheoryData<Type>();
        foreach (var entite in EntitesDu318)
        {
            data.Add(entite.Type);
        }

        return data;
    }
}
