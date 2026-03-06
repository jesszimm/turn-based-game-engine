# Turn-Based Tactical Game Engine

A modular turn-based tactical game engine built with **C# and .NET**, designed to demonstrate clean architecture, separation of concerns, and testable domain-driven design.

This project implements a console-based tactical grid battle game where two players take turns moving units and attacking on a grid-based battlefield. The architecture is intentionally designed to separate **game logic, application workflows, and presentation**, making the engine easy to extend to other interfaces such as a graphical UI or web client.

---

# Project Goals

This project was built to demonstrate:

* Clean Architecture principles
* Domain-driven design
* Command/Query separation
* Interface-based dependency injection
* Testable business logic
* Decoupled UI rendering
* Expandable engine design

The codebase is structured so the **core game rules are completely independent from the user interface**, allowing the engine to be reused with different frontends.

---

# Gameplay Overview

The game is played on a grid-based battlefield.

Players take turns performing actions with their units.

Available actions include:

* Move a unit to an adjacent tile
* Attack an enemy unit
* End the turn

Each unit has:

* Health
* Attack power
* Movement range
* Position on the grid

The game ends when one player loses all units.

---

# Architecture

The project follows a layered architecture that separates responsibilities into distinct modules.

```
TurnBasedGame
│
├── Domain
│   Core game rules and entities
│
├── Application
│   Game use cases, commands, and queries
│
├── Infrastructure
│   External implementations (future persistence, services)
│
├── ConsoleUI
│   Input handling and board rendering for the console interface
│
└── Tests
    Unit tests validating game behavior
```

---

# Layer Responsibilities

## Domain

Contains the core game logic.

Examples:

* Game entities
* Board state
* Unit behavior
* Combat rules

This layer contains **no dependencies on UI, frameworks, or infrastructure**.

---

## Application

Implements game workflows using commands and queries.

Examples:

* MoveUnitCommand
* AttackCommand
* GetBoardStateQuery

This layer orchestrates domain behavior but does not contain UI code.

---

## Console UI

Handles user interaction.

Responsibilities include:

* Reading player input
* Rendering the board
* Displaying game events

Because the UI depends only on interfaces defined in the Application layer, it can be replaced with another frontend without modifying the game engine.

---

## Infrastructure

Reserved for external systems such as:

* Save/load functionality
* Databases
* Network multiplayer

Currently minimal but structured for future expansion.

---

# Key Design Decisions

### Interface-Based Rendering

Board rendering is abstracted behind an interface:

```
IBoardRenderer
```

This allows different implementations such as:

* Console renderer
* GUI renderer
* Web renderer

without changing game logic.

---

### Command Pattern

Game actions are implemented using commands:

```
MoveUnitCommand
AttackCommand
```

This approach improves:

* testability
* separation of concerns
* extensibility

---

### Testable Domain Logic

Game rules live entirely in the Domain layer and can be unit tested without UI dependencies.

---

# Example Console Gameplay

```
Player 1 Turn

  A B C D E
1 . . . . .
2 . P . E .
3 . . . . .

Select action:
1 - Move
2 - Attack
3 - End Turn
```

---

# Running the Game

Requirements:

* .NET 9 SDK

Clone the repository:

```bash
git clone https://github.com/jesszimm/turn-based-game-engine.git
```

Run the game:

```bash
dotnet run --project TurnBasedGame.ConsoleUI
```

---

# Running Tests

```bash
dotnet test
```

---

# Future Improvements

Planned features include:

* Graphical UI (WinUI or MonoGame)
* Mouse-based input
* Animated combat
* AI opponents
* Save/load game state
* Network multiplayer

---

# What This Project Demonstrates

This repository demonstrates my ability to:

* design layered architectures
* implement clean separation of concerns
* build extensible systems
* write maintainable C# code
* structure testable domain logic

---

