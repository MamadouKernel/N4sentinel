// Affichage du mot de passe saisi.
//
// Servi depuis l'application, jamais en ligne : la politique de contenu n'autorise
// que « script-src 'self' », et un gestionnaire posé dans l'attribut onclick serait bloqué.
//
// Le champ reste de type password par défaut : c'est l'utilisateur qui décide de montrer
// sa saisie, jamais l'inverse.

function basculer(element, masquer) {
    if (!element) {
        return;
    }

    if (masquer) {
        element.setAttribute('hidden', '');
    } else {
        element.removeAttribute('hidden');
    }
}

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

            // Attribut et non propriété : « hidden » n'est réfléchi que sur les éléments
            // HTML. Sur un SVG, « element.hidden = true » ne pose qu'une propriété
            // JavaScript sans le moindre effet à l'écran.
            basculer(bouton.querySelector('.oeil'), afficher);
            basculer(bouton.querySelector('.oeil-barre'), !afficher);

            // Rendre la main au champ : sans cela, la suite de la saisie part dans le vide.
            champ.focus();
        });
    }
}

// FR-021 — rafraîchissement du suivi d'exécution.
//
// Les écrans de l'application se rechargent par formulaire POST classique. Une exécution
// engagée, elle, avance toute seule : l'exécuteur de fond franchit les étapes sans que
// personne ne clique. Sans rafraîchissement, l'exploitant regarde un écran figé pendant que
// l'arrêt se déroule — et c'est précisément le moment où il doit voir ce qui se passe.
//
// Le rechargement complet est assumé plutôt qu'un flux temps réel : celui-ci supposerait un
// mode de rendu interactif que le reste de l'application n'utilise pas.

let minuterieDeRafraichissement = null;

// Une saisie en cours n'est jamais interrompue : recharger pendant qu'un opérateur motive un
// contournement effacerait sa justification. Le rafraîchissement attend le cycle suivant.
function saisieEnCours() {
    const actif = document.activeElement;
    if (!actif) {
        return false;
    }

    const balise = actif.tagName;
    return balise === 'INPUT' || balise === 'TEXTAREA' || balise === 'SELECT' || actif.isContentEditable;
}

function brancherLeRafraichissement() {
    if (minuterieDeRafraichissement !== null) {
        clearInterval(minuterieDeRafraichissement);
        minuterieDeRafraichissement = null;
    }

    const cible = document.querySelector('[data-rafraichir-secondes]');
    if (!cible) {
        return;
    }

    const secondes = Number.parseInt(cible.dataset.rafraichirSecondes, 10);
    if (!Number.isFinite(secondes) || secondes < 1) {
        return;
    }

    minuterieDeRafraichissement = setInterval(() => {
        // Onglet en arrière-plan : rien à montrer, autant ne pas interroger le serveur.
        if (document.hidden || saisieEnCours()) {
            return;
        }

        window.location.reload();
    }, secondes * 1000);
}

function brancherLesComportements() {
    brancherLesBasculesDeMotDePasse();
    brancherLeRafraichissement();
}

document.addEventListener('DOMContentLoaded', brancherLesComportements);

// La navigation améliorée de Blazor remplace le contenu sans recharger la page :
// les écouteurs doivent être reposés sur le nouveau DOM.
if (window.Blazor && typeof window.Blazor.addEventListener === 'function') {
    window.Blazor.addEventListener('enhancedload', brancherLesComportements);
}
