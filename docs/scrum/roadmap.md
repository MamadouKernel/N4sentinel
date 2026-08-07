# Roadmap Sprints — N4 Sentinel V1

Vue d'ensemble sprint par sprint du `product-backlog.md`. Sprints de 2 semaines, ~15-25 points par sprint
(hypothèse initiale, à recalibrer sur la vélocité réelle après les sprints 2-3 — voir note en bas de page).
Chaque sprint respecte les dépendances : le moteur de workflows (Sprint 2) doit exister avant tout pilotage
réel (Sprints 4-6), qui doit exister avant que le tableau de bord ait quelque chose de réel à afficher
(Sprint 7), etc.

| Sprint | Thème | Stories (points) | Total pts | Statut |
|---|---|---|---|---|
| 0 | Fondations techniques | E12.1 Architecture .NET (8) · E12.2 Identity/rôles (5) · E12.3 Serilog (3) | 16 | **Fait** |
| 1 | Référentiel — Environnements & Composants | E1.1 CRUD environnement (5) · E1.3 Cycle de validation (5) · E1.2 CRUD composant (8) · E12.5 Hébergement Service Windows (5) | 23 | **Fait** |
| 2 | Connecteurs & moteur de workflows | E12.4 Connecteurs pluggables + Simulation (5) · E1.4 Workflows configurables/versionnés (13) | 18 | **Fait** |
| 3 | Mode simulation & préparation | E1.5 Test connectivité sans action mutative (5) · E3.1 Mode simulation (8) · E2.1 Sélection de scénario (5) | 18 | **Fait** |
| 4 | Premières opérations réelles (risque maîtrisé) | E2.2 Motif/référence obligatoires (5) · E3.4 Opération partielle/unitaire (8) · E3.6 Double validation (5) | 18 | À faire |
| 5 | Pilotage — arrêt complet | E3.2 Scénario d'arrêt complet (13) · E3.5 Arrêt sur échec / reprise (8) | 21 | À faire |
| 6 | Pilotage — démarrage complet | E3.3 Scénario de démarrage complet (13) · E1.6 Cartographie systèmes dépendants (5) | 18 | À faire |
| 7 | Tableau de bord & comptes | E4.1 Dashboard temps réel (8) · E4.2 Historique des opérations (5) · E11.1 Gestion des comptes (UI) (8) | 21 | À faire |
| 8 | Sécurité & audit transverse | E11.2 Séparation des responsabilités (5) · E11.3 Audit des rôles (3) · E10.1 Journal d'audit complet (8) | 16 | À faire |
| 9 | Supervision dossiers partagés / ActiveMQ | E5.1 Anomalies dossiers partagés (8) · E5.3 Synchro N4/Bridge/XPS, ActiveMQ (8) | 16 | À faire |
| 10 | Reconstitution & EDI | E5.2 Reconstitution sécurisée (8) · E6.1 Suivi intégrations EDI (8) | 16 | À faire |
| 11 | Diagnostic — collecte & règles | E7.1 Collecte automatique de signaux (8) · E7.3 Règles de diagnostic versionnées (8) | 16 | À faire |
| 12 | Diagnostic — moteur & logs | E7.2 Moteur de diagnostic (classification par domaine/confiance) (13) · E8.1 Analyse de logs (recherche/corrélation) (8) | 21 | À faire |
| 13 | Assistant documentaire N4 | E9.1 Assistant N4 (RAG, réponses sourcées) (13) · E9.2 Garde-fou "jamais d'action déclenchée" (3) | 16 | À faire |
| 14 | Clôture V1 | E9.3 SOP versionnées (8) · E10.2 Export de rapports (5) | 13 | À faire |

**Total V1 : 257 points sur 15 sprints** (4 faits, 11 à faire), soit environ **6-7 mois** au rythme de 2
semaines/sprint si la vélocité réelle confirme l'hypothèse de 15-25 pts/sprint.

## Notes de cadrage

- Ce séquencement est une **proposition de Sprint Planning indicative**, pas un engagement contractuel : à
  chaque Sprint Review, le contenu du sprint suivant sera reconfirmé avec le Product Owner (DSI) en fonction
  de la vélocité observée et des priorités qui évoluent.
- Les Sprints 2-6 (moteur de workflows → pilotage complet) sont le chemin critique : sans eux, aucune
  fonctionnalité "métier" visible pour un opérateur n'existe encore, seulement le référentiel. À signaler à
  la DSI si une démo intermédiaire est attendue plus tôt — on peut réordonner (ex. avancer le Sprint 7
  Dashboard) si une visibilité rapide est préférée à la profondeur fonctionnelle.
- Les Sprints 9-14 (supervision dossiers/EDI, diagnostic, assistant documentaire) sont largement
  parallélisables entre eux s'il y a plusieurs développeurs — ici planifiés en séquence car cette session
  travaille avec une seule "équipe" de développement.
- Detail des critères d'acceptation par story : `docs/scrum/product-backlog.md`. Comptes rendus détaillés
  (revue + rétrospective) des sprints déjà exécutés : `docs/scrum/sprints/sprint-0.md`,
  `docs/scrum/sprints/sprint-1.md`.
