# Unity FPS Project

Mon premier jeu vidéo : un FPS développé avec Unity 6 et l'Universal Render Pipeline. Projet personnel en cours, les retours sont les bienvenus.

![Unity](https://img.shields.io/badge/Unity-6000.4-000000?logo=unity&logoColor=white) ![C#](https://img.shields.io/badge/C%23-512BD4?logo=dotnet&logoColor=white) ![URP](https://img.shields.io/badge/Render-URP-222222)

## Ce qui est en place

- Déplacements du joueur et vue à la souris (`PlayerMovement`, `MouseMovement`)
- Armes avec tir de balles et gestion des munitions (`Weapon`, `Bullet`, `AmmoManager`)
- Objets interactifs : bouteilles qui se cassent, cibles qui tombent, cubes destructibles
- Gestion des sons (`SoundManager`)
- Nouveau système d'entrées Unity (`InputSystem_Actions`)

## Ouvrir le projet

1. Installer **Unity 6000.4.1f1** via Unity Hub.
2. Cloner le dépôt puis l'ajouter dans Unity Hub (« Add project from disk »).
3. Ouvrir la scène `Assets/Scenes/SampleScene.unity` et lancer Play.

Les packages de l'Asset Store (`Assets/ThirdParty/`) ne sont pas versionnés : ils ne sont pas redistribuables et trop lourds pour un dépôt public. Certains modèles peuvent donc manquer à l'ouverture.

## Structure des scripts

```text
Assets/Scripts/
├── Core/           AmmoManager, GlobalReferences, SoundManager
├── Player/         PlayerMovement, MouseMovement
├── Weapons/        Weapon, Bullet
└── Interactables/  BeerBottle, FallingTarget, Cubedemolisher
```
---

Réalisé par **Ethan Lalienne** · [GitHub](https://github.com/ethn924) · [Portfolio](https://ethn924.github.io/Portfolio/)
