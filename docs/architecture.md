# Dossier d'architecture — N4 Sentinel

**Version** 0.1 — Sprint 0
**Statut** Soumis à la DSI pour validation
**Référence** Cahier des charges N4 Sentinel v3 (CIT-CIV-DSI-RFP-0010-Rév.01), §3.15 et §3.18

---

## 1. Objet

Le §3.15 du cahier des charges pose un découpage logique recommandé et laisse l'architecture
détaillée à la proposition du développeur, sous validation de la DSI. Ce document est cette
proposition. Il décrit ce qui est construit, ce qui ne l'est pas, et pourquoi.

## 2. Découpage en couches

Le découpage suit littéralement le §3.15. Chaque couche est un projet .NET distinct, et le
respect des dépendances est vérifié par des tests automatisés
(`tests/N4Sentinel.Architecture.Tests`) — un couplage interdit fait échouer la génération.

| Projet | Couche du §3.15 | Responsabilité |
|---|---|---|
| `N4Sentinel.Web` | Interface | Visualisation, saisie, approbation, exécution, analyse |
| `N4Sentinel.Application` | API / Domaine | Règles applicatives, rôles, validation, exposition contrôlée |
| `N4Sentinel.Domain` | API / Domaine | Entités et invariants métier, sans dépendance technique |
| `N4Sentinel.Orchestration` | Orchestrateur | Dépendances, état persistant, timeout, retry, pause, reprise, compensation |
| `N4Sentinel.Connectors` | Connecteurs | Services Windows / PowerShell, SQL en lecture, HTTP/TCP, fichiers, monitoring |
| `N4Sentinel.Diagnostics` | Diagnostic | Normalisation, corrélation, règles, score de confiance, explication |
| `N4Sentinel.Knowledge` | Connaissance | Indexation, recherche, réponses sourcées, versions |
| `N4Sentinel.Data` | Données / Audit | Configuration, secrets référencés, historique, logs, rapports, piste d'audit |

### Règles de dépendance

```
Domain        ← ne dépend de rien
Application   ← Domain
Orchestration ┐
Connectors    │
Diagnostics   ├─ Application, Domain (jamais l'une de l'autre)
Knowledge     │
Data          ┘
Web           ← toutes (unique point de composition)
```

Les couches techniques ne se connaissent pas entre elles. L'orchestrateur ne référence pas
les connecteurs : il dépend d'un contrat déclaré dans `Application`, dont l'implémentation lui
est injectée. C'est ce qui permettra, au Sprint 3, de substituer un connecteur réel au
connecteur de simulation sans modifier une ligne du moteur.

**Limite connue du contrôle automatisé** : le compilateur élague les références de projet non
utilisées. Les tests détectent donc les couplages effectifs, pas les références déclarées mais
inertes. C'est le comportement voulu — c'est l'usage qui crée le couplage.

## 3. Choix techniques

| Sujet | Choix | Motif |
|---|---|---|
| Plateforme | .NET 10 (LTS) | Support long, aligné sur le parc Windows Server CIT |
| Interface | Blazor Server | Formulaires, validation et autorisation côté serveur ; pas de couche JavaScript à maintenir sur 25 sprints |
| Temps réel | SignalR (intégré à Blazor Server) | Le canal existe déjà pour le rendu ; pas de dépendance supplémentaire |
| Hébergement | Service Windows, sans IIS | Démarrage automatique au boot ; une dépendance de moins à administrer |
| Publication | Autonome `win-x64` | Le serveur cible n'a pas à disposer d'un runtime installé |
| Identifiants | GUID v7 | Préfixe horodaté : ordre d'insertion préservé, index peu fragmentés |

### Interface : pourquoi Blazor Server plutôt que la structure de la maquette

La maquette de démonstration est une application à page unique en JavaScript s'appuyant sur une
API REST. Reproduire cette structure telle quelle imposerait de réécrire à la main, en
JavaScript, l'autorisation par environnement, la validation des formulaires et la double
approbation — c'est-à-dire précisément ce que le cahier des charges veut voir tracé et fiable.

Blazor Server produit du HTML arbitraire : le rendu de la maquette est reproductible à
l'identique, sans en reprendre l'architecture. L'API REST n'est pas abandonnée pour autant —
elle est prévue au Lot 4, où l'application mobile en a un besoin réel.

## 4. Modèle de données

Les quinze lignes d'entités du §3.18 sont couvertes par dix-sept types
(`src/N4Sentinel.Domain/Entities`) : « Workflow / Version » et « SOP / Version » se traduisent
chacune par deux types, la version portant le contenu figé et l'entité racine l'identité stable.

