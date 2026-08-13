# Traçabilité — corpus SOP ↔ implémentation

**Source primaire** : `Corpus_Complet_Support_Navis_N4` — SOP-0 à SOP-3, guide de diagnostic,
scripts d'exploitation PowerShell (SOP-2).

Ce document existe à cause d'une erreur de méthode. Les Sprints 7 et 8 ont d'abord été
construits depuis le plan de sprints et les scripts PowerShell, qui **dérivent** des SOP sans
les remplacer. La lecture tardive de la source primaire a immédiatement révélé une règle
implémentée au tiers. Ce tableau existe pour que la prochaine règle ne soit pas découverte au
même moment.

> **Avertissement de méthode.** Les scripts d'exploitation ne sont pas une validation des SOP :
> leur README indique qu'ils n'ont jamais été exécutés contre un environnement N4 réel, et
> qu'ils ont été « écrits à partir de la documentation ». Deux lectures indépendantes du même
> document qui concordent réduisent le risque de contresens ; elles ne prouvent rien sur le
> terrain.

## Séquences

| Règle du corpus | Source | État |
|---|---|---|
| Arrêt : ECN4Web → ECN4 → XPS → Bridge → Standby → Cluster → Center | `Stop-N4Sequence.ps1` | `SequenceDArretDeReferenceN4`, vérifié à l'activation d'un workflow |
| Démarrage : Cluster → Center → Bridge → XPS → ECN4 | SOP-2 Fiche A | `SequenceDeDemarrageDeReferenceN4`, table **distincte** de l'arrêt |
| « Redémarrer dans l'ordre : Bridge, puis XPS, puis ECN4 (jamais dans le désordre) » | SOP-2 Fiche C | Couvert par la table de démarrage |
| Cluster Nodes un par un, chacun confirmé avant le suivant | SOP-2, plan S8 | `ControlesDeDemarrage.VerifierLeNoeudPrecedent`, branché sur le moteur |
| Standby non démarré automatiquement | `Start-N4Sequence.ps1` | Contraint s'il est séquencé, jamais généré |

Le démarrage **n'est pas** l'arrêt inversé : les Cluster Nodes démarrent avant le Center et
s'arrêtent après lui. Un test l'affirme explicitement, pour empêcher la simplification évidente.

## Dépendances

> SOP-2, annexe technique : « XPS a besoin du Bridge actif, le Bridge a besoin du Center,
> ECN4Web a besoin d'ECN4. Démarrer ou arrêter dans le mauvais ordre provoque des erreurs en
> cascade qui ressemblent à de nouveaux incidents alors qu'il s'agit simplement d'une séquence
> non respectée. »

Les trois liens sont portés par `ControlesDeDemarrage.VerifierLaDependance`, appliqués par le
moteur juste avant d'émettre. Un prérequis dégradé ne satisfait pas la dépendance ; un prérequis
absent du référentiel non plus.

## Mécanismes que le corpus documente et que l'application n'observe pas encore

| Mécanisme | Ce que dit le corpus | État |
|---|---|---|
| Rôle actif Center/Standby | « Basculement contrôlé par un verrou en base de données ou un verrou fichier ActiveMQ » (SOP-2) | Règle écrite et testée (`DetecterUnConflitDeCenter`) ; **aucun connecteur ne lit ce verrou** |
| Files JMX du Bridge | `QueueSize`, `DequeueCount`, `InFlightCount`, `ConsumerCount` ; « ConsumerCount à 0 est souvent le symptôme le plus fiable d'un nœud bloqué » | Aucun connecteur JMX — Sprint 22 au plan |
| Écart d'horloge | « Écart < 1 seconde », contrôle quotidien, Top 10 des causes de P1 selon Kaleris | Règle écrite et testée (`SynchronisationDesHorloges`) ; **aucun connecteur ne relit les horloges distantes** |
| Statut applicatif N4 | ACTIVE / LOADING / WAITING dans Cluster Services, « pas strictement identique au statut du service Windows » | C'est `FR-016` ; le connecteur Cluster Services n'existe pas |

Ces quatre lignes partagent une cause unique : **les accès techniques N4 ne sont pas ouverts**.
Les règles sont écrites parce qu'elles se testent sans accès ; leurs sources attendent.

## Pourquoi deux Center actifs est grave

Le plan S8 demande de « détecter le conflit où deux Center seraient actifs ». Le corpus dit
pourquoi, et c'est plus fort qu'un doublon de rôle : l'écriture concurrente Center/Standby
corrompt `db.data` (KahaDB), et alors « plus aucun service ne peut démarrer correctement ».
C'est la cause n°1 de la corruption `amq` traitée en Fiche A — un incident qui bloque tout
redémarrage, pas une gêne.

## Checklist quotidienne SOP-3 et couverture actuelle

| Contrôle quotidien | Couverture |
|---|---|
| Tous les nœuds ACTIFS dans Cluster Services | Partielle — état de service, pas statut applicatif |
| Horloges synchronisées, écart < 1 s | Règle écrite, source manquante |
| Indicateurs JMX sur le Center | Non couvert |
| Espace disque sur tous les serveurs | Non couvert — seuils du corpus : alerte sous 10 %, vigilance sous 20 % |
| Aucun lot EDI bloqué en UNKNOWN | Non couvert — Sprint 15 |
| Aucune nouvelle ligne ERROR dans `navis-apex.log` | Non couvert — Sprint 10 |
| Aucune tâche lourde en conflit avec le pic du matin | Non couvert |

## Ce qui reste à lire

SOP-1 (support Niveau 1) n'a pas été dépouillé ligne à ligne. SOP-0 l'a été pour ses règles
d'escalade, qui relèvent des Sprints 10 à 12 ; une phrase mérite d'y être reprise telle quelle,
parce qu'elle énonce le principe que le moteur applique déjà : **« le doute mène toujours à
l'escalade, jamais à une tentative non documentée. »**
