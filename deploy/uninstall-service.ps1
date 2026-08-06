<#
.SYNOPSIS
    Desinstalle le Service Windows N4 Sentinel.

.EXAMPLE
    .\uninstall-service.ps1
#>
[CmdletBinding()]
param(
    [string]$ServiceName = "N4Sentinel"
)

$ErrorActionPreference = "Stop"

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Ce script doit etre execute dans une console PowerShell lancee en Administrateur."
}

$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Host "Le service '$ServiceName' n'existe pas, rien a faire."
    return
}

if ($service.Status -ne "Stopped") {
    Write-Host "Arret du service '$ServiceName'..."
    Stop-Service -Name $ServiceName -Force
}

Write-Host "Suppression du service '$ServiceName'..."
sc.exe delete $ServiceName | Out-Null

Write-Host "Service '$ServiceName' desinstalle. Les variables d'environnement machine (ASPNETCORE_ENVIRONMENT, ConnectionStrings__DefaultConnection) n'ont pas ete supprimees automatiquement ; retirez-les manuellement si necessaire."
