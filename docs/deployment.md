# Déploiement — hébergement en Service Windows (pas d'IIS)

## Décision

N4 Sentinel n'est **pas** déployé sous IIS. L'application s'auto-héberge (Kestrel) et tourne comme un
**Service Windows** installé sur le serveur cible, démarré automatiquement avec la machine.

**Raison** : éviter la dépendance à IIS (module ASP.NET Core, pool d'applications, configuration
`web.config`) sur un poste où elle n'est pas nécessaire ; le service Windows est autosuffisant.

**Conséquence assumée pour l'instant** : le trafic est en **HTTP simple**, sans terminaison TLS ni reverse
proxy devant le service (décision DSI, réseau interne CIT de confiance). `UseHttpsRedirection()`/`UseHsts()`
ont été retirés de `Program.cs` en conséquence — les réintroduire si un reverse proxy ou une exposition hors
réseau de confiance est décidée plus tard.

## Comment ça marche

`Program.cs` appelle `builder.Host.UseWindowsService(options => options.ServiceName = "N4Sentinel");`. Cet
appel est un **no-op automatique** tant que le processus n'est pas démarré par le Service Control Manager
Windows : `dotnet run` en développement n'est pas affecté.

En production, la configuration vient de `appsettings.Production.json` (commité, sans secret) :
- Port d'écoute Kestrel : `http://0.0.0.0:5000` (à ajuster si besoin — coordonner avec la DSI réseau/pare-feu).
- Logs : Console (ignorée en mode service, sans conséquence), SQL Server (`ApplicationLogs`, source de vérité
  principale) et **Journal d'événements Windows** (source `N4Sentinel`, niveau `Warning` et au-dessus) —
  utile pour diagnostiquer un échec au démarrage avant même que la connexion SQL Server soit disponible.

La chaîne de connexion SQL Server de production **n'est jamais commitée**. Elle est fournie au moment de
l'installation via la variable d'environnement machine `ConnectionStrings__DefaultConnection` (voir script
d'installation ci-dessous), conformément à la règle du projet de ne jamais mettre de secret dans le dépôt.

## Publier et installer

Sur le serveur cible (Windows, droits Administrateur) :

```powershell
# 1. Publier (depuis un poste de build, ou directement sur le serveur si le SDK y est installé)
dotnet publish src\N4Sentinel.Web -c Release -o publish

# 2. Copier le dossier "publish" sur le serveur cible si nécessaire, puis appliquer les migrations EF Core
#    (une fois, avant le premier démarrage — voir README.md pour les deux contextes Identity + AppDbContext)

# 3. Installer le service (voir deploy/install-service.ps1 pour le détail des paramètres)
.\deploy\install-service.ps1 -PublishPath .\publish -ConnectionString "Server=<SQL_PROD>;Database=N4Sentinel;..."
```

Pour désinstaller : `.\deploy\uninstall-service.ps1`.

## Vérification post-installation

- `Get-Service N4Sentinel` doit afficher `Running`.
- Observateur d'événements Windows → Journaux Windows → Application → source `N4Sentinel` : doit être vide
  (ou ne contenir que des avertissements attendus) après un démarrage réussi.
- Table `ApplicationLogs` en base : doit recevoir des entrées `Information` dès le démarrage.
- `http://<serveur>:5000/` doit répondre (page de connexion).

## Compte de service

Par défaut, `New-Service` installe le service sous le compte `LocalSystem`. À réévaluer avec la DSI sécurité
selon les droits réellement nécessaires (accès réseau vers SQL Server de production, dossiers partagés N4
dans les sprints suivants) — un compte de service dédié à privilèges minimaux est préférable à terme.
