# Cursed Energy Trial

A Unity combat and physics demo inspired by *Jujutsu Kaisen*. You play as Gojo and use three cursed techniques (Red, Blue and Purple) to push, pull and erase everything in an arena, with a timed trial mode graded from Grade 4 up to Special Grade.

![Gameplay](docs/screenshot.jpg)

Gameplay video and a full write-up: [liamm.ca/projects/cursed-energy-trial](https://liamm.ca/projects/cursed-energy-trial)

## Features

- **Three physics-driven abilities.** Red charges into a repelling shockwave, Blue captures objects into a stable orbit, and Purple (Red and Blue cast within a short window) deletes whatever it passes through.
- **Destruction.** Objects topple, scatter and dissolve in real time. Purple's dissolve spreads from the exact point of impact.
- **Trial mode.** 60 seconds, 5 Red, 5 Blue and 2 Purple casts. Each object scores once, the first time it is meaningfully broken, and the run is graded on the anime's sorcerer ranks.
- **Responsive animation.** Casting plays on an upper-body layer, so you can keep moving and strafing while an ability charges.
- **Editor tooling.** Menu tools under *JJK Demo* generate the arena, the HUD prefab and the TextMesh Pro font assets.

## How it's built

Abilities are data, not code. Each one is an `AbilityDefinition` ScriptableObject made of three parts: a `HeldSphereProfile` (charge curve, radius, auto-fire hold), an `AbilityPhysicsProfile` subclass (Red push, Blue orbit, Purple erase) and an `AbilityFeedbackProfile` (audio, FOV kick, camera shake).

```
Input System actions
  → PlayerInputCommandAdapter      turns input into AbilityCommands on a channel
  → AbilityController              commits on press; Red + Blue in a short window upgrades to Purple
  → HeldSphereAbilityActor         charges while following the aim, then launches as a projectile
  → PhysicsInteractionService      non-allocating overlap query per tick, de-duplicated by rigidbody root
  → IDamageable / DestructibleObject
AbilityFeedbackBus                 camera and audio presenters subscribe; gameplay never references them
```

The trial sits on top without touching ability code. A state-machine manager runs the countdown, run and results. A per-colour limiter plugs into the controller's `CanActivate` hook, and score targets report to a grade ladder that drives the HUD.

Scripts live in `Assets/_Project/Scripts`, grouped by system: `Abilities`, `Physics`, `Destruction`, `Feedback`, `Input`, `Camera`, `Characters`, `Trial` and `Editor`.

## Tech

Unity 6 (6000.4.7f1), C#, URP 17 and Shader Graph, Input System, Animation Rigging, ProBuilder, Blender and Mixamo for animation.

## Opening the project

This repository contains the project's own code, scenes, prefabs, materials and shaders. Several third-party assets are **not included** because their licenses don't allow redistribution. The project needs them to compile and to look right:

| Asset | Source | Used for |
| --- | --- | --- |
| Toon Shaders Pro | Unity Asset Store | Cel shading on characters and environment (the editor tools reference it directly) |
| Free Quick Effects Vol. 1 (Gabriel Aguiar Prod.) | Unity Asset Store | Particle effects |
| Player character model | Fan-made | The Gojo model |
| Characters and animations | [Mixamo](https://www.mixamo.com) | Locomotion, cast poses and the target dummies |

Import them into `Assets/`, then open `Assets/_Project/Scenes/CursedEnergyTrial.unity`. Fonts and images are stored with [Git LFS](https://git-lfs.com), so install it before cloning.

## Credits and license

- Noise functions from [NoiseShader](https://github.com/keijiro/NoiseShader) by Keijiro Takahashi (MIT).
- Fonts: Bebas Neue, Oswald, Rajdhani, Russo One and Teko, under the SIL Open Font License 1.1.

Full license texts are in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

This is a non-commercial fan project. *Jujutsu Kaisen* and its characters are © Gege Akutami / Shueisha; this project is not affiliated with or endorsed by them or any other rights holder. The source is published for reference. © Liam Maiorino, all rights reserved.
