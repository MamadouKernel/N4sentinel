# Sprint 7 — Pilotage : démarrage complet & cartographie des systèmes dépendants

**Objectif de sprint** : permettre d'exécuter un scénario de démarrage complet de l'écosystème N4 dans
l'ordre canonique documenté par Navis (E3.3), et cartographier les systèmes dépendants externes au TOS N4
avec leur caractère pilotable ou non (E1.6).

## Sprint Backlog

| Story | Résultat |
|---|---|
| E3.3 — Scénario de démarrage complet respectant l'ordre Cluster Nodes → Center/Standby Node → Bridge → XPS → ECN4/ECN4Web | Fait |
| E1.6 — Cartographie des systèmes dépendants (CAMCO/GOS, DGPS, RMS/Reefer Runner, IPAKI, Scangate, EDI) et de leur caractère pilotable | Fait |

## Décisions de conception

- **E3.3 ne nécessite aucun nouveau moteur d'exécution** : le moteur pas-à-pas construit pour E3.2 (arrêt
  complet, Sprint 5) — `ExecuteNextOperationStepCommand`/`ConfirmOperationStepCommand`/`ResumeOperationRunCommand`
  — est générique sur `WorkflowType` et fonctionne à l'identique pour un workflow `Start`/`Full`. Rien n'a été
  dupliqué ; seule une donnée (un workflow correctement construit avec les noms de service et l'ordre exacts
  de `docs/navis-reference.md` §1-2) matérialise le scénario de démarrage.
- **Nouvelle règle de domaine : un prérequis doit être positionné avant l'étape qui en dépend.**
  `WorkflowVersion.PrerequisiteStepIds` existait déjà (capturé à la création d'une étape) mais n'était
  jusqu'ici qu'une annotation documentaire — rien n'empêchait un Administrateur de déclarer, par erreur, une
  étape "XPS" dépendante de "Bridge Daemon" tout en positionnant XPS avant Bridge Daemon dans la séquence.
  Comme l'exécution avance strictement dans l'ordre de `Position` (`OperationRun.NextPendingStep`), une telle
  incohérence aurait laissé XPS s'exécuter avant que Bridge Daemon ne soit `Succeeded` — violant "LA règle de
  séquencement la plus critique du système" (Bridge Daemon pleinement `Active` avant XPS, cf.
  `navis-reference.md` §2). `WorkflowVersion.UpdateStep` refuse désormais tout prérequis dont la `Position`
  n'est pas strictement antérieure à celle de l'étape modifiée, et `MoveStepUp`/`MoveStepDown` (qui échangent
  deux étapes adjacentes) refusent tout échange entre deux étapes lorsque l'une est le prérequis direct de
  l'autre — combinées à l'exécution strictement séquentielle déjà existante, ces deux garde-fous garantissent
  structurellement qu'un prérequis a toujours fini de s'exécuter avant l'étape qui en dépend, sans avoir
  besoin d'une vérification supplémentaire au moment de l'exécution.
- **E1.6 : nouvelle entité `DependentSystem`, distincte de `N4Component`.** Les six systèmes nommés
  (CAMCO/GOS, DGPS, RMS/Reefer Runner, IPAKI, Scangate, EDI) sont des intégrations externes au TOS Navis N4
  lui-même — contrairement à `N4Component`, ils ne sont jamais la cible d'une étape de workflow (aucune
  action Start/Stop/Restart n'a de sens dessus depuis N4 Sentinel) et n'ont pas les attributs techniques d'un
  composant N4 (service Windows, dépendances de séquencement...). Les modéliser comme des `N4Component`
  aurait pollué le sélecteur de composant du formulaire d'étape de workflow avec des entrées qu'aucun
  connecteur ne peut réellement piloter. `DependentSystem` réutilise volontairement l'enum
  `ComponentGovernance` existant (Pilotable / Supervisé uniquement / Non supervisé) plutôt que d'en dupliquer
  un — c'est exactement le vocabulaire "caractère pilotable ou non" demandé par la story, déjà éprouvé sur
  `N4Component`.
- **`DependentSystem` reste rattaché à un environnement** (comme `N4Component`, `Workflow`...), pas un
  catalogue global : cela permet de documenter par exemple qu'un EDI de bac à sable existe en UAT
  (Non supervisé) alors que l'EDI de Production est réellement surveillé (Supervisé uniquement) — cohérent
  avec le reste du référentiel, entièrement organisé par environnement.
- **Aucune donnée de démonstration codée en dur** : conformément à `navis-reference.md` §7 (qui signale
  explicitement que les données de démo des Sprints 1-2 ne devaient pas être reconduites telles quelles),
  le scénario de démarrage complet et la cartographie des systèmes dépendants ont été construits à la main
  dans le navigateur lors de la vérification de bout en bout, avec les noms de service réels — pas de seeder
  permanent ajouté au code.

## Vérification de bout en bout (navigateur)

Exécutée avec succès le 2026-08-07, environnement UAT :

1. **Composants réels** : 6 composants créés avec les noms de service exacts de `navis-reference.md` §1
   (Navis N4 Cluster Node, Navis N4 Center Node, Navis XPS Bridge Daemon, Navis XPS, Navis ECN4 Daemon,
   Navis ECN4Web), tous Pilotables.
2. **Workflow "Démarrage complet UAT"** (Type=Démarrage, Périmètre=Complet) construit avec 6 étapes dans
   l'ordre canonique §2 : Cluster Node → Center Node → Bridge Daemon → XPS (prérequis = Bridge Daemon) →
   ECN4 → ECN4Web.
3. **Règle de séquencement confirmée positivement** : déclarer Bridge Daemon comme prérequis de XPS (Bridge
   Daemon positionné avant) a été accepté sans erreur.
4. **Règle de séquencement confirmée négativement** : une tentative de faire remonter "Démarrer XPS"
   au-dessus de "Démarrer XPS Bridge Daemon" (bouton ↑) a été refusée avec le message *"Impossible d'inverser
   l'ordre de 'Démarrer XPS Bridge Daemon' et 'Démarrer XPS' : l'une est le prérequis direct de l'autre."* —
   l'ordre des étapes est resté inchangé.
5. **Cycle de validation puis exécution complète** : version soumise → validée → activée ; opération créée
   (auto-approuvée, environnement non-Production) ; les 6 étapes exécutées une à une via "Exécuter l'étape
   suivante" ont toutes atteint le statut **Réussie**, dans l'ordre, et l'opération est passée à **Terminée**.
   Bridge Daemon (étape 3) était bien `Succeeded` avant que XPS (étape 4) ne s'exécute.
6. **Cartographie des systèmes dépendants** : les 6 systèmes nommés par la story (CAMCO/GOS, DGPS,
   RMS/Reefer Runner, IPAKI, Scangate, EDI) créés pour l'environnement UAT avec une gouvernance Supervisé
   uniquement/Non supervisé selon leur nature ; confirmé qu'aucun n'apparaît dans le sélecteur de composant
   du formulaire de création d'étape de workflow (qui ne liste que les `N4Component`).

93 tests unitaires verts (66 Domain + 27 Application) après la vérification.
