using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// §3.18 — Shared Folder : catégorie, chemin logique, structure attendue, contrôles,
/// sauvegarde, état et dernière vérification.
/// </summary>
public class SharedFolder : Entity
{
    public Guid EnvironmentId { get; set; }

    public required string Nom { get; set; }

    public SharedFolderCategory Categorie { get; set; } = SharedFolderCategory.Autre;

    /// <summary>Chemin logique déclaré ; la résolution physique reste du ressort du connecteur.</summary>
    public required string CheminLogique { get; set; }

    /// <summary>Arborescence attendue, servant de référence aux contrôles de structure.</summary>
    public string? StructureAttendue { get; set; }

    public SharedFolderState Etat { get; set; } = SharedFolderState.Inconnu;

    public DateTimeOffset? DerniereVerification { get; set; }

    public DateTimeOffset? DerniereSauvegarde { get; set; }

    public string? EmplacementDeSauvegarde { get; set; }

    public bool SauvegardeVerifiee { get; set; }

    public List<SharedFolderCheck> Controles { get; set; } = [];
}

/// <summary>Contrôle périodique appliqué à un dossier partagé (présence, structure, volumétrie, âge).</summary>
public class SharedFolderCheck : Entity
{
    public Guid SharedFolderId { get; set; }

    public required string Libelle { get; set; }

    public required string TypeDeControle { get; set; }

    public string? SeuilAttendu { get; set; }

    public bool Actif { get; set; } = true;

    public DateTimeOffset? DernierResultatLe { get; set; }

    public bool? DernierResultatConforme { get; set; }
}
