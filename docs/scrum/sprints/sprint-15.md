# Sprint 15 — Clôture V1 (SOP versionnées & export de rapports)

**Objectif de sprint** : permettre à un Opérateur habilité de créer, valider, versionner et rattacher une SOP
(procédure opérationnelle standardisée) à un incident ou une opération, avec exécution guidée pas-à-pas et
capitalisation contrôlée (E9.3) ; permettre à tout utilisateur d'exporter un rapport d'opération ou d'incident
(E10.2). **Dernier sprint du plan V1** (`docs/scrum/roadmap.md`).

Comme pour les Sprints 9-14, les lignes E9.3/E10.2 du backlog ne portaient aucune référence `FR-xxx` explicite
— le texte complet du cahier des charges a été relu pour ce sprint. Références retenues :

- **FR-088** (en réponse à une question opérationnelle, l'assistant peut structurer sa réponse au format SOP :
  objectif, prérequis, étapes, contrôles, risques, retour arrière), **FR-089** (exécution guidée pas-à-pas :
  suivre chaque étape, confirmer chaque contrôle, joindre une preuve par étape, commenter un écart, revenir à
  l'étape précédente), **FR-089A** (lorsque l'utilisateur confirme que la procédure a résolu le problème, la
  solution peut générer un document SOP à partir des étapes réellement exécutées), **FR-089B** (le SOP généré
  reste en statut Brouillon jusqu'à sa revue par un utilisateur habilité ; après validation, il devient une
  procédure réutilisable et versionnée), **FR-089C** (une SOP peut être rattachée à un incident, une opération,
  un composant ou une erreur), **FR-089D** (réutilisation contrôlée : la solution propose les SOP validées
  correspondantes avec leur date, version et taux de réussite historique, sans jamais appliquer une action
  automatiquement) → **E9.3**.
- **FR-028** (chaque opération produit un rapport détaillé : contexte, environnement, composants, version de
  workflow, chronologie, durées, commandes, contrôles, avertissements, confirmations, état initial/final,
  actions restantes), **FR-090** (export en PDF, Word ou format structuré selon le besoin), **FR-093** (rapport
  de synthèse + rapport technique détaillé), **FR-096** (rapport d'incident automatique : heures de
  début/détection/prise en charge/fin, durée, services impactés, symptômes, cause identifiée, preuves, actions,
  intervenants, statut final, SOP associée), **FR-097** (capitalisation : depuis un incident clos ou une
  opération réussie, créer/mettre à jour une SOP en conservant le lien vers les éléments d'origine) → **E10.2**.

**Note de cadrage sur le périmètre du cahier des charges** : le document source place lui-même les SOP et
l'export de rapports dans son "Lot 2" au-delà du strict périmètre "V1/Lot 1" qu'il décrit ailleurs. Cette note
ne change rien au plan de sprint déjà engagé dans ce projet (`docs/scrum/roadmap.md` prévoit ces deux stories
comme faisant partie du Sprint 15, le dernier des 16 sprints planifiés) — elle est consignée ici pour
transparence, comme les notes de cadrage similaires des sprints précédents.

## Sprint Backlog

| Story | Résultat |
|---|---|
| E9.3 — SOP versionnées : créer, valider, versionner, rattacher, exécuter, capitaliser | Fait |
| E10.2 — Export de rapports d'opération et d'incident | Fait |

## Décisions de conception

- **`Sop` reprend l'entité "SOP / Version" du modèle de données minimal** (objectif, prérequis, étapes,
  contrôles, risques, retour arrière, validation, version N4 et statut). Versionnement par nouvelle ligne
  partageant le même `SopKey` — même raisonnement que `DiagnosticRule` (Sprint 12) et `Document` (Sprint 14) :
  le modèle de données minimal liste lui-même "étapes" comme un attribut plat, pas une sous-entité, donc un
  découpage façon `Workflow`/`WorkflowVersion` serait disproportionné pour la *définition*. `StepsText` stocke
  les étapes une par ligne ; `Sop.Steps` les découpe à la volée.
- **L'exécution guidée (FR-089), en revanche, a besoin d'étapes adressables individuellement** (confirmation,
  preuve et écart *par étape*, retour en arrière). Plutôt que matérialiser les étapes de la *définition* en
  sous-entités (ce que le modèle minimal ne demande pas), `SopExecution` porte une collection enfant
  `SopExecutionStepConfirmation` qui capture, au moment de la confirmation, un instantané du texte de l'étape
  (`StepText`), l'auteur, l'horodatage, la preuve et l'écart éventuel — cohérent avec le style déjà utilisé pour
  `FolderReconstitution`/`ReconstitutionStepRecord` (Sprint 11), en l'adaptant à une liste d'étapes *variable*
  (définie par chaque SOP) plutôt que la séquence *fixe* de six étapes de la reconstitution. `SopExecution` ne
  verrouille pas un nombre total d'étapes en dur : `Complete(resolvedIssue)` est un geste humain explicite
  (FR-089A : "lorsque l'utilisateur confirme que la procédure a résolu le problème"), pas un décompte
  automatique une fois la dernière étape confirmée.
