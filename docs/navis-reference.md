# Référence technique Navis — sources d'autorité du projet

**Ce document fait autorité pour toute décision de conception touchant à l'écosystème N4 réel** (séquencement,
noms de composants, statuts, journaux, causes d'incidents). En cas de divergence avec une hypothèse prise
dans une session antérieure de ce projet, cette page prévaut et le code/les données de démonstration doivent
être corrigés en conséquence.

Sources (dans `minignan/`, hors dépôt `N4Sentinel/`) :
- **[GUIDE]** `N4 3.8.25 Setup, Maintenance, and System Diagnostics Guide 1.pdf` — 1353 pages, guide
  administrateur officiel Navis. Références ci-dessous au format `[GUIDE p.NNN]`.
- **[ITADMIN]** `N4 IT Admin 2024 4.x Day1 Install-Startup (2) (1).pdf` — 61 pages, support de formation
  Kaleris/Navis (2024) pour administrateurs IT. Références au format `[ITADMIN p.NN]`.

Le cahier des charges (`Cahier_de_charge_N4_Sentinel_v3.docx`) reste le document de périmètre fonctionnel
(quoi construire), mais **le "comment ça marche réellement" vient de ces deux PDF**, pas d'hypothèses.

---

## 1. Architecture (composants réels)

D'après `[ITADMIN p.5]` (diagramme d'architecture simplifié) :

- **N4 Cluster Nodes** (plusieurs, ex. X/Y/Z) — nœuds applicatifs, gèrent Gate, EDI, etc. Un cluster spécial
  est recommandé pour les tâches lourdes : traitement EDI, rapports, jobs de fond, Gate, Expert Decking,
  Vessel/Rail Autostow, Statistics, PrimeRoute, Yard Crane/AHT Schedulers, EC (Equipment Control)
  `[ITADMIN p.5]`.
- **N4 Center Node** — hôte dédié qui gère la mise en file d'attente du travail entre les nœuds applicatifs ;
  nécessaire à la communication inter-applications ; les utilisateurs finaux ne doivent PAS y accéder
  directement `[ITADMIN p.6]`.
- **Standby Center Node** — bascule redondante du Center Node, sur un hôte dédié séparé du Center Node, des
  Cluster Nodes et de XPS `[ITADMIN p.6]`. Mécanisme de verrou (lock) via ActiveMQ (verrou base de données
  par défaut depuis N4 3.3, ou verrou fichier) : un seul des deux devient Center actif à la fois
  `[GUIDE p.450-451]`.
- **N4 DB** — SQL Server ou Oracle 19 ; recommandé sur hôte 64 bits dédié ; la base N4 Billing est
  généralement sur un hôte séparé `[ITADMIN p.15]`.
- **XPS Bridge Daemon** — daemon qui permet à XPS (technologie C++) d'utiliser la technologie Java pour gérer
  les données `[GUIDE p.464]`, `[ITADMIN p.6]`.
- **XPS Server** — en réalité composé de **4 sous-services** : `XPSDaemon`, `XPSControl`, `XPSGateService`,
  `XPSMessageService`, tous doivent afficher ACTIVE `[ITADMIN p.47]`. Un seul serveur XPS peut tourner à la
  fois (message d'erreur explicite sinon) `[GUIDE p.465]`.
- **ECN4 Daemon** et **ECN4Web** — pilotage des équipements automatisés (Equipment Control), conditionnel
  selon licence `[GUIDE p.457-458]`.
- **N4 Billing** — application de facturation, conditionnelle selon licence.
- **Kafka Broker(s)** — variante "N4 Architecture (Kafka HA)" en alternative à ActiveMQ pour la distribution
  de messages en haute disponibilité `[ITADMIN p.5]`.

**Noms de services Windows exacts** (à utiliser comme `Component.Name`/`Role` dans le référentiel N4 Sentinel
plutôt que des libellés génériques) `[GUIDE p.454]` :
```
Navis N4 Center Node
Navis N4 Cluster Node
Navis XPS
Navis XPS Bridge Daemon
Navis ECN4 Daemon
Navis ECN4Web
Navis N4 Billing
Navis LogCollector Tool
```

---

## 2. Séquence de démarrage (canonique)

`[GUIDE §1.10.9, p.457-458]`, confirmée et détaillée par `[ITADMIN p.36-47]` :

1. **Démarrer chaque N4 Cluster Node, un par un.** Attendre que le nœud précédent soit ACTIVE dans Cluster
   Services et complètement initialisé avant de démarrer le suivant — sinon des conflits de validation
   peuvent créer des extensions de code en double.
2. Se connecter au client N4 (niveau Yard) et vérifier le statut ACTIVE des services démarrés dans Cluster
   Services.
3. **Démarrer le Center Node** (peut prendre plusieurs minutes).
4. **Démarrer le Standby Center Node** — n'apparaît PAS dans Cluster Services tant qu'il n'a pas pris le
   relais ; visible dans Node Info Desk avec le statut "Initializing...".
5. **Démarrer le XPS Bridge Daemon.** Attendre qu'il soit complètement actif (cycle
   WAITING → LOADING → ACTIVE, `[ITADMIN p.46]`). **Ne jamais démarrer XPS avant que le Bridge soit
   complètement up** — c'est LA règle de séquencement la plus critique du système.
