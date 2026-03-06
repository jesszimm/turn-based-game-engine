using TurnBasedGame.Application.Commands;
using TurnBasedGame.Application.Interfaces;
using TurnBasedGame.Application.Queries;
using TurnBasedGame.ConsoleUI.InputHandlers;
using TurnBasedGame.ConsoleUI.Renderers;

namespace TurnBasedGame.ConsoleUI;

/// <summary>
/// Main game loop that orchestrates gameplay from setup to completion.
/// Handles game creation, turn management, and game over detection.
/// </summary>
public sealed class GameLoop
{
    private readonly IGameEngine _gameEngine;
    private readonly ConsoleInputHandler _inputHandler;
    private readonly IBoardRenderer _renderer;

    public GameLoop(
        IGameEngine gameEngine,
        ConsoleInputHandler inputHandler,
        IBoardRenderer renderer)
    {
        _gameEngine = gameEngine ?? throw new ArgumentNullException(nameof(gameEngine));
        _inputHandler = inputHandler ?? throw new ArgumentNullException(nameof(inputHandler));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    /// <summary>
    /// Runs the complete game from setup to game over.
    /// </summary>
    public void Run()
    {
        if (!RunSetup())
        {
            System.Console.WriteLine("Setup cancelled. Exiting...");
            return;
        }

        RunGameplay();

        ShowGoodbyeMessage();
    }

    private bool RunSetup()
    {
        _renderer.Clear();
        RenderWelcome();

        // Get board dimensions
        if (!GetBoardSize(out var width, out var height))
            return false;

        // Get player names
        if (!GetPlayerNames(out var playerNames))
            return false;

        // Create the game
        var createCommand = new CreateGameCommand
        {
            BoardWidth = width,
            BoardHeight = height,
            PlayerNames = playerNames
        };

        var result = _gameEngine.CreateGame(createCommand);

        if (result.IsFailure)
        {
            _renderer.RenderError(result.ErrorMessage!);
            return false;
        }

        _renderer.RenderSuccess("Game created successfully!");

        // Setup initial units
        SetupUnits(playerNames.Count);

        return true;
    }

    private void RunGameplay()
    {
        ShowInitialState();

        while (true)
        {
            // Check for game over
            if (IsGameOver())
            {
                ShowGameOverScreen();
                break;
            }

            // Get user input
            System.Console.Write("> ");
            var input = System.Console.ReadLine();

            // Handle command
            var shouldContinue = _inputHandler.HandleInput(input);
            if (!shouldContinue)
                break;

            System.Console.WriteLine();
        }
    }

    private void RenderWelcome()
    {
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine("═══════════════════════════════════════════════════════");
        System.Console.WriteLine("          TURN-BASED TACTICAL STRATEGY GAME            ");
        System.Console.WriteLine("═══════════════════════════════════════════════════════");
        System.Console.ResetColor();
        System.Console.WriteLine();
    }

    private bool GetBoardSize(out int width, out int height)
    {
        width = 0;
        height = 0;

        System.Console.WriteLine("Board Setup");
        System.Console.WriteLine("───────────────────────────────────────────────────────");

        // Get width
        while (true)
        {
            System.Console.Write("Enter board width (5-20, or 'q' to quit): ");
            var input = System.Console.ReadLine();

            if (input?.ToLowerInvariant() == "q")
                return false;

            if (int.TryParse(input, out width) && width >= 5 && width <= 20)
                break;

            _renderer.RenderError("Please enter a number between 5 and 20");
        }

        // Get height
        while (true)
        {
            System.Console.Write("Enter board height (5-20, or 'q' to quit): ");
            var input = System.Console.ReadLine();

            if (input?.ToLowerInvariant() == "q")
                return false;

            if (int.TryParse(input, out height) && height >= 5 && height <= 20)
                break;

            _renderer.RenderError("Please enter a number between 5 and 20");
        }

        System.Console.WriteLine();
        return true;
    }

    private bool GetPlayerNames(out List<string> playerNames)
    {
        playerNames = new List<string>();

        System.Console.WriteLine("Player Setup");
        System.Console.WriteLine("───────────────────────────────────────────────────────");

        // Get number of players
        int playerCount;
        while (true)
        {
            System.Console.Write("Number of players (2-4, or 'q' to quit): ");
            var input = System.Console.ReadLine();

            if (input?.ToLowerInvariant() == "q")
                return false;

            if (int.TryParse(input, out playerCount) && playerCount >= 2 && playerCount <= 4)
                break;

            _renderer.RenderError("Please enter a number between 2 and 4");
        }

        // Get player names
        for (int i = 0; i < playerCount; i++)
        {
            while (true)
            {
                System.Console.Write($"Enter name for Player {i + 1}: ");
                var name = System.Console.ReadLine();

                if (string.IsNullOrWhiteSpace(name))
                {
                    _renderer.RenderError("Name cannot be empty");
                    continue;
                }

                playerNames.Add(name.Trim());
                break;
            }
        }

        System.Console.WriteLine();
        return true;
    }

    private void SetupUnits(int playerCount)
    {
        System.Console.WriteLine("Setting up units...");
        System.Console.WriteLine();

        var stateResult = _gameEngine.GetGameState(new GetGameStateQuery());
        if (stateResult.IsFailure)
            return;

        var gameState = stateResult.Value!;

        // Create 2 units per player
        int playerIndex = 0;
        foreach (var player in gameState.Players)
        {
            // First unit - Warrior (high attack, low defense)
            var warrior = new PlaceUnitCommand
            {
                UnitName = "Warrior",
                OwnerId = player.Id,
                X = playerIndex * 2,
                Y = 0,
                MaxHealth = 100,
                AttackPower = 15,
                Defense = 5,
                MovementRange = 3
            };

            _gameEngine.PlaceUnit(warrior);

            // Second unit - Guardian (high defense, low attack)
            var guardian = new PlaceUnitCommand
            {
                UnitName = "Guardian",
                OwnerId = player.Id,
                X = playerIndex * 2 + 1,
                Y = 0,
                MaxHealth = 120,
                AttackPower = 10,
                Defense = 10,
                MovementRange = 2
            };

            _gameEngine.PlaceUnit(guardian);

            playerIndex++;
        }

        // Add some terrain variety
        AddTerrainVariety(gameState.BoardWidth, gameState.BoardHeight);

        _renderer.RenderSuccess("Setup complete! Type 'help' to see available commands.");
        System.Console.WriteLine();
    }

    private void AddTerrainVariety(int width, int height)
    {
        // Add some forests
        for (int i = 0; i < (width * height) / 10; i++)
        {
            var x = Random.Shared.Next(0, width);
            var y = Random.Shared.Next(2, height - 1); // Avoid first row

            _gameEngine.SetTerrain(new SetTerrainCommand
            {
                X = x,
                Y = y,
                TerrainType = "Forest"
            });
        }

        // Add some mountains
        for (int i = 0; i < (width * height) / 15; i++)
        {
            var x = Random.Shared.Next(0, width);
            var y = Random.Shared.Next(2, height - 1);

            _gameEngine.SetTerrain(new SetTerrainCommand
            {
                X = x,
                Y = y,
                TerrainType = "Mountain"
            });
        }
    }

    private void ShowInitialState()
    {
        var result = _gameEngine.GetGameState(new GetGameStateQuery());

        if (result.IsSuccess)
        {
            _renderer.RenderGame(result.Value!);
            System.Console.WriteLine("Type 'help' for available commands.");
            System.Console.WriteLine();
        }
    }

    private bool IsGameOver()
    {
        var result = _gameEngine.GetGameState(new GetGameStateQuery());

        if (result.IsFailure)
            return false;

        var activePlayers = result.Value!.Players.Count(p => p.IsActive);
        return activePlayers <= 1;
    }

    private void ShowGameOverScreen()
    {
        var result = _gameEngine.GetGameState(new GetGameStateQuery());

        if (result.IsSuccess)
        {
            _renderer.RenderGameOver(result.Value!);
        }
    }

    private void ShowGoodbyeMessage()
    {
        System.Console.WriteLine();
        System.Console.ForegroundColor = ConsoleColor.Cyan;
        System.Console.WriteLine("Thanks for playing!");
        System.Console.ResetColor();
    }
}