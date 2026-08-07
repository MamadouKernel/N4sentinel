# Sprint 16 — Clôture des écarts d'audit et de conformité

**Contexte** : ce sprint fait suite à un audit technique et un test d'intrusion complets menés sur la V1
(`docs/audit/audit-securite-2026-08-07.md`), puis à la question directe du Product Owner "est-ce que le cahier
des charges est respecté à 100%". La réponse honnête à cette date était non — quatre écarts fonctionnels
documentés dans `product-backlog.md` depuis les Sprints 8-9 (E10.1, E11.1, E11.1b, E11.2) et deux exigences
explicitement hors périmètre du plan (FR-066, FR-067) plus un format d'export partiel (FR-090, JSON seul).
Objectif de ce sprint : combler chacun de ces écarts qui relève réellement du code, et documenter honnêtement
celui qui ne le peut pas.

**Ce qui reste hors d'atteinte, quel que soit le code écrit** : la validation contre un vrai cluster N4/Navis
réel. Aucun accès réseau aux serveurs CIT n'a été fourni pour cette session, et le principe "pas d'automatisation
simulée" tenu depuis le Sprint 2 interdit de fabriquer un faux connecteur réel pour combler cet écart
artificiellement. Ceci reste un écart assumé, non traité par ce sprint.

## Sprint Backlog

| Story | Résultat |
|---|---|
| E10.1 — étendre l'audit au référentiel et au verrouillage de compte | Fait |
| E11.2 — généraliser le "contournement" au-delà des étapes RequiresApproval (FR-027) | Fait |
| FR-066 — comparaison avec une référence | Fait |
| FR-067 — paquet d'escalade | Fait |
| FR-090 — export PDF réel | Fait |
| E11.1 / E11.1b — rôles différenciés par environnement | Fait, périmètre du mécanisme complet + application partielle — voir décision |

## Décisions de conception

### E10.1 — audit complet
Onze commandes de mutation du référentiel (Environment×3, Component×2, Workflow×6) ainsi que
`LockUserAccountCommand`/`UnlockUserAccountCommand` reçoivent désormais un `ActorUserId` et implémentent
`IAuditableRequest` — même mécanisme générique que toutes les commandes déjà auditées depuis le Sprint 9, aucun
nouveau comportement d'infrastructure. Toutes les pages appelantes (11 fichiers Razor) ont été mises à jour pour
relayer l'identité de l'utilisateur courant via `AuthenticationStateProvider`, cohérent avec le pattern déjà
utilisé partout ailleurs dans l'application.

### E11.2 — contournement contrôlé (FR-027)
Le cahier des charges (texte relu intégralement pour ce sprint) exige : rôle habilité, motif obligatoire,
identification du risque accepté, confirmation explicite, et — en Production — l'approbation prévue par la
matrice de criticité ; un contrôle non déclaré contournable ne peut jamais être ignoré.

Point d'intégration trouvé : `WorkflowStepFailurePolicy.RequireManualDecision` existait déjà dans le modèle
depuis le Sprint 2 mais n'avait jamais reçu de comportement distinct — en pratique, tout échec d'étape
(quelle que soit sa politique, hors `ContinueWithWarning`) faisait simplement échouer l'opération, la seule
option étant `Resume()` (retenter la même étape). C'est exactement l'écart entre "contrôle déclaré
contournable" (`RequireManualDecision`) et "non contournable" (`StopWorkflow`) que FR-027 présuppose.

