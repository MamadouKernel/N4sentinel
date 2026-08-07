using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Un système externe dont dépend l'exploitation d'un environnement N4, mais qui n'appartient pas au TOS
/// Navis N4 lui-même (ex. CAMCO/GOS, DGPS, RMS/Reefer Runner, IPAKI, Scangate, EDI — E1.6). Distinct de
/// <see cref="N4Component"/> : jamais la cible d'une étape de workflow (aucune action de pilotage N4 Sentinel
/// n'a de sens dessus), pas d'attributs de composant technique N4 — uniquement son identité et son caractère
/// pilotable ou non (<see cref="ComponentGovernance"/>, réutilisé tel quel).
/// </summary>
public class DependentSystem
{
    private DependentSystem()
    {
        Name = string.Empty;
    }

    public DependentSystem(Guid environmentId, string name, string? description, ComponentGovernance governance)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom du système dépendant est obligatoire.");
        }

        Id = Guid.NewGuid();
        EnvironmentId = environmentId;
        Name = name.Trim();
        Description = description?.Trim();
        Governance = governance;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public ComponentGovernance Governance { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public void UpdateDetails(string name, string? description, ComponentGovernance governance)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom du système dépendant est obligatoire.");
        }

        Name = name.Trim();
        Description = description?.Trim();
        Governance = governance;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
