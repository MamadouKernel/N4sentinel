# Relance — accès techniques et environnement UAT

**À l'attention de la DSI et de l'équipe Infrastructure CIT**
**Complète la demande initiale déposée au Sprint 0** —
[`demande-acces-techniques.md`](demande-acces-techniques.md)

## Ce qui a changé depuis la demande initiale

La demande initiale était générique, faute de connaître le détail de l'installation. Le corpus
documentaire (SOP-0 à SOP-3, scripts d'exploitation) permet aujourd'hui de nommer précisément
ce qui doit être ouvert, service par service. **Il ne reste plus rien à préciser côté projet.**

Huit sprints sont développés et couverts par 339 tests automatisés. Le développement n'est pas
en attente : il continue sur ce qui peut se construire sans accès. Ce qui est en attente, c'est
la **preuve** — et elle ne viendra pas du code.

## Ce que l'absence d'accès bloque, précisément

| Sprint | Ce qui est écrit et testé | Ce qui manque pour le valider |
|---|---|---|
| S3 | Cinq connecteurs de collecte, consolidation multi-signaux | Un sixième connecteur (Cluster Services) impossible à écrire ; le connecteur SQL n'a jamais interrogé une base N4 ; `FR-016` reste « testé, non validé » |
| S4 | Huit états consolidés, alertes, tableau de bord | `FR-051`, `FR-050`, `FR-057` partiels ; `FR-056` et `FR-058` absents — tous suspendus à des signaux qu'aucun accès ne permet de produire |
| S7 | Exécution réelle, arrêt complet, masquage des secrets | `AC-05` — arrêt complet piloté d'un UAT — jamais démontré |
| S8 | Verrous de démarrage, chaîne de dépendances | Deux verrous sur six restent débranchés faute de signaux |

**Aucune de ces lignes ne se referme par du développement.** Chaque sprint supplémentaire livré
sans accès ajoute du code non validé contre le système réel.

## Accès demandés — version précisée

### 1. Compte de service Windows sur les serveurs N4

Le besoin exact : **démarrer, arrêter et interroger** les services nommément désignés
ci-dessous, sur les hôtes correspondants. Rien d'autre.

| Rôle | Service Windows |
|---|---|
| Center Node | `Navis N4 Center Node` |
| Cluster Node | `Navis N4 Cluster Node` |
| Standby Center Node | `Navis N4 Center Node` (même service, hôte distinct) |
| XPS Bridge Daemon | `Navis XPS Bridge Daemon` |
| XPS | `Navis XPS Service` |
| ECN4 | `Navis ECN4 Daemon` |
| ECN4 Web | `Navis ECN4web` |

> **Point à trancher.** SOP-3 prévoit pour le Niveau 2/3 un « compte Administrateur Windows
> local », et les scripts d'exploitation l'exigent également. La demande initiale, elle,
> réclamait le moindre privilège.
>
> Ces deux besoins ne sont pas les mêmes. Un ingénieur Niveau 2/3 doit pouvoir tout faire sur le
> serveur ; **N4 Sentinel n'a besoin que de piloter sept services nommés**. Ce droit s'accorde
> service par service, sans administrateur local, via les ACL de service Windows
> (`sc.exe sdset`). Nous recommandons cette voie : elle réduit la portée du compte, et un outil
> qui ne peut pas faire autre chose ne fera jamais autre chose.

### 2. Lecture SQL sur la base N4

Lecture seule — l'application n'écrit jamais dans la base du TOS. SOP-3 mentionne pour la
supervision le droit `VIEW SERVER STATE`, utile aux indicateurs de santé.

Un besoin identifié depuis : SOP-2 indique que le basculement Center/Standby est « contrôlé par
un verrou en base de données ou un verrou fichier ActiveMQ ». **C'est la seule source connue
permettant de savoir quelle instance détient réellement le rôle actif** — et donc de détecter
deux Center actifs simultanément, cause première de corruption `db.data`, après quoi plus aucun
service ne démarre. Sans cet accès, la règle existe dans le produit mais reste aveugle.

### 3. Accès JMX sur le nœud Center

Non identifié dans la demande initiale. SOP-2 et SOP-3 en font un point de contrôle quotidien :
`QueueSize`, `DequeueCount`, `InFlightCount`, `ConsumerCount` sur `bridge.*`. Un `ConsumerCount`
à zéro est, selon SOP-2, « souvent le symptôme le plus fiable d'un nœud bloqué ».

C'est aussi ce qui permettrait de tenir une règle d'exploitation que les scripts n'appliquent
aujourd'hui que par discipline humaine : **ne pas arrêter le Bridge tant que sa file n'est pas
vidée**.

### 4. Accès réseau aux serveurs, pour la lecture des horloges

SOP-3 fait de l'écart d'horloge un contrôle quotidien, seuil sous une seconde, et le situe au
Top 10 des causes de P1 identifiées par Navis/Kaleris. La raison donnée est directe : un écart
« fausse silencieusement les statuts affichés — un nœud actif peut apparaître DISCONNECTED ».

Pour N4 Sentinel, ce n'est pas un contrôle parmi d'autres : le moteur d'orchestration **décide**
sur ces statuts. Des horloges désynchronisées ne dégradent pas un affichage, elles corrompent la
donnée d'entrée de chaque décision d'arrêt ou de démarrage.

### 5. Endpoints HTTP et TCP, dossiers partagés, répertoires de logs

Inchangés par rapport à la demande initiale. Le corpus précise la cible du dossier partagé :
`\\<serveur>\NavisShared`, contenant `amq` et `conf`, dont l'intégrité conditionne tout
redémarrage.

## Environnement UAT — ce que « représentatif » veut dire

L'UAT est la condition du Sprint 7, dont le livrable de revue est l'arrêt complet piloté d'un
écosystème réel. Un UAT non représentatif ne vaut pas recette.

Représentatif signifie **reproduire la topologie**, pas seulement la fonction :

- **Plusieurs Cluster Nodes.** La séquence exige de les arrêter un par un, chacun confirmé avant
  le suivant — sinon le timeout Hazelcast de dix minutes s'applique. Avec un seul nœud, la règle
  la plus coûteuse à enfreindre n'est jamais exercée.
- **Un Standby Center Node.** Sans lui, ni la distinction rôle actif / service démarré, ni la
  détection du double actif ne peuvent être démontrées.
- **Bridge et XPS**, quelle que soit leur répartition sur les hôtes — le produit gère les deux
  cas, encore faut-il le prouver sur celui de CIT.
- **Une version N4 comparable à la Production.**

Les huit questions du [recensement](recensement-perimetre.md) restent sans réponse. Elles
suffisent à décrire l'UAT attendu.

## À renseigner par l'Infrastructure

| Élément | Réponse | Date d'engagement |
|---|---|---|
| Compte de service créé, ACL posées sur les sept services | | |
| Lecture SQL sur la base N4 (+ `VIEW SERVER STATE`) | | |
| Accès JMX au nœud Center | | |
| Flux réseau ouverts (services, HTTP, TCP, SMB) | | |
| Coffre à secrets retenu | | |
| Serveur applicatif UAT mis à disposition | | |
| Instance SQL Server applicative | | |
| Certificat serveur HTTPS émis | | |
| UAT représentatif — recensement complété | | |

## Ce que nous demandons concrètement

Une date d'engagement par ligne, même lointaine. Une date connue permet de replanifier ; une
ligne vide oblige à continuer à développer sans savoir si ce qui est écrit correspond à la
réalité.

À défaut d'ouverture complète, **l'accès en lecture seule sur l'UAT seul** — collecte, SQL, JMX,
horloges — débloquerait déjà les Sprints 3 et 4, soit neuf exigences fonctionnelles aujourd'hui
partielles ou absentes. Le pilotage réel peut attendre ; la preuve technique, elle, conditionne
tout le reste.
