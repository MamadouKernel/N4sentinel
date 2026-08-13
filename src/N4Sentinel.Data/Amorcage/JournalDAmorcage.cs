using Microsoft.Extensions.Logging;

namespace N4Sentinel.Data.Amorcage;

/// <summary>
/// Messages de journalisation générés à la compilation. Le générateur produit le contrôle
/// de niveau et évite le boxing des arguments : c'est ce qu'exige l'analyse de code sur un
/// chemin appelé à chaque démarrage.
/// </summary>
internal static partial class JournalDAmorcage
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Error,
        Message = "Création du profil {Profil} en échec : {Erreurs}")]
    public static partial void ProfilEnEchec(ILogger logger, string profil, string erreurs);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Environnement {Nom} créé.")]
    public static partial void EnvironnementCree(ILogger logger, string nom);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Aucun compte d'amorçage créé : renseigner Amorcage:EmailAdministrateur et "
                  + "Amorcage:MotDePasseAdministrateur (variables d'environnement ou secrets "
                  + "utilisateur) puis redémarrer.")]
    public static partial void AmorcageIncomplet(ILogger logger);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "Création du compte d'amorçage en échec : {Erreurs}")]
    public static partial void CompteDAmorcageEnEchec(ILogger logger, string erreurs);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Compte d'amorçage {Email} créé.")]
    public static partial void CompteDAmorcageCree(ILogger logger, string? email);
}
