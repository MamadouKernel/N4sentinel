# Déploiement

Le paquet est produit par le workflow `publication.yml` (GitHub Actions), sous forme d'une
publication autonome `win-x64` : le serveur cible n'a pas besoin du runtime .NET.

## Chaîne de déploiement

1. **Production du paquet** — automatique, dans GitHub Actions, à partir d'un tag `v*` ou d'un
   déclenchement manuel. Les tests passent avant la publication ; un échec de test ne produit
   aucun artefact.
2. **Transfert vers le réseau CIT** — manuel. Aucun exécuteur GitHub n'atteint le réseau du
   terminal ; automatiser cette étape supposerait un exécuteur auto-hébergé, à demander à la DSI.
3. **Installation** — `Install-N4Sentinel.ps1`, depuis un poste d'administration du domaine.

## Ce que l'Infrastructure fait, et que le script ne fait pas

| Opération | Pourquoi elle reste manuelle |
|---|---|
| Création du compte de service et saisie du mot de passe | SEC-003 — aucun secret ne transite par un script versionné |
| Renseignement de `appsettings.Production.json` | Contient les références de coffre et la chaîne de connexion propres au serveur |
| Ouverture du port HTTPS dans le pare-feu | Relève de la politique réseau CIT |
| Installation du certificat serveur | Relève de l'autorité de certification CIT |

## Vérification après installation

```powershell
Get-Service 'N4 Sentinel'
Invoke-WebRequest https://<serveur>/sante -UseBasicParsing
```

La sonde `/sante` répond `{"statut":"ok"}` et rien d'autre : elle ne renseigne pas un appelant
non authentifié sur l'état interne de l'application.
