# Sprint 9 — Sécurité et audit transverse

**Objectif de sprint** : empêcher un Administrateur d'approuver seul une étape sensible qu'il a lui-même
demandée (E11.2), auditer toute attribution/révocation de rôle (E11.3), et donner à l'Administrateur un
journal d'audit des validations et opérations (E10.1).

Les trois stories sont sous-spécifiées dans `docs/scrum/product-backlog.md` (une ligne chacune) ; le texte
complet du cahier des charges (FR-013, FR-014, FR-027, FR-091, FR-092, tableau des rôles) a été relu pour ce
sprint afin de cadrer précisément ce qui est construit et ce qui est explicitement différé.

## Sprint Backlog

| Story | Résultat |
|---|---|
| E11.2 — Séparation des responsabilités (un Administrateur ne peut pas approuver seul son propre contournement) | Fait, périmètre réduit — voir décision |
| E11.3 — Audit de l'attribution/révocation de rôle | Fait |
| E10.1 — Journal d'audit (qui, quoi, quand, résultat) | Fait, périmètre réduit — voir décision |

## Décisions de conception

- **"Contournement" (FR-027) n'a aucune représentation dans le domaine aujourd'hui**, et construire la
  fonctionnalité complète (motif obligatoire, identification du risque accepté, matrice de criticité pilotant
  une seconde validation en Production, distinction "contrôle non-contournable") est un chantier bien plus
  large que les 5 points de cette story — `docs/scrum/sprints/sprint-4.md` avait déjà noté explicitement que
  la règle "demandeur ≠ approbateur" posée pour E3.6 n'était qu'un garde-fou minimal, E11.2 étant une épopée
  séparée. **Ce sprint applique donc la même règle "demandeur ≠ confirmateur" au point de gate qui existe
  réellement aujourd'hui et qui se rapproche le plus d'un contournement** : une étape de workflow marquée
  `RequiresApproval` (par opposition à `RequiresConfirmation` simple) ne peut pas être confirmée par
  l'utilisateur qui a demandé l'opération (`ConfirmOperationStepCommandHandler`). La fonctionnalité complète
  (motif/risque/matrice de criticité) reste à construire dans une story dédiée quand un vrai connecteur réel
  (au-delà de la Simulation) rendra le risque du contournement tangible.
