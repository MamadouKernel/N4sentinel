namespace N4Sentinel.Domain.Services;

/// <summary>
/// État d'un composant tel que <b>constaté</b> avant de recalculer un plan (FR-029A).
///
/// Ce n'est pas un état supervisé en continu : tant qu'aucun connecteur réel n'est autorisé, l'information
/// provient d'un constat de l'opérateur ou d'une collecte ponctuelle. <see cref="Unknown"/> est donc la
/// valeur par défaut, et elle n'autorise jamais à écarter une étape — dans le doute, on la conserve.
/// </summary>
public enum ObservedComponentState
{
    /// <summary>État non constaté. L'étape est conservée : on ne saute rien sur une supposition.</summary>
    Unknown = 0,

    /// <summary>Composant constaté en fonctionnement.</summary>
    Running = 1,

    /// <summary>Composant constaté arrêté.</summary>
    Stopped = 2,
}
