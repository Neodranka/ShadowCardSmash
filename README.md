# ShadowCardSmash (Godot)

Godot 4.3 + C# rewrite of the ShadowCardSmash card game.
Design rules live in `../ShadowCardSmash/GAME_DESIGN_DOCUMENT.md`.

## Architecture (6 onion layers, top depends on bottom)

```
App     scenes/      Godot scenes (MainMenu / Lobby / Battle)
View    src/View     Godot Nodes that visualize state & play animations
Net     src/Net      Godot High-Level Multiplayer (RPC) action transport
Cards   src/Cards    One C# class per card, [Card(id)] reflection-registered
Engine  src/Engine   Rules engine, action queue, effect primitives (no Godot deps)
Domain  src/Domain   GameState / Enums / IDs (POCO)
```

`Engine` and `Domain` have zero Godot dependency — testable in pure .NET via `tests/`.

## Adding a new card

1. Create `src/Cards/<Class>/MyCard.cs` with a `[Card(id)]` attribute and override the relevant `OnPlay / OnDeath / OnTurnStart` hooks.
2. Create `resources/cards/<id>.tres` with the display data (description, art path, localizations).
3. Run tests in `tests/Cards.Tests/`.

## Layout

```
project.godot           Godot project descriptor
ShadowCardSmash.csproj  Main game assembly
src/                    Game code by onion layer
scenes/                 Godot .tscn files
resources/              .tres data files (cards, decks, themes)
art/                    Images, VFX, audio
tests/                  xUnit projects, pure .NET
```
