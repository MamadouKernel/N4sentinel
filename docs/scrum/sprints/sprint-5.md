# Sprint 5 — Pilotage : arrêt complet, confirmation aux étapes sensibles, reprise sur échec

**Objectif de sprint** : passer d'une exécution "tout ou rien" (Sprint 4) à une exécution **pas à pas**, qui
marque une pause explicite avant toute étape critique/destructrice ou nécessitant une confirmation
(Palier 1 du cahier des charges), et qui permet de **reprendre depuis le dernier point de contrôle valide**
après un échec plutôt que de relancer l'opération depuis le début.

## Sprint Backlog

| Story | Résultat |
|---|---|
| E3.2 — Scénario d'arrêt complet avec confirmation aux étapes sensibles | Fait |
| E3.5 — Arrêt sur échec + reprise depuis le dernier point de contrôle valide | Fait |

## Décisions de conception

- **Exécution refactorée en pas à pas** : `ExecuteOperationRunCommand` (Sprint 4, qui exécutait toutes les
  étapes en boucle dans un seul appel) est remplacé par `ExecuteNextOperationStepCommand`, qui traite une
  seule étape puis rend la main. C'est le changement structurel qui rend possible la pause avant confirmation
  — une boucle synchrone unique ne peut pas s'arrêter au milieu pour attendre un humain.
- **Confirmation = nouvel état, pas une action automatique sautée** : une étape dont `RequiresConfirmation`
  ou `RequiresApproval` est vraie passe au statut `AwaitingConfirmation` **sans appeler le connecteur** ;
  seule la commande explicite `ConfirmOperationStepCommand` (déclenchée par un clic opérateur) exécute
  réellement l'action. Aucune étape sensible ne s'exécute donc jamais sans un geste humain explicite.
- **"Reprise depuis le dernier point de contrôle valide" = reprise pilotée par l'opérateur, pas une
  automatisation en tâche de fond.** Quand une opération est `Failed`, `ResumeOperationRunCommand` remet
  l'étape en échec à `Pending` et repasse l'opération à `Running` ; les étapes déjà `Succeeded` ne sont jamais
  ré-exécutées. Décision assumée : implémenter de vraies nouvelles tentatives **automatiques** avec délai
  (`RetryIsAutomatic`/`RetryDelaySeconds` du workflow, FR-004) nécessiterait une infrastructure de tâches de
  fond (ex. `IHostedService`/queue) qui n'est pas justifiée tant que les connecteurs restent en mode
  Simulation — une fausse temporisation dans une requête HTTP serait trompeuse. Cette automatisation réelle
  est reportée à une story dédiée, une fois les connecteurs réels en place.
- **Compatibilité ascendante** : `ExecuteOperationRunCommand` du Sprint 4 est retiré au profit du nouveau
  modèle pas-à-pas — aucune opération n'était encore en cours d'exécution en base au moment du changement
  (l'unique opération `Completed`/`Failed` de test du Sprint 4 reste lisible, son historique n'est pas affecté).
- **Aucune migration EF Core nécessaire** : le nouveau statut `AwaitingConfirmation` est un ajout à un enum
  déjà mappé en colonne `string` (`HasConversion<string>()`) — pas de nouvelle colonne. Une migration test a
  été générée puis retirée (`dotnet ef migrations remove`) une fois confirmé qu'elle était vide, pour ne pas
  polluer l'historique de migrations avec une entrée sans effet.

## Vérification de bout en bout (navigateur)

Exécutée avec succès le 2026-08-07, environnement UAT, deux scénarios :

1. **Confirmation d'étape sensible** : workflow "Arrêt UAT avec confirmation" avec une étape
   `RequiresConfirmation=true` → l'opération passe en "En cours" mais l'étape reste **Confirmation requise**
   sans qu'aucune commande n'ait été envoyée au connecteur → clic sur "Confirmer et exécuter" → l'étape passe
   à Réussie et l'opération à Terminée.
2. **Échec puis reprise** : gouvernance du composant temporairement changée en "Supervisé uniquement" pour
   forcer un échec → opération **Échouée**, bouton "Reprendre" visible → gouvernance corrigée en "Pilotable"
   → clic sur "Reprendre" → l'étape repasse à **En attente**, l'opération à **En cours** (sans relancer les
   étapes déjà réussies, ici il n'y en avait qu'une) → nouvelle exécution → **Réussie** → opération
   **Terminée**.

82 tests unitaires verts (57 Domain + 25 Application) après la vérification.
