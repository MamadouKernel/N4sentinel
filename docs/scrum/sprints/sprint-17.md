# Sprint 17 — Séquencement configurable de l'arrêt et du démarrage

**Contexte** : le Product Owner a fourni les deux documents Navis d'autorité (*N4 IT Admin 2024 Day 1* et
*N4 3.8.25 Setup, Maintenance and System Diagnostics Guide*) ainsi que le cahier des charges, en demandant que
l'ordre d'arrêt, de démarrage et de redémarrage soit conforme, que le nombre de Cluster Nodes soit
paramétrable, et que **tout soit configurable**.

## Ce que disent réellement les sources

Les deux séquences ont été relues dans les PDF, pas déduites :

| | Arrêt `[GUIDE §1.10.7 p.455]` / `[CDC §8.4]` | Démarrage `[GUIDE §1.10.9 p.457-458]` / `[CDC §8.5]` |
|---|---|---|
| 1 | Confirmer arrêt des clients ECN4Web / Billing | Contrôles infrastructure, base, réseau, dossier partagé |
| 2 | Arrêter les clients XPS | Cluster Nodes, **un par un** |
| 3 | ECN4Web | Center Node |
| 4 | ECN4 | Standby Center Node |
| 5 | XPS | XPS Bridge Daemon |
| 6 | XPS Bridge Daemon | XPS |
| 7 | Standby Center Node | ECN4 puis ECN4Web |
| 8 | Cluster Nodes, **un par un** | Composants conditionnels (Billing…) |
| 9 | Center Node | — |
| 10 | Contrôle final | Tests de bout en bout |

**Le démarrage n'est pas l'inverse de l'arrêt.** « You must start the N4 Cluster nodes before you start the
Center node, the Standby Center node, the Bridge daemon, and XPS » `[GUIDE p.458]`. L'invariant commun est que
le Center Node suit immédiatement les Cluster Nodes — ce qui le place 2ᵉ au démarrage et dernier à l'arrêt.
Le cahier des charges le dit aussi explicitement : l'arrêt « ne doit pas être présenté comme une simple
inversion automatique du workflow de démarrage ».

**Contraintes propres aux N Cluster Nodes**, qui interdisent de les traiter comme un lot parallèle :
- démarrage — « Make sure the first Cluster node is ACTIVE [...] and fully initialized before starting the
  second cluster node », faute de quoi « various validations will conflict » `[GUIDE p.457]` (FR-030) ;
- arrêt — tout arrêter d'un coup fait tourner Hazelcast en rond pour redistribuer les caches et provoque une
  expiration à 10 minutes `[GUIDE p.455]`.

## Décisions de conception

### L'ordre est une donnée, pas du code
J'avais proposé de figer l'ordre dans le code au motif qu'il s'agit d'une contrainte produit Navis. **C'était
contraire au cahier des charges**, qui exige que « les séquences exactes doivent être : configurables par
environnement ; validées avec l'architecture réelle de CIT ; versionnées dans N4 Sentinel ; testées en UAT ».
Le Product Owner a tranché en ce sens. `SequenceTemplate` est donc une entité persistée, versionnée par clé
(précédent `DiagnosticRule`/`Document`), soumise au cycle de validation générique (FR-006), et portant
éventuellement un `EnvironmentId` pour qu'une Production et une UAT de topologies différentes aient chacune leur
ordre. Les séquences Navis ne sont que des **valeurs initiales**, installées si absentes et jamais écrasées.

### Le typage des composants est le préalable
`N4Component.Role` est un texte libre : aucune règle ne pouvait savoir qu'un composant *est* un Cluster Node.
`N4ComponentKind` (tiré des noms de services Windows réels `[GUIDE p.454]`) rend l'ordre calculable. Sans lui,
tout le reste serait resté une convention humaine.

### La paramétrabilité vient du dépliage, pas d'un réglage
Un palier vise un *type*, jamais un composant précis. `SequencePlanner` le déplie sur les composants
réellement déclarés : 3 Cluster Nodes donnent 3 étapes, 12 en donnent 12. Ajouter un nœud au référentiel suffit.
Les composants non pilotables sont exclus et signalés (FR-002) ; les paliers conditionnels sans composant sont
ignorés sans bruit (ECN4, Billing selon licence) ; un palier obligatoire vide remonte un avertissement.

### Paliers de contrôle
Les séquences du cahier des charges ne sont pas que des actions sur des services : elles s'ouvrent et se
referment sur des contrôles (prérequis d'infrastructure, arrêt des postes clients, contrôle final, recette de
bout en bout). `SequenceTierKind.Checkpoint` les exprime : une étape unique, sans composant, jamais escamotée
par un référentiel incomplet, et marquée « confirmation requise » puisqu'il s'agit d'un constat humain.

### FR-044 — interdiction de séquence invalide
Le contrôle est posé **à l'activation** d'une version, moment où le workflow devient réellement exécutable.
`ISequenceComplianceChecker` vit dans la couche Application car il croise trois agrégats (version de workflow,
séquence de référence, référentiel des composants) qu'aucune entité ne peut atteindre seule. En cas
d'inversion, `DomainRuleException` — sauf dérogation, l'unique échappatoire prévue par le texte (« sauf
workflow exceptionnel approuvé et documenté »). La dérogation reprend la séparation des responsabilités du
Sprint 16 : motif obligatoire, approbateur distinct du demandeur, et elle doit être posée **avant**
l'activation — l'accorder à une version déjà active reviendrait à régulariser après coup.

