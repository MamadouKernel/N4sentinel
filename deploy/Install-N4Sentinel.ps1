<#
.SYNOPSIS
    Installe ou met à jour N4 Sentinel en tant que service Windows.

.DESCRIPTION
    À exécuter depuis un poste d'administration du domaine, avec un compte disposant du droit
    d'installer un service sur le serveur applicatif. Le script est idempotent : relancé sur une
    installation existante, il arrête le service, remplace les binaires et redémarre.

    Ce que le script ne fait pas, volontairement :
      - il ne crée pas le compte de service et ne saisit aucun mot de passe (SEC-003) ;
      - il ne dépose aucun secret dans appsettings.Production.json ;
      - il n'ouvre aucun port dans le pare-feu.
    Ces trois opérations relèvent de l'Infrastructure CIT et sont tracées par elle.

.PARAMETER Source
    Dossier contenant le paquet publié.

.PARAMETER Destination
    Dossier d'installation sur le serveur cible.

.PARAMETER NomDuService
    Nom du service Windows.

.EXAMPLE
    .\Install-N4Sentinel.ps1 -Source .\publication -Destination 'D:\Applications\N4Sentinel'
#>

[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string] $Source,

    [Parameter(Mandatory = $true)]
    [string] $Destination,

    [string] $NomDuService = 'N4 Sentinel'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Source)) {
    throw "Dossier source introuvable : $Source"
}

$executable = Join-Path $Destination 'N4Sentinel.Web.exe'
$service = Get-Service -Name $NomDuService -ErrorAction SilentlyContinue

if ($service -and $service.Status -ne 'Stopped') {
    Write-Host "Arrêt du service $NomDuService..."
    Stop-Service -Name $NomDuService -Force
    $service.WaitForStatus('Stopped', '00:02:00')
}

Write-Host "Copie des binaires vers $Destination..."
if (-not (Test-Path $Destination)) {
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
}

# Le fichier de configuration du serveur n'est jamais écrasé par un déploiement : il porte
# les réglages propres à l'environnement, posés une fois par l'Infrastructure.
$aPreserver = @('appsettings.Production.json')
Get-ChildItem -Path $Source -Recurse -File | ForEach-Object {
    $relatif = $_.FullName.Substring($Source.Length).TrimStart('\', '/')
    if ($aPreserver -contains $relatif -and (Test-Path (Join-Path $Destination $relatif))) {
        Write-Host "  conservé : $relatif"
        return
    }

    $cible = Join-Path $Destination $relatif
    $dossierCible = Split-Path $cible -Parent
    if (-not (Test-Path $dossierCible)) {
        New-Item -ItemType Directory -Path $dossierCible -Force | Out-Null
    }

    Copy-Item -Path $_.FullName -Destination $cible -Force
}

if (-not $service) {
    Write-Host "Création du service $NomDuService..."
    Write-Host "Le compte de service doit être renseigné ensuite par l'Infrastructure :"
    Write-Host "  sc.exe config `"$NomDuService`" obj= <DOMAINE\compte> password= <saisi par l'Infrastructure>"

    New-Service -Name $NomDuService `
                -BinaryPathName "`"$executable`"" `
                -DisplayName $NomDuService `
                -Description 'Supervision, pilotage et diagnostic de l''écosystème Navis N4.' `
                -StartupType Automatic | Out-Null
}

Write-Host "Démarrage du service $NomDuService..."
Start-Service -Name $NomDuService

$etat = (Get-Service -Name $NomDuService).Status
Write-Host "Service $NomDuService : $etat"

if ($etat -ne 'Running') {
    throw "Le service n'a pas démarré. Consulter le journal des événements Windows."
}
