using TurnBasedGame.Domain.Entities;
using TurnBasedGame.Domain.ValueObjects;

namespace TurnBasedGame.Application.Services;

/// <summary>
/// Determines AI actions based on the current game state without performing any I/O.
/// </summary>
public sealed class AiDecisionService
{
    public AiDecision Decide(AiDecisionState state)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        var game = state.Game;
        var aiUnits = game.GetCurrentPlayerUnits().Where(u => u.IsAlive).ToList();
        var enemyUnits = game.GetOpponentUnits().Where(u => u.IsAlive).ToList();

        if (aiUnits.Count == 0 || enemyUnits.Count == 0)
            return Skip(state);

        state.FocusTargetId = UpdateFocusTarget(state.FocusTargetId, enemyUnits, state.Difficulty);

        if (game.HasAttackedThisTurn == false)
        {
            var attackDecision = TrySelectAttack(game, aiUnits, enemyUnits, state.Difficulty);
            if (attackDecision != null)
                return attackDecision;
        }

        if (game.HasMovedThisTurn == false)
        {
            var useHardLogic = state.Difficulty == AiDifficulty.Medium;
            var useImpossibleLogic = state.Difficulty == AiDifficulty.Hard;

            if (useHardLogic || useImpossibleLogic)
            {
                var opponent = game.GetOpponent();
                var opponentControlTurns = game.GetControlTurnsForPlayer(opponent.Id);
                var opponentOnControl = game.ControlTileEnabled &&
                    enemyUnits.Any(unit => unit.Position.Equals(game.ControlPosition));

                var isAggressivePhase = game.TurnNumber >= 10 || useImpossibleLogic;

                if (!isAggressivePhase &&
                    (!game.ControlTileEnabled || (opponentControlTurns < 2 && !opponentOnControl)))
                {
                    var retreat = TryFindRetreatMove(game, aiUnits, enemyUnits, out var retreatUnit, out var retreatPosition);
                    if (retreat)
                        return Move(retreatUnit.Id, retreatPosition);
                }
            }

            var bestMoveFound = TrySelectMove(
                game,
                aiUnits,
                enemyUnits,
                state.Difficulty,
                state.NextAiMoveUnitAbbreviation,
                state.FocusTargetId,
                out var chosenUnit,
                out var chosenPosition);

            if (bestMoveFound)
                return Move(chosenUnit.Id, chosenPosition);
        }

