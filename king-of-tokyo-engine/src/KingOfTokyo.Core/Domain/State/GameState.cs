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

    public void StartTurnForCurrentPlayer(int diceCount = 6, int maxRolls = 3)
    {
        var player = GetCurrentPlayer();

        if (!player.IsAlive)
        {
            throw new InvalidOperationException("Cannot start turn for a dead player.");
        }

        CurrentTurn = new TurnState(player.PlayerId, diceCount, maxRolls);
        CurrentTurn.SetPhase(TurnPhase.TurnStart);
        PendingDecision = null;

        if (player.TokyoSlot != TokyoSlot.None)
        {
            CurrentTurn.Flags.StartedTurnInTokyo = true;
        }
    }

    public void AdvanceToNextAlivePlayer()
    {
        if (_players.All(p => !p.IsAlive))
        {
            throw new InvalidOperationException("Cannot advance turn when all players are dead.");
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
