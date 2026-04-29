# Project Starter

## Quick Start

1. Open or create a scene inside `Assets/_Project/Scenes`.
2. Add an empty GameObject named `Bootstrap`.
3. Attach `GameBootstrap` to the `Bootstrap` object.
4. Add a character object with `Rigidbody2D`.
5. Attach `TopDownCharacterController2D` to that character.

## Current Structure

- `Scripts/Core`: game flow and bootstrap code
- `Scripts/Gameplay`: gameplay scripts
- `Scenes`: project scenes
- `Prefabs`: reusable prefabs
- `Art`: project art assets
- `UI`: project UI assets

## Suggested Next Step

Pick one core loop and build only that first:

- top-down action
- idle/clicker combat
- wave survival

## Idle Battle Setup

For a very first idle-combat prototype:

1. Create an empty object named `BattleRoot`.
2. Attach `AutoBattleController` to `BattleRoot`.
3. Create a player object and attach `AutoBattleUnit`.
4. Set the player's name, HP, damage, and attack interval.
5. Create an enemy object and attach `AutoBattleUnit`.
6. Set the enemy object's stats, then drag it into `enemyTemplate`.
7. Keep the player and enemy at fixed positions and adjust `enemySpawnOffsetX` on `AutoBattleController`.
8. Optionally attach `BackgroundScroller` to a tiled background and assign it in `backgroundScroller`.
9. Press Play. The knight auto-attacks, the enemy stays in place, and the battle loop repeats.

## Minimal Test Loop

If you want to test with only a player and one monster:

1. Place one player object in the scene.
2. Place one enemy template object in the scene.
3. Assign both to `AutoBattleController`.
4. Leave `loopOnPlayerDeath` enabled.
5. Press Play and watch the fight repeat automatically.

## Simple Attack Test

For the simplest test version:

1. Keep the enemy at a fixed position.
2. Let the player auto-attack only.
3. Tune the player's `attackInterval` for feel first.

## Trigger Setup

For proper melee timing, use layer detection with a trigger:

1. Put enemies on an `Enemy` layer.
2. Add a child object to the player for attack range.
3. Add a `CircleCollider2D` or `BoxCollider2D` to that child and enable `Is Trigger`.
4. Attach `AutoBattleSensor2D` to the same child.
5. Set the sensor's `targetLayers` to the `Enemy` layer.
6. Assign that sensor to `playerSensor` on `AutoBattleController`.
