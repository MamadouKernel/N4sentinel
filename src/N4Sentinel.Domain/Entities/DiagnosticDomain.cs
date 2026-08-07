namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Domaines de cause partagés entre un <see cref="DiagnosticSignal"/> collecté et une <see cref="DiagnosticRule"/>
/// (FR-062, FR-065) : le moteur de diagnostic du Sprint 13 (E7.2) classera ses hypothèses selon ce même
/// vocabulaire, d'où le partage délibéré de cet enum entre les deux entités plutôt que deux listes séparées.
/// </summary>
public enum DiagnosticDomain
{
    Network,
    Database,
    SystemVm,
    N4ClusterNodes,
    CenterStandbyNode,
    ActiveMqKahaDb,
    BridgeXps,
    Ecn4,
    SharedFolders,
    EdiInterfaces,
    Configuration,
    ExistingSupervision,
}
