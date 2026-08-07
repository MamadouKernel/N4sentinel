# Sprint 12 — Diagnostic : collecte de signaux & règles versionnées

**Objectif de sprint** : poser les deux fondations sur lesquelles s'appuiera le futur moteur de diagnostic
(E7.2, Sprint 13) — la collecte de signaux utiles à un incident (E7.1) et l'administration de règles de
diagnostic configurables et versionnées (E7.3), sans construire le moteur lui-même.

Comme pour les Sprints 9-11, les lignes E7.1/E7.3 du backlog ne portaient aucune référence `FR-xxx` — le texte
complet du cahier des charges a été relu pour ce sprint. Constat structurel : ni E7.1 ni E7.3 ne sont en
réalité numérotés dans le cahier des charges (la section "Collecte des signaux" et les deux phrases de vision
sur l'import manuel sont sans numéro), à l'exception de **FR-065** (règles de diagnostic) et **FR-006** (cycle
de validation générique, déjà réutilisé). Références retenues :

- Section non numérotée **"Collecte des signaux"** ("Selon les accès autorisés et les capacités techniques
  disponibles, N4 Sentinel doit collecter les signaux nécessaires à l'établissement d'un diagnostic" ; champs
  minimaux : source, composant/environnement, dates d'origine et de collecte, statut, fraîcheur,
  qualité/fiabilité, référence de corrélation ; "Lorsqu'un signal ne peut pas être collecté, la solution doit
  l'indiquer explicitement et préciser la cause : accès refusé, connecteur indisponible, timeout, source
  absente, format non reconnu ou contrôle non configuré. L'absence d'un signal ne doit jamais être interprétée
  comme une absence d'anomalie.") + les deux bullets de vision sur la collecte automatique et l'import manuel →
  **E7.1**.
- **FR-065** (règles de diagnostic administrables : configurables, versionnées, testables sans modification du
  code ; champs identifiant/version/domaine/conditions/sources/conclusion/sévérité/méthode de
  confiance/contrôles complémentaires/recommandations/statut de validation) et **FR-006** (cycle de validation
  Brouillon/À valider/Validé/Actif/Désactivé, explicitement étendu aux "règles de diagnostic") → **E7.3**.
- **Hors périmètre de ce sprint** (Sprint 13, E7.2/E8.1) : FR-060 à FR-064 et FR-066 à FR-069 (moteur de
  diagnostic, classification par domaine FR-062, niveau de confiance, packet d'escalade) et FR-070 à FR-079B
  (import/analyse de fichiers de logs). FR-068 ("collecte automatique et ciblée") et FR-079A/B emploient un
  vocabulaire proche de "collecte" mais dépendent du moteur et des règles applicables — ils appartiennent au
  Sprint 13, pas à celui-ci.

## Sprint Backlog

| Story | Résultat |
|---|---|
| E7.1 — Collecte automatique de signaux, avec import manuel en complément | Fait |
| E7.3 — Règles de diagnostic administrables et versionnées | Fait |

## Décisions de conception

- **`DiagnosticDomain` partagé entre `DiagnosticSignal` et `DiagnosticRule`.** Le cahier des charges répète
  quasiment le même vocabulaire de domaines de cause à trois endroits (table de collecte, FR-062, FR-065) —
  réseau, base de données, système/VM, N4 Cluster Nodes, Center/Standby, ActiveMQ/KahaDB, Bridge/XPS,
  ECN4/ECN4Web, dossiers partagés, interfaces EDI, configuration, supervision existante. Un seul enum partagé
  plutôt que deux évite une divergence de vocabulaire que le moteur de diagnostic du Sprint 13 (FR-062, qui
  classe explicitement ses hypothèses "selon les règles validées") devra de toute façon réconcilier.
- **"Connecteur indisponible" est un résultat de premier ordre, pas un contournement.** Le cahier des charges
  anticipe explicitement ce cas ("Lorsqu'un signal ne peut pas être collecté, la solution doit l'indiquer
  explicitement et préciser la cause [...] connecteur indisponible"). Tant qu'aucun connecteur réel n'existe
  (mode Simulation, décision Sprint 2), `SimulationDiagnosticSignalProvider` renvoie systématiquement
  `IsAvailable: false, UnavailableReason: ConnectorUnavailable` — un comportement conforme au texte, pas un
  contournement de la fonctionnalité. La collecte automatique réelle est un sprint ultérieur (Palier 2, hors
  périmètre V1, cf. décision similaire prise pour la reconstitution au Sprint 11).
- **Référence de corrélation en texte libre, pas de clé étrangère vers une entité Incident.** Aucune entité
  "Incident" n'existe encore dans le domaine (le concept apparaît dans la section "Cycle de traitement d'un
  incident" du cahier des charges mais reste hors périmètre V1 explicite de ce sprint) : `CorrelationReference`
  reprend le même esprit que `OperationRun.IncidentOrChangeReference` (E2.2) — une chaîne obligatoire que
  l'opérateur renseigne, sans intégrité référentielle imposée.
- **`DiagnosticRule` : versionnement par nouvelle ligne, pas par découpage `Workflow`/`WorkflowVersion`.**
  Contrairement au workflow (dont la complexité — étapes enfants ordonnées et mutables — justifie deux entités
  distinctes), le contenu d'une règle de diagnostic tient dans une poignée de champs scalaires. Une nouvelle
  version est une nouvelle ligne `DiagnosticRule` partageant le même `RuleKey` avec un `VersionNumber`
  incrémenté (`CreateNewVersion()`), plutôt qu'un parent `Workflow`-like avec des versions enfants — un
  découpage à deux entités aurait ajouté de la complexité sans bénéfice pour cette forme de donnée. Le cycle de
  validation (Brouillon/À valider/Validé/Actif/Désactivé) et la désactivation automatique de l'ancienne version
  Active lors de l'activation d'une nouvelle (cf. `Workflow.ActivateVersion`) sont en revanche directement
  repris, cohérents avec FR-006.
- **"Recommandations autorisées" = texte libre, pas une référence à l'entité SOP versionnée d'E9.3.** FR-065
  ne décrit aucun champ SOP dans sa liste de champs obligatoires, et le modèle de données minimal du cahier des
  charges ("Règle de diagnostic : signature, conditions, domaine, sévérité, recommandation, version") ne porte
  pas non plus de référence SOP. La véritable entité SOP versionnée (génération depuis des étapes réellement
  exécutées, validation en brouillon, réutilisation avec historique — FR-088/FR-089A-D) est un concept distinct
  et plus riche, prévu pour E9.3 (Sprint 15). Coupler `DiagnosticRule` à une entité qui n'existe pas encore
  aurait été prématuré — même raisonnement que la non-réutilisation d'un SOP par `FolderReconstitution` au
  Sprint 11.
- **`DiagnosticRule` n'est pas rattachée à un environnement.** Le modèle de données minimal du cahier des
  charges ne porte aucun champ environnement pour "Règle de diagnostic", et l'écran "Administration" regroupe
  les règles avec les connecteurs/seuils/rôles — des réglages de portée solution, pas environnement. Page admin
  globale (`/admin/diagnostic-rules`), cohérent avec `/admin/users` et `/admin/audit`.
- **Signaux non audités, création de règle non plus** (cohérent avec la décision Sprint 9 : l'audit E10.1 reste
  volontairement réduit aux actions sensibles déjà couvertes — approbations, confirmations d'étape, gestion de
  rôles). Ni `CreateSharedFolderCommand` ni les commandes `Workflow` ne sont auditées non plus ; les commandes
  de ce sprint suivent le même principe.

## Vérification de bout en bout (navigateur)

Exécutée avec succès le 2026-08-07, environnement Production :

1. **Collecte automatique** : depuis `/environments/{id}/diagnostics`, tentative de collecte (domaine Réseau,
   source "Cluster Node 1 - navis-apex.log", référence "INC-2026-042") → apparaît "Indisponible — Connecteur
   indisponible", fiabilité "Inconnue" (signal Simulation, comme attendu).
2. **Import manuel** : import d'un extrait (domaine Base de données, "ORA-00060: deadlock detected...") sur la
   même référence "INC-2026-042" → apparaît "Collecté", origine "Import manuel", fiabilité "Moyenne".
3. **Règle de diagnostic** : création de "RULE-NET-001" (Réseau, "Perte de paquets > 5% pendant 5 minutes...")
   depuis `/admin/diagnostic-rules/new` → Brouillon v1. Cycle complet Soumettre → Valider → Activer → statut
   "Actif" confirmé. Clic "Nouvelle version" → v2 Brouillon créée avec le contenu cloné de v1 (vérifié par
   capture d'écran). Cycle Soumettre → Valider → Activer sur v2 → v2 passe "Actif" **et v1 passe
   automatiquement "Désactivé"** (historique des versions confirmé) — exactement le comportement attendu
   d'`ActivateDiagnosticRuleCommandHandler`.

169 tests unitaires verts (107 Domain + 62 Application) après la vérification.
