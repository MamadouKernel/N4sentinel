# Sprint 0 — Cadrage, socle technique et accès

**Semaines 1–2 · Lot 1 · Statut : livré**

**Objectif** — poser une application déployable et une architecture validée, et lancer les
demandes qui conditionnent tout le reste.

**Livrable démontrable en revue** — une application vide mais déployée automatiquement, et le
dossier d'architecture soumis à la DSI.

---

## Contexte : reprise à zéro

Ce sprint repart d'un dépôt vide sur la branche `v2`. Le contenu précédent de `main` — vingt
sprints construits sur un backlog antérieur — reste intégralement consultable dans l'historique
git ; rien n'a été supprimé du dépôt distant. Le nouveau plan repose sur le cahier des charges
v3 et sur la maquette validée avec la DSI, dont le rendu devient contraignant.

## Contenu livré

### Squelette applicatif en couches

Huit projets, un par couche du §3.15, plus trois projets de tests :

```
src/N4Sentinel.Web            Interface
src/N4Sentinel.Application    API / Domaine — contrats et règles applicatives
src/N4Sentinel.Domain         API / Domaine — entités et invariants
src/N4Sentinel.Orchestration  Orchestrateur
src/N4Sentinel.Connectors     Connecteurs
src/N4Sentinel.Diagnostics    Diagnostic
src/N4Sentinel.Knowledge      Connaissance
src/N4Sentinel.Data           Données / Audit
```

Le respect du découpage n'est pas déclaratif : `tests/N4Sentinel.Architecture.Tests` fait
échouer la génération si une couche référence une couche qu'elle n'a pas le droit de connaître.

### Modèle de données — les quinze entités du §3.18

Dix-sept types dans `src/N4Sentinel.Domain/Entities` couvrent les quinze lignes du cahier des
charges ; « Workflow / Version » et « SOP / Version » se traduisent chacune par deux types.
Chaque entité porte en commentaire la ligne du §3.18 dont elle procède, et le test
`DataModelCoverageTests` échoue si la couverture régresse.

Trois choix structurants, détaillés dans `docs/architecture.md` :

- versionnement par nouvelle ligne pour les objets scalaires, couple racine / version pour les
  objets à structure enfant mutable ;
- `N4ComponentKind` typé — sans lui, aucune séquence d'arrêt ou de démarrage n'est calculable ;
- `StepErrorKind` distingue les cinq natures d'erreur que le §3.19 impose de ne pas confondre.

### Intégration continue

| Workflow | Déclencheur | Contenu |
|---|---|---|
| `ci.yml` | poussée, demande de fusion | Génération et tests sur `windows-latest`, avertissements traités comme erreurs, dépôt des résultats |
| `ci.yml` | idem | Échec si une dépendance porte une vulnérabilité connue |
| `publication.yml` | tag `v*` ou déclenchement manuel | Publication autonome `win-x64` après tests, avec scripts de déploiement |

L'installation sur les serveurs CIT reste manuelle : aucun exécuteur GitHub n'atteint le réseau
du terminal. C'est documenté comme une limite, pas présenté comme automatisé.

### Chiffrement (SEC-005)

- Redirection HTTPS avec port explicite — nécessaire en service Windows, où les variables
  d'environnement d'IIS n'existent pas.
- HSTS un an, sous-domaines inclus.
- Clés de protection persistées hors du répertoire applicatif et chiffrées par DPAPI au niveau
  machine : une copie du dossier de clés sur un autre serveur est inexploitable.
- Politique de contenu sans aucune origine externe, cohérente avec un réseau isolé.

Le chiffrement au niveau colonne en base attend que la base existe — Sprint 2.

### Hors code

| Livrable | Fichier | Statut |
|---|---|---|
| Atelier de validation des séquences réelles | `docs/cadrage/atelier-sequences.md` | Ordre du jour et grilles prêts, atelier à tenir |
| Recensement du périmètre exact | `docs/cadrage/recensement-perimetre.md` | Grille prête, à remplir avec l'Infrastructure |
| Demande formelle des accès techniques | `docs/cadrage/demande-acces-techniques.md` | Rédigée, à adresser à la DSI |
| Arbitrage ActiveMQ / Kafka | `docs/cadrage/arbitrage-activemq-kafka.md` | Position argumentée, décision DSI attendue |

Ces quatre livrables sont des documents, pas des décisions : leur clôture appartient à la DSI et
à l'Infrastructure.

## Exigences soldées

| Référence | Objet | État |
|---|---|---|
| §3.15 | Architecture cible en couches | Proposée, en attente de validation DSI |
| §3.18 | Modèle de données minimal | Couvert |
| SEC-005 | Chiffrement communications et données au repos | Communications faites ; au repos partiel (clés faites, colonnes en S2) |
| NFR-006, NFR-007 | Exigences non fonctionnelles de socle | Prises en compte dans la CI et la publication |

## Vérification

```
dotnet build N4Sentinel.slnx   → 0 avertissement, 0 erreur
dotnet test N4Sentinel.slnx    → 27 tests, 0 échec
```

Détail : 20 tests de domaine (couverture du §3.18, racine commune, format des identifiants),
7 tests d'architecture (règles de dépendance entre couches).

## Ce qui n'est pas fait, et pourquoi

- **Aucune authentification** — Sprint 1. En attendant, l'application n'expose aucune donnée :
  il n'y a rien à protéger.
- **Aucune persistance** — Sprint 2. Le modèle existe, la base n'est pas branchée.
- **Aucun connecteur** — Sprint 3. Rien ne parle à un serveur N4.
- **Aucun écran fonctionnel** — la reprise du rendu de la maquette commence au Sprint 1. Le
  gabarit Blazor est celui du modèle par défaut.
- **L'installation sur les serveurs CIT n'est pas automatisée** — voir plus haut.

## Points ouverts pour la revue de sprint

1. Le dossier d'architecture est-il validé par la DSI ? Tout le reste en découle.
2. Quelle solution de coffre à secrets CIT implémente `ISecretResolver` ?
3. Date d'engagement sur les accès techniques — le Sprint 3 en dépend entièrement.
4. Décision ActiveMQ / Kafka.

## Sprint suivant

**Sprint 1 — Identités, profils et journal d'audit** (semaines 3–4). Dépend de ce sprint pour le
socle applicatif. Objectif : rendre impossible toute action non authentifiée, non autorisée ou
non tracée, avant même qu'une action existe.
