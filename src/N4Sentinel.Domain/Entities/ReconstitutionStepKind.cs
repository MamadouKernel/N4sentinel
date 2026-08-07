namespace N4Sentinel.Domain.Entities;

/// <summary>Séquence fixe imposée par FR-059E pour une reconstitution sécurisée de dossier partagé.</summary>
public enum ReconstitutionStepKind
{
    StopComponents,
    Backup,
    VerifyBackupIntegrity,
    Reconstruct,
    ControlledRestart,
    FinalTests,
}
