# Sprint 7 — Exécution réelle et scénario d'arrêt complet

**Semaines 15–16 · Lot 1 · Statut : livré en développement, non recetté en UAT**

**Objectif** — la première action réelle du plan : arrêter un écosystème N4 dans l'ordre de
l'éditeur, sans rien casser.

**Livrable démontrable en revue** — arrêt complet piloté d'un environnement UAT. Il reste hors
d'atteinte : la réserve posée depuis le Sprint 0 n'a pas été levée (voir « Limites »).

---

## Ce qui a été livré

### Le contrat d'écriture, resté délibérément absent jusqu'ici

Le Sprint 3 avait écrit ses connecteurs en lecture seule et annoncé pourquoi : « un connecteur
sachant lire et écrire finirait par être utilisé pour écrire depuis un écran de consultation ».
`IConnecteurDeCommandes` est donc un contrat distinct d'`IConnecteurDeSignaux`, avec son propre
répartiteur — `RepartiteurDeCommandes`, pendant en écriture de `RepartiteurDeConnecteurs`. Une
action qu'aucun connecteur ne prend en charge ne lève pas d'exception : elle produit un résultat
`NonSupportee` motivé, qui fait échouer l'étape proprement plutôt que la boucle d'exécution.

Deux connecteurs le mettent en œuvre : `ConnecteurDeCommandeServiceWindows` et
`ConnecteurDeCommandeProcessus` — ce dernier modélisant l'arrêt « par gestionnaire de tâches »
du Standby Center Node que décrit le plan.

