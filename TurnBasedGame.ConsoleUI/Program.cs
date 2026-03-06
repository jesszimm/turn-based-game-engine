using TurnBasedGame.Application;
using TurnBasedGame.ConsoleUI.InputHandlers;
using TurnBasedGame.ConsoleUI.Renderers;
using TurnBasedGame.Domain.Services;

namespace TurnBasedGame.ConsoleUI;

/// <summary>
/// Main entry point for the console application.
/// Sets up dependencies and starts the game.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            // Manual dependency injection - no framework needed
            var combatResolver = new CombatResolver();
            var gameEngine = new GameEngine(combatResolver);
            var renderer = new ConsoleBoardRenderer();
            var inputHandler = new ConsoleInputHandler(gameEngine, renderer);
            var gameLoop = new GameLoop(gameEngine, inputHandler, renderer);

            // Run the game
            gameLoop.Run();
        }
        catch (Exception ex)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine($"Fatal error: {ex.Message}");
            System.Console.ResetColor();

            if (args.Contains("--debug"))
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Stack trace:");
                System.Console.WriteLine(ex.StackTrace);
            }
        }
    }
}