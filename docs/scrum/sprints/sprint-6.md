# Sprint 6 — Modernisation UI (Tailwind, navbar, configurabilité)

**Objectif de sprint** : sur demande explicite de la DSI, remplacer Bootstrap par Tailwind CSS, passer d'une
disposition en barre latérale à une **navbar** horizontale, garantir un rendu **100% responsive**, adopter un
design moderne et plat (**aucun dégradé**), et rendre l'application plus **configurable/modulable**
(image de marque et modules activables sans recompilation). Ce sprint est un pivot technique transverse —
il touche la structure partagée par toutes les pages déjà livrées (Sprints 1-5), donc il est traité **avant**
la suite fonctionnelle pour ne pas reconstruire deux fois les futures pages.

**Reprioritisation du backlog** : le contenu fonctionnel initialement prévu au "Sprint 6" (E3.3 Scénario de
démarrage complet, E1.6 Cartographie systèmes dépendants) est décalé au **Sprint 7**. Voir
`docs/scrum/roadmap.md` mis à jour en conséquence.

## Sprint Backlog (nouvelle story, hors périmètre initial du cahier des charges — demande DSI directe)

| Story | Résultat |
|---|---|
| Migration Bootstrap → Tailwind CSS | Prévu |
| Layout navbar responsive (suppression de la barre latérale) | Prévu |
| Design plat moderne, sans dégradé | Prévu |
| Configurabilité (image de marque, modules activables) | Prévu |

## Décisions de conception

- **Compatibilité de classes plutôt que réécriture page par page** : 20+ pages métier et ~30 pages Identity
  (scaffolding ASP.NET Core) utilisent des classes Bootstrap (`btn`, `table`, `badge`, `alert`, `form-control`,
  `nav`...). Plutôt que de réécrire individuellement chaque page, une couche `@layer components` Tailwind
  redéfinit ces mêmes noms de classes avec un rendu entièrement Tailwind — zéro dégradé, coins nets,
  ombres discrètes. Résultat : la totalité des pages existantes (y compris les pages Identity) changent
  d'apparence sans modification de leur balisage, et le futur design du produit se pilote depuis un seul
  fichier (`Styles/app.css`). C'est aussi ce qui rend le design "modulable" au sens strict : changer la charte
  graphique ne touche qu'un point d'entrée.
- **Palette ancrée sur l'identité visuelle CIT** : le logo fourni (`minignan/logo.png`) utilise un bleu marine
  et un doré/sable. La palette Tailwind (`@theme`) reprend ces teintes comme couleurs de marque plutôt qu'un
  bleu générique — cohérence avec le client réel plutôt qu'un choix arbitraire.
- **Navbar plutôt que sidebar** : `NavMenu.razor` (sidebar) est remplacé par un composant `Navbar.razor`
  horizontal, avec menu mobile en accordéon sous le seuil `md` (768px) — c'est la disposition demandée
  explicitement, et elle simplifie aussi le responsive (une sidebar fixe est plus difficile à rendre
  100% responsive qu'une navbar qui se replie).
- **Outillage Tailwind** : build via `@tailwindcss/cli` (Node.js, déjà disponible sur ce poste), source dans
  `Styles/app.css`, sortie compilée dans `wwwroot/app.css` **committée dans le dépôt** — l'application
  fonctionne donc en production même sans Node.js installé sur le serveur ; Node n'est nécessaire que pour
  modifier la feuille de style. Un target MSBuild optionnel régénère le CSS au build si Node est présent,
  sans faire échouer la compilation .NET s'il est absent.
- **Configurabilité** : nouvelle section `Branding` (nom d'application, organisation) et `Features`
  (`Environments`, activation du module dans la navigation) dans `appsettings.json`, liées via `IOptions<T>`
  (`Configuration/BrandingOptions.cs`, `Configuration/FeatureOptions.cs`). Ce n'est pas une gestion de thème
  complète (hors périmètre du cahier des charges initial) — c'est la configurabilité minimale directement
  exploitable sans redéploiement : renommer l'application, masquer un module pour un déploiement restreint.
- **Tables responsives** : les 10 pages qui affichent des tableaux (`table-hover`/`table-sm`) sont chacune
  encapsulées dans un conteneur `.table-responsive` (`overflow-x: auto` sur le conteneur, pas sur la page) —
  sur mobile, un tableau trop large pour l'écran défile horizontalement dans son propre cadre au lieu de
  forcer un défilement horizontal de toute la page, ce qui aurait violé l'exigence 100% responsive.

## Vérification de bout en bout (navigateur)

Exécutée le 2026-08-07 : `dotnet build` (0 erreur), 82 tests unitaires verts (57 Domain + 25 Application),
puis navigation dans le navigateur en viewport desktop (1280×800) et mobile (375×812) :

- **Navbar desktop** : logo N4/nom d'application, liens Accueil/Environnements, email utilisateur et bouton
  Se déconnecter alignés à droite — aucune barre latérale, aucun dégradé, couleurs plates navy/or.
- **Navbar mobile** : liens et actions repliés sous un bouton hamburger ; l'ouverture du menu affiche les
  mêmes éléments qu'en desktop en pile verticale, sans débordement horizontal de la page (confirmé par
  `document.documentElement.scrollWidth === window.innerWidth`).
- **Listes et détails** (Environnements, composants) : badges, boutons, tableaux rendus avec la nouvelle
  charte Tailwind ; ligne "Production" mise en évidence (fond rouge clair) comme avant la migration.
- **Formulaire** (création d'environnement) : champs, select, textarea et boutons Créer/Annuler stylés,
  aucune régression de mise en page en `form-floating`/`form-horizontal`.
- **Tables responsives** : sur mobile, la table "Environnements" (5 colonnes) défile horizontalement dans
  son propre cadre sans jamais élargir la page.
- **Point d'attention hors périmètre** : `/Account/Manage` renvoie une page "Not Found" dans cet
  environnement de vérification — comportement antérieur à ce sprint (aucun fichier Identity/Account n'a été
  modifié, seule la grille `.row`/`.col-lg-*` de `ManageLayout.razor` reste inchangée en substance). À
  investiguer dans un sprint dédié à l'Identity si confirmé en dehors de cet environnement de dev.
