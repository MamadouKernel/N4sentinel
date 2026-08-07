# Sprint 11 — Reconstitution sécurisée de dossier partagé & suivi EDI

**Objectif de sprint** : offrir aux opérateurs habilités un guide tracé et audité pour reconstituer un
dossier partagé après suspicion de corruption (E5.2), et un tableau de suivi des fichiers d'interface EDI par
type de message, partenaire et statut (E6.1).

Comme pour les Sprints 9 et 10, les lignes E5.2/E6.1 du backlog ne portaient aucune référence `FR-xxx` — le
texte complet du cahier des charges a été relu pour ce sprint. Références retenues :

- **FR-059E** (reconstitution sécurisée : après validation d'un utilisateur habilité, exécuter uniquement un
  workflow approuvé — arrêt des composants requis, sauvegarde préalable, vérification de l'intégrité de la
  sauvegarde, reconstitution, redémarrage contrôlé et tests finaux), **FR-059F** (mode guidé : si
  l'automatisation n'est pas autorisée ou si le cas dépasse les règles validées, présenter la procédure sous
  forme de SOP pas-à-pas, recueillir la confirmation et les preuves à chaque étape), **FR-059G** (protection
  des données : aucune suppression ou reconstruction sans sauvegarde, confirmation explicite, contrôle des
  services arrêtés et journal d'audit ; les cas incertains doivent être escaladés) → **E5.2**.
- **FR-059H** (tableau EDI : afficher les fichiers reçus, intégrés, en attente, rejetés ou non consommés, par
  type de message, partenaire et environnement), **FR-059I** (alertes EDI : délai dépassé, non consommé,
  échecs répétés, ou aucune intégration réussie sur une période définie), **FR-059J** (diagnostic EDI :
  associer le fichier, les logs, le message d'erreur et la chronologie) → **E6.1**.

## Sprint Backlog

| Story | Résultat |
|---|---|
| E5.2 — Reconstitution sécurisée d'un dossier partagé, guidée et tracée | Fait |
| E6.1 — Suivi des fichiers EDI reçus/consommés/en attente/rejetés/en erreur | Fait |

## Décisions de conception

- **Reconstitution guidée et tracée, pas une automatisation de bout en bout.** FR-059E, lu littéralement,
  décrit un "workflow approuvé" que la solution "exécute". Mais le cahier des charges situe lui-même
  l'orchestration automatique de bout en bout sans confirmation humaine au **Palier 2** ("Niveau
  d'automatisation du pilotage retenu par la DSI"), explicitement hors périmètre V1 : le Palier 1 retenu reste
  "semi-automatique", avec confirmation humaine à chaque action mutative, et aucun connecteur réel n'existe
  encore pour agir sur un dossier partagé ou un service (mode Simulation uniquement depuis le Sprint 2,
  confirmé pour la supervision au Sprint 10). `FolderReconstitution` implémente donc la séquence fixe de
  FR-059E (arrêt, sauvegarde, vérification, reconstitution, redémarrage, tests) comme un **guide SOP pas-à-pas
  audité** (FR-059F) — chaque étape exige une confirmation humaine explicite et peut porter une preuve — sans
  déclencher aucune action réelle de fichier ou de service. C'est la lecture cohérente avec toutes les
  décisions de cadrage précédentes sur le mode Simulation ; une automatisation réelle de la reconstitution est
  un candidat naturel pour le Palier 2, hors périmètre de cette session.
- **Pas de réutilisation du moteur `Workflow`/`OperationRun`.** Ce moteur cible des `N4Component` (actions
  Démarrer/Arrêter/Redémarrer via `IServerConnector`) ; une reconstitution porte sur un `SharedFolder` et sa
  séquence de 6 étapes fixes (arrêt/sauvegarde/vérification/reconstitution/redémarrage/tests) ne correspond pas
  au vocabulaire `WorkflowStepAction`. `FolderReconstitution` est une entité dédiée, légère, avec sa propre
  machine à états (`InProgress` → `Completed`/`Aborted`) et sa collection `ReconstitutionStepRecord`
  (qui/quand/preuve par étape), suivant le même raisonnement de non-réutilisation déjà posé pour
  `DependentSystem` (Sprint 7) et `SharedFolder`/`SyncEndpoint` (Sprint 10).
- **Pas de réutilisation de la future entité SOP versionnée d'E9.3.** Le cahier des charges lui-même sépare
  deux niveaux de "SOP" : FR-059F ne demande que de présenter le guide pas-à-pas et de recueillir
  confirmation/preuve par étape — pas de versionnement, de génération automatique depuis des étapes réellement
  exécutées, ni de réutilisation historique. Ces capacités (FR-088/FR-089A-D : génération après succès,
  validation en brouillon, réutilisation contrôlée avec taux de succès historique) appartiennent à la base
  documentaire et assistant N4 (Epic 9, E9.3, planifié au Sprint 15). Coupler `FolderReconstitution` à une
  entité SOP qui n'existe pas encore aurait été un couplage prématuré ; `FolderReconstitution` reste un
  enregistrement minimal, non versionné, scopé à un seul événement de reconstitution.
- **Suivi EDI déclaratif, pas une écoute réelle des flux.** Comme pour `SharedFolder`/`SyncEndpoint`, N4
  Sentinel ne dispose d'aucun connecteur EDI réel : `EdiFile` est alimenté par des actions explicites de
  l'opérateur (enregistrement d'un fichier reçu, puis marquage Consommé/Rejeté/Échec), pas par une écoute
  automatique d'un répertoire d'échange. Reprend le modèle de données "Fichier d'interface / EDI" du cahier
  des charges (type, partenaire, réception, consommation, statut, erreur, ancienneté).
- **Détection d'anomalie EDI = seuils simples, cohérent avec la décision du Sprint 10** (pas un moteur de
  règles, Epic 7 hors périmètre) : `EdiFile.HasAnomaly` est vrai si rejeté/en erreur, si le nombre de
  tentatives atteint 3, ou si le fichier reste reçu/en attente au-delà d'un délai attendu documenté (60
  minutes) — FR-059I.
