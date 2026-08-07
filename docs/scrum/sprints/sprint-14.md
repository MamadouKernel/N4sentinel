# Sprint 14 — Assistant documentaire N4

**Objectif de sprint** : offrir un assistant interrogeable en langage naturel sur le guide Navis N4 et les
procédures internes, avec réponses toujours sourcées (E9.1), et garantir structurellement que cet assistant ne
peut jamais déclencher d'action technique (E9.2).

Comme pour les Sprints 9-13, les lignes E9.1/E9.2 du backlog ne portaient aucune référence `FR-xxx` — le texte
complet du cahier des charges a été relu pour ce sprint. Références retenues, section "Base documentaire et
assistant N4" (FR-080 à FR-087, immédiatement avant les FR-088/089 de l'Epic 9.3 SOP, hors périmètre) :

- **FR-080** (corpus documentaire : guide Navis N4 3.8.25, procédures internes validées DSI, post-mortems,
  rapports d'analyse, notes de version Navis, fiches réflexes — chaque document versionné, daté et validé
  avant indexation), **FR-081** (recherche plein texte par symptôme/composant/erreur/opération), **FR-082**
  (question en langage naturel, réponse synthétique basée sur le corpus), **FR-083** (chaque réponse cite le
  document, la section et la page/emplacement), **FR-084** (l'assistant explique et propose, ne déclenche
  jamais une action technique directement), **FR-085** (en l'absence de source suffisante, le signaler et
  recommander une vérification/escalade), **FR-086** (documents versionnés, datés, associés aux versions N4,
  validés avant publication), **FR-087** (signalement d'une réponse incorrecte avec correction proposée,
  soumis à validation) → **E9.1/E9.2**.
- **Hors périmètre de ce sprint** : **FR-088/FR-089-série** (réponse au format SOP complet pour les questions
  opérationnelles, génération de SOP après résolution, réutilisation avec historique) — appartiennent
  explicitement à E9.3 (Sprint 15), qui suppose l'entité SOP versionnée qui n'existe pas encore.

## Sprint Backlog

| Story | Résultat |
|---|---|
| E9.1 — Assistant N4 : recherche, question-réponse, réponses sourcées | Fait |
| E9.2 — Garde-fou : l'assistant ne déclenche jamais d'action technique | Fait |

## Décisions de conception

- **`Document` reprend l'entité "Document" du modèle de données minimal** (titre, version, version N4,
  statut, source et indexation) et le vocabulaire des 6 catégories de FR-080. Versionnement par nouvelle ligne
  partageant le même `DocumentKey` — exactement le même raisonnement que `DiagnosticRule` (Sprint 12) : le
  contenu tient dans des champs scalaires, un découpage parent/enfant serait disproportionné. Réutilise le même
  cycle de validation générique (FR-006/086) : seul un document Actif est indexé (`Document.IsIndexed`).