| Ligne du §3.18 | Type(s) |
|---|---|
| Environnement | `N4Environment`, `EnvironmentResponsible` |
| Composant | `N4Component`, `ComponentEndpoint`, `ComponentDependency`, `ComponentCheck` |
| Workflow / Version | `Workflow`, `WorkflowVersion`, `WorkflowStepDefinition` |
| Exécution | `OperationExecution` |
| Étape d'exécution | `ExecutionStep` |
| Contrôle / Signal | `ControlSignal` |
| Incident / Diagnostic | `DiagnosticCase`, `DiagnosticHypothesis`, `DiagnosticEvidence` |
| Log importé | `ImportedLogFile` |
| Règle de diagnostic | `DiagnosticRule` |
| Document | `KnowledgeDocument` |
| Shared Folder | `SharedFolder`, `SharedFolderCheck` |
| Fichier d'interface / EDI | `EdiFile` |
| SOP / Version | `Sop`, `SopVersion`, `SopStepDefinition` |
| Association SOP | `SopAssociation` |
| Audit | `AuditEntry` |

### Deux partis pris de modélisation

**Versionnement par nouvelle ligne.** Les objets dont le contenu est scalaire — règle de
diagnostic, document — sont versionnés en créant une ligne partageant une clé stable et un
numéro de version incrémenté. Les objets à structure enfant mutable — workflow, SOP — utilisent
un couple racine / version. Dans les deux cas, une version validée n'est jamais éditée.

**Le typage des composants est structurant.** `N4ComponentKind` distingue Cluster Node, Center
Node, Bridge, ECN4Web, XPS, ActiveMQ. Sans ce typage, le rôle étant du texte libre, aucune
règle d'ordre d'arrêt ou de démarrage n'est calculable. Un composant non typé reste invisible
des séquences : c'est délibéré, et cela devra être signalé à l'exploitation lors de la saisie
du référentiel au Sprint 2.

## 5. Sécurité

| Exigence | Mise en œuvre au Sprint 0 | Sprint de complétion |
|---|---|---|
| SEC-005 chiffrement des communications | Redirection HTTPS, HSTS 365 jours, en-têtes de sécurité stricts | — |
| SEC-005 chiffrement au repos | Clés de protection persistées hors application et chiffrées par DPAPI machine | Chiffrement colonne en base : S2 |
| SEC-003 comptes techniques | Contrat `ISecretResolver` : l'application ne manipule que des références de coffre | Coffre réel : S3 |
| SEC-008 audit | Contrat `IAuditTrail` sans mise à jour ni suppression — la piste est en ajout seul | Journal complet : S1 |
| SEC-006 pas de console libre | `WorkflowStepDefinition.Action` désigne une action approuvée, jamais une commande saisie | Catalogue d'actions : S5 |
| SEC-001, SEC-002, SEC-004 | Non traités | S1 |

La politique de contenu n'autorise aucune origine externe. L'application est destinée à un
réseau isolé : un appel à un CDN n'y échouerait pas bruyamment, il dégraderait silencieusement
la page. Polices, styles et scripts sont servis par l'application.

## 6. Intégration continue

`ci.yml` — génération et tests sur `windows-latest`, à chaque poussée et chaque demande de
fusion. Les avertissements sont traités comme des erreurs. Un second travail échoue si une
dépendance porte une vulnérabilité connue.

`publication.yml` — produit une publication autonome `win-x64`, accompagnée des scripts de
déploiement, après passage des tests.

L'installation sur les serveurs CIT n'est pas automatisée : aucun exécuteur GitHub n'atteint le
réseau du terminal. Le paquet est produit automatiquement, puis installé par
`deploy/Install-N4Sentinel.ps1` depuis un poste d'administration du domaine. Cette étape
deviendra automatique si la DSI ouvre un exécuteur auto-hébergé.

## 7. Ce que ce socle ne fait pas encore

Énoncé explicitement, pour qu'aucune revue ne le prenne pour un oubli :

- aucune authentification, aucun rôle — Sprint 1 ;
- aucune persistance : le modèle de données existe, la base n'est pas encore branchée — Sprint 2 ;
- aucun connecteur : rien ne parle à un serveur N4 — Sprint 3 ;
- aucun écran fonctionnel : l'interface est le gabarit vide, la reprise du rendu de la maquette
  commence au Sprint 1.

## 8. Points en attente d'arbitrage DSI

1. **Accès techniques aux serveurs N4** — nécessaires avant le Sprint 3. Sans eux, le Lot 1
   n'est pas livrable : le cahier des charges exige l'exécution réelle des commandes en V1.
2. **Environnement UAT représentatif** — nécessaire avant le Sprint 7.
3. **ActiveMQ ou Kafka** — voir `docs/cadrage/arbitrage-activemq-kafka.md`.
4. **Coffre à secrets** — quelle solution CIT implémente `ISecretResolver` (Windows Credential
   Manager, Azure Key Vault, autre) ?
5. **Périmètre exact** — nombre de Cluster Nodes, présence d'ECN4, Billing, Bento. Voir
   `docs/cadrage/recensement-perimetre.md`.
