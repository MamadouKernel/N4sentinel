# N4 Sentinel

Application interne DSI de Côte d'Ivoire Terminal (CIT) pour standardiser, sécuriser et tracer les opérations
de supervision, pilotage (arrêt/démarrage/redémarrage) et diagnostic de l'écosystème **Navis N4** (Cluster
Nodes, Center/Standby Node, Bridge, XPS, ECN4/ECN4Web, SQL Server, ActiveMQ/KahaDB, dossiers partagés, EDI).

Développée en interne, en méthodologie Agile Scrum. Voir `docs/scrum/` pour le backlog produit, la vision, et
le suivi sprint par sprint. Le document de référence fonctionnel est
`../minignan/Cahier_de_charge_N4_Sentinel_v3.docx`. **Le comportement réel de l'écosystème N4 (séquences de
démarrage/arrêt, noms de composants, statuts, journaux, causes d'incidents) est ancré dans les guides Navis
officiels — voir [`docs/navis-reference.md`](docs/navis-reference.md), qui fait autorité sur tout le reste.**

## Stack technique

- **.NET 10** (LTS), Clean Architecture (Domain / Application / Infrastructure / Web)
- **Blazor Server** (interactivité serveur) pour l'UI
- **ASP.NET Core Identity** (authentification par rôles : Lecteur, Opérateur, Approbateur, Administrateur)
- **SQL Server** (LocalDB en développement), **EF Core 10**
- **MediatR** (CQRS) + **FluentValidation** (pipeline de validation automatique)
- **Serilog** (journalisation structurée, sinks Console + SQL Server)
- **xUnit**, **FluentAssertions 7.x** (licence Apache — la 8.x est commerciale), **NSubstitute**,
  **Testcontainers** (tests d'intégration SQL Server réel, nécessite Docker)

## Structure

```
N4Sentinel/
├── src/
│   ├── N4Sentinel.Domain/          Entités et règles métier pures
│   ├── N4Sentinel.Application/     CQRS (MediatR), DTOs, validation, interfaces d'infrastructure
│   ├── N4Sentinel.Infrastructure/  EF Core, repositories, connecteurs (implémentation Simulation)
│   └── N4Sentinel.Web/             Blazor Server, Identity, pages
├── tests/
│   ├── N4Sentinel.Domain.Tests
│   ├── N4Sentinel.Application.Tests
│   └── N4Sentinel.IntegrationTests   (nécessite Docker — sinon tests marqués skip)
└── docs/scrum/                     Vision, backlog produit, Definition of Done/Ready, sprints
```

## Démarrer en local

Prérequis : .NET 10 SDK, SQL Server LocalDB (`MSSQLLocalDB`), outil `dotnet-ef` (`dotnet tool install -g
dotnet-ef`).

```bash
dotnet build

# Appliquer les migrations (base + schéma Identity, puis référentiel métier)
dotnet ef database update --project src/N4Sentinel.Web --startup-project src/N4Sentinel.Web --context ApplicationDbContext
dotnet ef database update --project src/N4Sentinel.Infrastructure --startup-project src/N4Sentinel.Web --context AppDbContext

dotnet run --project src/N4Sentinel.Web
```

En développement, un compte Administrateur de démonstration est créé automatiquement au démarrage (voir
`appsettings.Development.json`, section `Seed` — à ne jamais reproduire tel quel en Production).

## Déploiement

Pas d'IIS : l'application s'auto-héberge et se déploie comme **Service Windows**. Voir
[`docs/deployment.md`](docs/deployment.md) et les scripts `deploy/install-service.ps1` /
`deploy/uninstall-service.ps1`.

## Tests

```bash
dotnet test tests/N4Sentinel.Domain.Tests
dotnet test tests/N4Sentinel.Application.Tests
dotnet test tests/N4Sentinel.IntegrationTests   # nécessite Docker ; sinon "skip" propre
```

## État d'avancement

Voir `docs/scrum/product-backlog.md` pour le détail. Sprint 0 (fondations) et Sprint 1 (référentiel
Environnements/Composants, FR-001/FR-002/FR-006) sont livrés. Le reste du périmètre V1 (workflows,
pilotage réel, tableau de bord, diagnostic, assistant documentaire, EDI, audit) est planifié sprint par
sprint dans `docs/scrum/sprints/`.
