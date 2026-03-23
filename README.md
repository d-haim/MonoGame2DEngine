# MonoGameEngine (WIP)

A lightweight, component-based 2D game engine built on top of MonoGame (XNA), inspired by Unity3D Scene graph and component composition.
The Engine is a work in progress and includes basic scene workflow (update loop, rendering and collision detection).
In the roadmap are:

- Input handlers for easy generic input events.
- Event bus that will include a Command Buffer and Event Propagation.
- Collision optimization (Nearest Neighbor, Auto Screen bounds detection, Circle Colliders).
- Sprite Atlas and Animation from configuration files.

## Features

- **Component-Based Architecture**: Composition-over-inheritance approach for game entities.
- **Automated Bootstrapping**: Roslyn-powered source generators for initialization and content loading.
- **Scene Management**: Efficient entity lifecycle management and YAML-based scene configuration.
- **Resolution Independence**: Built-in Viewport scaling for consistent rendering across different screen sizes.
- **Performance Focused**: Includes object pooling and optimized internal loops.

## Architecture Overview

### Core Engine

- **`GameEngine`**: The main entry point inheriting from `Microsoft.Xna.Framework.Game`. Manages the primary game loop.
- **`GameEntity`**: Abstract base class for all game objects. Components like `Transformation`, `SpriteRenderer`, and `Collider` are built-in.
- **`Scene`**: Manages entity collections and dispatches lifecycle events (Update, Draw, Collision) using a mix of reflection-based discovery and optimized lists.
- **`Viewport`**: Handles resolution scaling and transformation matrices.

### Built-in Components

- **`Transformation`**: Manages position, rotation, and scale.
- **`SpriteRenderer`**: Handles 2D texture rendering, effects, and color.
- **`Collider`**: Provides AABB (Axis-Aligned Bounding Box) collision detection and trigger support.

### Source Generation

The engine uses incremental source generators to eliminate boilerplate:

- **Bootstrapper**: Automatically invokes methods marked with `[OnInitialize]` and `[ContentLoad]`.
- **Scene Manager**: Generates scene loading logic from YAML configuration files.

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [MonoGame](https://www.monogame.net/)

### Building and Running

1. **Restore dependencies**:
   ```bash
   dotnet restore
   ```
2. **Build the project**:
   ```bash
   dotnet build
   ```
3. **Run the engine**:
   ```bash
   dotnet run
   ```

## Development Conventions

### Entity Lifecycle

Implement the following methods in your `GameEntity` subclasses for automatic discovery:

- `OnEnable()` / `OnDisable()`
- `OnUpdate(float deltaTime)`
- `OnDraw()`
- `OnCollision(GameEntity other)` / `OnTrigger(GameEntity other)`

### Attributes

- Use `[OnInitialize(priority)]` on static methods for automated startup logic.
- Use `[ContentLoad]` for automated asset loading.

### Project Structure

- `/Attributes`: Custom attributes for source generators.
- `/Components`: Built-in entity components.
- `/Generators`: Roslyn source generator projects.
- `/Utils`: Common utilities (ObjectPool, Loggers).
- `/Content`: MonoGame Content Pipeline files.

## License

This project is open-source and available under the MIT License.