`ActionsDePilotage` ferme au passage l'écart noté au dossier d'architecture (« Catalogue
d'actions : S5 », jamais livré) : `WorkflowStepDefinition.Action` était un texte libre depuis le
Sprint 0. Une étape ne peut désormais désigner qu'une action d'un catalogue fermé (SEC-006), et
aucune demande de commande ne transporte de secret — seule une référence de coffre circule, même
convention qu'au Sprint 3.

### L'ordre de l'éditeur, vérifié plutôt que recommandé

`SequenceDArretDeReferenceN4` porte le rang d'arrêt des sept types de composants documentés par
l'éditeur — ECN4Web, ECN4, XPS, Bridge daemon, Standby Center Node, Cluster Nodes, Center Node
en dernier. Il est vérifié **à l'activation de la version de workflow** (FR-029), pas au moment
de l'exécution : un scénario qui violerait l'ordre est refusé avant d'exister, plutôt que
d'échouer une fois l'arrêt engagé.

Cette référence ne remplace pas le graphe de dépendances du Sprint 2 et ne le contredit pas : le
graphe dit ce qui dépend de quoi, la séquence dit dans quel ordre l'éditeur exige que les rôles
N4 s'arrêtent. Les deux doivent être satisfaits.

Les postes clients ECN4Web/Billing et les clients XPS, que le plan cite en tête de séquence, ne
sont pas couverts : aucun type de composant ne les modélise aujourd'hui. C'est documenté dans la
classe plutôt que masqué par un rang arbitraire.

### Une étape ne part jamais toute seule quand un humain doit trancher

`PolitiqueDeLancement` consomme enfin `ConfirmationRequise` et `ApprobationRequise`, portés par
`WorkflowStepDefinition` depuis le Sprint 0 sans que rien ne les lise. C'est le pendant *avant*
lancement de `PolitiqueDeTransition`, qui ne couvrait que l'*après*. Une étape qui exige une
décision passe en `EnAttente` avec son motif ; elle n'est relancée qu'une fois la décision prise.

Simplification assumée : quand une étape porte les deux exigences, l'approbation — le geste le
plus fort — couvre la confirmation. `ExecutionStep` ne porte qu'une seule `Decision` (Sprint 0),
et les workflows réels ne combinent pas les deux.

### Composants déjà arrêtés, ignorés proprement

Avant d'émettre quoi que ce soit, `MoteurDOrchestration` relit l'état réel du composant visé et
le compare à l'état visé par l'action (`EvaluationDeCommande.EstDejaDansLEtatVise`). Une cible
déjà dans l'état voulu fait passer l'étape à `Ignore`, avec pour preuve « aucune commande
émise » — au lieu d'un ordre d'arrêt sur un service déjà arrêté, qui renvoie une erreur de
commande et ferait échouer toute la séquence sur un composant pourtant conforme.

Le doute ne vaut jamais dispense : un état `Inconnu` ou `AConfirmer` ne permet pas d'affirmer que
la cible est déjà atteinte, donc la commande part. C'est l'exigence de preuve du Sprint 4,
appliquée en sens inverse.

L'ordre n'est pas recalculé pour autant : les étapes restantes gardent leur rang et s'enchaînent
inchangées. Sauter celle qui n'a plus lieu d'être ne déplace rien, donc ne peut rompre aucune
dépendance.

### L'arrêt forcé, ouvert par le délai et par rien d'autre

C'est le point le plus dangereux du sprint, et il a trois verrous distincts.
`PolitiqueDEscalade` n'ouvre la possibilité qu'une fois dépassé le délai déclaré sur l'étape
(`TimeoutSecondes`) : un service N4 bloqué en *Stopping* finit très souvent par s'arrêter seul,
et forcer trop tôt tue un processus en train de vider proprement ses files. Un timeout nul ou
négatif ne vaut pas « forçage immédiat » — une définition de workflow incomplète ne devient pas
une autorisation de tuer un processus sans attendre.

S'y ajoutent, inchangés, la confirmation explicite par case à cocher et le droit
`ExecuterUneOperationSensible` sur l'environnement — jamais le droit d'exécution ordinaire, un
arrêt forcé étant par nature plus dangereux qu'une action autorisée courante.

Le refus est posé **dans le moteur**, pas seulement dans la visibilité du bouton : un POST direct
sur `/operations/{id}/etapes/{etapeId}/forcer` se heurte à la même règle. L'écran applique la
même fonction de domaine, jamais une seconde formulation qui pourrait diverger.

### Contournement et intervention manuelle

Le contournement exige le droit `DemanderUnContournement`, un motif obligatoire, un contrôle
déclaré `Contournable` dans la version **validée** du workflow, et une approbation par un acteur
distinct du demandeur — via `SeparationDesResponsabilites.PeutApprouverUnContournement`, écrite
au Sprint 1. Cette dernière règle vaut dans tous les environnements (§2.3.2), pas seulement en
Production : sortir du cadre validé est un geste fort partout.

L'intervention manuelle exige le droit `AjouterObservationOuPreuve` et une preuve — jamais
facultative : c'est un opérateur qui atteste un effet que la vérification automatique n'a pas su
établir.

### Masquage des secrets avant persistance (SEC-003)

`ExecutionStep.Preuve` annonçait « secrets masqués avant persistance » depuis le Sprint 0 sans
que rien ne l'applique. `MasquageDesSecrets` masque désormais les affectations de mot de passe,
jeton, clé d'API et les en-têtes `Bearer`, dans la preuve **et** dans le message d'erreur, pour
les commandes automatiques comme pour les preuves saisies à la main — une preuve recopiée depuis
une console contient tout aussi bien un mot de passe.

Le masquage est fait avant l'écriture, pas seulement à l'affichage : un secret déjà persisté est
un secret divulgué, que l'écran le montre ou non. Le reste d'une chaîne de connexion — serveur,
base — reste lisible : tout masquer rendrait la preuve inexploitable pour comprendre un échec.

Portée assumée : les motifs d'affectation les plus courants, pas une détection exhaustive.

### Suivi de l'exécution (FR-021) et volet d'aide à la décision

Le Sprint 5 avait renvoyé le rafraîchissement temps réel à ce sprint — « afficher en direct une
exécution qui n'existe pas n'apporte rien ». `/operations/{id}` se recharge maintenant toutes les
cinq secondes, et seulement tant que l'exécution avance d'elle-même : une exécution close ou en
pause ne bouge que sur action. Le minuteur vit dans `js/interface.js`, la politique de contenu
n'autorisant aucun script en ligne sans jeton. Une saisie en cours n'est jamais interrompue —
recharger pendant qu'un opérateur motive un contournement effacerait sa justification.

La preuve collectée est affichée par étape, ce qu'aucun écran ne faisait encore.

Le volet d'aide à la décision s'ouvre dès qu'une étape ralentit ou bloque. Il n'agit pas et ne
recommande rien : il énonce l'état constaté, la preuve déjà collectée, et chaque option ouverte
ou fermée **avec son motif**. Une option indisponible reste listée — savoir pourquoi on ne peut
pas forcer un arrêt vaut mieux qu'un bouton absent sans explication.

### L'exécution avance sans qu'on la pousse

`ExecuteurDeWorkflow` fait avancer les exécutions engagées d'une étape éligible à la fois, toutes
les cinq secondes, et s'arrête de lui-même dès qu'une décision humaine manque. Même garantie que
le collecteur du Sprint 3 : une exécution en échec n'arrête jamais la boucle des autres —
l'exploitation perdrait le pilotage de tout le reste au moment précis où une opération se passe
mal. Chaque exécution avance dans sa propre portée, pour qu'un contexte EF Core corrompu ne
contamine pas les suivantes.

`AvancerAsync` reste exposé à la main depuis l'écran : c'est le même appel, déclenché
immédiatement, pas une seconde mécanique.

### Aucun nouvel état inventé

`ExecutionStatus` reste à dix valeurs et `StepStatus` à dix également. La machine à états du
Sprint 5 n'a pas été assouplie : une commande qui répond « réussie » ne conclut jamais
directement, elle passe par `Verification`, et un effet qu'on ne peut pas relire bloque au lieu
de devenir un succès silencieux.

L'engagement vérifie en revanche deux signaux que la machine à états ne connaît pas — elle ne
voit que les statuts : la confirmation explicite (`ConfirmeeLe`) et la complétion du circuit
d'approbation. Sans cette vérification, `EnPreparation → EnCours` serait autorisée par la seule
machine à états, quoi qu'ait fait l'opérateur.

## Exigences

Les intitulés reprennent le plan de sprints ; le cahier des charges fait foi.

| Référence | Objet | État |
|---|---|---|
| FR-021 | Suivi de l'exécution rafraîchi, informations sensibles masquées | Fait |
| FR-026 | Intervention manuelle avec preuve obligatoire | Fait |
| FR-027 | Contournement : rôle habilité, motif, approbation distincte | Fait |
| FR-029 | Séquence d'arrêt dans l'ordre de l'éditeur, vérifiée à l'activation | Fait |
| FR-029A | Composants déjà arrêtés ignorés proprement | Fait |
| FR-029B | Service bloqué : preuves collectées, arrêt forcé seulement après délai | Fait |
| AC-05 | Arrêt complet piloté d'un environnement UAT | **Non vérifiable** — pas d'UAT |
| AC-13 | Volet d'aide à la décision sur étape lente ou bloquée | Fait |
| AC-17 | Aucune escalade automatique : confirmation et autorisation exigées | Fait |

## Vérification

Suite automatisée : **246 tests, 0 échec** (212 domaine, 17 connecteurs, 10 application,
7 architecture) — 188 au terme du Sprint 6, soit 58 ajoutés. Ils couvrent `SequenceDArretDeReferenceN4` (ordre conforme, Center Node avant
Cluster Nodes refusé, types hors catalogue non contraints), `EvaluationDeCommande` (aucun
résultat brut ne conclut directement ; état non établi bloque ; cible déjà dans l'état visé),
`PolitiqueDeLancement`, `PolitiqueDEscalade` (avant délai, au délai, après délai, étape jamais
lancée, timeout absent ou négatif), `MasquageDesSecrets` et le connecteur de commande de
processus.

`N4Sentinel.Application.Tests`, resté vide depuis le Sprint 0, porte désormais le **parcours
d'exécution joué contre une vraie base SQL Server LocalDB**, migrations appliquées, base créée
et supprimée par test. Une base réelle et non un double en mémoire : le fournisseur en mémoire
accepte des requêtes que SQL Server refuse et ignore les contraintes de colonnes — c'est
exactement l'écart qui avait laissé passer le `DefaultIfEmpty` non traduisible du Sprint 6.

| Ce que le parcours vérifie | Résultat |
|---|---|
| Composant déjà arrêté : aucune commande émise, étape `Ignore` | Vérifié |
| Composant opérationnel : commande émise, étape conclue sur l'état **relu** | Vérifié |
| État réel non conforme à l'état visé : l'exécution échoue | Vérifié |
| Preuve persistée en base, secret masqué, reste du diagnostic lisible | Vérifié |
| Arrêt forcé refusé à 30 s d'un délai de 120 s, accepté à 130 s | Vérifié |
| Étape à confirmation : rien n'est émis avant la décision, tout l'est après | Vérifié |
| Contournement refusé si non déclaré, accepté et étape ignorée si déclaré | Vérifié |
| Intervention manuelle : étape conclue, preuve masquée | Vérifié |
| Verrou d'environnement : seconde exécution refusée (FR-015) | Vérifié |

Ces tests ont été éprouvés par mutation : le masquage des secrets retiré du moteur,
`La_preuve_est_persistee_avec_les_secrets_masques` échoue. Un test vert qui ne rougit jamais ne
prouve rien.

> **Ce que ce parcours ne couvre pas.** Il exerce le moteur et sa persistance, pas la couche
> HTTP : les points d'entrée du Sprint 7 — `engager`, `avancer`, `forcer`, `contournement`,
> `intervention` — n'ont été traversés ni par un navigateur ni par un test. Les contrôles
> d'habilitation par environnement et la séparation demandeur/approbateur qu'ils portent
> restent donc vérifiés par lecture, pas par exécution. `AC-07` en fait partie (voir plus bas).

Application lancée, environnement de développement, base LocalDB :

| Contrôle | Résultat constaté |
|---|---|
| Démarrage de l'application | Aucune erreur ; seul l'avertissement attendu sur l'absence de relais SMTP |
| Feuille de style servie | 203 règles analysées par le navigateur, contre 128 avant correction |
| Attributs `style` restants dans le DOM | 0 sur l'écran de connexion, 0 dans l'ensemble des gabarits |
| Classes de remplacement effectivement appliquées | `margin-top: 24px`, `text-align: center`, `#f59e0b` relus par `getComputedStyle` |
| `js/interface.js` chargé sous la politique de contenu | Oui, fonctions de bascule et de rafraîchissement présentes |

Le parcours métier lui-même n'a pas été rejoué depuis un navigateur, faute de session
authentifiée. Il est en revanche couvert de bout en bout par les tests d'intégration décrits
ci-dessous, qui exercent le moteur contre une vraie base — une couverture rejouable à chaque
génération, là où un clic manuel ne vaut que le jour où il est fait.

## Défauts trouvés par la vérification en conditions réelles

Le rappel du Sprint 3 se vérifie une fois de plus : lancer l'application trouve ce que ni le
compilateur ni les tests ne voient.

- **Toute la mise en forme en ligne de l'application était morte.** La politique de contenu est
  `style-src 'self'`, sans `'unsafe-inline'` : le navigateur analyse chaque attribut
  `style="…"` puis le **jette**. L'élément conserve son attribut dans le HTML — donc rien ne
  paraît anormal à la lecture du source — mais `element.style.length` vaut 0 et la déclaration
  n'est jamais appliquée. Quatre-vingts attributs répartis sur cinq écrans, dont quarante-quatre
  sur la console d'orchestration livrée par ce sprint, ne produisaient aucun effet. Aucun
  message côté serveur, aucun test en échec : seule la console du navigateur le signalait.
  Corrigé en déplaçant l'intégralité de la mise en forme dans `app.css`, sans toucher à la
  politique — l'ouvrir à `'unsafe-inline'` aurait réglé le symptôme en affaiblissant la
  protection que la classe `EntetesDeSecurite` documente explicitement.
- **Bloc CSS orphelin.** `app.css` portait, après la règle `.panneau-detail-noeud`, quatre
  déclarations sans sélecteur suivies d'une accolade fermante — reliquat d'une édition. Le
  navigateur les ignore en récupération d'erreur ; elles ne stylaient rien.
- **Règle CSS dupliquée.** `svg.graphe-dependances path.lien` était définie deux fois à
  l'identique. La seconde définition a été retirée.
- **Six `<text>` SVG dans des blocs `@if`.** Razor y voit sa propre balise de transition, qui
  n'admet aucun attribut (RZ1023) : le tableau de bord ne compilait plus. Corrigé en ouvrant
  l'élément hors du bloc de code, la condition passant à l'intérieur — le SVG rendu est
  identique.
- **Deux constructeurs d'enregistrement appelés avec des arguments manquants** dans le même
  fichier (`LigneDeSupervision`, `EtatDeSupervisionDuComposant`).
- **Le délai d'arrêt forcé se serait ouvert au bout d'une seconde** si la définition d'étape
  était introuvable : la valeur de repli `0` traversait `Math.Max(1, …)`. L'absence
  d'information n'autorise rien — le verdict est désormais fermé dans ce cas.

## Limites et écarts assumés

- **AC-05 n'est pas atteignable en l'état.** L'arrêt complet piloté d'un UAT représentatif était
  le livrable de revue. Ni les accès techniques N4 ni l'environnement UAT n'ont été ouverts par
  l'Infrastructure — condition préalable posée avant le Sprint 3 pour l'un, avant le Sprint 7
  pour l'autre. Les connecteurs de commande sont exerçables contre des services Windows et des
  processus du poste de développement ; **rien n'a été exécuté contre un composant N4 réel**.
- **Les clients ECN4Web/Billing et XPS ne sont pas séquencés.** Aucun type de composant ne les
  modélise ; la séquence de référence ne couvre que les sept rôles serveurs.
- **La `Condition` d'une étape n'est toujours pas évaluée.** Le champ existe depuis le Sprint 0.
  Il reste non consommé : l'évaluation conditionnelle n'était pas au périmètre de ce sprint.
- **Le parallélisme n'est pas exercé.** `AvancerAsync` fait avancer une étape à la fois, dans
  l'ordre. `PlanificateurDeParallelisme` existe mais n'est pas branché sur l'exécution réelle.
- **Le rafraîchissement est un rechargement de page, pas un flux.** Un vrai temps réel supposerait
  un mode de rendu interactif que le reste de l'application n'utilise pas.
- **Le masquage des secrets n'est pas exhaustif.** Il couvre les affectations les plus courantes.
  Il ne dispense pas de la garantie principale : le catalogue fermé d'actions (SEC-006) ne
  transporte aucun secret.
- **`ForcerLArretAsync` ne couvre que l'arrêt de service Windows.** Les autres actions n'ont pas
  de variante forcée au catalogue, et la demande est refusée explicitement plutôt que ignorée.
- **La couche HTTP n'est couverte par aucun test.** Les points d'entrée portent les contrôles
  d'habilitation par environnement (SEC-004) et la séparation des responsabilités ; ils ne sont
  aujourd'hui vérifiables qu'en parcourant l'application authentifié. Les couvrir demanderait
  un hôte de test ASP.NET Core, non introduit ici.
- **`AC-07` reste dû.** L'exigence du Sprint 1 — « action Production non approuvée : impossible
  et auditée » — était explicitement reportée à ce sprint, au motif que « le scénario complet
  sera rejouable quand les opérations existeront (S7) ». Les opérations existent ; le scénario
  n'a pas été rejoué et ne figurait dans aucune table d'exigences de ce sprint. Il relève de la
  couche HTTP ci-dessus.

## Sprint suivant

**Sprint 8 — Scénario de démarrage complet** (semaines 17–18). Le plan pose lui-même la
dépendance : « l'arrêt doit fonctionner avant de pouvoir démontrer un démarrage complet ». Or
l'arrêt n'a pas été démontré. Tant que l'UAT n'est pas ouvert, le Sprint 8 hériterait de la même
réserve et livrerait, comme celui-ci, du code non recetté.
