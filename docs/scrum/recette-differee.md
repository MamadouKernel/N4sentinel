# Registre de recette différée

**Ce qui devra être rejoué le jour où les accès N4 et l'UAT s'ouvriront.**

## Pourquoi ce registre existe

Le développement ne s'arrête pas faute d'accès : il continue sur tout ce qui se construit et se
vérifie sans eux. Mais un sprint « livré » sans validation contre le système réel n'est pas un
sprint recetté, et la différence doit rester lisible — sinon elle se perd, et personne ne sait
plus, six mois après, ce qui a été prouvé et ce qui a été supposé.

Ce registre est la contrepartie de la décision d'avancer. Il transforme une dépendance bloquante
en dette identifiée, datée et vérifiable.

## Deux statuts, jamais confondus

| Statut | Signification |
|---|---|
| **Livré** | Code écrit, règles testées, parcours rejoué sur l'application lancée localement |
| **Recetté** | Vérifié contre un écosystème N4 réel, sur un UAT représentatif |

Un sprint peut être livré sans être recetté. Aucun ne peut être recetté sans être livré. La
recette V1 (Sprint 12) exige les deux.

## Ce qui attend, par sprint

### Sprint 3 — Connecteurs et preuve technique

- `FR-016` — état initial réel multi-signaux : mécanique testée, **jamais validée contre N4**.
- Connecteur Cluster Services : **inexistant**. Cinq connecteurs de collecte sur six.
- Connecteur SQL : n'a jamais interrogé une base N4 ; seuls ses refus sont exercés.
- Huitième statut consolidé (Cluster Services) : reste indéterminé.

### Sprint 4 — Supervision

- `FR-050` détection automatique de nouveaux nœuds — partiel.
- `FR-051` métriques CPU, mémoire, disque, processus — partiel.
- `FR-056` synchronisation N4-XPS — absent.
- `FR-057` vue réseau et base — partiel.
- `FR-058` lenteurs vues par N4 — absent.

### Sprint 6 — Préparation et simulation

- `FR-014` champs obligatoires en Production : rejoué côté message d'erreur, jamais de bout en
  bout sur un environnement de Production réel.

### Sprint 7 — Exécution réelle

- `AC-05` — arrêt complet piloté d'un environnement UAT. **Livrable de revue jamais démontré.**
- Parcours IHM : écrans et formulaires vérifiés par lecture, jamais parcourus.
- Cinq points d'entrée sur douze sans test HTTP.

### Sprint 8 — Démarrage complet

- Détection de deux Center actifs : règle écrite et testée, **aucune source** — SOP-2 la nomme
  pourtant (verrou en base ou verrou fichier ActiveMQ).
- Succès global conditionné à un test métier ou de synchronisation.
- Interdiction de créer ou supprimer des jobs pendant le démarrage des nœuds.

### Transverse — mécanismes documentés, non observés

- Écart d'horloge sous une seconde (SOP-3, Top 10 des causes de P1) — règle écrite, source
  manquante.
- Indicateurs JMX du Bridge : `QueueSize`, `DequeueCount`, `InFlightCount`, `ConsumerCount`.
- Espace disque par serveur — seuils du corpus : alerte sous 10 %, vigilance sous 20 %.

## Comment on avance malgré tout

**Construire le mécanisme, différer la seule validation N4.** C'est ce que le Sprint 3 a fait :
ses cinq connecteurs sont exercés contre de vraies ressources du poste de développement — de
vrais services Windows, de vrais ports, de vrais dossiers. Seule la cible change le jour de
l'UAT. Un connecteur d'horloge ou de disque suivra le même chemin : validé comme mécanisme,
en attente d'une cible N4.

**Écrire la règle même sans sa source.** Une règle de domaine se teste sans accès. La détection
du double Center et le contrôle d'horloge sont écrits, testés, et attendent un connecteur — pas
une conception.

**Ne jamais présenter une règle non alimentée comme opérante.** Le tableau des verrous du
Sprint 8 distingue explicitement ce qui est branché de ce qui attend sa source.

## Le jour où l'accès s'ouvre

Ce registre devient une liste d'exécution. Chaque ligne cochée est un sprint qui passe de
« livré » à « recetté », et le compte rendu du sprint concerné est mis à jour en conséquence —
pas réécrit : complété, pour que l'écart entre les deux dates reste visible.
