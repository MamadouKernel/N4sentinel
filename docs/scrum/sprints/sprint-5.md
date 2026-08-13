# Sprint 5 — Moteur d'orchestration

**Semaines 11–12 · Lot 1 · Statut : livré**

**Objectif** — un moteur d'exécution persistant, qui survit à sa propre panne sans rejouer
aveuglément une action déjà faite.

**Livrable démontrable en revue** — exécution reprise après arrêt brutal du serveur applicatif.

---

## La démonstration, d'abord

Une exécution a été laissée dans l'état « En cours », puis le processus du serveur a été **tué
brutalement** — pas arrêté proprement. Au redémarrage :

```
avant :  statut = En cours
après :  statut = Réconciliation requise
         « Reprise après redémarrage du serveur applicatif refusée.
           État réel non établi pour : Center Node, Bridge, XPS, Base N4…
           La reprise exige de savoir où l'on en est. »
```

Le moteur a retrouvé l'exécution, a refusé de la reprendre, et a dit pourquoi. C'est le
comportement attendu : une exécution retrouvée « en cours » après un redémarrage **n'était pas
en cours** — le processus qui la portait n'existe plus.

## Les vingt états du cahier des charges

FR-020 énumère dix états d'étape et dix états de workflow. Les deux énumérations sont fermées :
un onzième état inventé ne serait affichable nulle part.

Deux distinctions méritent d'être relevées, parce qu'elles sont faciles à écraser :

- **« Vérification » n'est pas « En cours ».** La commande est passée ; son effet reste à
  constater. Une étape ne conclut jamais directement depuis « En cours » — un test le vérifie.
- **« Annulation demandée » n'est pas « Annulé ».** FR-025 : une annulation n'interrompt jamais
  brutalement une commande engagée. Le moteur atteint d'abord un point sûr.

## Les règles dures

### Aucune reprise aveugle

Avant toute reprise, l'état réel est recollecté par la supervision du Sprint 4 et comparé à
l'état mémorisé. Trois issues, et une seule autorise la reprise :

| Constat | Issue |
|---|---|
| État réel conforme au mémorisé | Reprise autorisée |
| Divergence | **Réconciliation requise**, avec la liste des écarts |
| État non établi — Inconnu ou À confirmer | **Réconciliation requise**, sans prétendre à une divergence |

La troisième ligne compte autant que la deuxième : ne pas savoir n'est pas une divergence, et
le dire autrement serait inventer un constat.

Cette règle protège du scénario le plus coûteux : le serveur tombe pendant un arrêt, quelqu'un
termine l'opération à la main, le moteur redémarre et rejoue ses étapes sur un système qui n'est
plus dans l'état qu'il croit.

### Une seule opération mutative par environnement (FR-015)

Le verrou est **persisté**, pas tenu en mémoire — un verrou en mémoire disparaîtrait avec le
processus, laissant croire qu'aucune opération n'est en cours. Il porte une date d'expiration
pour la raison inverse : sans elle, une panne au mauvais moment bloquerait l'environnement
jusqu'à intervention manuelle en base.

### Parallélisme déclaré, puis vérifié (FR-023)

Le parallélisme n'est jamais déduit. Quatre refus possibles :

1. étape non déclarée indépendante dans la version validée ;
2. **type N4 dont le séquencement est imposé** — Cluster Node, Center, Standby, Bridge, XPS ;
3. même composant ou même serveur ;
4. ressource partagée commune.

La deuxième règle est la plus importante : les déclarer indépendants dans un workflow ne
rendrait pas la chose vraie. Les Cluster Nodes démarrent un par un, XPS attend le Bridge.

### Nouvelles tentatives (FR-004)

Aucune reprise automatique par défaut. Sur une **action critique ou destructrice**, la reprise
automatique est interdite — sauf autorisation explicite portée par une version validée du
workflow. Une action destructrice rejouée automatiquement, c'est une double suppression que
personne n'a demandée.

### Passage à l'étape suivante (FR-022)

Un contrôle bloquant ne peut être contourné que s'il a été **déclaré contournable dans une
version validée**, et le contournement exige alors une confirmation. Le contournement est donc
un paramètre du workflow, jamais une décision prise au moment de l'exécution : c'est ce qui
empêche qu'une nuit difficile devienne un précédent.

Un résultat non vérifiable interdit la poursuite par défaut.

## Vérification

Suite automatisée : **161 tests, 0 échec** (142 domaine, 12 connecteurs, 7 architecture).
Les 38 tests ajoutés couvrent la machine à états, les seuils, la politique de transition, le
planificateur de parallélisme et le contrôle de reprise.

Les tests d'architecture continuent de passer : le contrat de persistance du moteur a été placé
dans la couche partagée, précisément parce que la couche Données et l'Orchestrateur ne doivent
pas se connaître.

## Limites

- **Le moteur ne fait encore rien exécuter.** Il gère les états, les verrous, les reprises et
  les refus. L'exécution réelle des commandes suppose un contrat d'action que le Sprint 3 n'a
  délibérément pas créé — un connecteur sachant lire et écrire finirait par être utilisé pour
  écrire depuis un écran de consultation. La préparation d'une opération arrive au Sprint 6,
  l'exécution réelle au Sprint 7.
- **Aucun workflow n'est encore saisissable.** FR-003 demande des workflows configurables et
  versionnés ; les entités existent depuis le Sprint 0, mais leur écran de saisie relève du
  Sprint 6, avec la préparation d'opération.
- **La vue pas-à-pas est en lecture seule** et n'affiche rien tant qu'aucune exécution
  n'existe. Elle est livrée maintenant parce que FR-020 relève de ce sprint, et qu'un écran
  écrit après coup se plie mal aux états qu'il doit montrer.
- **Le rafraîchissement temps réel de cette vue** (FR-021) viendra avec l'exécution réelle :
  afficher en direct une exécution qui n'existe pas n'apporte rien.

## Sprint suivant

**Sprint 6 — Préparation d'une opération et mode simulation** (semaines 13–14). Il ne dépend
pas non plus des accès N4 : simuler une opération, c'est précisément ne rien exécuter. C'est
donc le dernier sprint pleinement livrable avant que l'absence d'accès ne devienne bloquante,
au Sprint 7.
