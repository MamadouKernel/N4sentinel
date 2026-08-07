namespace N4Sentinel.Domain.Entities;

/// <summary>États de suivi d'un fichier EDI (FR-059H : "reçus, intégrés, en attente, rejetés ou non consommés").</summary>
public enum EdiFileStatus
{
    Received,
    Pending,
    Consumed,
    Rejected,
    Error,
}
