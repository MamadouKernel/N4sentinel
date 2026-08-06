# Sprint 0 — Fondations techniques

**Objectif de sprint** : disposer d'une solution .NET 10 / Clean Architecture compilable, testable, avec
authentification par rôles et journalisation structurée, prête à accueillir les premières stories métier.

## Sprint Backlog

| Story | Résultat |
|---|---|
| E12.1 — Architecture Clean Architecture .NET + solution scaffoldée | Fait |
| E12.2 — Authentification par rôles (ASP.NET Core Identity) | Fait |
| E12.3 — Journalisation structurée centralisée (Serilog, sinks Console + SQL Server) | Fait |
| E11.1 (partiel) — Les 4 rôles applicatifs sont créés (Lecteur, Opérateur, Approbateur, Administrateur) et un compte Administrateur de démonstration est seedé en développement | Fait |

## Ce qui a été livré

- Solution `N4Sentinel.sln` (format `.slnx`), 7 projets : Domain, Application, Infrastructure, Web (Blazor
  Server), Domain.Tests, Application.Tests, IntegrationTests.
- Deux `DbContext` distincts sur la même base SQL Server (`ApplicationDbContext` pour Identity,
  `AppDbContext` pour le référentiel métier), chacun avec son propre historique de migrations
  (`__EFMigrationsHistory_Identity` / `__EFMigrationsHistory_App`) pour éviter tout conflit.
- Pipeline MediatR avec comportement de validation FluentValidation automatique sur toutes les commandes/
  requêtes (`ValidationBehavior<TRequest,TResponse>`).
- Dépôt Git local initialisé.

## Décisions techniques notables (à valider en rétrospective avec la DSI)

- **FluentAssertions figé en version 7.x** (et non la dernière 8.x) : la version 8 est passée sous licence
  commerciale payante pour un usage en entreprise. La 7.x reste sous licence Apache 2.0 libre.
- **SignalR non ajouté explicitement** : Blazor Server repose déjà nativement sur SignalR pour son circuit de
  rendu ; ajouter un Hub dédié sans consommateur aurait été de la sur-ingénierie prématurée. À réévaluer au
  Sprint où le tableau de bord temps réel multi-utilisateurs (Epic 4) sera construit.
- **Seed du compte Administrateur limité à l'environnement de développement** (`app.Environment.IsDevelopment()`)
  : la création automatique d'un compte à privilèges élevés ne doit jamais être un comportement par défaut en
  Production.
- **Collection de dépendances de composant stockée via le support natif EF Core 8+ "primitive collections"**
  (colonne JSON générée automatiquement) plutôt qu'une table de jointure ou un convertisseur manuel — moins de
  code, suffisant pour le besoin actuel (FR-002). Une table de jointure dédiée sera envisagée si le moteur de
  séquencement (Epic 1, E1.4) a besoin de requêtes relationnelles sur ce graphe.

## Rétrospective

- **Ce qui a bien fonctionné** : le template `dotnet new blazor --auth Individual` fournit un socle Identity
  complet (comptes, 2FA, passkeys, gestion de profil) qu'il aurait été coûteux de récrire — conservé tel
  quel dans `Web/Data`, seul le provider de base de données a été changé (SQLite → SQL Server).
- **Point de vigilance** : `dotnet remove package` a échoué avec un chemin relatif depuis la racine de la
  solution (bug/quirk de l'outil avec le nouveau format `.slnx`) ; contournement systématique en se plaçant
  dans le dossier du projet avant d'appeler les commandes `dotnet add/remove package`.
- **Action pour le sprint suivant** : dès que le moteur de workflows (Epic 1, E1.4) démarrera, prévoir les
  connecteurs serveurs (E12.4) en amont, car E3.x (pilotage réel) en dépend entièrement.
