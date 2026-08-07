# Sprint 8 — Tableau de bord de supervision & gestion des comptes

**Objectif de sprint** : donner à tout utilisateur authentifié une vue d'ensemble de l'état des environnements
et des opérations en cours (E4.1), un accès à l'historique complet des opérations passées depuis cette vue
(E4.2), et donner à l'Administrateur une interface pour gérer les comptes et rôles des utilisateurs (E11.1).

## Sprint Backlog

| Story | Résultat |
|---|---|
| E4.1 — Tableau de bord temps réel (environnements, composants critiques, alertes, opérations en cours) | Fait |
| E4.2 — Historique des opérations depuis le tableau de bord | Fait |
| E11.1 — Gestion des comptes (UI) | Fait, partiellement — voir décision ci-dessous |

## Décisions de conception

- **"Temps réel" = rafraîchissement périodique côté serveur, pas un bus d'événements dédié.** Aucune source de
  télémétrie live n'existe dans l'application (les connecteurs restent en mode Simulation, cf. E12.4) : bâtir
  une infrastructure de publication/abonnement (SignalR hub applicatif, file de messages) pour pousser des
  événements qui n'existent pas serait de la sur-ingénierie. Le tableau de bord Blazor Server (le circuit
  interactif est déjà une connexion persistante) relit périodiquement son résumé via un `PeriodicTimer` côté
  composant (toutes les 10 secondes) et se re-rend — un choix proportionné, révisable si une vraie source
  d'événements (connecteurs réels, webhooks) apparaît dans un sprint ultérieur.
- **Aucun sondage de santé des composants au chargement du dashboard.** Le test de connectivité (FR-007) est
  une action explicite et volontaire (bouton dédié par environnement, Sprint 1) — l'appeler automatiquement
  pour chaque composant de chaque environnement à chaque rafraîchissement du dashboard multiplierait les
  appels aux connecteurs sans qu'aucune décision utilisateur ne l'ait demandé. Le dashboard affiche donc des
  métriques déjà connues sans coût (nombre de composants, nombre de composants critiques, statut de
  l'environnement) plutôt qu'un état de santé live des composants.
- **"Alertes" = opérations échouées, pas un moteur de règles.** Aucun sous-système de diagnostic/alerting
  n'existe encore (Epic 7, sprints ultérieurs). Plutôt que d'inventer une notion d'alerte non spécifiée par le
  cahier des charges à ce stade, le dashboard remonte simplement les `OperationRun` au statut `Failed` comme
  section "Alertes" — honnête sur ce que l'application sait réellement détecter aujourd'hui.
- **Nouveau DTO `OperationRunSummaryDto`** (distinct de `OperationRunDto`) pour le dashboard et l'historique
  global : ces deux vues affichent une opération par ligne dans une liste cross-environnement (avec le nom de
  l'environnement, absent de `OperationRunDto`) et n'ont pas besoin du détail des étapes — éviter de charger
  `StepExecutions` pour chaque opération d'un tableau qui peut lister des dizaines d'opérations.
- **E11.1 reste dans la couche Web, comme `IdentitySeeder`.** La gestion des comptes ASP.NET Core Identity
  (rôles, verrouillage) est déjà traitée comme une préoccupation de la couche Web dans ce projet (pas de
  passage par MediatR/CQRS pour l'authentification) — cohérent avec cette frontière déjà établie plutôt que
  d'introduire une seconde façon de faire pour un seul écran.
- **Garde-fous sur son propre compte** : un Administrateur ne peut ni retirer son propre rôle Administrateur
  ni verrouiller son propre compte depuis cet écran — pour éviter un auto-blocage accidentel qui nécessiterait
  une intervention en base de données pour être corrigé.
- **Écart assumé sur E11.1 : rôles globaux, pas encore "différenciés par environnement".** L'énoncé complet de
  la story demande des rôles différenciés par environnement (ex. Opérateur sur UAT mais Lecteur sur
  Production). Ce sprint livre une gestion des rôles **globale** via `UserManager`/`RoleManager` ASP.NET Core
  Identity (rôle attribué à l'utilisateur, valable sur tous les environnements) — cohérent avec l'intégralité
  du modèle d'autorisation déjà en place depuis le Sprint 1 : chaque `[Authorize(Roles=...)]` et
  `AuthorizeView Roles="..."` de l'application (Environnements, Workflows, Opérations, Systèmes dépendants...)
  vérifie un rôle global, jamais un rôle scoped à un environnement précis. Livrer une différenciation par
  environnement maintenant exigerait de refondre ce modèle d'autorisation dans toute l'application (nouvelle
  entité de liaison utilisateur/environnement/rôle, gestionnaire d'autorisation par ressource, migration de
  chaque contrôle d'accès existant) — un chantier disproportionné par rapport aux 8 points de cette story et
  incohérent avec 7 sprints de précédent établi. Décision assumée : livrer la gestion de comptes globale
  maintenant, et transformer la différenciation par environnement en story dédiée si la DSI la confirme
  nécessaire, plutôt que de la improviser en fin de sprint.

## Vérification de bout en bout (navigateur)

Exécutée avec succès le 2026-08-07 :

- **Dashboard** (`/dashboard`) : compteurs corrects (2 environnements, 1 opération en cours, 1 en attente
  d'approbation), tableau des environnements avec composants/composants critiques/opérations en cours par
  environnement, section "Opérations en cours" et "Alertes" alimentées par les données réelles créées lors des
  sprints précédents.
- **Historique global** (`/operations`) : les 5 opérations existantes (4 UAT terminées + 1 Production en
  attente d'approbation) listées tous environnements confondus, triées par date décroissante, avec le nom de
  l'environnement.
- **Gestion des comptes** (`/admin/users`) : le compte admin seedé s'affiche avec le badge "Vous" ; vérifié en
  JavaScript que la case Administrateur est cochée **et désactivée**, et que le bouton "Verrouiller" est
  **désactivé** pour sa propre ligne — les deux garde-fous fonctionnent.

96 tests unitaires verts (66 Domain + 30 Application) après la vérification.
