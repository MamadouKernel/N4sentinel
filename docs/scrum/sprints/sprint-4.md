# Sprint 4 — Premières opérations réelles (risque maîtrisé)

**Objectif de sprint** : permettre le **premier pilotage réellement exécuté** (via le connecteur Simulation,
toujours aucun accès réseau réel — cf. décision Sprint 2) d'un workflow, avec les garde-fous minimaux exigés
par le cahier des charges avant toute action mutative en Production : motif/référence obligatoires (FR-011)
et double validation (lancement ≠ approbation).

## Sprint Backlog

| Story | Résultat |
|---|---|
| E2.2 — Motif et référence obligatoires en Production (FR-011) | Fait |
| E3.4 — Opération partielle/unitaire | Fait |
| E3.6 — Double validation (lancement ≠ approbation) | Fait |

## Décisions de conception

- **`OperationRun`** est le nouvel agrégat racine du pilotage réel : il référence un `Workflow` +
  `WorkflowVersion` (jamais une version Brouillon — seule une version Validée/Active peut faire l'objet d'une
  opération, comme pour la simulation Sprint 3), porte les champs FR-011, un statut de cycle de vie
  (`PendingApproval → Approved/Rejected → Running → Completed/Failed`), et un instantané d'exécution par étape
  (`OperationStepExecution`).
- **FR-011 conditionnel à l'environnement** : les 4 champs (motif, fenêtre d'intervention, impact, référence)
  sont obligatoires uniquement si l'environnement ciblé est de type Production — géré par une règle de domaine
  paramétrée (`isProductionEnvironment`), pas par une simple validation de formulaire côté UI qui pourrait être
  contournée.
- **Double validation = Production uniquement** : une opération sur un environnement non-Production passe
  directement à `Approved` (auto-approuvée) ; en Production, elle reste `PendingApproval` jusqu'à ce qu'un
  utilisateur **différent du demandeur** l'approuve. La règle "un Administrateur ne peut pas approuver son
  propre contournement" (E11.2, séparation des responsabilités complète) est une épopée dédiée au Sprint 8 —
  ici, on pose seulement le garde-fou minimal "demandeur ≠ approbateur", suffisant pour E3.6.
- **"Opération partielle/unitaire" (E3.4) découle du périmètre déjà défini au workflow** (FR `Scope` +
  `TargetComponentIds`, Sprint 2) — pas d'une sélection d'étapes à la volée au moment de l'exécution. Exécuter
  un workflow dont le `Scope` est `Partial` ou `Unit` EST l'opération partielle/unitaire. Décision cohérente
  avec l'architecture posée au Sprint 2, évite un système de sélection ad hoc redondant.
- **Aucune tentative automatique / reprise sur échec dans ce sprint** : chaque étape s'exécute une seule fois ;
  si elle échoue, la politique `OnFailurePolicy` de l'étape (Sprint 2, FR-004) détermine si l'exécution
  s'arrête, continue avec avertissement, ou nécessite une décision manuelle. Les vraies tentatives automatiques
  avec délai et la reprise depuis le dernier point de contrôle valide sont l'objet dédié du Sprint 5 (E3.5) —
  ne pas les préconstruire ici évite une machinerie d'exécution en arrière-plan (jobs/Hangfire) non justifiée
  avant que le besoin de reprise soit lui-même implémenté.
- **L'orchestration (boucle sur les étapes, appel au connecteur) vit dans l'Application**, jamais dans le
  Domain : `OperationRun` n'a aucune dépendance vers `IServerConnector`. Le handler `ExecuteOperationRunCommand`
  appelle le connecteur puis reporte chaque résultat via de petites méthodes de mutation du domaine
  (`RecordStepStarted`, `RecordStepSucceeded`, `RecordStepFailed`).

## Vérification de bout en bout (navigateur)

Exécutée avec succès le 2026-08-07, deux scénarios :

1. **Environnement Production** : lancement d'une opération sans les champs FR-011 → refusé côté domaine ;
   formulaire rempli (motif, fenêtre, impact, référence) → opération créée au statut **En attente
   d'approbation** ; le demandeur (même utilisateur admin) ne voit **pas** les boutons Approuver/Rejeter et un
   message explique que E3.6 exige un autre utilisateur — garde-fou vérifié de bout en bout (UI → Application →
   Domain).
2. **Environnement UAT** (non-Production, créé pour ce test avec un composant "Pilotable" et un workflow
   dédié) : opération créée directement au statut **Approuvée** (auto-approbation hors Production, conforme à
   la décision de conception) → bouton "Exécuter l'opération" → statut passe à **Terminée**, l'étape affiche
   **Réussie** avec le message renvoyé par `SimulationServerConnector` ("Action 'démarrage' simulée avec
   succès.") — persistance confirmée en base (`OperationRuns`, `OperationStepExecutions`).

76 tests unitaires verts (53 Domain + 23 Application) après la vérification.
