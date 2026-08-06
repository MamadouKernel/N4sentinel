<#
.SYNOPSIS
    Installe N4 Sentinel en tant que Service Windows (pas de dependance a IIS).

.DESCRIPTION
    A executer sur le serveur cible, avec des droits Administrateur, apres avoir publie l'application :
        dotnet publish src\N4Sentinel.Web -c Release -o publish

    Le service demarre le binaire auto-heberge (Kestrel) directement. La configuration Production
    (port d'ecoute, sinks de logs) provient de appsettings.Production.json - voir docs/deployment.md.

.PARAMETER PublishPath
    Dossier contenant le resultat de "dotnet publish" (N4Sentinel.Web.exe attendu a cet emplacement).

.PARAMETER ConnectionString
    Chaine de connexion SQL Server de production. Obligatoire : jamais commitee dans le depot,
    doit etre fournie par la DSI au moment de l'installation.

.EXAMPLE
    .\install-service.ps1 -PublishPath C:\Apps\N4Sentinel\publish -ConnectionString "Server=SQLPROD;Database=N4Sentinel;..."
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PublishPath,

    [Parameter(Mandatory = $true)]
    [string]$ConnectionString,

    [string]$ServiceName = "N4Sentinel",
    [string]$DisplayName = "N4 Sentinel"
)

$ErrorActionPreference = "Stop"

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Ce script doit etre execute dans une console PowerShell lancee en Administrateur."
}

$exePath = Join-Path $PublishPath "N4Sentinel.Web.exe"
if (-not (Test-Path $exePath)) {
    throw "Introuvable : $exePath. Executez d'abord 'dotnet publish src\N4Sentinel.Web -c Release -o `"$PublishPath`"'."
}

if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    throw "Le service '$ServiceName' existe deja. Utilisez uninstall-service.ps1 avant de reinstaller."
}

Write-Host "Configuration des variables d'environnement machine (ASPNETCORE_ENVIRONMENT, chaine de connexion)..."
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
[Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $ConnectionString, "Machine")

Write-Host "Creation du service '$ServiceName'..."
New-Service -Name $ServiceName `
    -BinaryPathName "`"$exePath`"" `
    -DisplayName $DisplayName `
    -Description "Supervision, pilotage et diagnostic de l'ecosysteme Navis N4 (CIT)." `
    -StartupType Automatic

Write-Host "Demarrage du service..."
Start-Service -Name $ServiceName

Write-Host "Service '$ServiceName' installe et demarre. Verifiez l'Observateur d'evenements (source '$ServiceName') en cas d'echec au demarrage."