- **Le tableau de bord (E4.1) est étendu une troisième fois, pas dupliqué** : la section "Alertes" existante
  (Sprint 10) inclut désormais aussi les fichiers EDI en anomalie, à côté des dossiers partagés et points de
  synchronisation — cohérent avec FR-054 qui définit une alerte unique tous domaines confondus.
- **Création de fichier EDI réservée à l'Administrateur** (cohérent avec la création de `SharedFolder`/
  `SyncEndpoint` au Sprint 10), mais **les transitions d'état (Consommé/Échec) et les actions de reconstitution
  sont réservées à Opérateur/Administrateur** (cohérent avec les autres actions de pilotage — lancement/
  confirmation d'opération). Les commandes de reconstitution (démarrage, confirmation d'étape, abandon)
  implémentent `IAuditableRequest` (E10.1) : ce sont des actions sensibles sur une suspicion de corruption en
  Production, au même titre que les confirmations d'étape sensible d'une opération (Sprint 5/9).

## Vérification de bout en bout (navigateur)

Exécutée avec succès le 2026-08-07, environnement Production :

1. **Reconstitution** : depuis `/environments/{id}/supervision`, lien "Reconstitution" sur le dossier "AMQ
   Store" (créé au Sprint 10) → page `/environments/{id}/supervision/shared-folders/{folderId}/reconstitution`.
   Démarrage d'une reconstitution avec motif "Suspicion de corruption KahaDB confirmée par le diagnostic" →
   apparaît "En cours", étape 1/6 ("Arrêt des composants requis") en attente. Confirmation de la première
   étape avec preuve "Cluster arrêté proprement, confirmé par capture d'écran" → l'étape passe en vert avec
   l'auteur, l'horodatage et la preuve affichés ; la prochaine étape ("Sauvegarde préalable") devient active ;
   l'historique affiche "1 / 6" étapes confirmées.
2. **EDI** : depuis le détail de l'environnement Production, lien "Suivi des intégrations EDI" →
   `/environments/{id}/edi`. Enregistrement d'un fichier reçu (type "BAPLIE", partenaire "Armateur X") → statut
   "Reçu". Clic sur "Échec" → statut passe à "En erreur", tentatives = 1, dernière erreur affichée.
3. **Dashboard** : la section "Alertes — dossiers partagés & synchronisation" (Sprint 10) affiche désormais
   correctement la ligne EDI en anomalie (Type "EDI", Nom "BAPLIE — Armateur X", l'erreur d'intégration) —
   confirme que l'extension du dashboard couvre bien les trois domaines (dossiers partagés, synchronisation,
   EDI) sans duplication de section.

148 tests unitaires verts (94 Domain + 54 Application) après la vérification.
