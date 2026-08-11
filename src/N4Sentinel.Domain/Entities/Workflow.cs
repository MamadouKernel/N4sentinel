using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — Workflow / Version : type, étapes, conditions, timeouts, retries, validations.
/// Le workflow porte l'identité stable ; le contenu exécutable vit dans ses versions.
/// </summary>
public class Workflow : Entity
{
    public Guid EnvironmentId { get; set; }

    public required string Nom { get; set; }

    public required WorkflowType Type { get; set; }

    public string? Description { get; set; }

    public List<WorkflowVersion> Versions { get; set; } = [];
}

/// <summary>
/// Version figée d'un workflow. Une version Active est exécutable ; toute modification
/// passe par une nouvelle version, jamais par l'édition d'une version déjà validée.
/// </summary>
public class WorkflowVersion : Entity
{
    public Guid WorkflowId { get; set; }

    public int NumeroDeVersion { get; set; } = 1;

    public ValidationStatus Statut { get; set; } = ValidationStatus.Brouillon;

    public string? CommentaireDeVersion { get; set; }

    public DateTimeOffset CreeLe { get; set; } = DateTimeOffset.UtcNow;

    public required string CreePar { get; set; }

    public DateTimeOffset? ValideLe { get; set; }

    public string? ValidePar { get; set; }

    public List<WorkflowStepDefinition> Etapes { get; set; } = [];
}

/// <summary>Définition d'une étape : ce que le moteur exécutera, et sous quelles garanties.</summary>
public class WorkflowStepDefinition : Entity
{
    public Guid WorkflowVersionId { get; set; }

    public int Ordre { get; set; }

    public required string Libelle { get; set; }

    /// <summary>Action approuvée à exécuter. Aucune console libre n'est exposée (SEC-006).</summary>
    public required string Action { get; set; }

    public Guid? ComposantCibleId { get; set; }

    /// <summary>Condition d'exécution évaluée avant l'étape ; l'étape est ignorée si elle est fausse.</summary>
    public string? Condition { get; set; }

    public int TimeoutSecondes { get; set; } = 300;

    public int NombreDeReessais { get; set; }

    public int DelaiEntreReessaisSecondes { get; set; } = 10;

    /// <summary>L'étape exige une confirmation humaine avant d'être lancée.</summary>
    public bool ConfirmationRequise { get; set; }

    /// <summary>L'étape exige une approbation par un acteur distinct du demandeur.</summary>
    public bool ApprobationRequise { get; set; }

    /// <summary>
    /// L'étape peut s'exécuter en parallèle d'autres étapes explicitement déclarées indépendantes.
    /// Le parallélisme n'est jamais déduit : il est déclaré dans la version validée.
    /// </summary>
    public bool IndependanteDesEtapesVoisines { get; set; }
}
