using N4Sentinel.Domain.Common;

namespace N4Sentinel.Domain.Habilitations;

/// <summary>
/// SEC-004 — résolution des permissions effectives d'un utilisateur sur un environnement donné.
///
/// La règle centrale, tirée du §2.3.2 : « Les membres de l'équipe de développement ne disposent
/// pas automatiquement de droits d'action en Production » et « Les droits doivent pouvoir être
/// différenciés par environnement, notamment entre UAT et Production ».
///
/// Traduction retenue : en Production, un profil global n'accorde que la consultation.
/// Toute permission d'action y exige une habilitation explicite sur cet environnement.
/// Hors Production, profils globaux et habilitations d'environnement s'additionnent.
///
/// Ce choix sépare voir et agir plutôt que de tout bloquer : un auditeur ou un TOS Manager
/// garde sa vue sur la Production sans qu'un opérateur y hérite d'un droit d'exécution.
/// </summary>
public static class ResolveurDHabilitations
{
    public static IReadOnlySet<Droit> Resoudre(
        IEnumerable<ProfilUtilisateur> profilsGlobaux,
        IEnumerable<ProfilUtilisateur> profilsSurLEnvironnement,
        EnvironmentType typeDEnvironnement)
    {
        ArgumentNullException.ThrowIfNull(profilsGlobaux);
        ArgumentNullException.ThrowIfNull(profilsSurLEnvironnement);

        var droitsGlobaux = DroitsParProfil.Pour(profilsGlobaux);
        var droitsLocaux = DroitsParProfil.Pour(profilsSurLEnvironnement);

        if (typeDEnvironnement != EnvironmentType.Production)
        {
            var cumul = new HashSet<Droit>(droitsGlobaux);
            cumul.UnionWith(droitsLocaux);
            return cumul;
        }

        // En Production : la lecture suit les profils globaux, l'action non.
        var effectives = new HashSet<Droit>(
            droitsGlobaux.Where(Droits.Lecture.Contains));
        effectives.UnionWith(droitsLocaux);
        return effectives;
    }

    public static bool Autorise(
        IEnumerable<ProfilUtilisateur> profilsGlobaux,
        IEnumerable<ProfilUtilisateur> profilsSurLEnvironnement,
        EnvironmentType typeDEnvironnement,
        Droit droit) =>
        Resoudre(profilsGlobaux, profilsSurLEnvironnement, typeDEnvironnement).Contains(droit);
}
