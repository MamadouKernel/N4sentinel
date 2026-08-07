# Sprint 13 — Diagnostic : moteur & analyse de logs

**Objectif de sprint** : construire le moteur de diagnostic qui consomme les fondations posées au Sprint 12
(signaux collectés, règles administrées) pour produire des hypothèses classées par domaine et niveau de
confiance (E7.2), et l'analyse de journaux techniques importés — résumé, signatures connues, recherche/
filtrage/corrélation (E8.1).

Comme pour les Sprints 9-12, les lignes E7.2/E8.1 du backlog ne portaient aucune référence `FR-xxx` — le texte
complet du cahier des charges a été relu pour ce sprint. Références retenues :

- **FR-060** (diagnostic à la demande : à partir d'un symptôme, composant, alerte, incident existant ou
  période), **FR-061** (corrélation temporelle des événements multi-serveurs), **FR-062** (classification des
  hypothèses par domaine, avec niveau de confiance), **FR-063** (explication du diagnostic : preuves à charge/
  décharge, règle appliquée et sa version, informations manquantes, contrôles recommandés, actions sûres/
  escalade — jamais une commande présentée comme exécutable automatiquement), **FR-064** (absence de
  conclusion fiable : ne jamais présenter une hypothèse comme certaine), **FR-069** (cinq niveaux de
  conclusion : Cause confirmée, Cause très probable, Causes multiples possibles, Informations insuffisantes,
  Aucune anomalie détectée — toujours borné au périmètre effectivement analysé) → **E7.2**.
- **FR-070** (import), **FR-071** (identification automatique), **FR-072** (parsing), **FR-073** (résumé),
  **FR-074** (verdict nuancé — jamais un simple OK/NOK), **FR-075** (signatures connues : erreurs DB, réseau/
  timeouts, Bridge/Center, slow consumers, files, deadlocks, KahaDB, mémoire, disque, cache/Hazelcast,
  démarrage incomplet), **FR-076** (contexte + regroupement des lignes identiques), **FR-077** (filtrage par
  période/niveau/composant/serveur/code/texte/corrélation), **FR-078** (masquage des secrets avant stockage),
  **FR-079** (rétention configurable) → **E8.1**.
- **Hors périmètre de ce sprint** : **FR-066** (comparaison avec une période saine/référence historique — aucune
  donnée de référence réelle n'existe), **FR-067** (paquet d'escalade avec empreintes de fichiers et masquage —
  suppose une couche de stockage d'archives qui n'existe pas), **FR-079A/FR-079B** (analyse du dernier journal
  sans téléversement / collecte ciblée automatique — supposent un connecteur réel vers les composants N4, comme
  FR-068).

## Sprint Backlog

| Story | Résultat |
|---|---|
| E7.2 — Moteur de diagnostic : classification par domaine et niveau de confiance | Fait |
| E8.1 — Analyse de journaux : import, résumé, signatures, recherche/filtrage/corrélation | Fait |

## Décisions de conception

- **`DiagnosticCase` reprend l'entité "Incident / Diagnostic" du modèle de données minimal** (symptôme, période,
  hypothèses, preuves, confiance, conclusion). `CorrelationReference` reste un champ texte libre — même
  raisonnement que `DiagnosticSignal` (Sprint 12) : aucune entité "Incident" distincte n'existe dans le domaine,
  et le cahier des charges ne décrit aucune référence structurée entre un diagnostic et les signaux/journaux
  qu'il exploite. C'est cette référence commune qui relie `DiagnosticCase`, `DiagnosticSignal` et
  `ImportedLogFile` d'un même incident.
