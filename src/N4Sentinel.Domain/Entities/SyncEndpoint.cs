using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Un point de synchronisation N4/Bridge/XPS ou une file ActiveMQ/KahaDB supervisée pour un environnement
/// (FR-056, E5.3). Strictement en lecture : jamais la cible d'une étape de workflow, aucune action de purge
/// automatique (le cahier des charges exclut explicitement toute suppression automatique de messages/files).
/// </summary>
public class SyncEndpoint
{
    private SyncEndpoint()
    {
        Name = string.Empty;
    }

    public SyncEndpoint(Guid environmentId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleException("Le nom du point de synchronisation est obligatoire.");
        }

        Id = Guid.NewGuid();
        EnvironmentId = environmentId;
        Name = name.Trim();
    }

    public Guid Id { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public string Name { get; private set; }

    /// <summary>Taille de la file de messages en attente (FR-056, table ActiveMQ/KahaDB).</summary>
    public int? QueueSize { get; private set; }

    /// <summary>Nombre de consommateurs actifs sur la file.</summary>
    public int? ConsumerCount { get; private set; }

    /// <summary>Date du dernier échange normal N4-XPS (FR-056).</summary>
    public DateTime? LastNormalExchangeUtc { get; private set; }

    public string? AnomalyDescription { get; private set; }

    public DateTime? LastCheckedUtc { get; private set; }

    private static readonly TimeSpan StaleExchangeThreshold = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Seuils simples documentés ici, pas un moteur de règles (cf. décision Sprint 10) : messages en attente
    /// sans consommateur, file anormalement longue, ou dernier échange normal trop ancien.
    /// </summary>
    public bool HasAnomaly =>
        (QueueSize > 0 && ConsumerCount == 0) ||
        QueueSize >= 1000 ||
        (LastNormalExchangeUtc is { } last && DateTime.UtcNow - last > StaleExchangeThreshold);

    public void RecordSyncCheck(
        int? queueSize, int? consumerCount, DateTime? lastNormalExchangeUtc, string? anomalyDescription)
    {
        QueueSize = queueSize;
        ConsumerCount = consumerCount;
        LastNormalExchangeUtc = lastNormalExchangeUtc;
        AnomalyDescription = anomalyDescription?.Trim();
        LastCheckedUtc = DateTime.UtcNow;
    }
}
