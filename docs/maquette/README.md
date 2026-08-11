# Maquette de référence

Ces trois fichiers sont la **référence visuelle et fonctionnelle contraignante** du projet,
telle qu'arrêtée avec la DSI. Ils ne sont pas compilés : ils sont versionnés ici pour que la
référence ne dépende pas d'un dossier extérieur au dépôt.

| Fichier | Ce qu'il fixe |
|---|---|
| `Index.cshtml` | Les huit onglets, leur ordre, leurs intitulés, la structure de chaque écran |
| `site.css` | L'identité visuelle : couleurs, typographie, espacements, cartes, badges d'état, tableaux |
| `app.js` | Les interactions attendues écran par écran |

## Les huit onglets, dans l'ordre

1. Diagramme d'architecture Navis N4 (reproduction 1:1, temps réel)
2. Supervision 360° temps réel
3. Pilotage et workflows
4. Moteur de diagnostic
5. Analyseur de logs
6. Assistant N4
7. Dossiers partagés et EDI
8. Registre d'audit

## Ce qui est repris, et ce qui ne l'est pas

**Repris à l'identique** : le rendu. Onglets, hiérarchie visuelle, composants, mise en page.
La conformité se vérifie écran par écran, par comparaison avec la maquette.

**Non repris** : la mécanique interne. La maquette est une démonstration — sa télémétrie est
tirée au hasard, son moteur de diagnostic est une suite de conditions sur trois symptômes avec
des scores de confiance écrits en dur, son assistant reconnaît trois mots-clés, et chacune de
ses étapes de workflow réussit toujours. Le cahier des charges exige l'exécution réelle des
commandes. Reproduire ce que la maquette **montre** est l'objectif ; reproduire la façon dont
elle le **fabrique** irait contre le cahier des charges.

**Deux adaptations assumées** :

- **Portage en Tailwind.** `site.css` sert de source de vérité pour les valeurs (couleurs,
  typographie, espacements), converties en jetons de thème. Le rendu est identique ; la feuille
  de style n'est pas recopiée.
- **Suppression des CDN.** La maquette charge ses polices et ses icônes depuis Internet.
  L'application est destinée à un réseau isolé : ces ressources sont internalisées.

## Ce que la maquette ne montre pas et qui existera quand même

La maquette n'a ni authentification, ni rôles, ni sélecteur d'utilisateur — son persona est
écrit en dur. Le cahier des charges impose une authentification avec double facteur, huit
profils et des droits distincts par environnement. Ces écrans s'ajoutent à la maquette ; ils ne
la contredisent pas.
