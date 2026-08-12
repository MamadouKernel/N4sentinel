# Sprint 1 — Identités, profils et journal d'audit

**Semaines 3–4 · Lot 1 · Statut : livré**

**Objectif** — rendre impossible toute action non authentifiée, non autorisée ou non tracée,
avant même qu'une action existe.

**Livrable démontrable en revue** — connexion avec second facteur, huit profils, tentative non
autorisée tracée.

---

## Ce qui a été livré

### Authentification avec second facteur par courriel (SEC-001)

Parcours complet : identifiants → code à six chiffres envoyé par courriel, valable cinq minutes
→ session. Le second facteur est posé à la création du compte, il ne s'active pas au choix de
l'utilisateur.

Les formulaires sont rendus en SSR statique et postés vers des points d'entrée HTTP classiques :
un circuit interactif Blazor ne peut pas écrire de cookie d'authentification.

Mots de passe : 12 caractères minimum, majuscule, minuscule, chiffre et caractère spécial.
Verrouillage après 5 échecs pendant 15 minutes.

**Canal du second facteur au choix de l'utilisateur** (demande DSI en cours de sprint). Deux
canaux dans `/compte/profil` : code par courriel, ou application d'authentification (TOTP).
Le second facteur lui-même reste obligatoire — SEC-001 l'impose, et aucun écran ne permet de
le désactiver. La seule question posée à l'utilisateur est « par où », jamais « faut-il ».

Trois garde-fous sur ce choix :

- **La bascule vers l'application exige un code valide.** Une clé mal recopiée ne peut donc pas
  enfermer l'utilisateur dehors : tant que l'application n'a pas prouvé qu'elle fonctionne, le
  canal reste le courriel.
- **Une régénération de clé** — téléphone perdu ou remplacé — remet immédiatement le canal sur
  le courriel, sans quoi l'utilisateur ne pourrait plus se connecter pour reconfigurer.
- **Chaque changement de canal est audité**, y compris les tentatives refusées pour code invalide.

L'application d'authentification a un intérêt propre sur un outil d'exploitation : elle génère
le code hors ligne. La connexion reste donc possible quand la messagerie CIT est indisponible —
c'est-à-dire, sur un outil de supervision, précisément quand on en a le plus besoin.

Le message d'erreur affiché ne distingue jamais un compte inexistant d'un mot de passe erroné.
Le motif exact, lui, est écrit au journal d'audit.

### Les huit profils du §2.3.2 (SEC-002)

Du Lecteur / Support N1 au TOS Manager — Consultation. Chaque profil est traduit en droits
élémentaires (`DroitsParProfil`), et c'est le droit, jamais le profil, qui est vérifié à
l'exécution. Les cinq familles du cahier des charges — lecture, diagnostic, exécution,
approbation, administration — sont attribuables séparément.

Deux points tenus littéralement :

- l'**Auditeur** ne dispose d'aucun droit d'action, ce qu'un test vérifie règle par règle ;
- l'**import de logs** n'est pas accordé d'office au Lecteur : « uniquement si cette
  autorisation lui est attribuée ».

### Droits différenciés par environnement (SEC-004)

`HabilitationEnvironnement` s'ajoute aux profils globaux sans les remplacer. La règle de
résolution traduit deux phrases du §2.3.2 — « les membres de l'équipe de développement ne
disposent pas automatiquement de droits d'action en Production » et « les droits doivent
pouvoir être différenciés par environnement » :

> **En Production, un profil global n'accorde que la consultation. Toute permission d'action y
> exige une habilitation explicite sur cet environnement.** Hors Production, profils globaux et
> habilitations s'additionnent.

Le choix sépare voir et agir plutôt que de tout bloquer : un auditeur garde sa vue sur la
Production sans qu'un opérateur y hérite d'un droit d'exécution.

L'environnement courant est affiché en permanence dans le bandeau, avec une pastille rouge pour
la Production.

### Journal d'audit horodaté et non modifiable (FR-091, FR-092)

Un intercepteur EF Core fait échouer tout enregistrement qui tenterait de modifier ou de
supprimer une entrée d'audit, d'où que vienne la tentative. L'horodatage est posé par la couche
de persistance, pas par l'appelant : personne ne peut antidater une entrée.