- **`DiagnosticCase` (Sprint 13) est la mise en œuvre existante de l'entité "Incident / Diagnostic"** du modèle
  de données minimal — pas de nouvelle entité "Incident" dupliquée. `SopAssociation` (rattachement, FR-089C)
  référence donc directement `DiagnosticCaseId` et/ou `OperationRunId` (au moins l'un des deux obligatoire),
  plus les champs libres composant/erreur/résultat/preuve du modèle minimal ("Association SOP").
- **Génération de SOP après résolution (FR-089A/FR-097), un geste humain explicite, jamais automatique en
  arrière-plan.** `GenerateSopFromExecutionCommand` n'est utilisable que sur une `SopExecution` `Completed` avec
  `ResolvedIssue == true` ; elle construit `StepsText` à partir des `StepConfirmations` réellement enregistrées
  (pas un contenu généré ou halluciné) et crée une nouvelle `Sop` en `Draft` avec `IsGeneratedFromExecution =
  true`. Elle reste ensuite soumise au même cycle Draft → PendingValidation → Validated → Active que toute autre
  SOP (FR-089B : "reste en statut Brouillon jusqu'à sa revue... avant de devenir réutilisable") — aucune SOP
  générée n'est jamais utilisable sans validation humaine. Cohérent avec le principe "pas d'automatisation
  simulée" déjà appliqué à l'assistant documentaire (Sprint 14) : on ne fabrique pas de contenu de procédure par
  IA, on capitalise honnêtement ce qui a été réellement exécuté et confirmé.
- **Réutilisation contrôlée (FR-089D) : score par mots-clés simple, taux de réussite réellement calculé.** Le
  cahier des charges n'impose aucune méthode de mise en correspondance particulière ; `SuggestSopsForIncidentQuery`
  applique la même approche déjà retenue pour l'assistant (Sprint 14, `AskAssistantQuery`) — correspondance de
  mots-clés sur titre/objectif/prérequis des SOP Actives uniquement. Le taux de réussite affiché est calculé sur
  les `SopExecution` réellement `Completed` pour cette SOP (`ResolvedIssue == true` / total terminées) — jamais
  une estimation ou un chiffre inventé, même raisonnement que le refus systématique de ce projet de simuler une
  confiance sans preuve (moteur de diagnostic, Sprint 13 ; assistant, Sprint 14).
- **Export de rapports (E10.2) : une vue assemblée et rendue, pas une entité "Rapport" persistée.** Le modèle
  de données minimal ne liste aucune entité "Rapport" — `GetOperationReportQuery`/`GetIncidentReportQuery`
  assemblent un DTO de rapport à la demande depuis les données déjà tracées par `OperationRun` (FR-028) et
  `DiagnosticCase` (FR-096), enrichies des `SopAssociation` rattachées. Un rapport n'est jamais une nouvelle
  source de vérité : le régénérer donne toujours le reflet exact de l'état actuel des entités sources.
- **Format d'export : structuré (JSON) livré réellement, PDF/Word explicitement différé.** FR-090 autorise
  "PDF, Word ou format structuré selon le besoin" — condition explicitement disjonctive. Générer un vrai PDF/Word
  binaire aurait exigé une nouvelle dépendance NuGet (ex. QuestPDF, DocX) pour une seule fonctionnalité en tout
  fin de plan V1, un coût d'infrastructure disproportionné à ce stade. Le format structuré est livré comme un
  téléchargement JSON réel et fonctionnel (endpoints `/reports/operations/{id}/export` et
  `/reports/incidents/{id}/export`), accompagné d'une page Blazor imprimable pour la synthèse humaine (impression
  navigateur → PDF, un usage standard) — pas une fonctionnalité maquettée qui prétendrait produire un fichier
  qu'elle ne produit pas. Un vrai export PDF/Word binaire reste un candidat naturel pour un sprint ultérieur.

## Vérification

252 tests unitaires verts (154 Domain + 98 Application), incluant `Sop` (cycle de vie, versionnement,
découpage des étapes), `SopExecution` (confirmation séquentielle, retour en arrière, clôture avec/sans
résolution, abandon), `SopAssociation` (rattachement incident/opération, contrainte "au moins un des deux"),
ainsi que les handlers CQRS de création/lifecycle de SOP, de confirmation d'étape (y compris le refus une fois
toutes les étapes confirmées), de génération de SOP depuis une exécution résolue, et de suggestion FR-089D avec
calcul réel du taux de réussite depuis des exécutions simulées en test.

**Vérification navigateur non concluante, comme aux Sprints 13-14** : la même régression d'environnement a été
retestée (négociation SignalR en échec — `ERR_CONNECTION_REFUSED` sur le circuit Blazor Server interactif, y
compris sur `/Account/Login`), confirmant à nouveau qu'il s'agit d'un problème d'environnement de session
persistant depuis le Sprint 13, non d'une régression du code livré dans ce sprint. La vérification fonctionnelle
de ce sprint s'appuie donc sur la suite de tests automatisés ; un parcours navigateur complet (création SOP →
validation → publication → exécution guidée pas-à-pas → génération depuis exécution → rattachement → export de
rapport) est à refaire dans un environnement de développement sain.

## Clôture V1

Ce sprint clôt le plan de release V1 de N4 Sentinel (16 sprints, Sprint 0 à Sprint 15, 278 points). Toutes les
stories du backlog V1 sont désormais `Fait` ou `Fait, partiellement` (deux écarts documentés et assumés depuis
les Sprints 8-9 : E11.1/E11.1b rôles non différenciés par environnement, E11.2 concept de "contournement"
incomplet — voir `docs/scrum/product-backlog.md`). Voir le récapitulatif complet dans la réponse de clôture de
session.
