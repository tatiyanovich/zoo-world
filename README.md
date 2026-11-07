# Zoo World

A small top-down simulation: frogs jump, snakes roam and eat, and the HUD counts deaths. Built with GameObject components, Zenject and uGUI.

![Gameplay](media/gameplay.gif)

## Run

Open **`src/zoo-world`**, not the repository root, in **Unity 6000.3.6f1**. Wait for package import, open `Assets/ZooWorld/Scenes/World.unity` and press **Play**. The simulation runs automatically.

Tests: **Window → General → Test Runner**, then **Run All** in **EditMode** and **PlayMode**.

## Architecture

Paths below are relative to `src/zoo-world/Assets/ZooWorld`. The diagram shows scene wiring, runtime flow and the components of an animal.

![Architecture](media/architecture.svg)

To add a species, compose an inactive prefab from `Animal`, a separate child Visual, `Rigidbody`, one root collider, `WanderDirection`, `AnimalMotor` and exactly one enabled movement component implementing `IAnimalMovement`. Each movement validates its own settings through `ValidateConfiguration`; a new movement needs no changes to `Animal`, the factory or the installer. Create an `AnimalDefinition` and add it to `AnimalCatalog`. Food role and movement are independent.

For a Predator, also add an enabled `TastyLabel` on the root and assign its child World Space Canvas with the `Tasty!` text, as in the Snake prefab. Configuration is validated once by `Scripts/Composition/WorldInstaller` before simulation.

Spawning tries up to 12 positions for a free spot. If all are occupied, it uses the last position, allowing overlap and an immediate collision. This keeps spawning on schedule without a population limit.

Regression tests live in `Tests/EditMode` and `Tests/PlayMode`.