Sont tracés : connexion réussie, connexion refusée, demande de second facteur, second facteur
refusé, compte verrouillé, déconnexion, accès refusé, création de compte, attribution et
révocation de profil ou d'habilitation.

**Limite assumée** : l'intercepteur protège de l'erreur de code, pas d'un accès direct à la
base. La révocation des droits UPDATE et DELETE sur la table `JournalDAudit` pour le compte
applicatif reste à poser par l'Infrastructure CIT.

### Traçage des échecs d'autorisation (SEC-008, AC-07)

Une règle de repli exige une authentification sur tout point d'entrée qui ne déclare rien :
l'oubli d'un attribut ne peut donc pas ouvrir un écran. Les refus sont écrits au journal avec le
motif et la ressource demandée.

### Règles de séparation des responsabilités

Elles sont dans le domaine, pas dans une page — une règle de séparation qui ne vit que dans
l'interface n'en est pas une :

- le demandeur d'une opération ne peut pas l'approuver lui-même en Production, ni dès que la
  double validation est requise ;
- un Administrateur N4 ne peut pas approuver son propre contournement, quel que soit
  l'environnement ;
- toute attribution ou révocation de rôle exige le droit correspondant, et est auditée.

Une révocation d'habilitation est horodatée, jamais supprimée : l'historique des droits doit
rester lisible après coup.

## Exigences soldées

| Référence | Objet | État |
|---|---|---|
| SEC-001 | Authentification applicative avec MFA par e-mail | Fait |
| SEC-002 | Moindre privilège, droits attribués séparément | Fait |
| SEC-004 | Séparation des environnements, environnement visible | Fait |
| SEC-008 | Audit des accès et des échecs d'autorisation | Fait |
| FR-091 | Journal d'audit — connexions, droits, configuration | Connexions et droits faits ; workflows, règles et documents à mesure qu'ils existent |
| FR-092 | Intégrité et horodatage du journal | Fait côté applicatif ; droits base à poser par l'Infrastructure |
| AC-07 | Action Production non approuvée : impossible et auditée | Règle et audit en place ; le scénario complet sera rejouable quand les opérations existeront (S7) |

## Vérification

Suite automatisée : **45 tests, 0 échec** (38 domaine, 7 architecture).

Parcours vérifié sur l'application réellement lancée, requête par requête :

| Étape | Résultat constaté |
|---|---|
| `GET /` sans session | 302 vers `/compte/connexion?ReturnUrl=%2F` |
| Mot de passe erroné | 302 vers `?erreur=identifiants`, refus tracé |
| Mot de passe correct | 302 vers `/compte/double-facteur`, code envoyé |
| Code erroné | 302 vers `?erreur=code`, refus tracé |
| Code correct | 302 vers `/`, session ouverte |
| `GET /` authentifié | 200 — droits effectifs affichés |
| Journal d'audit | 200 — succès et refus présents avec leur motif |
| Droits sur Production | Consultation seulement, pour un Administrateur de la solution global |
| Bascule vers UAT | Les droits d'administration apparaissent |
| Bascule vers l'application d'authentification, code erroné | Refusée, canal inchangé, refus tracé |
| Bascule avec un code TOTP valide | Canal modifié, changement tracé |
| Reconnexion, canal « application » | Le libellé change, **aucun courriel n'est émis**, la connexion aboutit |

La dernière ligne est la démonstration de SEC-004 : le même compte, le même instant, deux
environnements, deux jeux de droits.

### Thème clair ou sombre, au choix

Bascule disponible sur tous les écrans, y compris avant connexion. Le choix est retenu dans un
cookie et **appliqué au rendu côté serveur** : la page arrive dans la bonne couleur, sans le
clignotement d'un basculement fait après coup en JavaScript.

Les valeurs du thème clair sont celles de `body.theme-light` dans la maquette de référence.
Un jeton supplémentaire — la couleur du texte posé sur un aplat primaire — a dû être introduit :
sans lui, le libellé des boutons devenait illisible en clair.

La bascule est un bouton d'icône — soleil ou lune selon le thème proposé —, avec `aria-label`
et infobulle : une icône sans nom accessible n'est pas un contrôle utilisable.

### Affichage du mot de passe saisi

Une icône d'œil dans le champ de mot de passe bascule l'affichage de la saisie. Le champ reste
masqué par défaut : c'est l'utilisateur qui décide de montrer, jamais l'inverse.