6. **Démarrer XPS** (les 4 sous-services XPSDaemon/XPSControl/XPSGateService/XPSMessageService).
7. *(Si licence Equipment Control)* Démarrer **ECN4**, puis **ECN4Web** — uniquement après avoir vérifié
   N4+XPS+ECN4 actifs.
8. *(Si licence N4 Billing)* Démarrer le serveur applicatif N4 Billing.
9. Vérifier N4 et XPS actifs dans Cluster Services.
10. *(Si ECN4)* Vérifier la connexion ECN4↔ECN4Web, lancer le job de fond "Bento Server".
11. Activer le job de fond Purge/Archive.
12. Démarrer les clients XPS de la salle serveur (privilèges spéciaux), puis les autres clients XPS.

---

## 3. Séquence d'arrêt (canonique — n'est PAS le simple inverse du démarrage)

`[GUIDE §1.10.7, p.455]`, confirmée par `[ITADMIN p.59-60]` :

1. Arrêter les postes clients ECN4Web et N4 Billing.
2. Arrêter les clients XPS (y compris salle serveur).
3. Arrêter le service **ECN4Web**.
4. Arrêter le service **ECN4 Daemon**.
5. Arrêter le service **XPS**.
6. Arrêter le service **XPS Bridge Daemon**.
7. Arrêter le **Standby Center Node** — son statut Windows reste "INITIALIZING", donc l'arrêt normal du
   service peut échouer ; il faut parfois forcer via le Gestionnaire des tâches (`taskmgr.exe` →
   onglet Services → clic droit → Stop Service) `[GUIDE p.455]`, `[ITADMIN p.60]`.
8. Arrêter tous les **Cluster Nodes**.
9. Arrêter le **Center Node** en dernier.

**Point de conception important** : le Center Node démarre *avant* les nœuds applicatifs qui en dépendent
mais s'arrête *après* eux — ce n'est donc pas une simple inversion de séquence. Tout moteur de workflow (ou
tout gabarit de workflow créé au Sprint 3+ dans N4 Sentinel) doit encoder l'arrêt comme une séquence propre,
pas comme "l'inverse du démarrage".

**Avertissement de délai** : arrêter tous les nœuds N4 simultanément peut provoquer un délai allant jusqu'à
**10 minutes** (Hazelcast tente de redistribuer le cache de chaque nœud vers des nœuds également en train de
s'arrêter, jusqu'à un timeout de 10 min) `[GUIDE p.455-456]`. À encoder comme `TimeoutSeconds` réaliste sur
les étapes d'arrêt de Cluster Node dans les futurs gabarits de workflow.

---

## 4. Vocabulaire de statut réel (Cluster Services view)

`[ITADMIN p.38]` — ce sont les valeurs **exactes** affichées par le produit N4 pour un composant/service,
à utiliser comme référence pour tout futur enum de supervision (tableau de bord, Epic 4) plutôt qu'un
vocabulaire générique inventé :

| Statut | Signification |
|---|---|
| `LOADING` | Phase de démarrage normale |
| `WAITING` | Attend que le premier serveur N4 charge le cache |
| `ACTIVE` | Fonctionnement normal |
| `RECOVERING` | Le service récupère après une erreur (ex. crash) |
| `INITIALIZING` | Phase de démarrage normale |
| `SHUTDOWN` | Service arrêté proprement |
| `INACTIVE` | Heartbeat non reçu depuis plus de 2 minutes |
| `DISCONNECTED` | Heartbeat existe mais n'a pas atteint le Center Node |

Note pour N4 Sentinel : `IServerConnector.ComponentHealthStatus` (Application, Sprint 2) utilisait un enum
générique `{Unknown, Healthy, Degraded, Unhealthy}` — **corrigé** (voir journal des corrections, §7) pour
s'aligner sur ce vocabulaire réel.

---

## 5. Journaux techniques (emplacements et conventions réelles)

`[ITADMIN p.31-34]` :

