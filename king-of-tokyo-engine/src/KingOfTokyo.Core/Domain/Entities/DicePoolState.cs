using KingOfTokyo.Core.Domain.Enums;

namespace KingOfTokyo.Core.Domain.Entities;

public sealed class DicePoolState
{
    private readonly List<DieState> _dice;

    public IReadOnlyList<DieState> Dice => _dice;

    public DicePoolState(int diceCount)
    {
        if (diceCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(diceCount), "Dice count must be greater than zero.");
        }

        _dice = Enumerable.Range(0, diceCount)
            .Select(i => new DieState(i, DieFace.One))
            .ToList();
    }

    public void SetFace(int index, DieFace face)
    {
        GetDie(index).SetFace(face);
    }

    public void Lock(int index)
    {
        GetDie(index).Lock();
    }

    public void Unlock(int index)
    {
        GetDie(index).Unlock();
    }

    public void UnlockAll()
    {
        foreach (var die in _dice)
        {
            die.Unlock();
        }
    }

    private DieState GetDie(int index)
    {
        if (index < 0 || index >= _dice.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Invalid die index.");
        }

        return _dice[index];
    }
}