`OperationRun.OverrideFailedStep(...)` (nouvelle méthode, miroir de `Resume()`) contourne l'étape en échec :
motif et risque accepté obligatoires, contrôle vérifié comme réellement déclaré contournable (sinon
`DomainRuleException`), et — si `IsProductionEnvironment` (nouveau champ stocké sur `OperationRun`, dérivé du
paramètre déjà existant mais jamais persisté) — un second utilisateur distinct doit approuver. Auditée
(`OverrideFailedOperationStepCommand` implémente `IAuditableRequest`) et visible dans le rapport d'opération
(Sprint 15) — les deux exigences explicites de FR-027 ("doit être visible dans le rapport final et le journal
d'audit").

### FR-066 — comparaison avec une référence
Quatre types de référence prévus par le texte : période saine validée, exécution précédente réussie, valeurs
habituelles du même composant, autre nœud comparable. Les deux derniers partagent la même mécanique
(`ComparisonReferenceKind.ComponentSignalHistory`) — comparer les signaux déjà collectés pour un composant
choisi, que ce soit le même composant historiquement ou un autre nœud — plutôt que dupliquer la logique pour un
cas qui ne diffère que par le composant sélectionné par l'utilisateur.

Nouvelle entité `HealthyReferencePeriod` (créée directement à l'état validé par un Administrateur — pas de
cycle Draft/Validation complet, une période de référence est un simple constat, pas une configuration
versionnée). `CompareDiagnosticCaseToReferenceQuery` assemble deux listes de signaux côte à côte (le cas, la
référence) sans prétendre calculer un diagnostic différentiel automatique — l'interprétation reste humaine,
cohérent avec le principe "pas d'automatisation simulée". Une référence de plus de 90 jours ou sans aucun
signal déclenche l'avertissement explicitement exigé par le texte ("une référence ancienne ou incomplète ne
doit pas être utilisée sans avertissement").

### FR-067 — paquet d'escalade
Assemblé depuis les données déjà existantes (`DiagnosticCase`, `ImportedLogFile`) — aucune nouvelle entité
"paquet" persistée, même raisonnement que les rapports du Sprint 15. Le masquage des secrets exigé par le texte
("les secrets... doivent être masqués ou exclus avant la génération du paquet") est satisfait sans nouvelle
couche : les journaux inclus sont ceux déjà expurgés au moment de leur import (FR-076/077, Sprint 13) — le
paquet ne fait que citer un contenu déjà propre, jamais le contenu brut original. La liste et l'empreinte des
fichiers inclus réutilisent le hash SHA-256 déjà calculé à l'import.

### FR-090 — export PDF réel
Le Sprint 15 avait explicitement différé l'export PDF/Word binaire pour éviter une nouvelle dépendance en toute
fin de plan V1, ne livrant que l'export JSON structuré (une des deux options autorisées par FR-090 : "PDF, Word
ou format structuré selon le besoin"). Ce sprint ajoute QuestPDF (licence Community, gratuite pour une entité de
moins d'1M$ de chiffre d'affaires annuel — le cas d'un outil interne CIT non commercialisé) et génère un PDF
réel pour les deux rapports du Sprint 15. **Vérifié avec des données réelles** : le rapport d'opération d'une
opération de démarrage complet effectivement exécutée pendant cette session a été téléchargé et inspecté page
par page — rendu correct, chronologie complète, tableau lisible. Word (.docx) reste non couvert ; le texte du
cahier des charges n'impose qu'un des trois formats, pas les trois.

### E11.1 / E11.1b — rôles différenciés par environnement
**Le plus gros morceau de ce sprint, et le seul livré à périmètre partiel assumé.** Le mécanisme complet est
construit et testé : nouvelle entité `UserEnvironmentRole` (utilisateur, environnement, rôle, qui a attribué,
quand), un service `IEnvironmentAccessChecker` qui reste **strictement additif** — un utilisateur a accès si
son rôle global (Identity, existant depuis le Sprint 8) suffit encore aujourd'hui, OU s'il porte une attribution
`UserEnvironmentRole` pour ce rôle sur cet environnement précis. Décision de cadrage déterminante : ne jamais
retirer un droit déjà accordé par le modèle global, pour ne régresser aucune des autorisations déjà en place
sur le reste des ~90 tâches déjà livrées de la V1. Un Administrateur global garde un accès total à tout
environnement — un rôle de confiance système, pas un rôle métier scopé, cohérent avec le seul exemple donné par
le cahier des charges pour E11.1b ("Opérateur sur UAT, Lecteur sur Production" — qui ne porte que sur
Opérateur/Lecteur).

Une page d'administration (`/environments/{id}/roles`) permet d'attribuer/révoquer ces rôles.

**Application concrète du contrôle, page par page — périmètre assumé** : Blazor Server ne permet pas de
vérifier un rôle scopé à un paramètre de route via le seul attribut `[Authorize(Roles=...)]` (qui s'évalue
avant la liaison des paramètres) — chaque page environnement-scopée doit donc appeler explicitement
`IEnvironmentAccessChecker` dans `OnParametersSetAsync`. Ce remplacement a été fait sur les 5 pages qui
correspondent exactement à l'exemple donné par le cahier des charges — le référentiel et le pilotage réel
(`EnvironmentEdit`, `ComponentCreate`, `ComponentEdit`, `WorkflowCreate`, `OperationRunCreate`). **Les autres
pages environnement-scopées (étapes de workflow, systèmes dépendants, supervision dossiers partagés, EDI,
diagnostic) continuent de reposer sur le seul modèle de rôle global** — elles ne régressent rien (le modèle
global reste pleinement fonctionnel), mais n'offrent pas encore la granularité par environnement. Étendre le
même remplacement mécanique (`[Authorize(Roles=...)]` → `[Authorize]` + garde `IEnvironmentAccessChecker` dans
`OnParametersSetAsync`) aux pages restantes est un travail répétitif à faible risque, candidat naturel pour un
sprint de suite dédié plutôt qu'une extension mécanique non vérifiée en fin de session déjà longue.

## Vérification

306 tests unitaires verts (171 Domain + 135 Application), incluant `UserEnvironmentRole`,
`OperationRun.OverrideFailedStep` (contrôle non contournable, motif/risque manquants, Production sans
approbateur distinct, succès), `HealthyReferencePeriod`, `CompareDiagnosticCaseToReferenceQuery` (référence
récente/complète, ancienne/incomplète, exclusion des propres signaux du cas), `GetEscalationPackageQuery`
(masquage hérité, composants distincts), et les handlers CQRS de gestion des rôles par environnement.

Export PDF vérifié end-to-end avec des données réelles (voir décision FR-090 ci-dessus) — seul point de ce
sprint validé au-delà des tests unitaires, faute d'environnement navigateur interactif sain (même régression
SignalR documentée depuis le Sprint 13).