| Composant | Fichier principal | Emplacement |
|---|---|---|
| N4 Cluster/Center Node | `navis-apex.log` | `C:\ProgramData\Navis\[node]\logs\` |
| Bridge daemon | `navis-bridged.log` | `C:\ProgramData\Navis\bridge\logs\` |
| ECN4 | `navis-ecn4.log` | `C:\ProgramData\Navis\ecn4\logs\` |
| ECN4Web | `navis-ecn4web.log` | `C:\ProgramData\Navis\ecn4web\logs\` |
| XPS | `xps_yyyymmddhhmmss###.log`, `xps_messages_yyyymmddhhmmss###.log` | `C:\ProgramData\Navis\xps\log\` |
| Tomcat (hors XPS) | `[service-]stdout_yyyymmdd.log`, `[service-]stderr_yyyymmdd.log` | idem |
| Client XPS ("Sparcs N4 Client") | — | `..\Sparcs N4 Client\Private\Logs` |

- Rotation : `navis-apex.log` redémarre à 10 Mo (déclenché par la taille) ; le log XPS redémarre à chaque
  démarrage (déclenché par événement).
- Configuration des niveaux de log : `Log4j2.xml` dans
  `C:\ProgramData\Navis\center\webapps\apex\WEB-INF\classes\` — **écrasé à chaque upgrade N4** ; en garder
  une copie externe si personnalisé.
- Pour une escalade support : fournir tous les logs apex (N4), + logs XPS/bridge (si XPS), + logs ECN4/
  ECN4Web (si ECN4) `[ITADMIN p.32]`.

Directement exploitable pour l'Epic 8 (Analyse des fichiers de logs) : ces conventions de nommage/emplacement
doivent être le point de départ du connecteur de collecte de logs, pas une hypothèse générique.

---

## 6. Causes racines les plus fréquentes (Top 10 P1)

`[ITADMIN p.4]` — ce sont les causes d'incidents critiques les plus fréquentes rapportées à Navis. Base de
départ directe pour les règles du moteur de diagnostic (Epic 7, FR issues du cahier des charges) :

1. Gestion des données / Archive & Purge, clôture de visites navire, unités parties/retirées
2. Supervision réseau
3. **Procédure de démarrage/arrêt incorrecte** — la raison d'être même de N4 Sentinel
4. Corruption de fichiers AMQ (KahaDB)
5. Horloges désynchronisées entre serveurs (doivent être synchronisées à ±1 seconde via NTP)
6. Sauvegardes VM/physiques causant des problèmes
7. Incompatibilité logicielle tierce (Java, Windows C++, antivirus)
8. Indexation, fragmentation, reconstruction hors ligne
9. Échec du processus d'installation
10. Problèmes des nœuds Center et Standby

---

## 7. Journal des corrections apportées au code suite à cette lecture

- **`IServerConnector.ComponentHealthStatus`** (Application, `src/N4Sentinel.Application/Abstractions/
  IServerConnector.cs`) : remplacé `{Unknown, Healthy, Degraded, Unhealthy}` par les 8 statuts réels du
  Cluster Services view (`LOADING, WAITING, ACTIVE, RECOVERING, INITIALIZING, SHUTDOWN, INACTIVE,
  DISCONNECTED`), avec libellés français associés. Impact : `SimulationServerConnector`, tests concernés.
- Les données de démonstration créées en Sprint 1/2 ("Cluster Node 1", "Bridge", "Démarrer Cluster Node 1",
  "Vérifier santé Bridge") restent des données de *démonstration* de l'UI, pas des gabarits métier — les
  futurs Sprints 3-6 (workflows réels de démarrage/arrêt) doivent utiliser les noms de service exacts
  ci-dessus (§1) et encoder fidèlement les séquences des §2/§3, pas des exemples inventés.

## 8. Sujets non encore extraits (à approfondir aux sprints concernés)

- **Partie 2 du GUIDE — "System Diagnostics, Monitoring & Recovery"** (`[GUIDE p.810+]`) : arbre de
  diagnostic complet par catégorie de problème (Node Problems p.839, XPS Server/Client Problems p.867,
  Bridge/XPS communication p.883, ECN4/ECN4Web p.887, etc.). À extraire en détail avant le Sprint 11-12
  (Diagnostic — collecte & règles / moteur de diagnostic).
- **Ports réseau détaillés** (`[GUIDE p.443-449]`) : table complète par composant (XPS 13000-13099, ECN4
  13100-13199, N4 13200-13299, etc.) — à mobiliser lors de l'implémentation des connecteurs réels
  (au-delà de la Simulation), hors périmètre avant que CIT autorise les accès réseau réels.
- **Gestion des dossiers partagés et AMQ/KahaDB** (`[GUIDE p.450-453]`) : procédure de reconstruction d'index
  en cas de corruption — pertinent pour l'Epic 5 (Supervision dossiers partagés/ActiveMQ).
