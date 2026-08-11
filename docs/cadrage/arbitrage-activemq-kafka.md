# Note d'arbitrage — ActiveMQ ou Kafka

**Sprint 0 · décision attendue de la DSI**
**Statut** en attente

## De quoi il s'agit

Deux sujets sont souvent confondus sous ce titre. Les séparer est la première étape de
l'arbitrage.

1. **Le bus utilisé par N4 lui-même.** N4 s'appuie sur ActiveMQ et sa persistance KahaDB. Ce
   n'est pas un choix ouvert : c'est une donnée de l'écosystème que N4 Sentinel doit superviser
   telle qu'elle est. Le suivi des files et la détection de corruption KahaDB sont au programme
   du Lot 2.

2. **Le bus éventuellement utilisé par N4 Sentinel.** C'est le vrai objet de l'arbitrage : faut-il
   que l'application se dote d'un bus pour ses propres échanges internes ?

## Position de l'équipe projet sur le point 2

**Aucun bus n'est nécessaire en V1**, et en introduire un serait une complexité non justifiée.

Les besoins d'asynchronisme de l'application sont couverts sans bus :

| Besoin | Solution retenue |
|---|---|
| Reprise d'une exécution après redémarrage | État persistant en base, relu au démarrage |
| Rafraîchissement temps réel de l'interface | SignalR, déjà présent avec Blazor Server |
| Collecte périodique de signaux | Service hébergé cadencé |
| Notifications | Envoi direct, avec relance en cas d'échec |

Un bus ajouterait un composant à installer, superviser, sauvegarder et redémarrer — sur une
application dont la raison d'être est justement de réduire le nombre de choses fragiles à
piloter à la main.

## Ce qui ferait changer d'avis

L'arbitrage doit être réexaminé si l'un de ces éléments devient vrai :

- la DSI impose un bus d'entreprise pour toute application interne ;
- le Lot 3 (automatisation de bout en bout) exige une exécution distribuée sur plusieurs nœuds ;
- l'intégration au ticketing CIT ou à la supervision d'entreprise passe par une file imposée.

## Décision

| Champ | Valeur |
|---|---|
| Décision | *à compléter* |
| Décidé par | |
| Date | |
| Motif | |
