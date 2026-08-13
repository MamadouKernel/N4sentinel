# Atelier de validation des séquences réelles

**Sprint 0 · équipe Infrastructure CIT + équipe projet**
**Durée estimée** une demi-journée

## Pourquoi cet atelier

Le cahier des charges donne un ordre fonctionnel d'arrêt « sous réserve de validation avec la
configuration réelle ». Cette réserve n'est pas une formalité : c'est l'atelier qui la lève. Une
séquence fausse ne se voit pas en revue de code, elle se voit en production, un dimanche.

Les séquences sont **configurables par environnement et versionnées** dans l'application. Elles
ne sont pas figées dans le code : ce que produit cet atelier est une donnée de référence, pas
une spécification de développement.

## Ordre du jour

1. **Lecture de la séquence d'arrêt** telle que documentée par l'éditeur, composant par composant.
2. **Confrontation à la pratique CIT** : ce que fait réellement l'exploitation aujourd'hui, y
   compris les écarts assumés et leurs raisons.
3. **Séquence de démarrage** — traitée séparément. Le démarrage n'est pas l'inverse de l'arrêt.
4. **Barrières et temporisations** : quelles étapes attendent quoi, et pendant combien de temps.
5. **Cas de reprise** : que fait l'exploitation quand une étape échoue à mi-parcours.
6. **Validation formelle** de la séquence retenue par environnement.

## Grille à remplir — arrêt

| Ordre | Composant | Attendre quoi avant de passer à la suite ? | Timeout | Étape parallélisable ? | Confirmation humaine ? |
|---|---|---|---|---|---|
| 1 | | | | | |
| 2 | | | | | |

## Grille à remplir — démarrage

| Ordre | Composant | Prérequis bloquant | Timeout | Étape parallélisable ? | Confirmation humaine ? |
|---|---|---|---|---|---|
| 1 | | | | | |
| 2 | | | | | |

## Points à faire trancher explicitement

| # | Point | Décision | Justification |
|---|---|---|---|
| 1 | Les clusters sont-ils arrêtés strictement l'un après l'autre ? | | |
| 2 | Le Center Node s'arrête-t-il en dernier ? | | |
| 3 | Quelle temporisation entre l'arrêt d'un cluster et le suivant ? | | |
| 4 | Quelles étapes sont réellement indépendantes ? | | |
| 5 | Quelles étapes exigent une approbation par un second acteur ? | | |
| 6 | Quel est le dernier point stable où l'on peut revenir ? | | |

> Le parallélisme n'est jamais déduit par le moteur : il est déclaré étape par étape dans la
> version validée du workflow. Une étape non déclarée indépendante s'exécute en séquence.

## Sortie attendue

Un tableau signé par l'Infrastructure, par environnement, saisi comme séquence de référence
lors du sprint de séquencement. Tant qu'il n'est pas signé, aucune séquence n'est activable.