- **Pas de RAG/embeddings/moteur d'IA générative.** Le cahier des charges ne décrit aucune architecture
  technique précise pour la question-réponse ("un moteur de recherche/indexation documentaire et, si validé, un
  service de question-réponse augmenté par les sources" — explicitement conditionnel). Cette application n'a
  aucune infrastructure IA/LLM ; construire une fausse intelligence artificielle aurait été le même type de
  raccourci que les précédentes décisions de ce projet ont toujours refusé (connecteurs Simulation qui ne
  prétendent jamais agir réellement, moteur de diagnostic qui ne prétend jamais un score de confiance calculé
  sans preuve). `AskAssistantQuery` (FR-082) implémente une recherche de pertinence par mots-clés déterministe
  et réelle sur le contenu effectivement indexé : chaque ligne d'un document Actif est notée par le nombre de
  mots-clés de la question qu'elle contient, les meilleures lignes sont retournées comme sources — un
  comportement honnête ("voici ce que le corpus contient réellement"), pas une hallucination.
- **FR-083 (citer document, section, page/emplacement) satisfait par citation au niveau de la ligne**, en
  réutilisant directement le mécanisme de recherche ligne-par-ligne déjà construit pour `ImportedLogFile`
  (Sprint 13, FR-076/077) : chaque source cite le document (`DocumentKey`, titre, version) et le numéro de
  ligne concordante, avec l'extrait. C'est la forme la plus fine de localisation possible sans moteur de
  découpage en sections structurées (qui n'existe pas) — cohérent avec le choix déjà fait de ne pas construire
  d'analyseur de format de document dédié.
- **FR-084 (garde-fou) appliqué structurellement, pas par une simple règle métier.**
  `AskAssistantQueryHandler` ne dépend que d'`IDocumentRepository` en lecture — aucune dépendance à
  `IUnitOfWork` ni à aucun repository capable de muter l'état applicatif. La réponse est un DTO immuable
  composé uniquement d'extraits de documents : elle ne référence, ne contient et ne peut déclencher aucune
  commande. Un test unitaire dédié (`Handler_HasNoMutatingDependency_StructuralGuardRailFR084`) vérifie par
  réflexion que le constructeur du handler ne prend aucune dépendance mutante — le garde-fou est vérifié au
  niveau du type, pas seulement au niveau du comportement observé.
- **FR-085 (absence de source suffisante) : deux cas distincts, tous deux honnêtes.** Si la question ne
  contient aucun terme exploitable (moins de 3 caractères alphanumériques), l'assistant le signale sans même
  interroger le corpus. Si aucune ligne indexée ne correspond, l'assistant recommande une vérification manuelle
  ou une escalade vers l'équipe Infrastructure/support Navis — jamais une réponse présentée comme certaine sans
  preuve, cohérent avec la décision déjà prise pour FR-064 (absence de conclusion fiable, Sprint 13).
- **`AssistantFeedback` (FR-087) n'existe pas dans le modèle de données minimal** (qui ne liste que
  "Document"), mais FR-087 exige explicitement de tracer le signalement et sa validation — même raisonnement
  que `ReconstitutionStepRecord` (Sprint 11) : nécessaire à la traçabilité exigée par le texte sans être une
  entité nommée du modèle minimal. **Valider un signalement n'applique jamais automatiquement la correction au
  contenu du document** — cela reste une action distincte (nouvelle version du document + relecture),
  cohérent avec la discipline de version déjà appliquée partout ailleurs.
- **Pas de découpage par environnement.** `Document` et `AssistantFeedback` n'ont pas de champ
  `EnvironmentId` : le modèle de données minimal ne le prévoit pas, et l'écran "Assistant N4" du cahier des
  charges est décrit comme un écran global, pas un écran par environnement — cohérent avec le regroupement de
  la base documentaire sous l'administration générale de la solution (`/admin/documents`), au même titre que
  les règles de diagnostic (Sprint 12).

## Vérification

219 tests unitaires verts (132 Domain + 87 Application), incluant `Document` (cycle de vie, versionnement),
`AssistantFeedback` (signalement/validation/rejet), `AskAssistantQueryHandler` (réponse sourcée, absence de
source suffisante, **et le test structurel du garde-fou FR-084**), `SearchDocumentsQueryHandler` (FR-081) et
les handlers CQRS de gestion documentaire et de signalement.

**Vérification navigateur non concluante, comme au Sprint 13** : la même régression d'environnement (rendu
interactif Blazor Server affichant "Not Found" sur `/Account/Login`) a été retestée après un redémarrage
complet du serveur de développement — elle persiste, confirmant qu'il s'agit toujours d'un problème
d'environnement de session et non d'une régression du code de ce sprint (déjà isolé et documenté au Sprint 13
par comparaison avec le commit Sprint 12 précédemment vérifié avec succès). La vérification fonctionnelle de
ce sprint s'appuie donc sur la suite de tests automatisés ; une vérification navigateur complète (parcours
question → réponse sourcée → signalement, et cycle de vie complet d'un document) est à refaire dans un
environnement de développement sain.
