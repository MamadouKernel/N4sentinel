# Sprint 3 — Mode simulation & préparation d'opération

**Objectif de sprint** : permettre à un utilisateur habilité de sélectionner un scénario (workflow) validé et
actif pour un environnement, de tester la connectivité des composants sans action mutative, et de lancer une
**simulation** complète du workflow — sans exécuter aucune commande technique réelle — dont le résultat est
conservé. Toujours aucune exécution réelle (E3.2+ = Sprints 5-6).

## Sprint Backlog

| Story | Résultat |
|---|---|
| E1.5 — Test de connectivité sans action mutative | Fait |
| E3.1 — Mode simulation (FR-005) | Fait |
| E2.1 — Sélection de scénario (FR-010) | Fait |

## Décisions de conception

- **`WorkflowSimulation` est un agrégat immuable** : construit une seule fois par
  `SimulateWorkflowCommand`, jamais modifié après création — c'est un instantané ("snapshot"), pas une entité
  de travail. Conforme à FR-005 : "le résultat de la simulation doit pouvoir être conservé et rattaché à la
  demande d'opération."
- **La simulation appelle `IServerConnector.CheckHealthAsync` (lecture seule) pour chaque étape ciblant un
  composant** — jamais `StartAsync`/`StopAsync`/`RestartAsync`. C'est la garantie structurelle qu'aucune
  commande mutative n'est exécutée, pas seulement une convention respectée par accident.
- **Un "prérequis non satisfait" en Sprint 3 signifie : le composant ciblé par l'étape n'est pas `Controllable`**
  (gouvernance Supervisé uniquement / Non supervisé). L'ordre structurel des étapes est déjà garanti par le
  domaine (Sprint 2) — donc la simulation ne revalide pas l'ordre, elle expose plutôt les risques d'exécution
  réels (composant non pilotable, action critique/destructrice, confirmation/approbation requise).
- **Seuls les workflows avec une version Active sont proposés à la sélection de scénario** (FR-010 : "Seuls
  les scénarios validés et actifs... doivent être proposés"), cohérent avec le cycle de validation du
  Sprint 2.
- **Le test de connectivité (E1.5) n'est pas persisté** — contrairement à la simulation, c'est un contrôle
  ponctuel avant activation d'un environnement, pas un artefact à conserver dans l'historique (le cahier des
  charges ne l'exige pas pour FR-007, contrairement à FR-005 pour la simulation).
- **Grounding Navis** : les libellés de statut affichés reprennent exactement le vocabulaire réel du Cluster
  Services view (`docs/navis-reference.md` §4) — pas un vocabulaire inventé, cohérent avec la correction
  apportée à `ComponentHealthStatus` au Sprint 2.
- **Correctif de layering découvert en cours de sprint** : `ComponentHealthStatus` avait été placé dans
  `N4Sentinel.Application` au Sprint 2. Domain (couche la plus interne) ne peut pas référencer Application —
  déplacé vers `N4Sentinel.Domain.Entities`, `IServerConnector` s'appuie maintenant dessus par simple
  résolution de type. Aucune régression (0 avertissement, 61 tests toujours verts après correction).

## Vérification de bout en bout (navigateur)

Exécutée avec succès le 2026-08-07 : test de connectivité sur l'environnement Production (composant "Cluster
Node 1" → ACTIVE) → sélection de scénario (seul "Démarrage complet" v1 Active proposé, conforme FR-010) →
simulation lancée → résultat correctement affiché comme **Bloquant** : l'étape "Démarrer Cluster Node 1" est
refusée car le composant a la gouvernance "Supervisé uniquement" (pas Pilotable) — exactement le comportement
attendu, aucune commande mutative exécutée. Simulation persistée et consultable depuis l'historique. Incident
d'environnement sans rapport avec le code : l'instance SQL Server LocalDB s'est bloquée en cours de session
(named pipe, `sqlcmd` et l'appli échouaient tous deux) — résolu par `SqlLocalDB.exe stop -k` puis `start`.