## Vérification

349 tests unitaires verts (201 Domain + 148 Application). Les tests verrouillent notamment le piège central :
`Plan_StartAndStop_AreNotMirrorImages` échoue si une séquence est déduite en inversant l'autre. Un test avait
d'ailleurs attrapé une erreur de ma part — j'avais affirmé que le Center Node était dernier dans les deux sens.

Séquences vérifiées **en base** après exécution réelle du seeder (`sqlcmd`), pas seulement en mémoire :
2 séquences actives, 10 paliers chacune, dans l'ordre exact des tableaux ci-dessus. Migrations vérifiées via
`__EFMigrationsHistory_App` — `dotnet ef database update --no-build` avait affiché « Done. » sans rien
appliquer, sur un binaire antérieur à la migration.

### FR-042 — redémarrage roulant
`RollingRestartPlanner` découpe les Cluster Nodes en lots successifs de taille `total − seuil`, de sorte que
le nombre de nœuds encore disponibles ne descende jamais sous le seuil demandé — c'est ce qui distingue un
redémarrage roulant d'un arrêt complet déguisé. Navis décrit le principe (« restart some cluster nodes, wait
for them to be up and then do the next set » `[GUIDE p.842]`) sans garantie de service ; le seuil est
l'apport de N4 Sentinel, exigé par FR-042.

Refus explicites plutôt que comportements silencieux : seuil à zéro (« utilisez la séquence d'arrêt
complet »), seuil ne laissant aucun nœud à redémarrer, nœud non pilotable, aucun Cluster Node déclaré. Le tri
par nom rend les lots reproductibles d'un calcul à l'autre, donc auditables.

Vérifié dans le navigateur sur 5 nœuds avec un seuil de 3 : 3 lots (2, 2, 1), chaque lot laissant bien
3 nœuds disponibles.

### FR-046 / FR-047 — continuité et bascule du rôle Center
Le point dur n'est pas l'ordre des services mais le **verrou** : un seul Center détient le rôle actif à la
fois, arbitré par un verrou base de données (défaut depuis N4 3.3) ou fichier via ActiveMQ
`[GUIDE p.450-451]`. Conséquence : arrêter le primaire alors que le Standby tourne **provoque une bascule**,
et c'est le comportement nominal, pas un incident. D'où l'obligation de FR-046 de demander l'intention avant
d'agir — l'écran pose la question en premier, aucun déroulé n'est calculé avant.

*Continuité sur le primaire* : arrêter le Standby → arrêter le primaire → démarrer le primaire → **confirmer
qu'il a repris le rôle** → seulement alors relancer le Standby. C'est la procédure Navis à la lettre :
« restart the N4 Navis Center Node service on the main Center node and wait until it becomes active [...] once
the Center node is active, then start N4 on the Standby node » `[GUIDE §1.10.4 p.451]`.

*Bascule assumée* : vérifier l'aptitude du Standby **avant** d'arrêter le primaire — basculer vers un Standby
inapte laisserait l'environnement sans Center actif. Décision notable : le plan **n'inclut pas** le
redémarrage du primaire. Le relancer sans contrôle préalable est exactement le scénario « deux Center
actifs » que FR-047 interdit ; le plan s'arrête donc sur la vérification et le signale explicitement.

Les deux plans se terminent par un contrôle d'unicité du rôle actif, verrouillé par un test paramétré.
Refus explicites sur un référentiel incohérent : deux composants typés Center Node, aucun Center, Standby
absent alors qu'une bascule est demandée, composant non pilotable.

Vérifié dans le navigateur sur un couple Center/Standby : 7 étapes pour la continuité, 4 pour la bascule,
dans l'ordre attendu.

### FR-029A — arrêt adapté à l'état
« Ignorer proprement les composants déjà arrêtés et recalculer l'ordre à partir des services encore actifs,
sans rompre les dépendances. » Le filtrage intervient **avant** l'émission des étapes : le chaînage et la
numérotation étant construits au fil de l'émission, ils se réajustent d'eux-mêmes et aucune dépendance ne
peut pointer vers une étape écartée. Un test le verrouille en retirant le nœud du milieu d'une série de trois.

`ObservedComponentState.Unknown` est la valeur par défaut et **n'autorise jamais** à écarter une étape : dans
le doute, on la conserve. Les points de contrôle ne sont jamais écartés — ils ne dépendent d'aucun composant.

**Limite assumée** : l'état provient d'un **constat déclaré par l'opérateur**, pas d'une détection
automatique. Tant qu'aucun connecteur réel n'est autorisé, prétendre détecter l'état des services serait de
l'automatisation simulée — le principe tenu depuis le Sprint 2 l'interdit. Le recalcul, lui, est réel et
testé ; seule sa source d'entrée reste manuelle. Même parti pris que la reconstitution guidée du Sprint 11.

Vérifié dans le navigateur : un Cluster Node déclaré déjà démarré est écarté, le plan se renumérote sans
trou, les contrôles d'encadrement restent dus.

## Reste à faire

- FR-029A recalcul de l'ordre en ignorant les composants déjà arrêtés.
- Écran d'édition des séquences : la page `/admin/sequences` est en lecture seule, les commandes CQRS
  d'édition et de réordonnancement existent mais ne sont pas encore câblées à l'UI.
- Rattachement du `Kind` aux composants déjà enregistrés (ils sont tous `Unspecified` par défaut ; tant
  qu'ils ne sont pas typés, aucune séquence ne les inclut).