Les icônes sont des SVG intégrés et le comportement vit dans un fichier servi par l'application
(`wwwroot/js/interface.js`). Ni police d'icônes externe, ni gestionnaire `onclick` en ligne :
la première serait injoignable sur un réseau isolé, le second est bloqué par la politique de
contenu. Les écouteurs sont reposés après une navigation améliorée de Blazor, sinon la bascule
cesserait de répondre dès la deuxième page visitée.

### Défaut corrigé — politique de contenu contre carte d'importation

La politique posée au Sprint 0 (`script-src 'self'`) bloquait la carte d'importation que Blazor
émet en ligne : les modules JavaScript du cadriciel ne se chargeaient plus. Le défaut existait
depuis le Sprint 0 et n'avait pas été vu, faute d'avoir regardé la console du navigateur.

Corrigé par un jeton aléatoire tiré à chaque réponse, présent dans l'en-tête et porté par la
carte d'importation. Ouvrir la politique à `'unsafe-inline'` aurait été plus simple et aurait
autorisé, du même coup, tout script injecté. Vérifié : aucune erreur de console sur un onglet
neuf.

## Écart assumé — second facteur désactivé en développement

Demandé par la DSI en cours de sprint. `Authentification:SecondFacteurDesactive` court-circuite
l'étape de second facteur. C'est un écart à SEC-001, qui classe le MFA en « Must » : il est
documenté ici comme tel, et non présenté comme une fonctionnalité.

Trois garanties l'encadrent :

1. **L'application refuse de démarrer** si le réglage est actif hors développement — une
   exception au démarrage, pas un avertissement au journal : personne ne lit les journaux de
   démarrage d'un service Windows.
2. **Chaque connexion ainsi obtenue est tracée** sous « Second facteur contourné
   (développement) », jamais sous « Connexion réussie ». Le journal ne ment pas sur ce qui s'est
   réellement passé.
3. **La mécanique n'est pas retirée**, seulement court-circuitée. Les comptes gardent
   `TwoFactorEnabled` à vrai ; repasser le réglage à false rétablit le parcours complet sans
   redéploiement de code.

L'écran de connexion l'annonce lui aussi : quand le contournement est actif, il n'affiche plus
la promesse d'un code à recevoir.

Vérifié : démarrage refusé en Production avec ce réglage (`InvalidOperationException` citant
SEC-001), connexion directe en développement, entrée de contournement présente au journal.

## Écarts et limites

- **Aucun relais SMTP n'est configuré en développement** : les codes sont écrits dans
  `courriels-sortants/`. Ce n'est pas un envoi simulé présenté comme réussi — l'application
  journalise un avertissement à chaque démarrage et à chaque message. Le relais CIT reste à
  renseigner.
- **Le coffre à secrets est provisoire** : `CoffreDeConfiguration` lit la configuration du
  serveur. Il tient le contrat `ISecretResolver` sans offrir rotation ni journal d'accès. La
  solution CIT reste à désigner — question ouverte depuis le dossier d'architecture.
- **Deux environnements sont créés à l'amorçage** (Production, UAT) pour rendre la
  différenciation des droits démontrable. Le référentiel complet relève du Sprint 2.
- **Aucun compte n'est créé sans configuration explicite.** Si `Amorcage:EmailAdministrateur` et
  `Amorcage:MotDePasseAdministrateur` sont absents, l'application démarre et le signale, plutôt
  que de se doter d'un compte à mot de passe devinable.

## Dette technique traitée en cours de sprint

- **xunit 2.9.3 était déprécié** (le paquet renvoie vers xunit.v3) : les trois projets de tests
  sont passés à xunit v3.
- **L'audit des dépendances est maintenant appliqué à la génération** — `NuGetAudit` en mode
  `all`, niveau `low`. Combiné aux avertissements traités comme erreurs, un avis de sécurité
  publié sur une dépendance, même transitive, casse la build. Au moment du sprint, aucune
  dépendance vulnérable n'est signalée.
- **Base de développement renommée `N4Sentinel_v2`** : la base `N4Sentinel` du poste appartient
  à la version précédente du produit et n'a pas été touchée.

## Sprint suivant

**Sprint 2 — Référentiel : environnements et composants** (semaines 5–6). Objectif : l'écosystème
N4 de CIT saisi, validé, et ses dépendances visualisées.
