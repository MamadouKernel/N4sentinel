using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — SOP / Version : objectif, prérequis, étapes, contrôles, risques,
/// retour arrière, validation, version N4 et statut.
/// </summary>
public class Sop : Entity
{
    public required string Cle { get; set; }

    public required string Titre { get; set; }

    public DiagnosticDomain Domaine { get; set; } = DiagnosticDomain.Inconnu;

    public List<SopVersion> Versions { get; set; } = [];
}

/// <summary>Version figée d'une procédure opératoire. Seule une version Active est exécutable.</summary>
public class SopVersion : Entity
{
    public Guid SopId { get; set; }

    public int NumeroDeVersion { get; set; } = 1;

    public required string Objectif { get; set; }

    public string? Prerequis { get; set; }

    /// <summary>Contrôles à effectuer pour vérifier que la procédure a produit l'effet attendu.</summary>
    public string? Controles { get; set; }

    public string? Risques { get; set; }

    /// <summary>Procédure de retour arrière si la SOP ne peut pas être menée à son terme.</summary>
    public string? RetourArriere { get; set; }

    public string? VersionN4 { get; set; }

    public ValidationStatus Statut { get; set; } = ValidationStatus.Brouillon;

    public DateTimeOffset CreeeLe { get; set; } = DateTimeOffset.UtcNow;

    public required string CreeePar { get; set; }

    public DateTimeOffset? ValideeLe { get; set; }

    public string? ValideePar { get; set; }

    /// <summary>Exécution dont cette version a été capitalisée, lorsqu'elle en est issue.</summary>
    public Guid? IssueDeLExecutionId { get; set; }

    public List<SopStepDefinition> Etapes { get; set; } = [];
}

/// <summary>Étape d'une SOP : action guidée, jamais exécutée automatiquement sans confirmation.</summary>
public class SopStepDefinition : Entity
{
    public Guid SopVersionId { get; set; }

    public int Ordre { get; set; }

    public required string Libelle { get; set; }

    public required string Instruction { get; set; }

    public string? ControleAttendu { get; set; }

    /// <summary>L'étape exige une preuve saisie par l'opérateur avant de passer à la suivante.</summary>
    public bool PreuveRequise { get; set; }
}
