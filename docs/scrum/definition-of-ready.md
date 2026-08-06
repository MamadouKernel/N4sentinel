# Definition of Ready — N4 Sentinel

Une story peut entrer dans un Sprint Backlog si :

1. Elle est rattachée à une exigence identifiable du cahier des charges (FR-xxx ou section nommée), ou
   justifiée explicitement comme dette technique/infrastructure.
2. Son critère d'acceptation est formulable en une ou deux phrases vérifiables (pas d'ambiguïté sur ce qui
   fait qu'elle est "faite").
3. Elle est estimée en points (Fibonacci) par comparaison avec des stories déjà livrées.
4. Ses dépendances (autres stories, choix d'architecture) sont soit déjà Fait, soit explicitement planifiées
   dans le même sprint.
5. Elle ne dépend pas d'un accès externe non disponible (ex. accès réseau réel aux serveurs N4 de CIT) sans
   qu'une alternative de développement (simulation, mock) soit prévue.

Toute story qui ne remplit pas ces conditions reste au backlog en statut "À affiner", pas en Sprint Backlog.
