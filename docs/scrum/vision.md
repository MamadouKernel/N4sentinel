# Vision produit — N4 Sentinel

## Énoncé de vision

Pour les équipes DSI de Côte d'Ivoire Terminal qui exploitent l'écosystème Navis N4, **N4 Sentinel** est une
application interne qui standardise, sécurise et trace les opérations d'arrêt, de démarrage, de supervision et
de diagnostic de N4 — contrairement aux procédures manuelles actuelles, elle contrôle automatiquement le
séquencement, les prérequis et les autorisations avant chaque action.

## Problème adressé

- Les opérations d'arrêt/démarrage/redémarrage de N4 sont manuelles, dépendent de la disponibilité et de
  l'expertise individuelle des intervenants, et sont peu tracées.
- Une erreur de séquence (ex. démarrer XPS avant que le Bridge soit opérationnel) peut transformer une
  maintenance courante en incident.
- Le niveau de maîtrise des composants N4 et des procédures de diagnostic varie selon les intervenants,
  ce qui allonge le délai de résolution des incidents.

## Objectif produit (V1)

Piloter, superviser, diagnostiquer et documenter l'écosystème N4 avec un niveau d'automatisation configurable
(semi-automatique en V1, cf. §Palier 1 du cahier des charges), un mode simulation systématique, et une
traçabilité complète — sans jamais se substituer à l'expertise DSI/Navis pour les décisions qui l'exigent.

## Parties prenantes

- DSI de CIT (maîtrise d'ouvrage et d'œuvre — développement interne)
- TOS Manager / équipes d'exploitation N4 (utilisateurs opérateurs)
- Administrateurs N4 (référentiel, workflows, comptes)
- Support Navis (référence technique externe, non utilisateur direct)

## Sources de vérité

- **Périmètre fonctionnel (quoi construire)** : `Cahier_de_charge_N4_Sentinel_v3.docx` fait foi. Ce backlog
  en est la décomposition Scrum ; en cas de divergence, le cahier des charges prévaut.
- **Comportement réel de l'écosystème N4 (comment ça marche réellement)** : les deux documents Navis/Kaleris
  font foi, **pas des hypothèses** — `N4 3.8.25 Setup, Maintenance, and System Diagnostics Guide 1.pdf`
  (guide administrateur officiel Navis, 1353 pages) et `N4 IT Admin 2024 4.x Day1 Install-Startup (2)
  (1).pdf` (support de formation Kaleris). Toute conception touchant à la séquence de démarrage/arrêt, aux
  noms de composants, aux statuts de supervision, aux journaux techniques ou aux causes d'incidents doit
  être ancrée dans ces deux sources. Voir la synthèse consolidée et citée :
  [`docs/navis-reference.md`](../navis-reference.md).

## Hors périmètre (rappel)

Aucune action mutative non enregistrée/validée dans le référentiel, aucune exécution de commande arbitraire,
aucune action destructive déclenchée par l'assistant documentaire. Voir §Exclusions et limites du périmètre.
