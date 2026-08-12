# Sprint 6 — Préparation d'une opération et mode simulation

**Semaines 13–14 · Lot 1 · Statut : livré**

**Objectif** — tout ce qui précède la première commande réelle : simuler, contrôler, justifier,
faire approuver.

**Livrable démontrable en revue** — simulation d'un redémarrage complet, sans action réelle.

---

## Ce qui a été livré

### Saisie des workflows, préalable non listé mais nécessaire

Aucun scénario n'était saisissable avant ce sprint : les entités `Workflow` / `WorkflowVersion` /
`WorkflowStepDefinition` existaient depuis le Sprint 0 sans écran ni point d'entrée. `/pilotage/
workflows` et `/pilotage/workflow/{id}` les rendent saisissables — en-tête, versions, étapes —
en réutilisant tel quel le cycle de validation générique du Sprint 2
(`CycleDeValidation`, déjà appliqué à `ValidationStatus`, dont le statut de `WorkflowVersion` est
une instance). Toute modification de contenu est refusée dès qu'une version quitte le brouillon :
une nouvelle version, jamais une réédition.

### Mode simulation (FR-005)

`EvaluateurDePreChecks`, dans `N4Sentinel.Domain.Operations`, établit pour chaque étape d'un
scénario l'un des cinq statuts du plan — Satisfait, Avertissement, Bloquant, Non applicable,
Impossible à vérifier — à partir du seul état déjà collecté par la supervision. Aucune collecte
n'est jamais déclenchée depuis la préparation : `IServiceDeSupervision.LireAsync` est appelé,
jamais `CollecterAsync`.

Le verdict de chaque étape est **persisté**, pas recalculé à chaque affichage : un nouveau champ
`ExecutionStep.StatutDuPreCheck` porte le statut exact, `Preuve` porte le motif. C'est la pièce du
dossier annoncée par le plan — « son résultat est conservé et rattaché à la demande d'opération,
ce n'est pas un aperçu jetable ». Seul un pré-check Bloquant fait aussi passer `ExecutionStep.
Statut` à `Bloque` : les autres verdicts laissent l'étape `AVenir`, la nuance restant dans le
pré-check plutôt que dans l'état d'avancement réel, qui appartient au Sprint 7.

Risques et prérequis non satisfaits, à l'inverse, sont recalculés à chaque consultation de
l'aperçu — même traitement que l'analyse d'impact du référentiel (Sprint 2), jamais figés.

### Sélection d'un scénario compatible avec les habilitations (FR-010)

`/operations/nouvelle` ne propose que les versions de workflow **Actives**, rattachées à
l'environnement courant, et pour lesquelles l'utilisateur détient le droit requis —
`ExecuterUneOperationSensible` si la version est marquée sensible, `ExecuterUneOperationAutorisee`
sinon. Le filtrage est vérifié côté serveur, pas seulement caché côté écran.

### Champs obligatoires en Production (FR-014)

`ValidateurDeDemande` : le motif seul est exigé hors Production ; en Production, référence
d'incident, fenêtre d'intervention, périmètre et impact attendu s'y ajoutent, chacun signalé
indépendamment s'il manque.

### Circuit d'approbation simple ou double (FR-013)

`TypeDeCircuitDApprobation` se fixe avec la version de workflow, jamais au moment de
l'approbation. `EvaluateurDeCircuit` compte les approbateurs distincts déjà enregistrés
(`ExecutionApproval`, une ligne par décision) ; chaque tentative individuelle passe d'abord par
`SeparationDesResponsabilites.PeutApprouverUneOperation`, déjà écrite et testée au Sprint 1 —
aucune règle de séparation n'a été redupliquée ici.

C'est le premier endroit de l'application où `IServiceDHabilitations` gate réellement une
écriture par environnement, plutôt que de simplement l'afficher : `/operations/preparer` et
`/operations/{id}/approuver` vérifient le droit sur l'environnement visé, indépendamment de la
politique d'autorisation globale posée sur le groupe de routes.

### Écran d'avertissement final et confirmation explicite (FR-011, AC-01)

`/operations/{id}/apercu` affiche pré-checks, risques, prérequis non satisfaits et circuit
d'approbation, puis exige une case cochée avant de soumettre. Sans elle, la soumission est
refusée et tracée — vérifié en conditions réelles, voir plus bas.

### Aucun nouvel état inventé

`ExecutionStatus` reste à dix valeurs. `EnPreparation` sans circuit configuré reste
`EnPreparation` — « le plan est en cours de constitution, rien n'est engagé » restait déjà vrai.
Un circuit configuré fait passer l'exécution à `EnAttenteDApprobation`, transition déjà autorisée
par `MachineAEtats` depuis le Sprint 5. Une fois le circuit satisfait, l'exécution y reste :
c'est `DemarrerAsync` (Sprint 7) qui l'engagera réellement.

## Exigences

| Référence | Objet | État |
|---|---|---|
| FR-005 | Mode simulation, sans commande | Fait |
| FR-010 | Sélection de scénario compatible avec les habilitations | Fait |
| FR-011 | Confirmation explicite avant soumission | Fait |
| FR-012 | Pré-check automatique à cinq statuts | Fait |
| FR-013 | Circuit d'approbation simple ou double, approbateurs distincts | Fait |
| FR-014 | Champs obligatoires en Production | Fait |
| AC-01 | Écran d'avertissement final | Fait |

## Vérification

Suite automatisée : **188 tests, 0 échec** (169 domaine, 12 connecteurs, 7 architecture). Les 27
tests ajoutés couvrent `EvaluateurDePreChecks` (les cinq statuts, y compris le cas des workflows
sans direction unique), `EvaluateurDeCircuit` (Aucun/Simple/Double, doublons ignorés) et
`ValidateurDeDemande` (motif seul hors Production, cinq champs indépendants en Production).

Parcours rejoué sur l'application réellement lancée, environnement UAT, base LocalDB :

| Étape | Résultat constaté |
|---|---|
| Composant `Center Node UAT`, cycle Brouillon → Actif | Chaque transition appliquée, « Utilisable pour une opération » passe à Oui |
| Workflow `DemarrageComplet`, version v1, une étape ciblant ce composant | Version Brouillon → À valider → Validé → Actif |
| `/operations/nouvelle` sur UAT | Seul le scénario actif et habilité apparaît, motif seul requis |
| Simulation lancée | Aperçu généré : pré-check **Impossible à vérifier** (composant jamais collecté), aucun relevé de supervision créé |
| Soumission sans cocher la confirmation | Refusée et tracée, l'exécution reste `EnPreparation` |
| Soumission confirmée, circuit Simple | Transition vers `EnAttenteDApprobation` |
| Approbation par le demandeur lui-même (UAT, circuit Simple, hors Production) | Acceptée — la séparation stricte ne s'applique qu'en Production ou en circuit Double |
| Seconde tentative d'approbation par le même acteur | Refusée, tracée, registre inchangé |

## Défauts trouvés par la vérification en conditions réelles

Quatre défauts n'étaient pas visibles à la compilation ni dans la suite automatisée — le rappel
du Sprint 3 reste vrai : rejouer le parcours sur l'application réellement lancée trouve ce que les
tests unitaires ne voient pas.

- **Requête non traduisible.** Le calcul du prochain numéro de version utilisait
  `Select(...).DefaultIfEmpty(0).MaxAsync()` : EF Core ne sait pas traduire un `DefaultIfEmpty`
  paramétré à cet endroit. Corrigé en `Select(v => (int?)v.NumeroDeVersion).MaxAsync() ?? 0`.
- **Liaison de formulaire sur champ optionnel de type valeur.** Un `<input type="datetime-local">`
  ou un `<select>` laissé sur son option vide soumet une chaîne vide, jamais un champ absent :
  `DateTimeOffset?` et `Guid?` échouent à la liaison sur une chaîne vide, alors qu'ils
  acceptent l'absence. Corrigé en acceptant `string?` et en analysant la valeur côté serveur,
  pour les fenêtres d'intervention et le composant cible d'une étape. Une case à cocher non
  cochée est, elle, absente du formulaire : `confirmation` a reçu une valeur par défaut.
- **Ajout via une collection de navigation déjà suivie.** `execution.Approbations.Add(...)` sur
  une exécution chargée par requête (donc déjà suivie par le contexte) a produit un `UPDATE` au
  lieu d'un `INSERT` — Entity Framework Core interprète l'ajout comme une modification quand la
  clé de la nouvelle ligne est déjà renseignée à la construction (GUID v7 généré côté client).
  L'écriture échouait avec `DbUpdateConcurrencyException`. Corrigé en revenant au patron déjà
  suivi ailleurs dans l'application : `contexte.Add(nouvelleApprobation)` plutôt que l'ajout à la
  collection du parent.
- **Portée d'un `AuthorizeView` basé sur les rôles globaux.** Le formulaire d'approbation était
  gardé par `<AuthorizeView Policy="Droit:Approuver">`, qui ne connaît que les rôles Identity
  globaux. Un droit gagné par habilitation d'environnement — la voie normale pour `Approuver`,
  volontairement différenciée par SEC-004 — restait invisible. Corrigé en calculant la visibilité
  via `IServiceDHabilitations.AutoriseAsync` sur l'environnement de l'exécution, à l'image de ce
  que fait déjà `Home.razor` pour l'affichage des droits effectifs.

## Limites et écarts assumés

- **Aucune commande réelle.** `IMoteurDOrchestration.DemarrerAsync` n'est appelé nulle part dans
  ce sprint. L'engagement d'une exécution approuvée reste au Sprint 7.
- **La `Condition` d'une étape n'est pas évaluée.** Le champ existe depuis le Sprint 0, se saisit
  et s'affiche ; son évaluation est un mécanisme d'exécution, pas de préparation.
- **Le parallélisme n'est pas prévisualisé.** `PlanificateurDeParallelisme` s'applique à
  l'exécution réelle, pas à l'aperçu de simulation.
- **Pas de rafraîchissement temps réel.** Comme le reste de l'application, les écrans se
  rechargent par formulaire POST classique.
- **Le chemin FR-014 n'a été rejoué manuellement que côté message d'erreur, pas de bout en
  bout en Production** — la logique elle-même est couverte par neuf tests unitaires
  (`ValidateurDeDemandeTests`), identiques quel que soit l'environnement appelant.

## Sprint suivant

**Sprint 7 — Exécution réelle et scénario d'arrêt complet** (semaines 15–16), la première action
réelle du plan. Il reste bloqué tant que les accès techniques N4 et un environnement UAT
représentatif ne sont pas ouverts par l'Infrastructure — la réserve posée depuis le Sprint 0 n'a
pas changé.
