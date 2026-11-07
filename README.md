# Zoo World

A small top-down simulation: frogs jump, snakes roam and eat, and the HUD counts deaths. Built with GameObject components, Zenject and uGUI.

![Gameplay](media/gameplay.gif)

## Run

Open **`src/zoo-world`**, not the repository root, in **Unity 6000.3.6f1**. Wait for package import, open `Assets/ZooWorld/Scenes/World.unity` and press **Play**. The simulation runs automatically.

Tests: **Window → General → Test Runner**, then **Run All** in **EditMode** and **PlayMode**.

## Architecture

Paths below are relative to `src/zoo-world/Assets/ZooWorld`. Solid arrows show runtime flow; dotted arrows show configuration and injected dependencies.

```mermaid
flowchart TD
    subgraph setup["Scene and configuration"]
        World["Scenes/World.unity<br/>SceneContext + WorldInstaller"]
        Settings["Settings/<br/>AnimalCatalog · AnimalDefinition<br/>SimulationSettings"]
        Prefabs["Prefabs/Animals/<br/>Frog · Snake"]
        Settings -.-> World
        Settings -. "prefab + food role" .-> Prefabs
    end

    subgraph simulation["Scripts/Simulation"]
        Spawner["AnimalSpawner<br/>choose species + position every 1–2 s"]
        Bounds["ArenaBounds · IRandomSource<br/>camera bounds + randomness"]
        Food["FoodChain + FoodChainRules<br/>resolve contact → one death"]
        Stats["DeathStatistics"]
    end

    subgraph animals["Scripts/Animals and Movement/"]
        Factory["AnimalFactory<br/>instantiate → inject → initialize → activate"]
        Animal["Animal<br/>role · SpawnId · lifetime"]
        Movement["WanderDirection → LinearMovement / JumpMovement<br/>AnimalMotor → Rigidbody + Collider<br/>child Visual animates independently"]
        Animal --- Movement
    end

    subgraph ui["Scripts/UI"]
        HUD["DeathCounterView<br/>uGUI counters"]
        Tasty["TastyLabel<br/>animal's world-space label"]
    end

    World -. "Composition/WorldInstaller binds scene services" .-> Spawner
    World -.-> Factory
    World -.-> Food
    Settings -.-> Spawner
    Prefabs -.-> Factory
    Bounds -.-> Spawner
    Bounds -.-> Movement
    Spawner --> Factory --> Animal
    Animal -- "collision" --> Food
    Food -- "successful death" --> Stats
    Stats -- "Changed" --> HUD
    Food -- "survivor's Animal.Ate" --> Tasty
```

To add a species, compose a prefab from `Animal`, `WanderDirection`, `AnimalMotor` and one movement component; create an `AnimalDefinition` and add it to `AnimalCatalog`. Food role and movement are independent; shared logic has no species-specific branches. Configuration is validated once by `Scripts/Composition/WorldInstaller` before simulation.

Regression tests live in `Tests/EditMode` and `Tests/PlayMode`.
