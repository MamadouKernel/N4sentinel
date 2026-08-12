# Sprint 3 — Connecteurs et preuve technique

**Semaines 7–8 · Lot 1 · Statut : livré, avec une réserve majeure**

**Objectif** — établir l'état réel d'un composant à partir de plusieurs signaux, et savoir dire
« je ne sais pas » quand ils manquent.

---

## La réserve, d'abord

Le plan désigne ce sprint comme le plus risqué et le déclare **bloqué sans les accès techniques
CIT**. Ces accès n'ont pas été ouverts. Aucun serveur N4 n'a été interrogé, et **rien de ce qui
suit ne constitue une validation contre l'écosystème N4 de CIT**.

Ce qui a été livré est donc :

- la mécanique complète de collecte et de consolidation, **exercée contre de vraies
  ressources** — services Windows, ports TCP, dossiers, endpoints HTTP — sur le poste de
  développement ;
- pour les Cluster Services N4, qui exigent un accès au cluster, **un connecteur qui refuse de
  répondre** plutôt qu'un connecteur qui invente.

La preuve technique attendue par le cahier des charges — lire l'état d'un vrai nœud N4 — reste
à faire, et ne pourra l'être qu'après ouverture des accès.

## Ce qui a été livré

### Consolidation de l'état réel (FR-016)

`ConsolidationDEtat`, dans le domaine, applique les trois règles du cahier des charges :

1. **Un service déclaré Running ne suffit pas.** Le signal de service porte un marqueur « non
   concluant isolément » : seul, il produit « À confirmer », jamais « Opérationnel ».
2. **Des signaux contradictoires donnent « À confirmer »**, jamais une moyenne. On ne tranche
   pas à la majorité l'état d'un composant de production.
3. **L'absence d'un signal n'est jamais une absence d'anomalie.** Un contrôle qui n'a pas
   répondu est compté comme manquant, et des signaux favorables mais incomplets donnent
   « À confirmer ».

`ComponentHealth` gagne l'état **`AConfirmer`**, distinct d'`Inconnu` : le premier signifie
« des signaux existent mais ne permettent pas de conclure », le second « aucun signal
exploitable ». Chaque état est rendu avec sa **justification** — un état affiché sans motif
n'est pas opposable en revue d'incident.

### Statuts Cluster Services

`StatutClusterService` porte les sept statuts effectivement attestés par le guide Navis
3.8.25 : ACTIVE, INACTIVE, INITIALIZING, STARTING, DISCONNECTED, FAILED, UNKNOWN.

**Le plan en annonce huit ; le guide n'en atteste que sept.** Le huitième n'a pas été inventé :
toute valeur non reconnue est ramenée à `Inconnu`, ce qui produit « À confirmer » et non
« opérationnel ». La liste est à confirmer contre une vue Cluster Services réelle.

Seul ACTIVE est favorable. Un nœud en initialisation est traité comme dégradé, conformément à
la contrainte N4 : chaque nœud doit être **pleinement ACTIVE** avant le lancement du suivant.

### Connecteurs

| Connecteur | Ce qu'il lit | Vérifié contre |
|---|---|---|
| Service Windows | État d'un service nommé | Service WMI réel du poste |
| Port TCP | Ouverture et latence | Port réellement ouvert et port fermé |
| Endpoint HTTP | Code de retour et latence, GET seulement | Adresse invalide |
| Dossier partagé | Accessibilité en lecture, UNC compris | Dossier réel et dossier absent |
| SQL Server | Catalogue en lecture seule | Refus des contrôles hors catalogue |
| Cluster Services | — | Refuse de répondre, motif explicite |

**SEC-006 — aucune console libre.** Le connecteur SQL ne transporte pas de requête : le
référentiel nomme un contrôle du catalogue approuvé (disponibilité, sessions, verrous, requêtes
lentes) et le connecteur choisit lui-même le texte SQL. Une requête arrivant du référentiel
serait une console libre à retardement. La session est en outre ouverte en `ApplicationIntent
= ReadOnly` : sur une base à réplicas, une écriture y échouerait au niveau serveur.