- **Le journal d'audit n'audite que les commandes qui portent déjà un acteur explicite, et seulement leur
  succès.** Auditer *toutes* les commandes CQRS exigerait d'abord de faire transiter l'utilisateur courant
  jusqu'à chacune (la plupart des pages de création/édition du référentiel — Environnements, Composants,
  Workflows, Systèmes dépendants — n'ont aujourd'hui aucun accès à l'utilisateur authentifié) : un chantier
  transverse en soi, hors de portée de ce sprint. Ce sprint audite les 4 commandes qui correspondent aux
  "validations" citées par l'énoncé de la story et qui portent déjà l'identité de l'acteur :
  `CreateOperationRunCommand`, `ApproveOperationRunCommand`, `RejectOperationRunCommand`,
  `ConfirmOperationStepCommand` — plus les deux nouvelles commandes de gestion de rôle (E11.3).
  N'auditer que les succès évite un risque de correction concret : le comportement établi de tous les
  handlers CQRS de cette base de code est d'appeler `SaveChangesAsync` en toute dernière instruction, après
  toutes les mutations du domaine — si le pipeline d'audit appelait `SaveChangesAsync` après un échec pour
  persister l'entrée d'audit d'échec, il persisterait **aussi** silencieusement toute mutation de domaine
  déjà suivie par le change tracker EF Core avant l'exception, alors que ce state n'était jamais censé être
  enregistré. Auditer un échec correctement nécessiterait un contexte de persistance isolé (`IDbContextFactory`)
  — non introduit ce sprint pour éviter la corruption silencieuse de données plutôt qu'un journal d'audit
  incomplet mais sûr. Les échecs restent visibles dans les logs Serilog existants (non structurés pour
  l'audit, mais présents).
- **`AuditEntry` est un agrégat volontairement sans méthode de mutation** (aucun `Update`/`Delete`) — reflète
  au niveau du domaine l'exigence FR-092 "non modifiable par les opérateurs" : rien dans le code applicatif
  ne peut modifier une entrée d'audit après sa création, seule son insertion est possible.
- **E11.3 corrige au passage un écart d'architecture** relevé pendant le cadrage : `UserList.razor` (Sprint 8)
  appelait `UserManager<ApplicationUser>` directement depuis la page Blazor, contournant entièrement la
  couche Application — cohérent avec `IdentitySeeder` (qui a la même particularité, mais qui est un script de
  démarrage, pas une action utilisateur à auditer) mais pas avec le reste de l'application, où toute mutation
  passe par MediatR. Nouvelle abstraction `IUserRoleService` (interface dans Application, implémentation dans
  Web via `UserManager`/`RoleManager` — même pattern d'inversion de dépendance que `IServerConnector`, dont
  l'implémentation vit dans Infrastructure) : `GrantRoleCommand`/`RevokeRoleCommand` (audités) et
  `LockUserAccountCommand`/`UnlockUserAccountCommand` (non audités, hors périmètre littéral d'E11.3) routent
  désormais toutes les mutations de compte par MediatR, `ListUsersQuery` remplace la boucle directe sur
  `UserManager.Users` dans la page.

## Bug transverse découvert et corrigé : `ValidationBehavior`/`AuditBehavior` n'agissaient sur aucune
## commande "sans réponse" depuis le Sprint 0

En vérifiant `AuditBehavior` dans le navigateur, aucune entrée d'audit n'apparaissait pour `GrantRoleCommand`
alors que la commande s'exécutait bien (le rôle était réellement attribué). Investigation :

- Ce projet utilise **MediatR 14.2.0**, dont le paquet `MediatR.Contracts` 2.0.1 a changé la hiérarchie des
  interfaces par rapport aux versions plus anciennes de MediatR : `IRequest` (sans type de réponse) **n'hérite
  plus de `IRequest<Unit>`** — ce sont deux interfaces sœurs distinctes, toutes deux dérivées de `IBaseRequest`.
  Confirmé par réflexion sur l'assembly `MediatR.Contracts.dll` installée.
- `ValidationBehavior<TRequest, TResponse>` et `AuditBehavior<TRequest, TResponse>` étaient tous deux
  contraints par `where TRequest : IRequest<TResponse>` (copié depuis un exemple MediatR pré-v12). Pour toute
  commande déclarée `: IRequest` (sans réponse — la majorité des commandes de mutation de cette base de code :
  `ApproveOperationRunCommand`, `RejectOperationRunCommand`, `ConfirmOperationStepCommand`,
  `UpdateEnvironmentCommand`, `ChangeEnvironmentStatusCommand`, `UpdateComponentCommand`, etc.), cette
  contrainte n'était **jamais satisfaite** : .NET ne peut construire `ValidationBehavior<TRequest, Unit>` que
  si `TRequest : IRequest<Unit>`, ce qui n'est plus vrai. Résultat : pour **toutes ces commandes**, le
  conteneur DI résolvait silencieusement une liste vide de `IPipelineBehavior<,>` — ni la validation
  FluentValidation, ni (maintenant) l'audit ne s'exécutaient, **sans la moindre exception ni avertissement**.
  Les commandes qui retournent une valeur (`CreateEnvironmentCommand`, `CreateComponentCommand`,
  `CreateOperationRunCommand`, `CreateWorkflowCommand`...) étaient épargnées car elles implémentent
  `IRequest<TResponse>` directement.
- **Correctif** : la contrainte des deux comportements est désormais `where TRequest : notnull` — la seule
  contrainte réellement exigée par `MediatR.IPipelineBehavior<TRequest, TResponse>` lui-même (vérifié par
  réflexion). Aucun changement de comportement pour les commandes qui fonctionnaient déjà ; les commandes
  "sans réponse" bénéficient enfin de la validation et de l'audit.
- **Impact** : ce bug préexistait à ce sprint (probablement depuis le Sprint 0, date d'introduction de
  `ValidationBehavior`) et touchait potentiellement la validation FluentValidation de nombreuses commandes
  antérieures — pas uniquement l'audit ajouté ce sprint. Aucune régression fonctionnelle connue n'a été
  identifiée en pratique (les formulaires Blazor valident déjà côté client via `DataAnnotationsValidator`,
  et les règles métier critiques comme "demandeur ≠ approbateur" sont appliquées dans le domaine, pas
  seulement via FluentValidation) — mais la validation serveur FluentValidation elle-même était un filet de
  sécurité silencieusement absent pour ces commandes jusqu'à ce correctif.
- Non ajouté ce sprint : un test de régression générique qui échouerait automatiquement si cette contrainte
  était réintroduite par erreur (ex. un test vérifiant qu'au moins un comportement se déclenche pour un type
  `IRequest` factice). `AuditBehaviorTests` utilise désormais des types `: IRequest` (et non `: IRequest<Unit>`)
  précisément pour couvrir ce cas à l'avenir, mais un test dédié au niveau du conteneur DI serait plus robuste
  — candidat pour un sprint de renforcement des tests.

## Vérification de bout en bout (navigateur)

Exécutée le 2026-08-07 :

1. **E11.2** : `ConfirmOperationStepCommandHandler` refuse une confirmation d'étape `RequiresApproval` par le
   demandeur de l'opération (test unitaire ; non re-testé au navigateur ce sprint, la construction d'un
   scénario `RequiresApproval` dédié ayant déjà été couverte au niveau unitaire).
2. **E11.3 / E10.1** : sur `/admin/users`, attribution du rôle "Approbateur" au compte admin — **d'abord
   observée comme silencieusement non auditée** (le rôle changeait bien, mais `/admin/audit` restait vide).
   Diagnostic par journalisation temporaire ayant mené à la découverte du bug transverse ci-dessus. Après
   correctif (`where TRequest : notnull`), nouvelle attribution de rôle : `/admin/audit` affiche
   correctement l'entrée *"Attribution de rôle — Rôle 'Approbateur' attribué à l'utilisateur '...' — Réussi"*
   avec date et auteur.
3. **E10.1 (historique global)** : confirmé que `/operations` continue de lister les 5 opérations existantes
   (aucune régression du travail du Sprint 8).

105 tests unitaires verts (66 Domain + 39 Application) après la vérification et le correctif.
