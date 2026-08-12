// Affichage du mot de passe saisi.
//
// Servi depuis l'application, jamais en ligne : la politique de contenu n'autorise
// que « script-src 'self' », et un gestionnaire posé dans l'attribut onclick serait bloqué.
//
// Le champ reste de type password par défaut : c'est l'utilisateur qui décide de montrer
// sa saisie, jamais l'inverse.

function brancherLesBasculesDeMotDePasse() {
    const boutons = document.querySelectorAll('[data-bascule-mot-de-passe]');

    for (const bouton of boutons) {
        if (bouton.dataset.branche === 'oui') {
            continue;
        }

        const champ = document.getElementById(bouton.dataset.basculeMotDePasse);
        if (!champ) {
            continue;
        }

        bouton.dataset.branche = 'oui';
        bouton.addEventListener('click', () => {
            const afficher = champ.type === 'password';

            champ.type = afficher ? 'text' : 'password';

            const libelle = afficher ? 'Masquer le mot de passe' : 'Afficher le mot de passe';
            bouton.setAttribute('aria-label', libelle);
            bouton.setAttribute('aria-pressed', afficher ? 'true' : 'false');
            bouton.title = libelle;

            const oeil = bouton.querySelector('.oeil');
            const oeilBarre = bouton.querySelector('.oeil-barre');
            if (oeil && oeilBarre) {
                oeil.hidden = afficher;
                oeilBarre.hidden = !afficher;
            }

            // Rendre la main au champ : sans cela, la suite de la saisie part dans le vide.
            champ.focus();
        });
    }
}

document.addEventListener('DOMContentLoaded', brancherLesBasculesDeMotDePasse);

// La navigation améliorée de Blazor remplace le contenu sans recharger la page :
// les écouteurs doivent être reposés sur le nouveau DOM.
if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
    window.Blazor.addEventListener('enhancedload', brancherLesBasculesDeMotDePasse);
}
