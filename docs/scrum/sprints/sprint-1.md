# Sprint 1 — Référentiel : Environnements & Composants

**Objectif de sprint** : permettre à un Administrateur de gérer le référentiel de base (environnements et
composants de l'écosystème N4) avec le cycle de validation complet, condition préalable à tout workflow.

## Sprint Backlog

| Story | Résultat |
|---|---|
| E1.1 — Créer/modifier/désactiver un environnement | Fait |
| E1.3 — Cycle de validation Brouillon → À valider → Validé → Actif → Désactivé | Fait |
| E1.2 — Enregistrer les composants d'un environnement (attributs FR-002 + dépendances) | Fait |

## Ce qui a été livré

- **Domain** : `N4Environment` (cycle de validation avec garde-fous — transitions invalides lèvent
  `DomainRuleException`) et `N4Component` (champs FR-002, gestion des dépendances avec garde-fou
  anti-auto-référence).
- **Application** : commandes/requêtes CQRS (`CreateEnvironmentCommand`, `UpdateEnvironmentCommand`,
  `ChangeEnvironmentStatusCommand`, `ListEnvironmentsQuery`, `GetEnvironmentByIdQuery` et leurs équivalents
  Component), validées par FluentValidation.
- **Infrastructure** : `AppDbContext`, configurations EF Core, repositories `EfEnvironmentRepository` /
  `EfComponentRepository`, migration `InitialReferentiel` appliquée sur LocalDB.
- **Web** : pages Blazor `/environments` (liste, bandeau rouge distinctif pour tout environnement de type
  Production), `/environments/new`, `/environments/{id}` (détail + composants + actions de transition de
  statut), `/environments/{id}/edit`, `/environments/{id}/components/new`,
  `/environments/{envId}/components/{id}/edit`. CRUD réservé au rôle Administrateur (`[Authorize(Roles = ...)]`),
  lecture ouverte à tout utilisateur authentifié.
- **Tests** : 18 tests Domain, 7 tests Application (tous verts), 2 tests d'intégration EF Core/SQL Server
  réel via Testcontainers (marqués skip sur ce poste car Docker n'y est pas installé — voir
  `sprint-0.md` pour le contexte).

## Ce qui n'est PAS dans ce sprint (assumé, périmètre suivant)

- Le moteur de séquencement/workflows (E1.4) : les dépendances entre composants sont stockées mais aucune
  validation de cycle complexe ni exécution n'existe encore — c'est le Sprint 2.
- La suppression physique d'un environnement ou d'un composant : volontairement absente, conforme au cahier
  des charges ("créer, modifier, **désactiver** et consulter" — jamais "supprimer").
- Un bandeau d'environnement global permanent dans le layout : reporté à Sprint 2/3 quand une notion
  d'"opération en cours dans tel environnement" existera réellement (cf. décision Sprint 0 sur SignalR) ; pour
  l'instant, la distinction Production est portée par les pages Environnements elles-mêmes (badge rouge).

## Revue de sprint (démo)

Scénario testé de bout en bout dans le navigateur (voir tâche de vérification finale) : connexion avec le
compte Administrateur seedé → création d'un environnement Production → création d'un composant Bridge avec
dépendance vers un Cluster Node existant → transition Brouillon → À valider → Validé → Actif.

## Rétrospective

- **Ce qui a bien fonctionné** : découper Environment et Component en deux entités agrégats distincts (plutôt
  qu'un agrégat unique Environment possédant Components) a simplifié les commandes — chaque composant se
  modifie indépendamment sans recharger tout l'environnement.
- **Point de vigilance pour la suite** : la validation "un composant ne peut pas dépendre de lui-même" est
  faite à deux niveaux (Domain + FluentValidation côté Update) — c'est volontaire (défense en profondeur) mais
  à garder synchronisé si la règle évolue.
- **Bug trouvé en vérification manuelle (corrigé dans ce sprint)** : les pages rendaient en Static SSR par
  défaut (aucun `@rendermode` n'était déclaré nulle part), ce qui faisait perdre la saisie des formulaires
  entre la frappe et la soumission (`EditForm`/`@bind-Value` nécessitent un circuit interactif pour rester
  synchronisés avec le modèle serveur). Corrigé en déclarant `<Routes @rendermode="InteractiveServer" />`
  dans `App.razor`, rendant toute l'application interactive par défaut — cohérent avec le besoin (formulaires,
  cases à cocher de dépendances, futurs boutons d'action temps réel). Détecté uniquement grâce au test de bout
  en bout dans le navigateur, pas par les tests unitaires ni la compilation : confirme la valeur de l'étape de
  vérification manuelle en fin de sprint pour tout ce qui touche l'UI Blazor.

## Décision d'hébergement ajoutée en cours de sprint (E12.5)

Sur demande explicite de la DSI : pas de déploiement IIS. L'application s'auto-héberge (Kestrel) comme
**Service Windows**, en HTTP simple sur le réseau interne (pas de TLS/reverse proxy pour l'instant — à
revoir si l'exposition évolue). Ajout de `UseWindowsService()` dans `Program.cs` (no-op en développement),
d'un sink Serilog Event Log en Production, et des scripts `deploy/install-service.ps1` /
`uninstall-service.ps1`. Détail complet dans `docs/deployment.md`.

## Vérification de bout en bout (navigateur)

Exécutée avec succès le 2026-08-06 : connexion admin → création environnement "Production" (code PROD) →
création composant "Cluster Node 1" → cycle complet de statut Brouillon → À valider → Validé → Actif → retour
à la liste (bandeau rouge PRODUCTION visible) → données vérifiées directement dans SQL Server LocalDB.
