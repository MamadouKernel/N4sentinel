# Definition of Done — N4 Sentinel

Une story n'est **Fait** que si toutes les conditions suivantes sont vraies :

1. **Code** : implémenté dans la couche appropriée (Domain / Application / Infrastructure / Web), sans
   contourner l'architecture Clean Architecture (le Domain ne référence jamais Infrastructure/Web).
2. **Compilation** : `dotnet build` sur la solution entière — 0 erreur, 0 avertissement.
3. **Tests** :
   - Toute règle métier (Domain) a un test unitaire xUnit correspondant.
   - Tout handler CQRS (Application) a un test avec repository mocké (NSubstitute) couvrant au moins le
     cas nominal et un cas d'erreur/validation.
   - `dotnet test` — tous les tests Domain/Application passent. Les tests d'intégration passent si Docker
     est disponible, sinon ils sont marqués skip explicitement (jamais rouges silencieusement).
4. **Migrations** : toute modification du modèle de données a une migration EF Core générée et appliquée
   avec succès sur une base LocalDB de vérification.
5. **UI** (si applicable) : la page est accessible dans le navigateur, l'autorisation par rôle est vérifiée
   (un rôle non habilité ne voit pas l'action), et le scénario nominal a été testé manuellement.
6. **Traçabilité métier** : toute story touchant à une exigence FR-xxx du cahier des charges référence cet
   identifiant dans le code (commentaire) ou la documentation associée.
7. **Documentation Scrum** : le backlog (`product-backlog.md`) et le sprint courant
   (`sprints/sprint-N.md`) sont mis à jour avec le statut réel de la story.
8. **Pas de régression volontaire** : aucune fonctionnalité précédemment Fait n'est cassée (vérifié par les
   tests existants qui continuent de passer).

## Ce qui n'est PAS requis à ce stade (V1 précoce)

- Couverture de tests exhaustive à 100 % — priorité aux règles métier et aux chemins critiques.
- Tests de performance/charge — hors périmètre avant que le pilotage réel (Sprint 2+) n'existe.
- Déploiement en environnement CIT réel — cette session travaille sur poste de développement local.
