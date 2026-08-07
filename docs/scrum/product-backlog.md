# Product Backlog — N4 Sentinel V1

Convention : story points en suite de Fibonacci (1, 2, 3, 5, 8, 13). Priorité MoSCoW reprise du cahier des
charges (`[Must]`/`[Should]`/`[Could]`). Statut : `À faire`, `En cours`, `Fait`. Chaque story référence son
exigence FR-xxx d'origine quand elle existe.

Product Owner : DSI de CIT. Scrum Master : ce dépôt / cette IA, en session. Équipe de développement : idem
(cf. `docs/scrum/definition-of-done.md`).

---

## Epic 1 — Référentiel technique et configuration (FR-001 à FR-007)

Fondation de tout le reste : rien n'est exécutable tant que les environnements, composants, workflows et
règles de diagnostic ne sont pas enregistrés et validés dans le référentiel.

| # | User Story | Pts | Priorité | Statut |
|---|---|---|---|---|
| E1.1 | En tant qu'Administrateur, je veux créer/modifier/désactiver un environnement (Prod, UAT, autre) afin de délimiter le périmètre d'action de la solution. (FR-001) | 5 | Must | **Fait** (Sprint 1) |
| E1.2 | En tant qu'Administrateur, je veux enregistrer les composants d'un environnement avec leurs attributs minimaux (rôle, serveur, IP/DNS, OS, criticité, gouvernance, dépendances) afin de construire la cartographie. (FR-002) | 8 | Must | **Fait** (Sprint 1) |
| E1.3 | En tant qu'Administrateur, je veux que le cycle de validation (Brouillon → À valider → Validé → Actif → Désactivé) soit appliqué à chaque environnement afin d'empêcher l'usage d'une configuration non validée en Production. (FR-006) | 5 | Must | **Fait** (Sprint 1) |
| E1.4 | En tant qu'Administrateur, je veux définir des workflows versionnés (étapes, dépendances, critères de réussite, délais, politiques d'échec) sans toucher au code afin de faire évoluer les procédures sans déploiement. (FR-003, FR-004) | 13 | Must | **Fait** (Sprint 2) |
| E1.5 | En tant qu'Administrateur, je veux tester la connectivité et les contrôles d'un environnement sans action mutative avant son activation. (FR-007) | 5 | Must | **Fait** (Sprint 3) |
| E1.6 | En tant qu'Administrateur, je veux cartographier les systèmes dépendants (CAMCO/GOS, DGPS, RMS/Reefer Runner, IPAKI, Scangate, EDI) et leur caractère pilotable ou non. | 5 | Should | À faire |

## Epic 2 — Préparation d'une opération (FR-010, FR-011)

| # | User Story | Pts | Priorité | Statut |
|---|---|---|---|---|
| E2.1 | En tant qu'Opérateur, je veux sélectionner un scénario compatible avec mes habilitations et l'environnement choisi. (FR-010) | 5 | Must | **Fait** (Sprint 3) |
| E2.2 | En tant qu'Opérateur, je veux être obligé de saisir motif, fenêtre d'intervention, périmètre, impact et référence d'incident/changement avant toute opération mutative en Production. (FR-011) | 5 | Must | **Fait** (Sprint 4) |

## Epic 3 — Pilotage de l'exécution et mode simulation (FR-005 et suite)

| # | User Story | Pts | Priorité | Statut |
|---|---|---|---|---|
| E3.1 | En tant qu'Opérateur, je veux lancer une simulation d'un workflow (étapes, dépendances, risques, prérequis non satisfaits) sans exécuter aucune commande. (FR-005) | 8 | Must | **Fait** (Sprint 3) |
| E3.2 | En tant qu'Opérateur habilité, je veux exécuter un scénario d'arrêt complet de l'écosystème N4 dans l'ordre requis, avec confirmation à chaque étape sensible. | 13 | Must | À faire |
| E3.3 | En tant qu'Opérateur habilité, je veux exécuter un scénario de démarrage complet respectant l'ordre Cluster Nodes → Center/Standby Node → Bridge → XPS → ECN4/ECN4Web. | 13 | Must | À faire |
| E3.4 | En tant qu'Opérateur habilité, je veux exécuter une opération partielle ou unitaire sur un composant ou groupe de composants autorisé. | 8 | Must | **Fait** (Sprint 4) |
| E3.5 | En tant qu'Opérateur, je veux que la progression s'arrête automatiquement si un prérequis ou critère de réussite n'est pas satisfait, avec reprise depuis le dernier point de contrôle valide. | 8 | Must | À faire |
| E3.6 | En tant qu'Approbateur, je veux valider le lancement d'un workflow en Production et les contournements/arrêts forcés (double validation). | 5 | Must | **Fait** (Sprint 4) |

## Epic 4 — Tableau de bord de supervision

| # | User Story | Pts | Priorité | Statut |
|---|---|---|---|---|
| E4.1 | En tant qu'utilisateur authentifié, je veux voir en temps réel l'état des environnements, composants critiques, alertes et opérations en cours. | 8 | Must | À faire |
| E4.2 | En tant qu'utilisateur, je veux consulter l'historique des opérations passées depuis le tableau de bord. | 5 | Should | À faire |

## Epic 5 — Supervision des dossiers partagés et ActiveMQ/KahaDB

