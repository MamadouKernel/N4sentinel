namespace N4Sentinel.Web.Securite;

/// <summary>
/// Assouplissements d'authentification réservés au développement.
///
/// SEC-001 classe le double facteur en « Must » pour la V1. Le contournement ci-dessous est
/// donc un écart assumé, demandé pour le confort de développement : il n'est ni un défaut ni
/// un oubli. Deux garanties l'encadrent — l'application refuse de démarrer si l'écart est
/// activé hors développement (<see cref="Verifier"/>), et chaque connexion ainsi obtenue est
/// tracée dans le journal d'audit comme un contournement, pas comme une connexion ordinaire.
///
/// La mécanique du second facteur n'est pas retirée : elle est court-circuitée. Remettre la
/// valeur à false suffit à la rétablir, sans redéploiement de code.
/// </summary>
public sealed class OptionsDAuthentification
{
    public const string Section = "Authentification";

    /// <summary>Court-circuite l'étape de second facteur. Interdit hors développement.</summary>
    public bool SecondFacteurDesactive { get; set; }

    /// <summary>
    /// Fait échouer le démarrage si l'écart est demandé ailleurs qu'en développement.
    /// Un garde-fou qui se contenterait d'un avertissement au journal ne protégerait de rien :
    /// personne ne lit les journaux de démarrage d'un service Windows.
    /// </summary>
    public void Verifier(IHostEnvironment environnement)
    {
        ArgumentNullException.ThrowIfNull(environnement);

        if (SecondFacteurDesactive && !environnement.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"Authentification:SecondFacteurDesactive est activé dans l'environnement "
                + $"« {environnement.EnvironmentName} ». SEC-001 impose le double facteur : "
                + "ce réglage n'est toléré qu'en développement. L'application ne démarre pas.");
        }
    }
}
