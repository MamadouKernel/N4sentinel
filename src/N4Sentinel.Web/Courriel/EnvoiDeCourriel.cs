using System.Globalization;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using N4Sentinel.Application.Abstractions;

namespace N4Sentinel.Web.Courriel;

/// <summary>Paramètres du relais de messagerie. Le mot de passe est une référence de coffre, pas une valeur.</summary>
public sealed class OptionsDeCourriel
{
    public string? Serveur { get; set; }

    public int Port { get; set; } = 25;

    public bool Ssl { get; set; }

    public string Expediteur { get; set; } = "n4sentinel@citcotedivoire.com";

    public string? Utilisateur { get; set; }

    /// <summary>Référence de secret, résolue à l'envoi. Jamais le mot de passe lui-même (SEC-003).</summary>
    public string? ReferenceDuMotDePasse { get; set; }

    public bool EstConfigure => !string.IsNullOrWhiteSpace(Serveur);
}

/// <summary>Envoi par relais SMTP CIT.</summary>
public sealed class EnvoiDeCourrielSmtp(
    OptionsDeCourriel options,
    ISecretResolver coffre) : IEnvoiDeCourriel
{
    public async Task EnvoyerAsync(
        string destinataire,
        string objet,
        string corps,
        CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(options.Serveur, options.Port) { EnableSsl = options.Ssl };

        if (!string.IsNullOrWhiteSpace(options.Utilisateur)
            && !string.IsNullOrWhiteSpace(options.ReferenceDuMotDePasse))
        {
            var motDePasse = await coffre.ResoudreAsync(options.ReferenceDuMotDePasse, cancellationToken);
            client.Credentials = new NetworkCredential(options.Utilisateur, motDePasse);
        }

        using var message = new MailMessage(options.Expediteur, destinataire, objet, corps);
        await client.SendMailAsync(message, cancellationToken);
    }
}

/// <summary>
/// Repli de développement : le message est écrit dans un fichier au lieu d'être envoyé.
///
/// Ce n'est pas un envoi simulé présenté comme réussi — l'application journalise à chaque
/// démarrage qu'aucun relais n'est configuré, et le code du second facteur est réellement
/// consultable dans le fichier. Ce repli est refusé hors développement.
/// </summary>
public sealed class EnvoiDeCourrielVersFichier(
    string dossier,
    ILogger<EnvoiDeCourrielVersFichier> journal) : IEnvoiDeCourriel
{
    public async Task EnvoyerAsync(
        string destinataire,
        string objet,
        string corps,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(dossier);

        var horodatage = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        var chemin = Path.Combine(dossier, $"{horodatage}.txt");

        var contenu =
            $"À : {destinataire}{Environment.NewLine}"
            + $"Objet : {objet}{Environment.NewLine}"
            + $"{new string('-', 60)}{Environment.NewLine}"
            + corps;

        await File.WriteAllTextAsync(chemin, contenu, cancellationToken);
        JournalDeCourriel.MessageEcritDansUnFichier(journal, chemin);
    }
}

internal static partial class JournalDeCourriel
{
    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Warning,
        Message = "Aucun relais SMTP configuré : le message a été écrit dans {Chemin}.")]
    public static partial void MessageEcritDansUnFichier(ILogger logger, string chemin);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "Courriel:Serveur n'est pas renseigné. Les codes de second facteur seront "
                  + "écrits sur disque au lieu d'être envoyés. Configuration inacceptable hors développement.")]
    public static partial void RelaisNonConfigure(ILogger logger);
}