        return Skip(state);
    }

    private static AiDecision? TrySelectAttack(Game game, List<Unit> aiUnits, List<Unit> enemyUnits, AiDifficulty difficulty)
    {
        var useHardLogic = difficulty == AiDifficulty.Medium;
        var useImpossibleLogic = difficulty == AiDifficulty.Hard;

        var attackOptions = (
            from attacker in aiUnits
            from defender in enemyUnits
            where attacker.Position.IsAdjacentTo(defender.Position, includeDiagonals: true)
            select new AttackOption(
                attacker,
                defender,
                game.ControlTileEnabled && (useHardLogic || useImpossibleLogic) &&
                defender.Position.Equals(game.ControlPosition),
                defender.Stats.CurrentHealth <= attacker.Stats.AttackPower,
                GetAiAttackerPriority(attacker, difficulty),
                defender.Stats.CurrentHealth)).ToList();

        if (useImpossibleLogic)
        {
            var aiTotalHp = GetTotalHp(aiUnits);
            var enemyTotalHp = GetTotalHp(enemyUnits);
            var bestScore = int.MinValue;
            AttackOption? bestOption = null;

            foreach (var option in attackOptions)
            {
                if (!IsAttackValid(game, option.Attacker, option.Defender))
                    continue;

                var score = ScoreAttackImpossible(game, option, enemyUnits, aiTotalHp, enemyTotalHp);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestOption = option;
                }
            }

            if (bestOption != null)
                return Attack(bestOption.Attacker.Id, bestOption.Defender.Id);
        }

        var killOptions = attackOptions.Where(option => option.IsKill).ToList();
        if (useHardLogic || useImpossibleLogic)
        {
            var opponentId = game.Player1.Id == game.CurrentPlayer.Id
                ? game.Player2.Id
                : game.Player1.Id;
            var opponentControlTurns = game.GetControlTurnsForPlayer(opponentId);
            var opponentOnControl = attackOptions.Any(option => option.DefenderOnControl);

            if (useImpossibleLogic)
            {
                // Hard AI now uses the most aggressive policy and never avoids risky kills.
            }
            else if (opponentControlTurns < 2 && !opponentOnControl)
            {
                killOptions = killOptions
                    .Where(option => !IsGuaranteedDeath(option.Attacker, option.Defender, enemyUnits))
                    .ToList();
            }
        }

        if (killOptions.Count > 0)
        {
            var killChoice = killOptions
                .OrderBy(option => option.AttackerPriority)
                .ThenBy(option => option.DefenderHealth)
                .FirstOrDefault(option => IsAttackValid(game, option.Attacker, option.Defender));

            if (killChoice != null)
                return Attack(killChoice.Attacker.Id, killChoice.Defender.Id);
        }

        if (attackOptions.Count > 0)
        {
            var orderedAttacks = attackOptions
                .OrderByDescending(option => option.DefenderOnControl)
                .ThenBy(option => option.AttackerPriority)
                .ThenByDescending(option => useHardLogic || useImpossibleLogic ? option.IsKill : false)
                .ThenBy(option => difficulty == AiDifficulty.Medium ? option.DefenderHealth : 0)
                .ThenBy(option => option.DefenderHealth)
                .ToList();

            foreach (var option in orderedAttacks)
            {
                if (IsAttackValid(game, option.Attacker, option.Defender))
                    return Attack(option.Attacker.Id, option.Defender.Id);
            }
        }

        return null;
    }

    private static bool IsAttackValid(Game game, Unit attacker, Unit defender)
    {
        if (game.IsGameOver)
            return false;
        if (attacker.OwnerId != game.CurrentPlayer.Id)
            return false;
        if (game.HasAttackedThisTurn)
            return false;
        if (!attacker.IsAlive || !defender.IsAlive)
            return false;

        return attacker.CanAttackPosition(defender.Position, defender.OwnerId);
    }

    private static AiDecision Attack(Guid attackerId, Guid defenderId) => new()
    {
        ActionType = AiDecisionAction.Attack,
        UnitId = attackerId,
        TargetId = defenderId
    };

    private static AiDecision Move(Guid unitId, Position position) => new()
    {
        ActionType = AiDecisionAction.Move,
        UnitId = unitId,
        TargetPosition = position
    };

    private static AiDecision Skip(AiDecisionState state) => new()
    {
        ActionType = AiDecisionAction.Skip
    };

    private static Guid? UpdateFocusTarget(Guid? currentFocus, List<Unit> enemyUnits, AiDifficulty difficulty)
    {
        if (currentFocus != null && enemyUnits.Any(unit => unit.Id == currentFocus))
            return currentFocus;

        if (difficulty == AiDifficulty.Hard && currentFocus != null)
            return currentFocus;

        return enemyUnits.OrderBy(unit => unit.Stats.CurrentHealth).FirstOrDefault()?.Id;
    }

    private static int GetAiAttackerPriority(Unit attacker, AiDifficulty difficulty)
    {
        var abbreviation = GetUnitAbbreviation(attacker);
        return (difficulty, abbreviation) switch
        {
            (AiDifficulty.Easy, 'S') => 0,
            (AiDifficulty.Easy, 'W') => 1,
            (AiDifficulty.Medium, 'W') => 0,
            (AiDifficulty.Medium, 'S') => 1,
            (AiDifficulty.Hard, 'W') => 0,
            (AiDifficulty.Hard, 'S') => 1,
            _ => 2
        };
    }

    private static int GetUnitMovePriority(Unit unit, char preferredFirst, char preferredSecond)
    {
        var abbreviation = GetUnitAbbreviation(unit);
        if (abbreviation == preferredFirst)
            return 0;

        if (abbreviation == preferredSecond)
            return 1;

        return 2;
    }

    private static char GetUnitAbbreviation(Unit unit)
    {
        var first = unit.Name.FirstOrDefault(char.IsLetterOrDigit);
        return first == default ? '?' : char.ToUpperInvariant(first);
    }

    private static int ChebyshevDistance(Position a, Position b)
    {
        return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }

    private static bool IsGuaranteedDeath(Unit attacker, Unit target, List<Unit> enemies)
    {
        var attackerHp = attacker.Stats.CurrentHealth;
        return enemies
            .Where(enemy => enemy.Id != target.Id)
            .Any(enemy =>
                enemy.Position.IsAdjacentTo(attacker.Position, includeDiagonals: true) &&
                enemy.Stats.AttackPower >= attackerHp);
    }

    private static bool TryFindRetreatMove(
        Game game,
        List<Unit> aiUnits,
        List<Unit> enemyUnits,
        out Unit retreatUnit,
        out Position retreatPosition)
    {
        retreatUnit = aiUnits[0];
        retreatPosition = retreatUnit.Position;
        var retreatFound = false;
        var bestDistance = -1;

        foreach (var unit in aiUnits)
        {
            if (!enemyUnits.Any(enemy =>
                    enemy.Position.IsAdjacentTo(unit.Position, includeDiagonals: true) &&
                    enemy.Stats.AttackPower >= unit.Stats.CurrentHealth))
                continue;

            var validMoves = game.Board.GetValidMovePositions(unit).ToList();
            foreach (var move in validMoves)
            {
                var distance = enemyUnits
                    .Select(enemy => ChebyshevDistance(move, enemy.Position))
                    .DefaultIfEmpty(int.MinValue)
                    .Min();

                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    retreatUnit = unit;
                    retreatPosition = move;
                    retreatFound = true;
                }
            }
        }

        return retreatFound;
    }

    private static bool TrySelectMove(
        Game game,
        List<Unit> aiUnits,
        List<Unit> enemyUnits,
        AiDifficulty difficulty,
        char nextAiMoveUnitAbbreviation,
        Guid? focusTargetId,
        out Unit chosenUnit,
        out Position chosenPosition)
    {
        chosenUnit = aiUnits[0];
        chosenPosition = chosenUnit.Position;
        var bestScore = int.MinValue;

        var preferredFirst = nextAiMoveUnitAbbreviation;
        var preferredSecond = preferredFirst == 'W' ? 'S' : 'W';

        var isAggressivePhase = difficulty == AiDifficulty.Medium && game.TurnNumber >= 10;
        var isImpossibleLogic = difficulty == AiDifficulty.Hard;
        var aiTotalHp = GetTotalHp(aiUnits);
        var enemyTotalHp = GetTotalHp(enemyUnits);

        foreach (var unit in aiUnits)
        {
            var validMoves = game.Board.GetValidMovePositions(unit).ToList();
            foreach (var move in validMoves)
            {
                var isControlMove = game.ControlTileEnabled && move.Equals(game.ControlPosition);

                if (difficulty == AiDifficulty.Medium && !isAggressivePhase && !isControlMove && !IsMoveSafeHard(unit, move, enemyUnits))
                    continue;

                var score = ScoreMove(unit, move, enemyUnits, difficulty, preferredFirst, preferredSecond, focusTargetId,
                    game.ControlTileEnabled, game.ControlPosition, isAggressivePhase || isImpossibleLogic, aiTotalHp, enemyTotalHp);
                if (score > bestScore)
                {
                    bestScore = score;
                    chosenUnit = unit;
                    chosenPosition = move;
                }
            }
        }

        return bestScore != int.MinValue;
    }

    private static bool IsMoveSafeHard(Unit unit, Position move, List<Unit> enemyUnits)
    {
        foreach (var enemy in enemyUnits)
        {
            if (!enemy.Position.IsAdjacentTo(move, includeDiagonals: true))
                continue;

            var hpAfterAttack = unit.Stats.CurrentHealth - enemy.Stats.AttackPower;
            if (hpAfterAttack <= enemy.Stats.CurrentHealth)
                return false;
        }

        return true;
    }

    private static int ScoreMove(
        Unit unit,
        Position move,
        List<Unit> enemyUnits,
        AiDifficulty difficulty,
        char preferredFirst,
        char preferredSecond,
        Guid? focusTargetId,
        bool controlTileEnabled,
        Position controlPosition,
        bool isAggressivePhase,
        int aiTotalHp,
        int enemyTotalHp)
    {
        var minDistance = enemyUnits
            .Select(enemy => ChebyshevDistance(move, enemy.Position))
            .DefaultIfEmpty(int.MaxValue)
            .Min();

        var focusDistance = enemyUnits
            .Where(enemy => enemy.Id == focusTargetId)
            .Select(enemy => ChebyshevDistance(move, enemy.Position))
            .DefaultIfEmpty(minDistance)
            .Min();

        var unitPreference = GetUnitMovePriority(unit, preferredFirst, preferredSecond);
        var controlDistance = controlTileEnabled ? ChebyshevDistance(move, controlPosition) : int.MaxValue;
        var controlBonus = controlTileEnabled && controlDistance == 0 ? 40 : 0;
        var controlAdjacencyBonus = controlTileEnabled && controlDistance == 1 ? 12 : 0;
        var aggressionBonus = isAggressivePhase ? 6 : 0;
        var hpAdvantage = aiTotalHp - enemyTotalHp;
        var aggressionBias = hpAdvantage >= 0 ? 6 : -2;

        return difficulty switch
        {
            AiDifficulty.Easy => -minDistance * 10 - unitPreference,
            AiDifficulty.Medium => (-minDistance * 8) - (focusDistance * 4) - (unitPreference * 2)
                                   + controlBonus + controlAdjacencyBonus + aggressionBonus,
            AiDifficulty.Hard => (-minDistance * 6) - (focusDistance * 6) - (unitPreference * 2)
                                 + (controlBonus * 2) + (controlAdjacencyBonus * 2) + (aggressionBonus * 2)
                                 + aggressionBias,
            _ => -minDistance * 10
        };
    }

    private static int GetTotalHp(IEnumerable<Unit> units)
    {
        return units.Sum(unit => unit.Stats.CurrentHealth);
    }

    private static int ScoreAttackImpossible(
        Game game,
        AttackOption option,
        List<Unit> enemyUnits,
        int aiTotalHp,
        int enemyTotalHp)
    {
        var score = 0;

        if (option.DefenderOnControl)
            score += 80;

        if (option.IsKill)
            score += 60;

        var defenderValue = option.Defender.Name.Contains("Warrior", StringComparison.OrdinalIgnoreCase) ? 20 : 12;
        score += defenderValue;

        var hpAdvantage = aiTotalHp - enemyTotalHp;
        score += hpAdvantage >= 0 ? 8 : -6;

        var retaliationDamage = enemyUnits
            .Where(enemy => enemy.Id != option.Defender.Id)
            .Where(enemy => enemy.Position.IsAdjacentTo(option.Attacker.Position, includeDiagonals: true))
            .Select(enemy => enemy.Stats.AttackPower)
            .DefaultIfEmpty(0)
            .Max();

        if (retaliationDamage >= option.Attacker.Stats.CurrentHealth)
            score -= option.IsKill ? 10 : 40;
        else
            score -= retaliationDamage / 2;

        return score;
    }

    private sealed record AttackOption(
        Unit Attacker,
        Unit Defender,
        bool DefenderOnControl,
        bool IsKill,
        int AttackerPriority,
        int DefenderHealth);
}
