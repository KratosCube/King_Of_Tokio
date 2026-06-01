namespace KingOfTokyo.Core.Domain.Entities;

public sealed class PlayerStatusState
{
    public int PoisonTokens { get; private set; }
    public int ShrinkTokens { get; private set; }

    public void AddPoisonTokens(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        PoisonTokens += amount;
    }

    public void RemovePoisonTokens(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        PoisonTokens = Math.Max(0, PoisonTokens - amount);
    }

    public void ClearPoisonTokens()
    {
        PoisonTokens = 0;
    }

    public void AddShrinkTokens(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        ShrinkTokens += amount;
    }

    public void RemoveShrinkTokens(int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }

        ShrinkTokens = Math.Max(0, ShrinkTokens - amount);
    }

    public void ClearShrinkTokens()
    {
        ShrinkTokens = 0;
    }
}
