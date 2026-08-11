# Demande formelle d'accès techniques

**Sprint 0 · à adresser à la DSI et à l'équipe Infrastructure CIT**
**Échéance impérative** avant le début du Sprint 3

## Pourquoi maintenant

Le cahier des charges exige l'**exécution réelle** des commandes d'arrêt, de démarrage et de
redémarrage en V1 — pas une simulation. Le Sprint 3 est celui de la preuve technique : lire
l'état réel d'un serveur N4 par plusieurs signaux croisés. Sans accès à cette date, ce sprint ne
peut pas être livré, et tout le Lot 1 glisse d'autant.

Cette demande est déposée au Sprint 0, six semaines avant le besoin, précisément pour que le
délai d'obtention ne devienne pas le chemin critique du projet.

## Accès demandés

| # | Accès | Portée | Usage | Criticité |
|---|---|---|---|---|
| 1 | Compte de service sur les serveurs N4 | UAT d'abord, Production ensuite | Interrogation et pilotage des services Windows | Bloquant S3 |
| 2 | Lecture SQL sur la base N4 | UAT, Production | Signaux d'état, corrélation de diagnostic | Bloquant S3 |
| 3 | Lecture des répertoires de logs | UAT, Production | Analyse de journaux | Bloquant S10 |
| 4 | Accès HTTP/TCP aux endpoints N4 | UAT, Production | Contrôles de disponibilité | Bloquant S3 |
| 5 | Accès aux dossiers partagés | UAT, Production | Supervision de structure et d'intégrité | Bloquant S13 |
| 6 | Console d'administration ActiveMQ | UAT, Production | Supervision des files | Bloquant S13 |
| 7 | Serveur applicatif d'hébergement | UAT | Déploiement de N4 Sentinel | Bloquant S0 |
| 8 | Instance SQL Server applicative | UAT | Base de N4 Sentinel | Bloquant S2 |

## Principe de moindre privilège

Le compte demandé n'a pas besoin d'être administrateur du domaine. Le strict nécessaire :

- démarrage, arrêt et interrogation des **services N4 nommément désignés** — pas de tous les
  services du serveur ;
- **lecture seule** sur la base N4 — l'application n'écrit jamais dans la base du TOS ;
- **lecture seule** sur les répertoires de logs.

Aucun mot de passe n'est stocké dans l'application ni dans le dépôt : les secrets sont désignés
par référence et résolus depuis un coffre au moment de l'appel (SEC-003). La solution de coffre
retenue par CIT reste à préciser.

## À renseigner par l'Infrastructure

| Élément | Réponse | Date d'engagement |
|---|---|---|
| Compte de service créé | | |
| Coffre à secrets retenu | | |
| Serveur applicatif UAT mis à disposition | | |
| Instance SQL Server applicative | | |
| Certificat serveur HTTPS émis | | |
| Ouverture des flux réseau | | |

## Conséquence d'un retard

| Date d'obtention | Impact |
|---|---|
| Avant le Sprint 3 | Aucun |
| Pendant le Sprint 3 | Le sprint bascule sur le connecteur de simulation ; la preuve technique glisse |
| Après le Sprint 3 | Les Sprints 4 à 9 sont livrés sans validation contre un système réel — la recette V1 ne peut pas être prononcée |

Ce tableau n'est pas une pression de calendrier : c'est la conséquence mécanique d'une exigence
que le cahier des charges a lui-même posée.
