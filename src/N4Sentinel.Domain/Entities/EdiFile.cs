using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Suivi d'un fichier d'interface EDI rattaché à un environnement (E6.1). Reprend le modèle de données
/// "Fichier d'interface / EDI" du cahier des charges : type de message, partenaire, réception, consommation,
/// statut, erreur, ancienneté et incident (FR-059H/I/J). Comme pour <see cref="SharedFolder"/> et
/// <see cref="SyncEndpoint"/> (Sprint 10), ce n'est jamais la cible d'une étape de workflow : N4 Sentinel ne
/// dispose d'aucun connecteur EDI réel (mode Simulation uniquement, cf. décision Sprint 2) — le suivi est
/// déclaratif (l'opérateur enregistre réception/consommation/échec), pas une écoute réelle des flux.
/// </summary>
public class EdiFile
{
    private EdiFile()
    {
        MessageType = string.Empty;
        PartnerName = string.Empty;
    }

    public EdiFile(Guid environmentId, string messageType, string partnerName)
    {
        if (string.IsNullOrWhiteSpace(messageType))
        {
            throw new DomainRuleException("Le type de message EDI est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(partnerName))
        {
            throw new DomainRuleException("Le partenaire EDI est obligatoire.");
        }

        Id = Guid.NewGuid();
        EnvironmentId = environmentId;
        MessageType = messageType.Trim();
        PartnerName = partnerName.Trim();
        Status = EdiFileStatus.Received;
        ReceivedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid EnvironmentId { get; private set; }

    /// <summary>Type de message (ex. BAPLIE) — FR-059H.</summary>
    public string MessageType { get; private set; }

    public string PartnerName { get; private set; }

    public EdiFileStatus Status { get; private set; }

    public DateTime ReceivedAtUtc { get; private set; }

    public DateTime? ConsumedAtUtc { get; private set; }

    /// <summary>Nombre de tentatives d'intégration échouées (FR-059I : "échoue plusieurs fois").</summary>
    public int AttemptCount { get; private set; }

    public string? LastErrorMessage { get; private set; }

    public DateTime? LastAttemptAtUtc { get; private set; }

    private static readonly TimeSpan ExpectedDelayThreshold = TimeSpan.FromMinutes(60);
    private const int RepeatedFailureThreshold = 3;

    /// <summary>
    /// Seuils simples documentés ici, pas un moteur de règles (cf. décision Sprint 10) : rejeté ou en erreur,
    /// échecs répétés, ou fichier reçu/en attente depuis plus longtemps que le délai attendu (FR-059I).
    /// </summary>
    public bool HasAnomaly =>
        Status is EdiFileStatus.Rejected or EdiFileStatus.Error ||
        AttemptCount >= RepeatedFailureThreshold ||
        (Status is EdiFileStatus.Received or EdiFileStatus.Pending &&
            DateTime.UtcNow - ReceivedAtUtc > ExpectedDelayThreshold);

    public void MarkPending()
    {
        EnsureNotTerminal();
        Status = EdiFileStatus.Pending;
    }

    public void MarkConsumed()
    {
        EnsureNotTerminal();
        Status = EdiFileStatus.Consumed;
        ConsumedAtUtc = DateTime.UtcNow;
    }

    public void MarkRejected(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainRuleException("Le motif de rejet d'un fichier EDI est obligatoire.");
        }

        EnsureNotTerminal();
        Status = EdiFileStatus.Rejected;
        LastErrorMessage = reason.Trim();
    }

    public void RecordFailedAttempt(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new DomainRuleException("Le message d'erreur d'une tentative échouée est obligatoire.");
        }

        EnsureNotTerminal();
        AttemptCount++;
        LastAttemptAtUtc = DateTime.UtcNow;
        LastErrorMessage = errorMessage.Trim();
        Status = EdiFileStatus.Error;
    }

    private void EnsureNotTerminal()
    {
        if (Status == EdiFileStatus.Consumed)
        {
            throw new DomainRuleException("Un fichier EDI déjà consommé ne peut plus changer d'état.");
        }
    }
}
