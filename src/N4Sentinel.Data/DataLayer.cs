using System.Reflection;

namespace N4Sentinel.Data;

/// <summary>
/// §3.15 — couche Données / Audit : configuration, secrets référencés, historique, logs,
/// rapports et piste d'audit. Les secrets ne sont stockés que par référence (SEC-003) ;
/// les données sensibles au repos sont chiffrées (SEC-005).
/// </summary>
public static class DataLayer
{
    public static Assembly Assembly => typeof(DataLayer).Assembly;
}