**SEC-003 — secrets par référence.** La chaîne de connexion est assemblée à l'appel à partir
d'une référence de coffre. Le mot de passe n'existe qu'en mémoire, le temps de la requête, et
n'est jamais journalisé.

**Le contrat de collecte ne sait que lire.** `IConnecteurDeSignaux` n'expose aucune méthode
d'action. Les actions de pilotage relèveront d'un autre contrat : un connecteur sachant lire
et écrire finirait par être utilisé pour écrire depuis un écran de consultation.

### Test de configuration (FR-007)

Écran de test par composant, sans aucune action mutative. La propriété n'est pas tenue par
discipline : le service n'a accès qu'au contrat de collecte, qui ne sait que lire.

Un composant sans contrôle de santé actif est déclaré **Inconnu**, pas « en bonne santé » :
c'est un composant dont personne ne peut rien dire.

## Vérification

Suite automatisée : **105 tests, 0 échec** (86 domaine, 12 connecteurs, 7 architecture).

Test de configuration exécuté sur l'application, contre des ressources réelles du poste :

| Signal | Verdict | Détail constaté |
|---|---|---|
| Service Windows | Favorable | `Winmgmt : Running` — marqué non concluant isolément |
| Port TCP | Favorable | `127.0.0.1:7294 ouvert en 3 ms` |
| Cluster Services | Indisponible | « accès au cluster N4 non ouvert » |
| **État consolidé** | **À confirmer** | « Signaux favorables mais incomplets » |

C'est la démonstration de l'objectif du sprint : deux signaux réels et favorables ne suffisent
pas à conclure tant qu'un troisième manque. L'application dit « je ne sais pas ».

## Un défaut trouvé par les tests

Le test qui postulait qu'un port local fermé produit un refus a échoué : sur ce poste, la
tentative expire au lieu d'être refusée. Le connecteur avait raison — il a répondu
« indisponible » —, c'est le test qui supposait un comportement réseau non garanti.

La distinction est conservée et elle compte : un **refus** est une information sur le
composant, un **silence** est une information sur le réseau. Les confondre masquerait une
panne réseau derrière un verdict de composant en panne, ou l'inverse.

## Exigences

| Référence | Objet | État |
|---|---|---|
| FR-016 | État initial réel multi-signaux | Mécanique faite et testée ; non validée contre N4 |
| FR-007 | Test de configuration sans action mutative | Fait |
| SEC-003 | Secrets en coffre, jamais affichés ni journalisés | Fait |
| SEC-006 | Aucune console libre | Fait — catalogue de contrôles approuvés |
| §3.10.2 | Connecteurs de collecte | 5 connecteurs réels, Cluster Services en attente d'accès |

## Limites

- **Aucun accès N4.** Cluster Services, heartbeats, rôles Center/Standby et files ActiveMQ ne
  sont pas lus. Le connecteur de repli le dit à chaque appel plutôt que de produire un état.
- **Le huitième statut Cluster Services reste inconnu** — à confirmer avec l'Infrastructure.
- **Le connecteur SQL n'a pas été exercé contre une base réelle** : seuls ses refus le sont.
  Le brancher sur la base de développement aurait rendu les tests dépendants d'une instance
  locale, donc instables en intégration continue.
- **La collecte n'est pas encore périodique.** Ce sprint établit l'état à la demande ; la
  supervision continue relève du Sprint 4.

## Sprint suivant

**Sprint 4 — Supervision et tableau de bord** (semaines 9–10). Il consomme cette consolidation
pour cartographier un environnement en temps réel. Il héritera de la même réserve tant que les
accès ne sont pas ouverts : un tableau de bord alimenté par des signaux indisponibles affichera
des états « À confirmer », ce qui est correct, mais peu utile à l'exploitation.
