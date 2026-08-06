# Sprint 2 — Connecteurs pluggables & moteur de workflows configurables

**Objectif de sprint** : permettre à un Administrateur de définir des workflows versionnés (étapes ordonnées,
dépendances, critères de réussite, seuils/délais/retry, points de confirmation/approbation) sans toucher au
code, et poser l'abstraction de connecteur serveur (mode Simulation uniquement) sur laquelle s'appuiera le
pilotage réel à partir du Sprint 4. **Ce sprint ne exécute aucun workflow** — l'exécution (E3.x) est planifiée
Sprints 4-6, une fois le moteur de séquencement et le mode simulation (Sprint 3) en place.

## Sprint Backlog

| Story | Résultat |
|---|---|
| E12.4 — Connecteurs serveurs pluggables, implémentation Simulation par défaut | Fait |
| E1.4 — Workflows configurables et versionnés (FR-003, FR-004) | Fait |

## Décisions de conception

- **Versioning par copie** : un `Workflow` est un conteneur nommé (nom, type, environnement, périmètre) qui
  possède plusieurs `WorkflowVersion`. Modifier une version qui n'est plus au statut Brouillon crée
  automatiquement une nouvelle version (clone des étapes) plutôt que d'autoriser une édition en place —
  applique directement "Toute modification doit créer une nouvelle version" (FR-003).
- **Un seul statut Actif à la fois par workflow** : activer une nouvelle version désactive automatiquement
  l'ancienne version Active du même workflow (cohérent avec le cycle de validation déjà utilisé pour les
  environnements, FR-006, réutilisé ici sous un enum dédié `WorkflowVersionStatus` pour ne pas coupler les
  deux agrégats).
- **Garde-fou FR-004 encodé en règle de domaine testable** : une étape marquée destructrice/critique ne peut
  pas avoir de nouvelle tentative automatique sauf autorisation explicite — modélisé par
  `IsCriticalOrDestructive` + `RetryIsAutomatic` + `AutomaticRetryExplicitlyAuthorized`, avec levée d'une
  `DomainRuleException` si la combinaison interdite est tentée.
- **Ordre des étapes géré par position de liste**, pas par un champ `Order` éditable manuellement (source de
  bugs de doublons) — ajout en fin de liste, réordonnancement par déplacement haut/bas.
- **Rollback** : à ce stade, seulement des métadonnées descriptives (`AllowsRollback`, `RollbackNotes`) sur la
  version — l'orchestration réelle du retour arrière est un algorithme d'exécution qui appartient au moteur de
  pilotage (Sprints 4-6), hors périmètre de la seule *définition* de workflow.
- **`IServerConnector`** : interface définie dans Application, implémentation `SimulationServerConnector` dans
  Infrastructure (aucun accès réseau réel), enregistrée par défaut dans le conteneur DI. **Non consommée par
  une UI ce sprint** (rien n'exécute encore de workflow) — c'est un préalable volontaire au Sprint 3 (mode
  simulation) et Sprint 4+ (pilotage réel), assumé et documenté plutôt que laissé implicite.
- **Ordre des étapes rendu explicite (`WorkflowStep.Position`)** : la conception initiale s'appuyait sur la
  position en mémoire d'une `List<WorkflowStep>`, ce qui n'est pas fiable après un rechargement EF Core depuis
  SQL Server (aucune garantie d'ordre sans `ORDER BY`). Corrigé avant l'écriture de la couche Infrastructure en
  ajoutant une colonne `Position` persistée, source de vérité unique ; `WorkflowVersion.Steps` la trie
  systématiquement à la lecture. Bug de conception détecté et corrigé pendant l'implémentation, pas en
  vérification finale — documenté ici pour mémoire.

## Ce qui n'est PAS dans ce sprint (assumé, périmètre suivant)

- Aucune exécution réelle ou simulée d'un workflow (E3.1 mode simulation, Sprint 3).
- Aucun test de connectivité (E1.5, Sprint 3).
- Détection de cycles complexes dans le graphe de dépendances entre étapes : seul le cas trivial
  (auto-référence, prérequis inexistant) est gardé côté domaine, comme pour les dépendances de composants au
  Sprint 1 — la détection de cycles globale relève du futur moteur d'exécution.

## Vérification de bout en bout (navigateur)

Exécutée avec succès le 2026-08-06 : création d'un workflow "Démarrage complet" (type Démarrage, périmètre
Complet) sur l'environnement Production → ajout de 2 étapes ("Démarrer Cluster Node 1", "Vérifier santé
Bridge") → réordonnancement (↑) vérifié → cycle de statut Brouillon → À valider → Validé → Actif sur la
version 1 → création d'une version 2 (clonage automatique des 2 étapes dans le nouvel ordre, statut Brouillon,
version 1 restée Active) → données vérifiées directement dans SQL Server (tables `Workflows`,
`WorkflowVersions`, `WorkflowSteps`, colonne `Position` correcte).

Point relevé pendant la vérification (hors régression du sprint) : la modification directe de la valeur d'un
champ via JavaScript (`element.value = ...` puis `dispatchEvent`) ne suffit pas à mettre à jour le modèle
Blazor Server lié par `@bind-Value` — seul un événement `input`/`change` déclenché par une vraie interaction
utilisateur (ou l'outil `form_input` du navigateur) est capté. À garder en tête pour toute automatisation de
test UI future sur ce projet.
