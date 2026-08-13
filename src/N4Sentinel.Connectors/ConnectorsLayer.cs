using System.Reflection;

namespace N4Sentinel.Connectors;

/// <summary>
/// §3.15 — couche Connecteurs : services Windows / PowerShell ou mécanisme approuvé,
/// SQL en lecture, HTTP/TCP, fichiers et logs, monitoring.
/// Aucune console libre n'est exposée (SEC-006) : seules des actions approuvées sont émises.
/// </summary>
public static class ConnectorsLayer
{
    public static Assembly Assembly => typeof(ConnectorsLayer).Assembly;
}
