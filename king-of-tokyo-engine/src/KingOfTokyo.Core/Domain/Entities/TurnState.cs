using KingOfTokyo.Core.Decisions;
using KingOfTokyo.Core.Domain.Enums;
using KingOfTokyo.Core.Domain.State;

namespace KingOfTokyo.Core.Domain.Entities;

public sealed class TurnState
{
    private readonly Queue<TokyoLeaveDecisionContext> _pendingTokyoLeaveDecisions = new();

    public int CurrentPlayerId { get; }
    public TurnPhase Phase { get; private set; }
    public int RollCountUsed { get; private set; }
    public int MaxRolls { get; private set; }
    public int DiceCount { get; }
    public DicePoolState DicePool { get; }
    public bool DiceResolved { get; private set; }
    public bool PurchasePhaseFinished { get; private set; }
    public TurnFlags Flags { get; }

    public bool HasPendingTokyoLeaveDecisions => _pendingTokyoLeaveDecisions.Count > 0;

    public TurnState(int currentPlayerId, int diceCount = 6, int maxRolls = 3)
    {
        if (currentPlayerId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentPlayerId));
        }

        if (diceCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(diceCount));
        }

        if (maxRolls <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRolls));
        }

        CurrentPlayerId = currentPlayerId;
        DiceCount = diceCount;
        MaxRolls = maxRolls;
        DicePool = new DicePoolState(diceCount);
        Flags = new TurnFlags();

        Phase = TurnPhase.NotStarted;
        RollCountUsed = 0;
        DiceResolved = false;
        PurchasePhaseFinished = false;
    }

    public void SetPhase(TurnPhase phase)
    {
        Phase = phase;
    }

    public void IncrementRollCount()
    {
        if (RollCountUsed >= MaxRolls)
        {
            throw new InvalidOperationException("Maximum number of rolls already used.");
        }

        RollCountUsed++;
    }

    public void MarkDiceResolved()
    {
        DiceResolved = true;
        Phase = TurnPhase.DiceResolved;
    }

    public void MarkPurchasePhaseFinished()
    {
        PurchasePhaseFinished = true;
    }

    public void EnqueueTokyoLeaveDecisions(IEnumerable<TokyoLeaveDecisionContext> contexts)
    {
        ArgumentNullException.ThrowIfNull(contexts);

        foreach (var context in contexts)
        {
            _pendingTokyoLeaveDecisions.Enqueue(context);
        }
    }

    public TokyoLeaveDecisionContext PeekTokyoLeaveDecision()
    {
        if (_pendingTokyoLeaveDecisions.Count == 0)
        {
            throw new InvalidOperationException("There is no pending Tokyo leave decision.");
        }

        return _pendingTokyoLeaveDecisions.Peek();
    }

    public TokyoLeaveDecisionContext DequeueTokyoLeaveDecision()
    {
        if (_pendingTokyoLeaveDecisions.Count == 0)
        {
            throw new InvalidOperationException("There is no pending Tokyo leave decision.");
        }

        return _pendingTokyoLeaveDecisions.Dequeue();
    }

    public void ClearTokyoLeaveDecisions()
    {
        _pendingTokyoLeaveDecisions.Clear();
    }

        public void AddExtraRolls(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        MaxRolls += amount;
    }
}