# Sprint 4 — Supervision et tableau de bord

**Semaines 9–10 · Lot 1 · Statut : livré, périmètre réduit par l'absence d'accès N4**

**Objectif** — donner à la DSI une vue temps réel exploitable de l'état de chaque environnement.

---

## Ce qui a été livré

### Les huit états consolidés (FR-052)

`EtatDeSupervision` porte les huit états exigés — Disponible, Dégradé, Indisponible, Démarrage,
Arrêt, Inconnu, Maintenance, Non supervisé — **plus « À confirmer »**, qu'impose FR-016.
Les confondre ferait disparaître la distinction entre « je n'ai aucun signal » et « j'ai des
signaux qui ne concordent pas ». Un test vérifie que les huit libellés du cahier des charges
existent tous.

L'ordre des règles d'évaluation n'est pas indifférent :

1. **Non supervisé** court-circuite tout : rien n'est collecté.
2. **Maintenance** prime sur n'importe quel signal défavorable. Un composant volontairement
   arrêté pendant une intervention affiché « Indisponible » apprendrait aux exploitants à
   ignorer l'écran.
3. **Une transition observée prime sur un état bas.** Un service en `StartPending` n'est pas un
   service en panne. La transition est constatée par le connecteur — seul endroit où
   l'information existe — puis portée par le signal.
4. Sinon, la consolidation du Sprint 3 s'applique.

### Alertes (FR-054)

`DetecteurDAlertes` couvre les six motifs du cahier des charges — timeout, échec, incohérence
d'état, file qui augmente, heartbeat ancien, ressource critique — et en ajoute un septième :
**donnée trop ancienne**. Sans lui, le tableau de bord afficherait indéfiniment le dernier état
connu comme s'il était courant, ce qui est la façon la plus discrète de mentir.

Les règles vivent dans le domaine : une alerte qui n'existerait qu'à l'affichage disparaîtrait
dès qu'on regarde ailleurs.

**Aucune alerte n'est levée en maintenance ni sur un composant non supervisé** — les deux cas
où l'anomalie est attendue.

Une file qui croît demande **deux relevés** : une mesure isolée ne dit rien d'une tendance.

### Collecte et actualisation (FR-053)

Un service de fond collecte toutes les 60 secondes (réglable), enregistre les relevés et purge
au-delà de la rétention configurée (SEC-009).

**Lire et collecter sont deux opérations distinctes.** Afficher le tableau de bord ne déclenche
aucun appel réseau : dix exploitants devant le même écran multiplieraient sinon par dix la
charge sur les serveurs supervisés. Le rafraîchissement à la demande existe, mais il est
réservé au droit de gestion du référentiel, et il est tracé.

**Une collecte qui échoue n'arrête jamais la boucle.** L'exploitation perdrait la supervision
au moment précis où un composant devient injoignable — c'est-à-dire quand elle en a le plus
besoin.

La date de la donnée la plus récente est affichée en permanence, avec son ancienneté.

### Code couleur accessible (FR-055)

Chaque état porte un **libellé** ; la couleur ne fait que l'appuyer. Le cahier des charges
l'exige au §7 (« statuts compréhensibles sans dépendre uniquement des couleurs »), et un écran
de salle technique mal calibré suffit à rendre la couleur seule inutilisable.

### Composants à valider (FR-050)

Un composant non activé au référentiel est supervisé mais sa justification porte explicitement
« aucune action autorisée ». La détection automatique de nouveaux nœuds, elle, suppose la
lecture des Cluster Services — voir les limites.

## Vérification

Suite automatisée : **123 tests, 0 échec** (104 domaine, 12 connecteurs, 7 architecture).

Tableau de bord observé sur l'application, alimenté par la collecte automatique :

| Constat | Résultat |
|---|---|
| Collecte de fond | démarrée, intervalle 60 s |
| Donnée la plus récente | « il y a 2 s » |
| Composants cartographiés | 6 |
| Composants sans contrôle | **Inconnu** — pas « en bonne santé » |
| Composant avec 3 contrôles dont un indisponible | **À confirmer** |
| Alertes levées | 7, dont « aucun relevé disponible » sur les composants critiques |
| Rafraîchissement à la demande | effectué et tracé |

## Limites — le périmètre réduit

Les accès techniques CIT ne sont toujours pas ouverts. Trois blocs du plan ne sont donc **pas**
livrés, et n'ont pas été remplacés par des approximations :

- **Vue détaillée CPU, mémoire, disque, processus** (FR-051, partiel). Ces métriques supposent
  un accès système aux serveurs N4. Les signaux disponibles — service, port, endpoint, dossier,
  SQL — sont affichés ; les autres sont absents, pas simulés.
- **Synchronisation N4-XPS** (FR-056) et **lenteurs vues par N4** (FR-058). Elles reposent
  entièrement sur les heartbeats, files et logs N4, inaccessibles. Le détecteur d'alertes porte
  déjà les règles « heartbeat ancien » et « file qui augmente » : elles se déclencheront dès que
  les signaux correspondants existeront, sans modification.
- **Détection automatique de nouveaux nœuds** (FR-050, partiel). Elle suppose la lecture des
  Cluster Services. La moitié applicable est faite : tout composant non activé est signalé et
  n'autorise aucune action.

**Vue réseau et base (FR-057)** est partiellement couverte : les connecteurs TCP et SQL en
lecture seule alimentent disponibilité, latence et requêtes lentes. Ils n'ont simplement aucune
base N4 à interroger.

Autres limites :

- **Pas de rafraîchissement automatique de la page.** L'écran affiche l'âge de la donnée ;
  il faut recharger pour la mettre à jour. Le temps réel par SignalR viendra avec le pilotage,
  qui en a un besoin plus fort.
- **La rétention des relevés est configurée mais non différenciée** par type de donnée, alors
  que SEC-009 demande des durées distinctes pour logs, rapports et audits.

## Sprint suivant

**Sprint 5 — Moteur d'orchestration** (semaines 11–12). Il ne dépend pas des accès N4 : un
moteur qui reprend après un arrêt brutal se construit et se prouve sans serveur distant. C'est
le dernier sprint du Lot 1 qui puisse avancer à pleine vitesse sans ouverture des accès.
