# N4 Sentinel

Application interne de la DSI de Côte d'Ivoire Terminal pour **superviser, piloter et
diagnostiquer l'écosystème Navis N4** : cartographie temps réel, opérations d'arrêt et de
démarrage tracées et approuvées, analyse de journaux, diagnostic outillé et base documentaire.

Elle remplace des procédures aujourd'hui manuelles, sensibles à l'erreur et non traçables.

## État

**Sprint 1 livré** — socle technique, modèle de données, intégration continue, puis
authentification avec second facteur, huit profils, droits différenciés par environnement et
journal d'audit non modifiable. L'application se connecte ; les écrans métier arrivent à partir
du Sprint 2.

Le plan complet — 25 sprints, 4 lots, 148 exigences — est dans
[`docs/plan-de-sprints.html`](docs/plan-de-sprints.html).

> La branche `main` porte une version antérieure du produit, construite sur un backlog
> précédent. Le développement en cours vit sur `v2`, reparti d'un dépôt vide sur la base du
> cahier des charges v3 et de la maquette validée.

## Démarrer

```bash
dotnet build N4Sentinel.slnx
dotnet test N4Sentinel.slnx
dotnet run --project src/N4Sentinel.Web
```

Prérequis : SDK .NET 10 et SQL Server LocalDB.

Avant le premier démarrage, poser le compte d'amorçage — il n'est jamais versionné (SEC-003) :

```bash
dotnet user-secrets set "Amorcage:EmailAdministrateur" "prenom.nom@citcotedivoire.com" --project src/N4Sentinel.Web
```

Le mot de passe se pose de la même façon, sur la clé `Amorcage:MotDePasseAdministrateur`.
Sans ces deux valeurs, l'application démarre et signale qu'aucun compte n'a été créé.

Faute de relais SMTP configuré, les codes de second facteur sont écrits dans
`src/N4Sentinel.Web/courriels-sortants/`, avec un avertissement au journal.

## Organisation du dépôt

| Chemin | Contenu |
|---|---|
| `src/` | Les huit couches applicatives du §3.15 du cahier des charges |
| `tests/` | Tests de domaine, d'application et d'architecture |
| `deploy/` | Installation en service Windows |
| `docs/architecture.md` | Dossier d'architecture soumis à la DSI |
| `docs/maquette/` | Référence visuelle contraignante |
| `docs/cadrage/` | Livrables de cadrage du Sprint 0 |
| `docs/scrum/sprints/` | Compte rendu de chaque sprint |

## Architecture

Huit projets, un par couche, avec des règles de dépendance vérifiées par des tests : une couche
qui référence une couche interdite fait échouer la génération. Le détail, les choix techniques
et leurs motifs sont dans [`docs/architecture.md`](docs/architecture.md).

## Deux conditions préalables au plan

Elles ne relèvent pas du développement, mais elles conditionnent la livraison du Lot 1 :

1. **Accès techniques aux serveurs N4 ouverts avant le Sprint 3.**
2. **Environnement UAT représentatif disponible avant le Sprint 7.**

Sans eux, le Lot 1 n'est pas livrable : le cahier des charges exige l'exécution réelle des
commandes en V1, pas une simulation. Voir
[`docs/cadrage/demande-acces-techniques.md`](docs/cadrage/demande-acces-techniques.md).
