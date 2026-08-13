# Recensement du périmètre N4 — grille à compléter

**Sprint 0 · à remplir avec l'équipe Infrastructure CIT**

Sans ce recensement, le référentiel du Sprint 2 se construit sur des hypothèses, et les
séquences d'arrêt et de démarrage deviennent incalculables. Chaque ligne non renseignée est un
risque reporté sur un sprint ultérieur.

## 1. Environnements

| Environnement | Existe ? | Représentatif de la Production ? | Responsable | Fenêtre d'intervention |
|---|---|---|---|---|
| Production | | — | | |
| UAT | | | | |
| Formation | | | | |
| Intégration | | | | |

> L'UAT doit être représentatif **avant le Sprint 7** : c'est là que la première opération
> réelle est exécutée. Un UAT non représentatif ne vaut pas recette.

## 2. Composants par environnement

À dupliquer pour chaque environnement.

| Composant | Type | Nombre | Serveur(s) | Service Windows | Criticité |
|---|---|---|---|---|---|
| Cluster Node | ClusterNode | | | | |
| Center Node | CenterNode | | | | |
| Bridge | Bridge | | | | |
| ECN4 Web | Ecn4Web | | | | |
| XPS | Xps | | | | |
| ActiveMQ | ActiveMq | | | | |
| Base de données N4 | BaseDeDonnees | | | | |
| Billing | Billing | | | | |
| Bento | Bento | | | | |

## 3. Questions fermées à trancher

| # | Question | Réponse | Décidé par |
|---|---|---|---|
| 1 | Combien de Cluster Nodes en Production ? | | |
| 2 | ECN4 est-il déployé ? | | |
| 3 | Billing est-il déployé ? | | |
| 4 | Bento est-il déployé ? | | |
| 5 | Version exacte de N4 en Production | | |
| 6 | Version exacte de N4 en UAT | | |
| 7 | Le Center Node est-il redondé ? | | |
| 8 | Quels systèmes dépendants sont critiques à l'arrêt ? | | |

## 4. Dossiers partagés et flux EDI

| Dossier / flux | Chemin logique | Catégorie | Partenaire | Fréquence |
|---|---|---|---|---|
| | | | | |

## 5. Suites

- Les composants recensés sont saisis dans le référentiel au **Sprint 2**.
- Le typage (`Type` ci-dessus) conditionne le calcul des séquences : un composant laissé sans
  type reste invisible des séquences d'arrêt et de démarrage.
- Les séquences réelles sont validées lors de l'atelier décrit dans
  `atelier-sequences.md`.
