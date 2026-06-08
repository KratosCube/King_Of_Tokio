using KingOfTokyo.Core.Abstractions;
using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Entities;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.ValueObjects;

namespace KingOfTokyo.Core.Domain.State;

public sealed class GameState
{
    private readonly List<PlayerState> _players;
    private readonly List<GameEventBase> _eventLog = new();
    private readonly Queue<ScheduledTurnState> _scheduledTurns = new();
    private ScheduledTurnState? _nextScheduledTurn;

    public Guid GameId { get; }
    public long Version { get; private set; }
    public GameStatus Status { get; private set; }
    public GameOptions Options { get; }
    public IReadOnlyList<PlayerState> Players => _players;
    public TurnState? CurrentTurn { get; private set; }
    public TokyoState Tokyo { get; }
    public MarketState Market { get; }
    public int CurrentPlayerIndex { get; private set; }
    public WinnerInfo? WinnerInfo { get; private set; }
    public PendingDecision? PendingDecision { get; private set; }
    public IReadOnlyList<GameEventBase> EventLog => _eventLog;
    public IReadOnlyList<int> ScheduledTurnPlayerIds => _scheduledTurns.Select(turn => turn.PlayerId).ToArray();
    public IReadOnlyList<ScheduledTurnState> ScheduledTurns => _scheduledTurns.ToArray();
    public int NextTurnDiceCountModifier => _nextScheduledTurn?.DiceCountModifier ?? 0;

    public GameState(IEnumerable<PlayerState> players, GameOptions options, Guid? gameId = null)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(options);

        _players = players.ToList();

        if (_players.Count != options.PlayerCount)
        {
            throw new InvalidOperationException("Player count does not match game options.");
        }

        if (_players.Select(p => p.PlayerId).Distinct().Count() != _players.Count)
        {
            throw new InvalidOperationException("Player ids must be unique.");
        }

        GameId = gameId ?? Guid.NewGuid();
        Version = 0;
        Status = GameStatus.Setup;
        Options = options;
        Tokyo = new TokyoState(options.UseBay);
        Market = new MarketState();
        CurrentPlayerIndex = 0;
    }

    public PlayerState GetCurrentPlayer() => _players[CurrentPlayerIndex];

    public PlayerState GetPlayerById(int playerId)
    {
        var player = _players.FirstOrDefault(p => p.PlayerId == playerId);

        if (player is null)
        {
            throw new InvalidOperationException($"Player with id {playerId} was not found.");
        }

        return player;
    }

    public IReadOnlyList<PlayerState> GetAlivePlayers() =>
        _players.Where(p => p.IsAlive).ToList();

    public void StartGame()
    {
        if (Status != GameStatus.Setup)
        {
            throw new InvalidOperationException("Game can only be started from setup state.");
        }

        Status = GameStatus.Running;
        PendingDecision = null;
    }

    public void StartTurnForCurrentPlayer(
        int diceCount = 6,
        int maxRolls = 3,
        bool isExtraTurn = false,
        int diceCountModifier = 0)
    {
        var player = GetCurrentPlayer();

        if (!player.IsAlive)
        {
            throw new InvalidOperationException("Cannot start turn for a dead player.");
        }

        CurrentTurn = new TurnState(player.PlayerId, diceCount, maxRolls, isExtraTurn, diceCountModifier);
        CurrentTurn.SetPhase(TurnPhase.TurnStart);
        PendingDecision = null;

        if (player.TokyoSlot != TokyoSlot.None)
        {
            CurrentTurn.Flags.StartedTurnInTokyo = true;
        }
    }

    public void ScheduleExtraTurn(int playerId, int diceCountModifier = 0)
    {
        var player = GetPlayerById(playerId);
        if (!player.IsAlive)
        {
            throw new InvalidOperationException("Cannot schedule an extra turn for a dead player.");
        }

        _scheduledTurns.Enqueue(new ScheduledTurnState(playerId, diceCountModifier));
    }

    public ScheduledTurnState? ConsumeNextScheduledTurnForCurrentPlayer()
    {
        if (_nextScheduledTurn is null)
        {
            return null;
        }

        if (_nextScheduledTurn.PlayerId != GetCurrentPlayer().PlayerId)
        {
            throw new InvalidOperationException("Scheduled turn does not belong to the current player.");
        }

        var scheduledTurn = _nextScheduledTurn;
        _nextScheduledTurn = null;
        return scheduledTurn;
    }

    public void AdvanceToNextAlivePlayer()
    {
        if (_players.All(p => !p.IsAlive))
        {
            throw new InvalidOperationException("Cannot advance turn when all players are dead.");
        }

        CurrentTurn = null;
        _nextScheduledTurn = null;

        while (_scheduledTurns.Count > 0)
        {
            var scheduledTurn = _scheduledTurns.Dequeue();
            var scheduledPlayerIndex = _players.FindIndex(player => player.PlayerId == scheduledTurn.PlayerId);

            if (scheduledPlayerIndex >= 0 && _players[scheduledPlayerIndex].IsAlive)
            {
                CurrentPlayerIndex = scheduledPlayerIndex;
                _nextScheduledTurn = scheduledTurn;
                return;
            }
        }

        var startIndex = CurrentPlayerIndex;

        do
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % _players.Count;
        }
        while (!_players[CurrentPlayerIndex].IsAlive && CurrentPlayerIndex != startIndex);
    }

    public void SetPendingDecision(PendingDecision? pendingDecision)
    {
        PendingDecision = pendingDecision;
    }

    public void ClearPendingDecision()
    {
        PendingDecision = null;
    }

    public void RecordSuccessfulCommand(IEnumerable<GameEventBase>? eventsToRecord)
    {
        Version++;

        if (eventsToRecord is null)
        {
            return;
        }

        _eventLog.AddRange(eventsToRecord);
    }

    public void FinishGame(WinnerInfo winnerInfo)
    {
        ArgumentNullException.ThrowIfNull(winnerInfo);

        Status = GameStatus.Finished;
        WinnerInfo = winnerInfo;
        PendingDecision = null;

        if (CurrentTurn is not null)
        {
            CurrentTurn.SetPhase(TurnPhase.Finished);
        }
    }
}

public sealed record ScheduledTurnState(
    int PlayerId,
    int DiceCountModifier = 0);
