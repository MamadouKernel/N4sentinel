# Sprint 2 — Référentiel : environnements et composants

**Semaines 5–6 · Lot 1 · Statut : livré**

**Objectif** — décrire l'écosystème N4 de CIT dans l'application, pour qu'aucune action ne
puisse viser un composant inconnu.

**Livrable démontrable en revue** — écosystème saisi et validé, dépendances visualisées.

---

## Ce qui a été livré

### Environnements (FR-001)

Création, modification, consultation et changement de statut. Chaque environnement porte sa
propre cartographie. La Production reste identifiée en permanence — pastille rouge dans le
bandeau, posée au Sprint 1.

Un environnement **actif ne peut pas être modifié** : il faut d'abord le désactiver. Changer la
cartographie sous les pieds d'une exploitation qui s'y appuie serait précisément ce que ce
référentiel doit empêcher.

### Inventaire des composants (FR-002)

Les onze attributs demandés par le cahier des charges sont couverts : nom logique, rôle,
environnement, serveur ou machine virtuelle, adresse IP, nom DNS, endpoints autorisés, système
d'exploitation, service ou mécanisme, dépendances, contrôles de santé, criticité, responsable,
et caractère pilotable.

Deux composants ne peuvent pas porter le même nom dans un même environnement : une opération
viserait sinon une cible ambiguë.

### Typage N4 (§2.4)

`N4ComponentKind` couvre les douze types du cahier des charges : Cluster Node, Center Node,
**Standby Center Node**, **Bridge daemon**, XPS, ECN4, ECN4Web, base de données, ActiveMQ,
dossier partagé, composant conditionnel, système dépendant, plus l'infrastructure réseau.

Le Bridge daemon, absent de la maquette, entre ici dans le modèle — comme l'annonçait le point
d'attention du plan.

**La validation d'un composant non typé est refusée.** Sans type, aucune règle d'ordre n'est
calculable : le composant serait déclaré exploitable tout en restant invisible des séquences.
La liste des composants signale visuellement ceux qui restent à typer.

### Distinction pilotable / supervisé / non supervisé (§2.4)

`ModeDePilotage` porte les trois cas. **Par défaut, un composant n'est que supervisé** :
accorder le pilotage doit être un geste délibéré, pas un effet de bord de la saisie.

### Graphe de dépendances (§2.4)

`GrapheDeDependances`, dans le domaine, sert les quatre usages que le cahier des charges assigne
à la cartographie :

| Usage du §2.4 | Mise en œuvre |
|---|---|
| Déterminer l'ordre des workflows | `OrdreDeDemarrage` — tri topologique déterministe |
| Vérifier les prérequis | `PrerequisDirectsDe` |
| Analyser l'impact d'une action unitaire | `ImpactDeLArretDe` — cascade complète |
| Empêcher une séquence incompatible | `DetecterUnCycle` |

**Une dépendance qui formerait un cycle est refusée à la saisie**, avec le chemin du cycle écrit
au journal d'audit — pas un simple « impossible ». Un cycle rend l'ordre de démarrage
incalculable ; le découvrir au milieu d'un arrêt de production, un dimanche, coûterait
autrement plus cher qu'un refus au moment de la saisie.

L'écran `/referentiel/dependances` affiche le graphe en SVG, une colonne par palier de
démarrage, et l'ordre calculé sous forme de tableau.

### Cycle de validation (FR-006)

Brouillon → À valider → Validé → Actif → Désactivé, avec les seules transitions énumérées :

- aucun raccourci ne mène à « Actif » — sinon ce statut ne garantirait rien ;
- valider n'active pas : la validation atteste du contenu, l'activation engage l'exploitation ;
- un objet désactivé repart en brouillon, car ce qui a justifié sa désactivation peut ne plus
  être vrai ;
- **seul un objet actif est utilisable pour une opération** (FR-002).

## Exigences soldées

| Référence | Objet | État |
|---|---|---|
| FR-001 | Gestion des environnements | Fait |
| FR-002 | Inventaire des composants | Fait |
| FR-006 | Cycle de validation | Fait pour environnements et composants ; workflows, seuils et règles suivront avec les entités concernées |
| §2.4 | Cartographie, typage, dépendances, impact | Fait |

## Vérification

Suite automatisée : **71 tests, 0 échec** (64 domaine, 7 architecture). Les 19 tests ajoutés
couvrent le cycle de validation et le graphe : ordre de démarrage, détection de cycle avec
restitution du chemin, impact en cascade, arêtes hors périmètre, doublons.

Parcours vérifié sur l'application lancée, avec la chaîne du §2.4 :

| Étape | Résultat constaté |
|---|---|
| Création de 4 composants typés | Base N4, Center Node, Bridge, XPS |
| Chaîne de dépendances | Center←Base, Bridge←Center, XPS←Bridge |
| Ordre de démarrage calculé | Base N4 → Center Node → Bridge → XPS |
| Graphe SVG | 4 boîtes sur 4 colonnes (x = 20, 270, 520, 770), 3 liens |
| Dépendance formant un cycle | Refusée, chemin du cycle tracé |
| Auto-dépendance | Refusée |
| Brouillon → Actif directement | Refusée |
| Brouillon → À valider → Validé → Actif | Acceptée |
| Modification d'un composant actif | Refusée |
| Validation d'un composant non typé | Refusée |
| Journal d'audit | Créations, changements de statut, dépendances et refus présents |

## Limites

- **Les données saisies pendant la vérification sont des données de démonstration.** Quatre
  composants N4 et un composant de test restent dans la base de développement. Ils illustrent
  la chaîne du §2.4 ; ils ne décrivent pas l'écosystème réel de CIT, qui viendra du recensement
  mené avec l'Infrastructure (`docs/cadrage/recensement-perimetre.md`, toujours à remplir).
- **FR-007 — test de la configuration technique sans action mutative** n'est pas dans ce
  sprint : il suppose des connecteurs, donc le Sprint 3.
- **L'ordre d'arrêt calculé reste théorique.** Il découle des seules dépendances déclarées. Le
  séquencement réel de l'écosystème N4 relève d'un sprint ultérieur et s'en écartera : le
  démarrage n'est pas l'exact inverse de l'arrêt, et les Cluster Nodes démarrent un par un.
- **Suppression de composants non exposée.** Retirer un composant du référentiel, alors qu'il
  peut être cité par des dépendances et bientôt par des workflows, demande une règle de
  conservation que ce sprint n'a pas tranchée. La désactivation tient ce rôle en attendant.

## Sprint suivant

**Sprint 3 — Connecteurs et preuve technique** (semaines 7–8), le sprint le plus risqué du plan.
Il dépend entièrement de l'ouverture des accès techniques, demandée au Sprint 0 et toujours sans
date d'engagement.
