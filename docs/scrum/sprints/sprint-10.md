# Sprint 10 — Supervision des dossiers partagés et ActiveMQ/KahaDB

**Objectif de sprint** : détecter et afficher les anomalies des dossiers partagés N4 (E5.1) et superviser la
synchronisation N4/Bridge/XPS et l'accumulation de messages ActiveMQ/KahaDB (E5.3).

Comme pour le Sprint 9, les lignes E5.1/E5.3 du backlog ne portent aucune référence `FR-xxx` — le texte
complet du cahier des charges a été relu pour ce sprint. Références retenues :

- **FR-059A** (catégorisation des dossiers partagés : configuration N4, stockage ActiveMQ/KahaDB, échanges
  EDI, archives, dossiers d'erreur), **FR-059B** (contrôles de santé : accessibilité, droits, espace,
  latence, structure, fichiers obligatoires, ancienneté, croissance anormale), **FR-059C** (consommation :
  fichiers reçus/consommés/en attente/bloqués/en erreur), **FR-059D** (détection de corruption — distinguer
  suspicion et corruption confirmée) → **E5.1**.
- **FR-056** (synchronisation N4-XPS : état, retards de messages, incohérences, date du dernier échange
  normal) et la table de collecte des signaux ActiveMQ/KahaDB (taille des files, nombre de consommateurs,
  compteurs enqueue/dequeue, débit, erreurs de persistance) → **E5.3**.
- **FR-054** (alertes : timeout, échec, incohérence d'état, file qui augmente, heartbeat ancien, ressource
  critique) → alimente la section "Alertes" du tableau de bord (Sprint 8) avec les anomalies détectées ici.

## Sprint Backlog

| Story | Résultat |
|---|---|
| E5.1 — Anomalies des dossiers partagés (état, capacité, structure, accessibilité) | Fait |
| E5.3 — Supervision synchronisation N4/Bridge/XPS et accumulation ActiveMQ/KahaDB | Fait |

## Décisions de conception

- **Deux nouvelles entités légères, rattachées à un environnement, sans cible de workflow** —
  `SharedFolder` et `SyncEndpoint` — suivant exactement le précédent posé par `DependentSystem` au Sprint 7 :
  un dossier partagé ou un point de synchronisation Bridge/XPS/ActiveMQ n'est jamais la cible d'une étape de
  workflow (aucune action Démarrer/Arrêter/Redémarrer n'a de sens dessus) et ne partage pas les attributs
  techniques (hôte, service Windows) d'un `N4Component`. Les modéliser comme des `N4Component` aurait pollué
  le sélecteur de composant du formulaire d'étape de workflow, exactement le raisonnement déjà retenu pour
  `DependentSystem`.
- **Pas de réutilisation de `ComponentHealthStatus`.** Ce vocabulaire (`LOADING/WAITING/ACTIVE/...`) décrit le
  cycle de vie d'un service Cluster Services — un dossier partagé n'est pas "en cours de démarrage", il est
  sain ou anormal selon des dimensions différentes (capacité, structure, accessibilité). `SharedFolder` et
  `SyncEndpoint` portent chacun leur propre instantané d'anomalie et une propriété calculée `HasAnomaly`,
  pas une machine à états.
- **Détection d'anomalie = seuils simples dans le domaine, pas un moteur de règles.** Cohérent avec la
  décision du Sprint 8 sur les "Alertes" du dashboard (opérations en échec, pas un moteur de diagnostic) :
  aucun sous-système de règles configurables n'existe encore (Epic 7, sprints ultérieurs). `SharedFolder`
  déclare une anomalie si inaccessible, structure invalide, corruption suspectée/confirmée, ou capacité
  utilisée ≥ 90%. `SyncEndpoint` déclare une anomalie si des messages s'accumulent sans consommateur, si la
  file dépasse un seuil, ou si le dernier échange normal date de plus de 15 minutes — des seuils simples et
  documentés dans le code, pas configurables ce sprint (candidat naturel pour l'Epic 7, qui a explicitement
  vocation à rendre les règles de diagnostic versionnées et configurables).
- **`ISupervisionSignalProvider` : nouvelle abstraction strictement en lecture**, distincte d'`IServerConnector`
  (qui porte les actions Start/Stop/Restart). Un dossier partagé ou une file ActiveMQ n'ont pas d'action
  applicable — le cahier des charges exclut explicitement "toute suppression automatique et non supervisée de
  messages, de files ou de fichiers ActiveMQ/KahaDB". Seule implémentation : `SimulationSupervisionSignalProvider`,
  qui renvoie des valeurs saines simulées (préfixe `[SIMULATION]` dans les logs, comme
  `SimulationServerConnector`) — aucun accès réseau réel, cohérent avec la contrainte déjà posée depuis le
  Sprint 2.
- **Aucun sondage automatique périodique.** Comme pour le test de connectivité (FR-007, Sprint 1) et le choix
  du Sprint 8 de ne pas sonder la santé des composants à chaque rafraîchissement du dashboard, la vérification
  d'un dossier partagé ou d'un point de synchronisation est une action explicite ("Vérifier maintenant"),
  jamais automatique — le dashboard affiche le dernier instantané connu, pas un sondage en direct.
- **Le dashboard (E4.1, Sprint 8) est étendu, pas dupliqué** : la section "Alertes" affiche désormais aussi
  les dossiers partagés et points de synchronisation en anomalie, à côté des opérations échouées — cohérent
  avec FR-054 qui définit une alerte unique couvrant timeout/échec/incohérence/file croissante/heartbeat
  ancien/ressource critique, tous domaines confondus.

## Vérification de bout en bout (navigateur)

Exécutée avec succès le 2026-08-07, environnement Production :

1. **Dossier partagé** : création "AMQ Store" (catégorie ActiveMQ/KahaDB, chemin réseau) depuis
   `/environments/{id}/supervision/shared-folders/new`, apparaît dans la liste avec "Jamais vérifié" ; clic
   sur "Vérifier" → capacité 40%, structure OK, corruption Aucune, horodatage de dernière vérification mis à
   jour (signal simulé sain).
2. **Point de synchronisation** : création "Bridge Queue" depuis `/environments/{id}/supervision/sync-endpoints/new`,
   apparaît dans la liste.
3. **Dashboard** : la section "Alertes — dossiers partagés & synchronisation" (nouvelle) est correctement
   vide tant qu'aucune anomalie n'est détectée — comportement attendu puisque le fournisseur de signaux
   Simulation renvoie systématiquement des valeurs saines. La logique de remontée d'une anomalie réelle est
   couverte par un test unitaire dédié (`Handle_IncludesSharedFolderAndSyncEndpointAnomaliesInSupervisionAlerts`).

125 tests unitaires verts (79 Domain + 46 Application) après la vérification.