- **Le moteur (`DiagnosticEngineService`) évalue réellement les `DiagnosticRule` Actives contre les données déjà
  réunies** — signaux collectés/importés (Sprint 12) et journaux analysés (ce sprint), filtrés par domaine et
  par référence de corrélation. C'est un calcul déterministe sur des données réelles présentes dans
  l'application, pas un score simulé ou arbitraire : il satisfait FR-062 (classification par domaine et
  confiance) et FR-063 (la règle appliquée et sa version sont figées sur l'hypothèse au moment de sa création,
  pour qu'une évolution ultérieure de la règle ne modifie jamais rétroactivement une explication déjà rendue).
  Un utilisateur habilité peut aussi ajouter une hypothèse manuelle, cohérent avec la flexibilité de FR-060
  ("message d'erreur ou signature connue").
- **FR-066 et FR-067 restent hors périmètre** : la comparaison avec une période saine validée suppose des
  données de référence historiques qui n'existent pas encore (aucun connecteur réel, Sprint 2), et le paquet
  d'escalade (rapport/archive avec empreintes de fichiers, masquage de secrets, export) suppose une couche de
  gestion de fichiers/archives qui n'existe pas dans l'application — deux candidats naturels pour un sprint
  ultérieur une fois ces fondations posées.
- **`ImportedLogFile` reprend l'entité "Log importé" du modèle de données minimal** (source, période, hash,
  emplacement, rétention, statut d'analyse) — distincte de `DiagnosticCase`, cohérent avec le cahier des
  charges qui décrit "Diagnostic" et "Analyse de logs" comme deux écrans et deux entités séparés, reliés
  seulement de façon informelle via la référence de corrélation partagée.
- **Import par contenu texte collé, pas par téléversement de fichier/archive binaire.** FR-070 et SEC-007
  (contrôle de sécurité des fichiers importés) supposent une couche de traitement de fichiers (upload, scan
  antivirus, extraction d'archive, validation de format) qui n'existe pas encore dans l'application — la
  construire pour ce seul sprint aurait été disproportionné. Le contenu collé est haché (SHA-256, FR-072 :
  "hash") et les secrets sont masqués par expression régulière avant tout stockage (FR-078) — un mécanisme
  réel, pas simulé.
- **FR-072 (parsing) et FR-075 (signatures) implémentés par comptage de lignes et détection de sous-chaînes
  connues, pas par un analyseur syntaxique par format.** Le cahier des charges n'impose aucun format de journal
  particulier (syslog, JSON...) — un analyseur complet par format serait une fonctionnalité à part entière hors
  de portée raisonnable pour ce sprint. Le comptage ERROR/WARN et la table de signatures connues (erreurs DB,
  KahaDB, Bridge, mémoire, disque, Hazelcast, démarrage incomplet...) restent des vérités simples et
  documentées dans le code, cohérent avec la décision Sprint 10 sur les seuils de détection d'anomalie.
- **FR-077 (filtrage) implémenté comme filtre structuré (texte libre + niveau), pas une simple recherche plein
  texte** — avec regroupement des occurrences identiques et quelques lignes de contexte avant/après la première
  occurrence (FR-076), à la fois par fichier et en recherche globale cross-fichiers.

## Vérification

169 → 196 tests unitaires verts (121 Domain + 75 Application), couvrant `DiagnosticCase` (classification,
conclusion, cycle de vie), `ImportedLogFile` (masquage des secrets, hachage, verdicts d'analyse),
`DiagnosticEngineService` (évaluation réelle des règles actives contre signaux/journaux) et l'ensemble des
handlers CQRS (création de cas, ajout d'hypothèse manuelle et via règle, conclusion, import/analyse de journal).

**Vérification navigateur non concluante** : une régression d'environnement (rendu interactif Blazor Server
affichant systématiquement "Not Found" sur toute page nécessitant une authentification, y compris
`/Account/Login` lui-même) est apparue pendant ce sprint. Investigation approfondie : le rendu HTML statique
(SSR) est correct, seul le rendu interactif après connexion du circuit SignalR échoue ; le comportement est
identique après reconstruction complète (`dotnet clean`), redémarrage du serveur, et — déterminant — se
reproduit à l'identique sur le commit Sprint 12 déjà vérifié avec succès en navigateur dans ce même
environnement plus tôt dans la session. Ce n'est donc pas une régression du code de ce sprint, mais un problème
d'environnement (processus de développement / outil de prévisualisation) apparu en cours de session. La
vérification fonctionnelle de ce sprint s'appuie donc sur la suite de tests automatisés ; une vérification
navigateur complète est à refaire dans un environnement de développement sain.