| # | User Story | Pts | Priorité | Statut |
|---|---|---|---|---|
| E5.1 | En tant qu'Opérateur, je veux être alerté d'une anomalie sur un dossier partagé (état, capacité, structure, accessibilité). | 8 | Must | À faire |
| E5.2 | En tant qu'Opérateur habilité, je veux déclencher une procédure de reconstitution sécurisée d'un dossier partagé après validation, ou suivre une SOP guidée selon le niveau de risque. | 8 | Should | À faire |
| E5.3 | En tant qu'utilisateur, je veux superviser la synchronisation N4/Bridge/XPS et détecter les accumulations de messages ActiveMQ/KahaDB. | 8 | Must | À faire |

## Epic 6 — Suivi des intégrations EDI

| # | User Story | Pts | Priorité | Statut |
|---|---|---|---|---|
| E6.1 | En tant qu'utilisateur, je veux suivre les fichiers EDI reçus, consommés, en attente, rejetés ou en erreur. | 8 | Should | À faire |

## Epic 7 — Diagnostic et analyse des incidents

| # | User Story | Pts | Priorité | Statut |
|---|---|---|---|---|
| E7.1 | En tant qu'Opérateur, je veux que la collecte des journaux/événements/métriques utiles à un incident soit automatique, avec import manuel en complément. | 8 | Must | À faire |
| E7.2 | En tant qu'Opérateur, je veux que le moteur de diagnostic classe les causes possibles par domaine (réseau, base de données, système/VM, services, composants N4, ActiveMQ/KahaDB, Bridge/XPS, dossiers partagés, EDI) et niveau de confiance. | 13 | Must | À faire |
| E7.3 | En tant qu'Administrateur, je veux définir et versionner des règles de diagnostic validées (pattern → cause probable → SOP associée). | 8 | Must | À faire |

## Epic 8 — Analyse des fichiers de logs

| # | User Story | Pts | Priorité | Statut |
|---|---|---|---|---|
| E8.1 | En tant qu'Opérateur, je veux rechercher, filtrer et corréler des journaux techniques importés ou collectés. | 8 | Must | À faire |

## Epic 9 — Base documentaire et assistant N4

| # | User Story | Pts | Priorité | Statut |
|---|---|---|---|---|
| E9.1 | En tant qu'utilisateur, je veux interroger en langage naturel le guide Navis N4 et les procédures internes et obtenir une réponse sourcée. | 13 | Must | À faire |
| E9.2 | En tant qu'utilisateur, je veux que l'assistant ne puisse jamais déclencher directement une action technique (garde-fou explicite testé). | 3 | Must | À faire |
| E9.3 | En tant qu'Opérateur habilité, je veux créer, valider, versionner et rattacher une SOP à un incident ou une opération. | 8 | Should | À faire |

## Epic 10 — Historique, rapports et audit

| # | User Story | Pts | Priorité | Statut |
|---|---|---|---|---|
| E10.1 | En tant qu'Administrateur, je veux consulter un journal d'audit complet (qui, quoi, quand, résultat) de toutes les opérations, validations et dérogations. | 8 | Must | À faire |
| E10.2 | En tant qu'utilisateur, je veux exporter un rapport d'opération ou d'incident. | 5 | Should | À faire |

## Epic 11 — Sécurité, utilisateurs et rôles (FR transverses)

| # | User Story | Pts | Priorité | Statut |
|---|---|---|---|---|
| E11.1 | En tant qu'Administrateur, je veux gérer les comptes et l'attribution des 4 rôles (Lecteur, Opérateur, Approbateur, Administrateur), différenciés par environnement. | 8 | Must | En cours (rôles créés Sprint 0, gestion UI à faire) |
| E11.2 | En tant que système, je dois empêcher un Administrateur d'approuver seul son propre contournement (séparation des responsabilités). | 5 | Must | À faire |
| E11.3 | En tant qu'Administrateur, je veux que toute attribution/modification/révocation de rôle soit auditée. | 3 | Must | À faire |

## Epic 12 — Infrastructure & plateforme (non-fonctionnel)

| # | User Story | Pts | Priorité | Statut |
|---|---|---|---|---|
| E12.1 | En tant qu'équipe de dev, je veux une architecture Clean Architecture .NET avec CI locale (build+tests) afin de garder le code testable et évolutif. | 8 | Must | **Fait** (Sprint 0) |
| E12.2 | En tant qu'équipe de dev, je veux une authentification par rôles (ASP.NET Core Identity) prête à l'emploi. | 5 | Must | **Fait** (Sprint 0) |
| E12.3 | En tant qu'équipe de dev, je veux une journalisation structurée centralisée (Serilog) dès la V1 afin de dogfooder le principe de centralisation des logs. | 3 | Should | **Fait** (Sprint 0) |
| E12.4 | En tant qu'équipe de dev, je veux des connecteurs serveurs pluggables avec une implémentation Simulation par défaut (aucun accès réseau réel tant que non autorisé). | 5 | Must | **Fait** (Sprint 2, préalable à E3.x) |
| E12.5 | En tant que DSI, je veux que la solution s'héberge en Service Windows autonome (pas d'IIS), avec scripts d'installation et journalisation Event Log, afin de simplifier l'exploitation. | 5 | Must | **Fait** (Sprint 1) |

---

## Total estimé V1 (indicatif)

~230 points répartis sur les 12 epics. Au rythme d'un sprint de 2 semaines par lot de 15-25 points (vélocité à
affiner après 2-3 sprints réels), la V1 complète représente de l'ordre de 10 à 14 sprints, soit 5 à 7 mois —
cohérent avec l'ampleur du cahier des charges (pilotage + diagnostic + assistant documentaire + EDI + audit).
Les sprints suivants affineront cette estimation (vélocité réelle observée).
